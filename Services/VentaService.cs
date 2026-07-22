using LaMediaCancha.Models;
using LaMediaCancha.Models.ViewModels;
using System;
using System.Collections.Generic;
using System.Configuration;
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
                    ISNULL(SUM(lp.CantidadActual), 0) as StockDisponible
                FROM Producto p
                LEFT JOIN LoteProductoVenta lp ON p.ProductoId = lp.ProductoId AND lp.Activo = 1 AND lp.CantidadActual > 0
                WHERE p.Activo = 1
                GROUP BY p.ProductoId, p.Codigo, p.Nombre, p.PrecioVenta
                ORDER BY p.Nombre";

            using (var conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                using (var cmd = new SqlCommand(query, conn))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        productos.Add(new VentaModels.ProductoVenta
                        {
                            ProductoId = (int)reader["ProductoId"],
                            Codigo = reader["Codigo"].ToString(),
                            Nombre = reader["Nombre"].ToString(),
                            PrecioVenta = (decimal)reader["PrecioVenta"],
                            StockDisponible = (decimal)reader["StockDisponible"],
                            Lotes = ObtenerLotesPorProducto((int)reader["ProductoId"])
                        });
                    }
                }
            }
            return productos;
        }

        public VentaModels.ProductoVenta ObtenerProductoPorId(int productoId)
        {
            string query = @"
                SELECT p.ProductoId, p.Codigo, p.Nombre, p.PrecioVenta,
                       ISNULL(SUM(lp.CantidadActual), 0) as StockDisponible
                FROM Producto p
                LEFT JOIN LoteProductoVenta lp ON p.ProductoId = lp.ProductoId AND lp.Activo = 1 AND lp.CantidadActual > 0
                WHERE p.ProductoId = @ProductoId AND p.Activo = 1
                GROUP BY p.ProductoId, p.Codigo, p.Nombre, p.PrecioVenta";

            using (var conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                using (var cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@ProductoId", productoId);
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return new VentaModels.ProductoVenta
                            {
                                ProductoId = (int)reader["ProductoId"],
                                Codigo = reader["Codigo"].ToString(),
                                Nombre = reader["Nombre"].ToString(),
                                PrecioVenta = (decimal)reader["PrecioVenta"],
                                StockDisponible = (decimal)reader["StockDisponible"],
                                Lotes = ObtenerLotesPorProducto(productoId)
                            };
                        }
                    }
                }
            }
            return null;
        }

        public List<VentaModels.LoteDisponible> ObtenerLotesPorProducto(int productoId)
        {
            var lotes = new List<VentaModels.LoteDisponible>();

            string query = @"
                SELECT LoteProductoVentaId as LoteId, NumeroLote, CantidadActual as Cantidad, 
                       PrecioVenta as PrecioUnitario, FechaIngreso, FechaVencimiento
                FROM LoteProductoVenta
                WHERE ProductoId = @ProductoId AND Activo = 1 AND CantidadActual > 0
                ORDER BY FechaVencimiento ASC, FechaIngreso ASC";

            using (var conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                using (var cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@ProductoId", productoId);
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            lotes.Add(new VentaModels.LoteDisponible
                            {
                                LoteId = (int)reader["LoteId"],
                                NumeroLote = reader["NumeroLote"].ToString(),
                                Cantidad = (decimal)reader["Cantidad"],
                                PrecioUnitario = (decimal)reader["PrecioUnitario"],
                                FechaIngreso = (DateTime)reader["FechaIngreso"],
                                FechaVencimiento = reader["FechaVencimiento"] != DBNull.Value ? (DateTime?)reader["FechaVencimiento"] : null
                            });
                        }
                    }
                }
            }
            return lotes;
        }


        public RecetaModels.Receta ObtenerRecetaPorProducto(int productoTerminadoId)
        {
            var receta = new RecetaModels.Receta
            {
                Detalles = new List<RecetaModels.RecetaDetalle>()
            };

            string queryReceta = @"
                SELECT r.RecetaId, r.ProductoTerminadoId, r.NombreReceta, r.Rendimiento, r.Instrucciones, r.Activo, r.FechaCreacion,
                       p.Nombre as ProductoTerminadoNombre
                FROM Receta r
                INNER JOIN Producto p ON r.ProductoTerminadoId = p.ProductoId
                WHERE r.ProductoTerminadoId = @ProductoTerminadoId AND r.Activo = 1";

            using (var conn = new SqlConnection(_connectionString))
            {
                conn.Open();

                using (var cmd = new SqlCommand(queryReceta, conn))
                {
                    cmd.Parameters.AddWithValue("@ProductoTerminadoId", productoTerminadoId);
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            receta.RecetaId = (int)reader["RecetaId"];
                            receta.ProductoTerminadoId = (int)reader["ProductoTerminadoId"];
                            receta.ProductoTerminadoNombre = reader["ProductoTerminadoNombre"].ToString();
                            receta.NombreReceta = reader["NombreReceta"].ToString();
                            receta.Rendimiento = (decimal)reader["Rendimiento"];
                            receta.Instrucciones = reader["Instrucciones"]?.ToString();
                            receta.Activo = (bool)reader["Activo"];
                            receta.FechaCreacion = (DateTime)reader["FechaCreacion"];
                        }
                        else
                        {
                            return null;
                        }
                    }
                }

                string queryDetalles = @"
                    SELECT rd.RecetaDetalleId, rd.RecetaId, rd.ProductoCompraId, rd.CantidadNecesaria,
                           pc.Nombre as ProductoCompraNombre, 
                           um.Nombre as UnidadMedidaNombre,
                           um.Abreviatura as UnidadMedidaAbreviatura,
                           ISNULL(SUM(lc.CantidadActual), 0) as StockDisponible
                    FROM RecetaDetalle rd
                    INNER JOIN ProductoCompra pc ON rd.ProductoCompraId = pc.ProductoCompraId
                    INNER JOIN UnidadMedida um ON rd.UnidadMedidaId = um.UnidadMedidaId
                    LEFT JOIN LoteCompra lc ON rd.ProductoCompraId = lc.ProductoCompraId AND lc.Activo = 1 AND lc.CantidadActual > 0
                    WHERE rd.RecetaId = @RecetaId
                    GROUP BY rd.RecetaDetalleId, rd.RecetaId, rd.ProductoCompraId, rd.CantidadNecesaria,
                             pc.Nombre, um.Nombre, um.Abreviatura";

                using (var cmd = new SqlCommand(queryDetalles, conn))
                {
                    cmd.Parameters.AddWithValue("@RecetaId", receta.RecetaId);
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            receta.Detalles.Add(new RecetaModels.RecetaDetalle
                            {
                                RecetaDetalleId = (int)reader["RecetaDetalleId"],
                                RecetaId = (int)reader["RecetaId"],
                                ProductoCompraId = (int)reader["ProductoCompraId"],
                                ProductoCompraNombre = reader["ProductoCompraNombre"].ToString(),
                                CantidadNecesaria = (decimal)reader["CantidadNecesaria"],
                                UnidadMedidaNombre = reader["UnidadMedidaNombre"].ToString(),
                                UnidadMedidaAbreviatura = reader["UnidadMedidaAbreviatura"].ToString(),
                                StockDisponible = (int)reader["StockDisponible"]
                            });
                        }
                    }
                }
            }

            return receta;
        }

        public int ObtenerStockProductoCompra(int productoCompraId)
        {
            string query = "SELECT ISNULL(SUM(CantidadActual), 0) FROM LoteCompra WHERE ProductoCompraId = @ProductoCompraId AND Activo = 1 AND CantidadActual > 0";
            using (var conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                using (var cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@ProductoCompraId", productoCompraId);
                    return Convert.ToInt32(cmd.ExecuteScalar());
                }
            }
        }

        private decimal ObtenerPrecioUnitarioProductoCompra(int productoCompraId)
        {
            string query = @"
        SELECT TOP 1 PrecioUnitario 
        FROM LoteCompra 
        WHERE ProductoCompraId = @ProductoCompraId AND Activo = 1 AND CantidadActual > 0
        ORDER BY FechaIngreso ASC"; 

            using (var conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                using (var cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@ProductoCompraId", productoCompraId);
                    var result = cmd.ExecuteScalar();
                    return result != DBNull.Value ? Convert.ToDecimal(result) : 0;
                }
            }
        }

        private string ObtenerNombreProductoCompra(int productoCompraId)
        {
            string query = "SELECT Nombre FROM ProductoCompra WHERE ProductoCompraId = @ProductoCompraId";
            using (var conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                using (var cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@ProductoCompraId", productoCompraId);
                    return cmd.ExecuteScalar()?.ToString() ?? "Producto";
                }
            }
        }

        private List<LoteCompraFIFO> ObtenerLotesCompraFIFO(int productoCompraId, int cantidad)
        {
            var lotes = new List<LoteCompraFIFO>();
            int cantidadRestante = cantidad;

            string query = @"
        SELECT LoteCompraId, CantidadActual
        FROM LoteCompra
        WHERE ProductoCompraId = @ProductoCompraId AND Activo = 1 AND CantidadActual > 0
        ORDER BY FechaVencimiento ASC, FechaIngreso ASC";

            using (var conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                using (var cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@ProductoCompraId", productoCompraId);
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read() && cantidadRestante > 0)
                        {
                            int loteId = (int)reader["LoteCompraId"];
                            int cantidadDisponible = (int)reader["CantidadActual"];
                            int cantidadUsar = Math.Min(cantidadRestante, cantidadDisponible);

                            lotes.Add(new LoteCompraFIFO
                            {
                                LoteId = loteId,
                                CantidadUsar = cantidadUsar
                            });

                            cantidadRestante -= cantidadUsar;
                        }
                    }
                }
            }

            if (cantidadRestante > 0)
            {
                throw new Exception($"No hay suficiente stock de materias primas. Faltan {cantidadRestante} unidades");
            }

            return lotes;
        }

        private decimal CalcularCostoProducto(List<dynamic> ingredientes, decimal rendimiento, decimal stockInicial)
        {
            decimal costoTotal = 0;

            foreach (var ing in ingredientes)
            {
                decimal precioUnitario = ObtenerPrecioUnitarioProductoCompra(ing.id);
                decimal cantidadTotal = ing.cantidad * stockInicial / rendimiento;
                costoTotal += precioUnitario * cantidadTotal;
            }

            return costoTotal / stockInicial;
        }

        public RecetaModels.VerificacionStockViewModel VerificarStockParaVenta(int productoId, decimal cantidad)
        {
            var resultado = new RecetaModels.VerificacionStockViewModel
            {
                ProductoId = productoId,
                CantidadSolicitada = cantidad,
                HayStock = true,
                Detalles = new List<RecetaModels.RecetaDetalleVerificacion>()
            };

            var producto = ObtenerProductoPorId(productoId);
            resultado.ProductoNombre = producto?.Nombre ?? "Producto";

            var receta = ObtenerRecetaPorProducto(productoId);

            if (receta == null)
            {
                resultado.EsProductoSimple = true;
                var stock = (int)producto.StockDisponible;
                if (stock < cantidad)
                {
                    resultado.HayStock = false;
                    resultado.Mensaje = $"Stock insuficiente. Disponible: {stock}, Necesita: {cantidad}";
                }
                return resultado;
            }

            resultado.EsProductoSimple = false;

            foreach (var detalle in receta.Detalles)
            {
                decimal cantidadTotal = detalle.CantidadNecesaria * cantidad / receta.Rendimiento;
                var stock = ObtenerStockProductoCompra(detalle.ProductoCompraId);
                bool suficiente = stock >= (int)Math.Ceiling(cantidadTotal);

                var verificacion = new RecetaModels.RecetaDetalleVerificacion
                {
                    ProductoCompraId = detalle.ProductoCompraId,
                    ProductoCompraNombre = detalle.ProductoCompraNombre,
                    CantidadNecesaria = detalle.CantidadNecesaria,
                    CantidadTotal = cantidadTotal,
                    UnidadMedida = detalle.UnidadMedidaAbreviatura,
                    StockDisponible = stock,
                    Suficiente = suficiente
                };

                resultado.Detalles.Add(verificacion);

                if (!suficiente)
                {
                    resultado.HayStock = false;
                }
            }

            if (!resultado.HayStock)
            {
                resultado.Mensaje = "No hay suficiente stock de materias primas para completar esta venta";
            }

            return resultado;
        }

        // ==================== FIFO ====================

        public List<LoteSeleccionado> AplicarFIFO(int productoId, decimal cantidad)
        {
            var lotesSeleccionados = new List<LoteSeleccionado>();
            decimal cantidadRestante = cantidad;

            string query = @"
                SELECT LoteProductoVentaId as LoteId, NumeroLote, CantidadActual, PrecioVenta
                FROM LoteProductoVenta
                WHERE ProductoId = @ProductoId AND Activo = 1 AND CantidadActual > 0
                ORDER BY FechaVencimiento ASC, FechaIngreso ASC";

            using (var conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                using (var cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@ProductoId", productoId);
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read() && cantidadRestante > 0)
                        {
                            var loteId = (int)reader["LoteId"];
                            var numeroLote = reader["NumeroLote"].ToString();
                            var cantidadDisponible = (decimal)reader["CantidadActual"];
                            var precioUnitario = (decimal)reader["PrecioVenta"];

                            decimal cantidadUsar = Math.Min(cantidadRestante, cantidadDisponible);

                            lotesSeleccionados.Add(new LoteSeleccionado
                            {
                                LoteId = loteId,
                                NumeroLote = numeroLote,
                                Cantidad = cantidadUsar,
                                PrecioUnitario = precioUnitario,
                                Subtotal = cantidadUsar * precioUnitario
                            });

                            cantidadRestante -= cantidadUsar;
                        }
                    }
                }
            }

            if (cantidadRestante > 0)
            {
                throw new Exception($"No hay suficiente stock. Faltan {cantidadRestante} unidades");
            }

            return lotesSeleccionados;
        }


        public int RegistrarVenta(VentaViewModel model, int usuarioId, string usuarioNombre)
        {
            int ventaId = 0;

            using (var conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                using (var transaction = conn.BeginTransaction())
                {
                    try
                    {
                        string numeroFactura = GenerarNumeroFactura();

                        string ventaQuery = @"
                    INSERT INTO Venta (NumeroFactura, FechaVenta, ClienteNombre, ClienteDocumento, ClienteTelefono, 
                                      TipoPago, Subtotal, Impuesto, Descuento, Total, Observaciones, Estado, 
                                      UsuarioId, UsuarioNombre, FechaCreacion)
                    VALUES (@NumeroFactura, GETDATE(), @ClienteNombre, @ClienteDocumento, @ClienteTelefono, 
                            @TipoPago, @Subtotal, @Impuesto, @Descuento, @Total, @Observaciones, 'Completada', 
                            @UsuarioId, @UsuarioNombre, GETDATE());
                    SELECT SCOPE_IDENTITY();";

                        using (var cmd = new SqlCommand(ventaQuery, conn, transaction))
                        {
                            cmd.Parameters.AddWithValue("@NumeroFactura", numeroFactura);
                            cmd.Parameters.AddWithValue("@ClienteNombre", model.ClienteNombre ?? "Consumo en mesa");
                            cmd.Parameters.AddWithValue("@ClienteDocumento", model.ClienteDocumento ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@ClienteTelefono", model.ClienteTelefono ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@TipoPago", model.TipoPago ?? "Efectivo");
                            cmd.Parameters.AddWithValue("@Subtotal", model.Subtotal);
                            cmd.Parameters.AddWithValue("@Impuesto", model.Impuesto);
                            cmd.Parameters.AddWithValue("@Descuento", model.Descuento);
                            cmd.Parameters.AddWithValue("@Total", model.Total);
                            cmd.Parameters.AddWithValue("@Observaciones", model.Observaciones ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@UsuarioId", usuarioId);
                            cmd.Parameters.AddWithValue("@UsuarioNombre", usuarioNombre);
                            ventaId = Convert.ToInt32(cmd.ExecuteScalar());
                        }

                        if (model.Carrito != null)
                        {
                            foreach (var item in model.Carrito)
                            {
                                string detalleQuery = @"
                            INSERT INTO DetalleVenta (VentaId, ProductoId, ProductoCodigo, ProductoNombre, 
                                                     Cantidad, PrecioUnitario, Descuento, Subtotal)
                            VALUES (@VentaId, @ProductoId, @ProductoCodigo, @ProductoNombre, 
                                    @Cantidad, @PrecioUnitario, @Descuento, @Subtotal)";

                                using (var cmd = new SqlCommand(detalleQuery, conn, transaction))
                                {
                                    cmd.Parameters.AddWithValue("@VentaId", ventaId);
                                    cmd.Parameters.AddWithValue("@ProductoId", item.ProductoId);
                                    cmd.Parameters.AddWithValue("@ProductoCodigo", item.Codigo ?? "");
                                    cmd.Parameters.AddWithValue("@ProductoNombre", item.Nombre);
                                    cmd.Parameters.AddWithValue("@Cantidad", item.Cantidad);
                                    cmd.Parameters.AddWithValue("@PrecioUnitario", item.PrecioUnitario);
                                    cmd.Parameters.AddWithValue("@Descuento", item.Descuento);
                                    cmd.Parameters.AddWithValue("@Subtotal", item.Subtotal);
                                    cmd.ExecuteNonQuery();
                                }

                                // Descontar stock del producto terminado
                                if (item.LotesSeleccionados != null && item.LotesSeleccionados.Any())
                                {
                                    foreach (var lote in item.LotesSeleccionados)
                                    {
                                        string updateLoteQuery = @"
                                    UPDATE LoteProductoVenta 
                                    SET CantidadActual = CantidadActual - @Cantidad
                                    WHERE LoteProductoVentaId = @LoteId";

                                        using (var cmd = new SqlCommand(updateLoteQuery, conn, transaction))
                                        {
                                            cmd.Parameters.AddWithValue("@LoteId", lote.LoteId);
                                            cmd.Parameters.AddWithValue("@Cantidad", lote.Cantidad);
                                            cmd.ExecuteNonQuery();
                                        }
                                    }
                                }
                            }
                        }

                        transaction.Commit();
                    }
                    catch
                    {
                        transaction.Rollback();
                        throw;
                    }
                }
            }

            return ventaId;
        }

        private string GenerarNumeroFactura()
        {
            string query = "SELECT ISNULL(MAX(CAST(SUBSTRING(NumeroFactura, 4, LEN(NumeroFactura)) AS INT)), 0) + 1 FROM Venta WHERE NumeroFactura LIKE 'INV-%'";
            using (var conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                using (var cmd = new SqlCommand(query, conn))
                {
                    int correlativo = Convert.ToInt32(cmd.ExecuteScalar());
                    return $"INV-{correlativo:D8}";
                }
            }
        }

        public VentaViewModel ObtenerVentaPorId(int ventaId)
        {
            var venta = new VentaViewModel
            {
                Carrito = new List<CarritoItem>()
            };

            // CORREGIDO: Usar DetalleVenta en lugar de VentaDetalle
            string query = @"
        SELECT v.VentaId, v.NumeroFactura, v.FechaVenta, v.ClienteNombre, v.ClienteDocumento, 
               v.ClienteTelefono, v.TipoPago, v.Subtotal, v.Impuesto, v.Descuento, v.Total, v.Observaciones,
               dv.DetalleVentaId, dv.ProductoId, dv.ProductoCodigo, dv.ProductoNombre, 
               dv.Cantidad, dv.PrecioUnitario, dv.Descuento as DetalleDescuento, dv.Subtotal as DetalleSubtotal
        FROM Venta v
        LEFT JOIN DetalleVenta dv ON v.VentaId = dv.VentaId
        WHERE v.VentaId = @VentaId";

            using (var conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                using (var cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@VentaId", ventaId);
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            if (venta.VentaId == 0)
                            {
                                venta.VentaId = (int)reader["VentaId"];
                                venta.NumeroFactura = reader["NumeroFactura"].ToString();
                                venta.FechaVenta = (DateTime)reader["FechaVenta"];
                                venta.ClienteNombre = reader["ClienteNombre"]?.ToString();
                                venta.ClienteDocumento = reader["ClienteDocumento"]?.ToString();
                                venta.ClienteTelefono = reader["ClienteTelefono"]?.ToString();
                                venta.TipoPago = reader["TipoPago"]?.ToString();
                                venta.Subtotal = (decimal)reader["Subtotal"];
                                venta.Impuesto = (decimal)reader["Impuesto"];
                                venta.Descuento = (decimal)reader["Descuento"];
                                venta.Total = (decimal)reader["Total"];
                                venta.Observaciones = reader["Observaciones"]?.ToString();
                            }

                            if (reader["DetalleVentaId"] != DBNull.Value)
                            {
                                venta.Carrito.Add(new CarritoItem
                                {
                                    ProductoId = (int)reader["ProductoId"],
                                    Codigo = reader["ProductoCodigo"].ToString(),
                                    Nombre = reader["ProductoNombre"].ToString(),
                                    Cantidad = (decimal)reader["Cantidad"],
                                    PrecioUnitario = (decimal)reader["PrecioUnitario"],
                                    Descuento = (decimal)reader["DetalleDescuento"],
                                    Subtotal = (decimal)reader["DetalleSubtotal"]
                                });
                            }
                        }
                    }
                }
            }

            return venta;
        }

        // ==================== GESTIÓN DE PRODUCTOS ====================

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
                    return (int)cmd.ExecuteScalar() > 0;
                }
            }
        }

        public int AgregarProducto(string nombre, string codigo, decimal precio)
        {
            string query = @"
                INSERT INTO Producto (Nombre, Codigo, PrecioVenta, SubDepartamentoId, PresentacionId,
                                     PrecioCompra, EstaEnOferta, Activo, FechaCreacion, TipoProductoId)
                VALUES (@Nombre, @Codigo, @Precio, 4, 5, 0, 0, 1, GETDATE(), 2);
                SELECT SCOPE_IDENTITY();";

            using (var conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                using (var cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@Nombre", nombre);
                    cmd.Parameters.AddWithValue("@Codigo", codigo);
                    cmd.Parameters.AddWithValue("@Precio", precio);
                    return Convert.ToInt32(cmd.ExecuteScalar());
                }
            }
        }

        public bool EditarProducto(int productoId, string nombre, string codigo, decimal precio)
        {
            string query = @"
                UPDATE Producto 
                SET Nombre = @Nombre, Codigo = @Codigo, PrecioVenta = @Precio, FechaModificacion = GETDATE()
                WHERE ProductoId = @ProductoId AND Activo = 1";

            using (var conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                using (var cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@ProductoId", productoId);
                    cmd.Parameters.AddWithValue("@Nombre", nombre);
                    cmd.Parameters.AddWithValue("@Codigo", codigo);
                    cmd.Parameters.AddWithValue("@Precio", precio);
                    return cmd.ExecuteNonQuery() > 0;
                }
            }
        }

        public bool EliminarProducto(int productoId)
        {
            string query = "UPDATE Producto SET Activo = 0 WHERE ProductoId = @ProductoId";
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

        // ==================== MATERIAS PRIMAS ====================

        public List<object> ObtenerMateriasPrimas()
        {
            var materias = new List<object>();
            string query = "SELECT ProductoCompraId, Codigo, Nombre, UnidadMedida, StockActual FROM ProductoCompra WHERE Activo = 1 ORDER BY Nombre";

            using (var conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                using (var cmd = new SqlCommand(query, conn))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        materias.Add(new
                        {
                            ProductoCompraId = (int)reader["ProductoCompraId"],
                            Codigo = reader["Codigo"].ToString(),
                            Nombre = reader["Nombre"].ToString(),
                            UnidadMedida = reader["UnidadMedida"].ToString(),
                            StockActual = (int)reader["StockActual"]
                        });
                    }
                }
            }
            return materias;
        }

        // ==================== CREAR PRODUCTO COMPLETO ====================

        public int AgregarProductoCompleto(string nombre, string codigo, decimal precio, decimal rendimiento, List<object> ingredientes, decimal stockInicial = 10)
        {
            int productoId = 0;
            int recetaId = 0;

            using (var conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                using (var transaction = conn.BeginTransaction())
                {
                    try
                    {
                        // ==================== 1. CREAR PRODUCTO TERMINADO ====================
                        string queryProducto = @"
                    INSERT INTO Producto (Nombre, Codigo, PrecioVenta, SubDepartamentoId, PresentacionId,
                                         PrecioCompra, EstaEnOferta, Activo, FechaCreacion, TipoProductoId)
                    VALUES (@Nombre, @Codigo, @Precio, 4, 5, 0, 0, 1, GETDATE(), 2);
                    SELECT SCOPE_IDENTITY();";

                        using (var cmd = new SqlCommand(queryProducto, conn, transaction))
                        {
                            cmd.Parameters.AddWithValue("@Nombre", nombre);
                            cmd.Parameters.AddWithValue("@Codigo", codigo);
                            cmd.Parameters.AddWithValue("@Precio", precio);
                            productoId = Convert.ToInt32(cmd.ExecuteScalar());
                        }

                        // ==================== 2. CREAR LOTE DEL PRODUCTO TERMINADO (STOCK INICIAL) ====================
                        string queryLote = @"
                    INSERT INTO LoteProductoVenta (ProductoId, NumeroLote, CantidadInicial, CantidadActual, PrecioCompra, PrecioVenta, FechaIngreso, Activo)
                    VALUES (@ProductoId, @NumeroLote, @CantidadInicial, @CantidadInicial, @PrecioCompra, @PrecioVenta, GETDATE(), 1)";

                        using (var cmd = new SqlCommand(queryLote, conn, transaction))
                        {
                            cmd.Parameters.AddWithValue("@ProductoId", productoId);
                            cmd.Parameters.AddWithValue("@NumeroLote", $"LOTE-{DateTime.Now:yyyyMMddHHmmss}");
                            cmd.Parameters.AddWithValue("@CantidadInicial", stockInicial);
                            cmd.Parameters.AddWithValue("@PrecioCompra", precio * 0.6m);
                            cmd.Parameters.AddWithValue("@PrecioVenta", precio);
                            cmd.ExecuteNonQuery();
                        }

                        // ==================== 3. CREAR RECETA ====================
                        string queryReceta = @"
                    INSERT INTO Receta (ProductoTerminadoId, NombreReceta, Rendimiento, Activo, FechaCreacion)
                    VALUES (@ProductoId, @NombreReceta, @Rendimiento, 1, GETDATE());
                    SELECT SCOPE_IDENTITY();";

                        using (var cmd = new SqlCommand(queryReceta, conn, transaction))
                        {
                            cmd.Parameters.AddWithValue("@ProductoId", productoId);
                            cmd.Parameters.AddWithValue("@NombreReceta", "Receta " + nombre);
                            cmd.Parameters.AddWithValue("@Rendimiento", rendimiento);
                            recetaId = Convert.ToInt32(cmd.ExecuteScalar());
                        }

                        // ==================== 4. CREAR DETALLES DE RECETA ====================
                        foreach (var ing in ingredientes)
                        {
                            // Obtener valores usando reflexión
                            var props = ing.GetType().GetProperties();
                            string codigoMP = "";
                            decimal cantidad = 0;

                            foreach (var prop in props)
                            {
                                if (prop.Name == "codigo")
                                    codigoMP = prop.GetValue(ing)?.ToString() ?? "";
                                if (prop.Name == "cantidad")
                                    cantidad = Convert.ToDecimal(prop.GetValue(ing));
                            }

                            // Obtener ProductoCompraId
                            string getIdQuery = "SELECT ProductoCompraId FROM ProductoCompra WHERE Codigo = @Codigo AND Activo = 1";
                            int productoCompraId = 0;

                            using (var cmd = new SqlCommand(getIdQuery, conn, transaction))
                            {
                                cmd.Parameters.AddWithValue("@Codigo", codigoMP);
                                var result = cmd.ExecuteScalar();
                                if (result != null)
                                {
                                    productoCompraId = Convert.ToInt32(result);
                                }
                                else
                                {
                                    throw new Exception($"No se encontró la materia prima con código: {codigoMP}");
                                }
                            }

                            string queryDetalle = @"
                        INSERT INTO RecetaDetalle (RecetaId, ProductoCompraId, CantidadNecesaria, UnidadMedidaId)
                        VALUES (@RecetaId, @ProductoCompraId, @Cantidad, 1)";

                            using (var cmd = new SqlCommand(queryDetalle, conn, transaction))
                            {
                                cmd.Parameters.AddWithValue("@RecetaId", recetaId);
                                cmd.Parameters.AddWithValue("@ProductoCompraId", productoCompraId);
                                cmd.Parameters.AddWithValue("@Cantidad", cantidad);
                                cmd.ExecuteNonQuery();
                            }
                        }

                        transaction.Commit();
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        throw new Exception($"Error al crear producto: {ex.Message}");
                    }
                }
            }

            return productoId;
        }


        // Clase auxiliar
        private class LoteCompraFIFO
        {
            public int LoteId { get; set; }
            public int CantidadUsar { get; set; }
        }
    }
}