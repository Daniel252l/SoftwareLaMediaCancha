using LaMediaCancha.Models;
using LaMediaCancha.Models.ViewModels;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;

namespace LaMediaCancha.Services
{
    public class VentaService
    {
        private readonly string _connectionString;

        public VentaService()
        {
            _connectionString = ConfigurationManager.ConnectionStrings["LaMediaCanchaDB"].ConnectionString;
        }

        public List<VentaModels.ProductoVenta> ObtenerProductosConLotes()
        {
            var productos = new List<VentaModels.ProductoVenta>();

            string query = @"
        SELECT 
            p.ProductoId,
            p.Codigo,
            p.Nombre,
            p.PrecioVenta,
            ISNULL(SUM(l.CantidadActual), 0) AS StockLotes,
            ISNULL(i.ExistenciaActual, 0) AS StockInventario
        FROM Producto p
        LEFT JOIN Lote l ON p.ProductoId = l.ProductoId 
            AND l.Activo = 1 
            AND l.Estado = 'Activo'
        LEFT JOIN Inventario i ON p.ProductoId = i.ProductoId
        WHERE p.Activo = 1
        GROUP BY p.ProductoId, p.Codigo, p.Nombre, p.PrecioVenta, i.ExistenciaActual
        ORDER BY p.Nombre";

            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand(query, conn))
            {
                conn.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        decimal stockLotes = 0;
                        if (reader["StockLotes"] != DBNull.Value)
                        {
                            stockLotes = Convert.ToDecimal(reader["StockLotes"]);
                        }

                        decimal stockInventario = 0;
                        if (reader["StockInventario"] != DBNull.Value)
                        {
                            stockInventario = Convert.ToDecimal(reader["StockInventario"]);
                        }

                        decimal stockTotal = stockLotes > 0 ? stockLotes : stockInventario;

                        var producto = new VentaModels.ProductoVenta
                        {
                            ProductoId = Convert.ToInt32(reader["ProductoId"]),
                            Codigo = reader["Codigo"]?.ToString()?.Trim() ?? "",
                            Nombre = reader["Nombre"]?.ToString()?.Trim() ?? "",
                            PrecioVenta = reader["PrecioVenta"] != DBNull.Value ? Convert.ToDecimal(reader["PrecioVenta"]) : 0,
                            StockDisponible = stockTotal,
                            Lotes = new List<VentaModels.LoteDisponible>()
                        };
                        productos.Add(producto);
                    }
                }
            }

            // Cargar lotes para productos con stock
            foreach (var producto in productos.Where(p => p.StockDisponible > 0))
            {
                var lotes = ObtenerLotesPorProducto(producto.ProductoId);
                if (lotes.Any())
                {
                    producto.Lotes = lotes;
                }
            }

            return productos;
        }

        public List<VentaModels.LoteDisponible> ObtenerLotesPorProducto(int productoId)
        {
            string query = @"
        SELECT 
            l.LoteId,
            ISNULL(l.NumeroLoteInterno, '') AS NumeroLote,
            ISNULL(l.CantidadActual, 0) AS Cantidad,
            ISNULL(l.PrecioUnitario, 0) AS PrecioUnitario,
            l.FechaIngreso,
            l.FechaVencimiento
        FROM Lote l
        WHERE l.ProductoId = @ProductoId
          AND l.Activo = 1
          AND l.Estado = 'Activo'
          AND l.CantidadActual > 0
        ORDER BY l.FechaIngreso ASC";  // FIFO: más antiguo primero

            var lotes = new List<VentaModels.LoteDisponible>();

            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@ProductoId", productoId);
                conn.Open();

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        lotes.Add(new VentaModels.LoteDisponible
                        {
                            LoteId = Convert.ToInt32(reader["LoteId"]),
                            NumeroLote = reader["NumeroLote"]?.ToString()?.Trim() ?? "",
                            Cantidad = reader["Cantidad"] != DBNull.Value ? Convert.ToDecimal(reader["Cantidad"]) : 0,
                            PrecioUnitario = reader["PrecioUnitario"] != DBNull.Value ? Convert.ToDecimal(reader["PrecioUnitario"]) : 0,
                            FechaIngreso = reader["FechaIngreso"] != DBNull.Value ? Convert.ToDateTime(reader["FechaIngreso"]) : DateTime.Now,
                            FechaVencimiento = reader["FechaVencimiento"] != DBNull.Value ? (DateTime?)reader["FechaVencimiento"] : null
                        });
                    }
                }
            }

            return lotes;
        }

        private decimal ObtenerPrecioProducto(int productoId)
        {
            string query = "SELECT ISNULL(PrecioVenta, 0) FROM Producto WHERE ProductoId = @ProductoId";
            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@ProductoId", productoId);
                conn.Open();
                var result = cmd.ExecuteScalar();
                return result != DBNull.Value ? Convert.ToDecimal(result) : 0;
            }
        }

        private decimal ObtenerStockInventario(int productoId)
        {
            string query = "SELECT ISNULL(ExistenciaActual, 0) FROM Inventario WHERE ProductoId = @ProductoId";
            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@ProductoId", productoId);
                conn.Open();
                var result = cmd.ExecuteScalar();
                return result != DBNull.Value ? Convert.ToDecimal(result) : 0;
            }
        }

        public List<LoteSeleccionado> AplicarFIFO(int productoId, decimal cantidadSolicitada)
        {
            // IMPORTANTE: Ordenar por FechaIngreso ASC (más antiguo primero)
            var lotes = ObtenerLotesPorProducto(productoId)
                            .OrderBy(l => l.FechaIngreso)  // ← ASC = más antiguo primero
                            .ToList();

            var lotesSeleccionados = new List<LoteSeleccionado>();
            decimal cantidadRestante = cantidadSolicitada;
            decimal precioProducto = ObtenerPrecioProducto(productoId);

            foreach (var lote in lotes)
            {
                if (cantidadRestante <= 0) break;

                // Tomar del lote más antiguo primero
                decimal cantidadTomar = Math.Min(lote.Cantidad, cantidadRestante);

                lotesSeleccionados.Add(new LoteSeleccionado
                {
                    LoteId = lote.LoteId,
                    NumeroLote = lote.NumeroLote,
                    Cantidad = cantidadTomar,
                    PrecioUnitario = lote.PrecioUnitario,
                    Subtotal = cantidadTomar * lote.PrecioUnitario
                });

                cantidadRestante -= cantidadTomar;
            }

            if (cantidadRestante > 0)
            {
                decimal stockInventario = ObtenerStockInventario(productoId);
                if (stockInventario >= cantidadRestante)
                {
                    lotesSeleccionados.Add(new LoteSeleccionado
                    {
                        LoteId = 0,
                        NumeroLote = "STOCK_DIRECTO",
                        Cantidad = cantidadRestante,
                        PrecioUnitario = precioProducto,
                        Subtotal = cantidadRestante * precioProducto
                    });
                    cantidadRestante = 0;
                }
            }

            if (cantidadRestante > 0)
            {
                decimal disponible = cantidadSolicitada - cantidadRestante;
                throw new Exception($"No hay suficiente inventario. Disponible: {disponible} unidades");
            }

            return lotesSeleccionados;
        }

        public int RegistrarVenta(VentaViewModel model, int usuarioId, string usuarioNombre)
        {
            int ventaId = 0;
            string numeroFactura = $"VEN-{DateTime.Now:yyyyMMddHHmmss}";

            using (var conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                using (var transaction = conn.BeginTransaction())
                {
                    try
                    {
                        string insertVenta = @"
                            INSERT INTO Venta (NumeroFactura, FechaVenta, ClienteNombre, ClienteDocumento, 
                                               ClienteTelefono, TipoPago, Subtotal, Impuesto, Descuento, Total, 
                                               Estado, Observaciones, UsuarioId, UsuarioNombre)
                            VALUES (@NumeroFactura, GETDATE(), @ClienteNombre, @ClienteDocumento, 
                                    @ClienteTelefono, @TipoPago, @Subtotal, @Impuesto, @Descuento, @Total, 
                                    'Completada', @Observaciones, @UsuarioId, @UsuarioNombre);
                            SELECT SCOPE_IDENTITY();";

                        using (var cmd = new SqlCommand(insertVenta, conn, transaction))
                        {
                            cmd.Parameters.AddWithValue("@NumeroFactura", numeroFactura);
                            cmd.Parameters.AddWithValue("@ClienteNombre", string.IsNullOrEmpty(model.ClienteNombre) ? "Consumidor Final" : model.ClienteNombre);
                            cmd.Parameters.AddWithValue("@ClienteDocumento", string.IsNullOrEmpty(model.ClienteDocumento) ? DBNull.Value : (object)model.ClienteDocumento);
                            cmd.Parameters.AddWithValue("@ClienteTelefono", string.IsNullOrEmpty(model.ClienteTelefono) ? DBNull.Value : (object)model.ClienteTelefono);
                            cmd.Parameters.AddWithValue("@TipoPago", model.TipoPago);
                            cmd.Parameters.AddWithValue("@Subtotal", model.Subtotal);
                            cmd.Parameters.AddWithValue("@Impuesto", model.Impuesto);
                            cmd.Parameters.AddWithValue("@Descuento", model.Descuento);
                            cmd.Parameters.AddWithValue("@Total", model.Total);
                            cmd.Parameters.AddWithValue("@Observaciones", string.IsNullOrEmpty(model.Observaciones) ? DBNull.Value : (object)model.Observaciones);
                            cmd.Parameters.AddWithValue("@UsuarioId", usuarioId);
                            cmd.Parameters.AddWithValue("@UsuarioNombre", usuarioNombre);
                            ventaId = Convert.ToInt32(cmd.ExecuteScalar());
                        }

                        foreach (var item in model.Carrito)
                        {
                            string codigoLimpio = (item.Codigo ?? "").Trim();
                            if (string.IsNullOrEmpty(codigoLimpio))
                            {
                                string queryCode = "SELECT LTRIM(RTRIM(Codigo)) FROM Producto WHERE ProductoId = @PId";
                                using (var cmdCode = new SqlCommand(queryCode, conn, transaction))
                                {
                                    cmdCode.Parameters.AddWithValue("@PId", item.ProductoId);
                                    codigoLimpio = cmdCode.ExecuteScalar()?.ToString() ?? "S/C";
                                }
                            }

                            string nombreLimpio = (item.Nombre ?? "").Trim();
                            if (string.IsNullOrEmpty(nombreLimpio))
                            {
                                string queryNombre = "SELECT LTRIM(RTRIM(Nombre)) FROM Producto WHERE ProductoId = @PId";
                                using (var cmdNombre = new SqlCommand(queryNombre, conn, transaction))
                                {
                                    cmdNombre.Parameters.AddWithValue("@PId", item.ProductoId);
                                    nombreLimpio = cmdNombre.ExecuteScalar()?.ToString() ?? "Sin nombre";
                                }
                            }

                            string insertDetalle = @"
                                INSERT INTO DetalleVenta (VentaId, ProductoId, ProductoCodigo, ProductoNombre, 
                                                          Cantidad, PrecioUnitario, Descuento, Subtotal)
                                VALUES (@VentaId, @ProductoId, @ProductoCodigo, @ProductoNombre, 
                                        @Cantidad, @PrecioUnitario, @Descuento, @Subtotal);
                                SELECT SCOPE_IDENTITY();";

                            int detalleVentaId = 0;
                            using (var cmd = new SqlCommand(insertDetalle, conn, transaction))
                            {
                                cmd.Parameters.AddWithValue("@VentaId", ventaId);
                                cmd.Parameters.AddWithValue("@ProductoId", item.ProductoId);
                                cmd.Parameters.AddWithValue("@ProductoCodigo", codigoLimpio);
                                cmd.Parameters.AddWithValue("@ProductoNombre", nombreLimpio);
                                cmd.Parameters.AddWithValue("@Cantidad", item.Cantidad);
                                cmd.Parameters.AddWithValue("@PrecioUnitario", item.PrecioUnitario);
                                cmd.Parameters.AddWithValue("@Descuento", item.Descuento);
                                cmd.Parameters.AddWithValue("@Subtotal", item.Subtotal);
                                detalleVentaId = Convert.ToInt32(cmd.ExecuteScalar());
                            }

                            foreach (var lote in item.LotesSeleccionados)
                            {
                                if (lote.LoteId > 0)
                                {
                                    string insertDetalleLote = @"
                                        INSERT INTO DetalleVentaLote (DetalleVentaId, LoteId, Cantidad, PrecioUnitario, Subtotal)
                                        VALUES (@DetalleVentaId, @LoteId, @Cantidad, @PrecioUnitario, @Subtotal)";

                                    using (var cmd = new SqlCommand(insertDetalleLote, conn, transaction))
                                    {
                                        cmd.Parameters.AddWithValue("@DetalleVentaId", detalleVentaId);
                                        cmd.Parameters.AddWithValue("@LoteId", lote.LoteId);
                                        cmd.Parameters.AddWithValue("@Cantidad", lote.Cantidad);
                                        cmd.Parameters.AddWithValue("@PrecioUnitario", lote.PrecioUnitario);
                                        cmd.Parameters.AddWithValue("@Subtotal", lote.Subtotal);
                                        cmd.ExecuteNonQuery();
                                    }

                                    string updateLote = @"
                                        UPDATE Lote 
                                        SET CantidadActual = CantidadActual - @Cantidad,
                                            Estado = CASE WHEN CantidadActual - @Cantidad <= 0 THEN 'Agotado' ELSE 'Activo' END
                                        WHERE LoteId = @LoteId";

                                    using (var cmd = new SqlCommand(updateLote, conn, transaction))
                                    {
                                        cmd.Parameters.AddWithValue("@Cantidad", lote.Cantidad);
                                        cmd.Parameters.AddWithValue("@LoteId", lote.LoteId);
                                        cmd.ExecuteNonQuery();
                                    }
                                }
                                else
                                {
                                    string updateInventario = @"
                                        UPDATE Inventario 
                                        SET ExistenciaActual = ExistenciaActual - @Cantidad
                                        WHERE ProductoId = @ProductoId";

                                    using (var cmd = new SqlCommand(updateInventario, conn, transaction))
                                    {
                                        cmd.Parameters.AddWithValue("@Cantidad", lote.Cantidad);
                                        cmd.Parameters.AddWithValue("@ProductoId", item.ProductoId);
                                        cmd.ExecuteNonQuery();
                                    }
                                }
                            }
                        }

                        transaction.Commit();
                        return ventaId;
                    }
                    catch
                    {
                        transaction.Rollback();
                        throw;
                    }
                }
            }
        }

        public VentaModels.Venta ObtenerVentaPorId(int ventaId)
        {
            VentaModels.Venta venta = null;

            string queryVenta = @"SELECT * FROM Venta WHERE VentaId = @VentaId";

            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand(queryVenta, conn))
            {
                cmd.Parameters.AddWithValue("@VentaId", ventaId);
                conn.Open();

                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        venta = new VentaModels.Venta
                        {
                            VentaId = Convert.ToInt32(reader["VentaId"]),
                            NumeroFactura = reader["NumeroFactura"]?.ToString() ?? "",
                            FechaVenta = reader["FechaVenta"] != DBNull.Value ? Convert.ToDateTime(reader["FechaVenta"]) : DateTime.Now,
                            ClienteNombre = reader["ClienteNombre"]?.ToString() ?? "",
                            ClienteDocumento = reader["ClienteDocumento"]?.ToString(),
                            ClienteTelefono = reader["ClienteTelefono"]?.ToString(),
                            TipoPago = reader["TipoPago"]?.ToString() ?? "",
                            Subtotal = reader["Subtotal"] != DBNull.Value ? Convert.ToDecimal(reader["Subtotal"]) : 0,
                            Impuesto = reader["Impuesto"] != DBNull.Value ? Convert.ToDecimal(reader["Impuesto"]) : 0,
                            Descuento = reader["Descuento"] != DBNull.Value ? Convert.ToDecimal(reader["Descuento"]) : 0,
                            Total = reader["Total"] != DBNull.Value ? Convert.ToDecimal(reader["Total"]) : 0,
                            Estado = reader["Estado"]?.ToString() ?? "",
                            Observaciones = reader["Observaciones"]?.ToString(),
                            UsuarioNombre = reader["UsuarioNombre"]?.ToString() ?? "",
                            Detalles = ObtenerDetallesVenta(ventaId)
                        };
                    }
                }
            }

            return venta;
        }

        public List<VentaModels.DetalleVenta> ObtenerDetallesVenta(int ventaId)
        {
            var detalles = new List<VentaModels.DetalleVenta>();

            string queryDetalle = @"
                SELECT dv.*, 
                       dvl.LoteId, dvl.Cantidad AS CantidadLote, dvl.PrecioUnitario AS PrecioLote, dvl.Subtotal AS SubtotalLote,
                       l.NumeroLoteInterno
                FROM DetalleVenta dv
                LEFT JOIN DetalleVentaLote dvl ON dv.DetalleVentaId = dvl.DetalleVentaId
                LEFT JOIN Lote l ON dvl.LoteId = l.LoteId
                WHERE dv.VentaId = @VentaId";

            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand(queryDetalle, conn))
            {
                cmd.Parameters.AddWithValue("@VentaId", ventaId);
                conn.Open();

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        int detalleId = Convert.ToInt32(reader["DetalleVentaId"]);
                        var detalle = detalles.FirstOrDefault(d => d.DetalleVentaId == detalleId);

                        if (detalle == null)
                        {
                            detalle = new VentaModels.DetalleVenta
                            {
                                DetalleVentaId = detalleId,
                                VentaId = Convert.ToInt32(reader["VentaId"]),
                                ProductoId = Convert.ToInt32(reader["ProductoId"]),
                                ProductoCodigo = reader["ProductoCodigo"]?.ToString()?.Trim() ?? "",
                                ProductoNombre = reader["ProductoNombre"]?.ToString()?.Trim() ?? "",
                                Cantidad = reader["Cantidad"] != DBNull.Value ? Convert.ToDecimal(reader["Cantidad"]) : 0,
                                PrecioUnitario = reader["PrecioUnitario"] != DBNull.Value ? Convert.ToDecimal(reader["PrecioUnitario"]) : 0,
                                Descuento = reader["Descuento"] != DBNull.Value ? Convert.ToDecimal(reader["Descuento"]) : 0,
                                Subtotal = reader["Subtotal"] != DBNull.Value ? Convert.ToDecimal(reader["Subtotal"]) : 0,
                                LotesUtilizados = new List<VentaModels.DetalleVentaLote>()
                            };
                            detalles.Add(detalle);
                        }

                        if (reader["LoteId"] != DBNull.Value)
                        {
                            detalle.LotesUtilizados.Add(new VentaModels.DetalleVentaLote
                            {
                                LoteId = Convert.ToInt32(reader["LoteId"]),
                                NumeroLote = reader["NumeroLoteInterno"]?.ToString()?.Trim() ?? "",
                                Cantidad = reader["CantidadLote"] != DBNull.Value ? Convert.ToDecimal(reader["CantidadLote"]) : 0,
                                PrecioUnitario = reader["PrecioLote"] != DBNull.Value ? Convert.ToDecimal(reader["PrecioLote"]) : 0,
                                Subtotal = reader["SubtotalLote"] != DBNull.Value ? Convert.ToDecimal(reader["SubtotalLote"]) : 0
                            });
                        }
                    }
                }
            }

            return detalles;
        }

       
        // ==================== MÉTODOS PARA GESTIÓN DE PRODUCTOS ====================

