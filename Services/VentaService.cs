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
                    ISNULL(SUM(l.CantidadActual), 0) AS StockReal
                FROM Producto p
                LEFT JOIN Lote l ON p.ProductoId = l.ProductoId 
                    AND l.Activo = 1 
                    AND l.Estado = 'Activo'
                WHERE p.Activo = 1
                GROUP BY p.ProductoId, p.Codigo, p.Nombre, p.PrecioVenta
                ORDER BY p.Nombre";

            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand(query, conn))
            {
                conn.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var producto = new VentaModels.ProductoVenta
                        {
                            ProductoId = (int)reader["ProductoId"],
                            Codigo = reader["Codigo"].ToString().Trim(),
                            Nombre = reader["Nombre"].ToString().Trim(),
                            PrecioVenta = (decimal)reader["PrecioVenta"],
                            StockDisponible = (decimal)reader["StockReal"],
                            Lotes = new List<VentaModels.LoteDisponible>()
                        };
                        productos.Add(producto);
                    }
                }
            }

            foreach (var producto in productos.Where(p => p.StockDisponible > 0))
            {
                producto.Lotes = ObtenerLotesPorProducto(producto.ProductoId);
            }

            return productos;
        }

        public List<VentaModels.LoteDisponible> ObtenerLotesPorProducto(int productoId)
        {
            string query = @"
                SELECT 
                    l.LoteId,
                    l.NumeroLoteInterno AS NumeroLote,
                    l.CantidadActual,
                    l.PrecioUnitario,
                    l.FechaIngreso,
                    l.FechaVencimiento
                FROM Lote l
                WHERE l.ProductoId = @ProductoId
                  AND l.Activo = 1
                  AND l.Estado = 'Activo'
                  AND l.CantidadActual > 0
                ORDER BY l.FechaIngreso ASC";

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
                            LoteId = (int)reader["LoteId"],
                            NumeroLote = reader["NumeroLote"].ToString().Trim(),
                            Cantidad = (decimal)reader["CantidadActual"],
                            PrecioUnitario = (decimal)reader["PrecioUnitario"],
                            FechaIngreso = (DateTime)reader["FechaIngreso"],
                            FechaVencimiento = reader["FechaVencimiento"] as DateTime?
                        });
                    }
                }
            }

            return lotes;
        }

        public List<LoteSeleccionado> AplicarFIFO(int productoId, decimal cantidadSolicitada)
        {
            var lotes = ObtenerLotesPorProducto(productoId);
            var lotesSeleccionados = new List<LoteSeleccionado>();
            decimal cantidadRestante = cantidadSolicitada;

            foreach (var lote in lotes)
            {
                if (cantidadRestante <= 0) break;

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
                throw new Exception($"No hay suficiente inventario. Disponible: {cantidadSolicitada - cantidadRestante} unidades");
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
                        // 1. Insertar encabezado de venta
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

                        // 2. Insertar detalles y lotes
                        foreach (var item in model.Carrito)
                        {
                            // Obtener código limpio — si viene vacío desde JS, buscarlo en BD
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

                            // Obtener nombre limpio
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

                            // Insertar detalle venta
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

                            // Insertar lotes utilizados y actualizar inventario
                            foreach (var lote in item.LotesSeleccionados)
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
        public List<VentaModels.Venta> BuscarVentas(BuscarVentaViewModel filtro)
        {
            var ventas = new List<VentaModels.Venta>();

            string query = @"
        SELECT 
            VentaId,
            NumeroFactura,
            FechaVenta,
            ClienteNombre,
            ClienteDocumento,
            Total,
            Estado
        FROM Venta
        WHERE 1=1";

            if (!string.IsNullOrEmpty(filtro.NumeroFactura))
                query += " AND NumeroFactura LIKE @NumeroFactura";
            if (!string.IsNullOrEmpty(filtro.NumeroDocumento))
                query += " AND ClienteDocumento LIKE @NumeroDocumento";
            if (filtro.FechaInicio.HasValue)
                query += " AND CAST(FechaVenta AS DATE) >= @FechaInicio";
            if (filtro.FechaFin.HasValue)
                query += " AND CAST(FechaVenta AS DATE) <= @FechaFin";
            if (!string.IsNullOrEmpty(filtro.Estado))
                query += " AND Estado = @Estado";

            query += " ORDER BY FechaVenta DESC";

            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand(query, conn))
            {
                if (!string.IsNullOrEmpty(filtro.NumeroFactura))
                    cmd.Parameters.AddWithValue("@NumeroFactura", "%" + filtro.NumeroFactura + "%");
                if (!string.IsNullOrEmpty(filtro.NumeroDocumento))
                    cmd.Parameters.AddWithValue("@NumeroDocumento", "%" + filtro.NumeroDocumento + "%");
                if (filtro.FechaInicio.HasValue)
                    cmd.Parameters.AddWithValue("@FechaInicio", filtro.FechaInicio.Value);
                if (filtro.FechaFin.HasValue)
                    cmd.Parameters.AddWithValue("@FechaFin", filtro.FechaFin.Value);
                if (!string.IsNullOrEmpty(filtro.Estado))
                    cmd.Parameters.AddWithValue("@Estado", filtro.Estado);

                conn.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        ventas.Add(new VentaModels.Venta
                        {
                            VentaId = (int)reader["VentaId"],
                            NumeroFactura = reader["NumeroFactura"].ToString(),
                            FechaVenta = (DateTime)reader["FechaVenta"],
                            ClienteNombre = reader["ClienteNombre"].ToString(),
                            ClienteDocumento = reader["ClienteDocumento"]?.ToString(),
                            Total = (decimal)reader["Total"],
                            Estado = reader["Estado"].ToString()
                        });
                    }
                }
            }

            return ventas;
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
                            VentaId = (int)reader["VentaId"],
                            NumeroFactura = reader["NumeroFactura"].ToString(),
                            FechaVenta = (DateTime)reader["FechaVenta"],
                            ClienteNombre = reader["ClienteNombre"].ToString(),
                            ClienteDocumento = reader["ClienteDocumento"]?.ToString(),
                            ClienteTelefono = reader["ClienteTelefono"]?.ToString(),
                            TipoPago = reader["TipoPago"].ToString(),
                            Subtotal = (decimal)reader["Subtotal"],
                            Impuesto = (decimal)reader["Impuesto"],
                            Descuento = (decimal)reader["Descuento"],
                            Total = (decimal)reader["Total"],
                            Estado = reader["Estado"].ToString(),
                            Observaciones = reader["Observaciones"]?.ToString(),
                            UsuarioNombre = reader["UsuarioNombre"].ToString(),
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
                        int detalleId = (int)reader["DetalleVentaId"];
                        var detalle = detalles.FirstOrDefault(d => d.DetalleVentaId == detalleId);

                        if (detalle == null)
                        {
                            detalle = new VentaModels.DetalleVenta
                            {
                                DetalleVentaId = detalleId,
                                VentaId = (int)reader["VentaId"],
                                ProductoId = (int)reader["ProductoId"],
                                ProductoCodigo = reader["ProductoCodigo"].ToString().Trim(),
                                ProductoNombre = reader["ProductoNombre"].ToString().Trim(),
                                Cantidad = (decimal)reader["Cantidad"],
                                PrecioUnitario = (decimal)reader["PrecioUnitario"],
                                Descuento = (decimal)reader["Descuento"],
                                Subtotal = (decimal)reader["Subtotal"],
                                LotesUtilizados = new List<VentaModels.DetalleVentaLote>()
                            };
                            detalles.Add(detalle);
                        }

                        if (reader["LoteId"] != DBNull.Value)
                        {
                            detalle.LotesUtilizados.Add(new VentaModels.DetalleVentaLote
                            {
                                LoteId = (int)reader["LoteId"],
                                NumeroLote = reader["NumeroLoteInterno"].ToString().Trim(),
                                Cantidad = (decimal)reader["CantidadLote"],
                                PrecioUnitario = (decimal)reader["PrecioLote"],
                                Subtotal = (decimal)reader["SubtotalLote"]
                            });
                        }
                    }
                }
            }

            return detalles;
        }
    }
}