public bool ExisteCodigoProducto(string codigo, int? productoIdExcluir = null)
{
    string query = "SELECT COUNT(*) FROM Producto WHERE Codigo = @Codigo AND Activo = 1";
    
    if (productoIdExcluir.HasValue)
    {
        query += " AND ProductoId != @ProductoIdExcluir";
    }
    
    using (var conn = new SqlConnection(_connectionString))
    {
        conn.Open();
        using (var cmd = new SqlCommand(query, conn))
        {
            cmd.Parameters.AddWithValue("@Codigo", codigo);
            if (productoIdExcluir.HasValue)
            {
                cmd.Parameters.AddWithValue("@ProductoIdExcluir", productoIdExcluir.Value);
            }
            int count = (int)cmd.ExecuteScalar();
            return count > 0;
        }
    }
}

        public int AgregarProducto(string nombre, string codigo, decimal precio, int subDepartamentoId = 1, int presentacionId = 1, int tipoProductoId = 1)
        {
            // Verificar si el código ya existe
            string checkQuery = "SELECT COUNT(*) FROM Producto WHERE Codigo = @Codigo";

            using (var conn = new SqlConnection(_connectionString))
            {
                conn.Open();

                using (var checkCmd = new SqlCommand(checkQuery, conn))
                {
                    checkCmd.Parameters.AddWithValue("@Codigo", codigo);
                    int existe = (int)checkCmd.ExecuteScalar();
                    if (existe > 0)
                    {
                        throw new Exception("Ya existe un producto con este código");
                    }
                }

                // Insertar con todas las columnas requeridas
                string query = @"
            INSERT INTO Producto (
                SubDepartamentoId,
                PresentacionId,
                MarcaId,
                EstanteId,
                ColorId,
                TallaId,
                Codigo,
                CodigoBarras,
                Nombre,
                Descripcion,
                PrecioCompra,
                PrecioVenta,
                EstaEnOferta,
                PrecioOferta,
                FechaInicioOferta,
                FechaFinOferta,
                Activo,
                FechaCreacion,
                FechaModificacion,
                TipoProductoId
            )
            VALUES (
                @SubDepartamentoId,
                @PresentacionId,
                @MarcaId,
                @EstanteId,
                @ColorId,
                @TallaId,
                @Codigo,
                @CodigoBarras,
                @Nombre,
                @Descripcion,
                @PrecioCompra,
                @PrecioVenta,
                0,
                0,
                NULL,
                NULL,
                1,
                GETDATE(),
                NULL,
                @TipoProductoId
            );
            SELECT SCOPE_IDENTITY();";

                using (var cmd = new SqlCommand(query, conn))
                {
                    // Campos obligatorios
                    cmd.Parameters.AddWithValue("@SubDepartamentoId", subDepartamentoId);
                    cmd.Parameters.AddWithValue("@PresentacionId", presentacionId);
                    cmd.Parameters.AddWithValue("@TipoProductoId", tipoProductoId);

                    // Campos que pueden ser NULL pero tienen valor por defecto
                    cmd.Parameters.AddWithValue("@MarcaId", DBNull.Value);
                    cmd.Parameters.AddWithValue("@EstanteId", DBNull.Value);
                    cmd.Parameters.AddWithValue("@ColorId", DBNull.Value);
                    cmd.Parameters.AddWithValue("@TallaId", DBNull.Value);

                    // Campos del producto
                    cmd.Parameters.AddWithValue("@Codigo", codigo);
                    cmd.Parameters.AddWithValue("@CodigoBarras", codigo); // Usar el mismo código como código de barras
                    cmd.Parameters.AddWithValue("@Nombre", nombre);
                    cmd.Parameters.AddWithValue("@Descripcion", nombre); // Usar el nombre como descripción
                    cmd.Parameters.AddWithValue("@PrecioCompra", precio * 0.6m); // Precio de compra como 60% del precio venta
                    cmd.Parameters.AddWithValue("@PrecioVenta", precio);

                    return Convert.ToInt32(cmd.ExecuteScalar());
                }
            }
        }

        public bool EditarProducto(int productoId, string nombre, string codigo, decimal precio, int subDepartamentoId = 1, int presentacionId = 1, int tipoProductoId = 1)
        {
            string query = @"
        UPDATE Producto 
        SET 
            SubDepartamentoId = @SubDepartamentoId,
            PresentacionId = @PresentacionId,
            Codigo = @Codigo,
            CodigoBarras = @CodigoBarras,
            Nombre = @Nombre,
            Descripcion = @Descripcion,
            PrecioCompra = @PrecioCompra,
            PrecioVenta = @PrecioVenta,
            TipoProductoId = @TipoProductoId,
            FechaModificacion = GETDATE()
        WHERE ProductoId = @ProductoId AND Activo = 1";

            using (var conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                using (var cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@ProductoId", productoId);
                    cmd.Parameters.AddWithValue("@SubDepartamentoId", subDepartamentoId);
                    cmd.Parameters.AddWithValue("@PresentacionId", presentacionId);
                    cmd.Parameters.AddWithValue("@Codigo", codigo);
                    cmd.Parameters.AddWithValue("@CodigoBarras", codigo);
                    cmd.Parameters.AddWithValue("@Nombre", nombre);
                    cmd.Parameters.AddWithValue("@Descripcion", nombre);
                    cmd.Parameters.AddWithValue("@PrecioCompra", precio * 0.6m);
                    cmd.Parameters.AddWithValue("@PrecioVenta", precio);
                    cmd.Parameters.AddWithValue("@TipoProductoId", tipoProductoId);
                    return cmd.ExecuteNonQuery() > 0;
                }
            }
        }

        public bool EliminarProducto(int productoId)
{
    // Eliminación lógica - solo desactivar
    string query = "UPDATE Producto SET Activo = 0, FechaModificacion = GETDATE() WHERE ProductoId = @ProductoId";
    
    using (var conn = new SqlConnection(_connectionString))
    {
        conn.Open();
        using (var cmd = new SqlCommand(query, conn))
        {
            cmd.Parameters.AddWithValue("@ProductoId", productoId);
            return cmd.ExecuteNonQuery() > 0;
        }
    }
}

public bool EliminarProductoFisico(int productoId)
{
    // Eliminación física - borrar permanentemente
    using (var conn = new SqlConnection(_connectionString))
    {
        conn.Open();
        using (var transaction = conn.BeginTransaction())
        {
            try
            {
                // Primero eliminar lotes relacionados
                string deleteLotesQuery = "DELETE FROM Lote WHERE ProductoId = @ProductoId";
                using (var cmd = new SqlCommand(deleteLotesQuery, conn, transaction))
                {
                    cmd.Parameters.AddWithValue("@ProductoId", productoId);
                    cmd.ExecuteNonQuery();
                }
                
                // Luego eliminar el producto
                string deleteProductoQuery = "DELETE FROM Producto WHERE ProductoId = @ProductoId";
                using (var cmd = new SqlCommand(deleteProductoQuery, conn, transaction))
                {
                    cmd.Parameters.AddWithValue("@ProductoId", productoId);
                    int result = cmd.ExecuteNonQuery();
                    transaction.Commit();
                    return result > 0;
                }
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }
    }
}

// Método para obtener todos los productos (incluyendo inactivos para administración)
public List<ProductoModels.Producto> ObtenerTodosLosProductos()
{
    var productos = new List<ProductoModels.Producto>();
    string query = "SELECT ProductoId, Nombre, Codigo, PrecioVenta, Activo FROM Producto ORDER BY Nombre";
    
    using (var conn = new SqlConnection(_connectionString))
    {
        conn.Open();
        using (var cmd = new SqlCommand(query, conn))
        using (var reader = cmd.ExecuteReader())
        {
            while (reader.Read())
            {
                productos.Add(new ProductoModels.Producto
                {
                    ProductoId = (int)reader["ProductoId"],
                    Nombre = reader["Nombre"].ToString(),
                    Codigo = reader["Codigo"].ToString(),
                    PrecioVenta = (decimal)reader["PrecioVenta"],
                    Activo = (bool)reader["Activo"]
                });
            }
        }
    }
    return productos;
}
    }
}