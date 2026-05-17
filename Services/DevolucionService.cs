using LaMediaCancha.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;
using static LaMediaCancha.Models.DevolucionModels;

namespace LaMediaCancha.Services
{
    public class DevolucionService
    {
        private readonly string _connectionString;

        public DevolucionService()
        {
            _connectionString = ConfigurationManager.ConnectionStrings["LaMediaCanchaDB"].ConnectionString;
        }

        public int RegistrarDevolucion(RegistrarDevolucionRequest request)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                using (var transaction = conn.BeginTransaction())
                {
                    try
                    {
                        // =============================================
                        // 1. Validar que ningún producto esté en oferta
                        // =============================================
                        string productoIds = string.Join(",", request.Productos.Select(p => p.ProductoId));
                        string checkOfertaQuery = $@"
                            SELECT COUNT(*) FROM DetalleCompra 
                            WHERE CompraId = @CompraId 
                              AND ProductoId IN ({productoIds})
                              AND EstabaEnOferta = 1";

                        using (var cmd = new SqlCommand(checkOfertaQuery, conn, transaction))
                        {
                            cmd.Parameters.AddWithValue("@CompraId", request.CompraId);
                            int enOferta = (int)cmd.ExecuteScalar();
                            if (enOferta > 0)
                            {
                                throw new Exception("No se pueden devolver productos que fueron comprados en oferta");
                            }
                        }

                        // =============================================
                        // 2. Obtener datos de la compra
                        // =============================================
                        string getCompraQuery = @"
                            SELECT NumeroDocumento, FechaCompra, Subtotal, Total,
                                   CASE WHEN EXISTS(SELECT 1 FROM DetalleCompra WHERE CompraId = @CompraId AND EstabaEnOferta = 1) 
                                        THEN 1 ELSE 0 END AS TeniaProductosEnOferta
                            FROM EncabezadoCompra
                            WHERE CompraId = @CompraId";

                        string numeroDocCompra = "";
                        DateTime fechaCompra = DateTime.Now;
                        bool teniaProductosEnOferta = false;

                        using (var cmd = new SqlCommand(getCompraQuery, conn, transaction))
                        {
                            cmd.Parameters.AddWithValue("@CompraId", request.CompraId);
                            using (var reader = cmd.ExecuteReader())
                            {
                                if (reader.Read())
                                {
                                    numeroDocCompra = reader["NumeroDocumento"].ToString();
                                    fechaCompra = (DateTime)reader["FechaCompra"];
                                    teniaProductosEnOferta = (bool)reader["TeniaProductosEnOferta"];
                                }
                            }
                        }

                        // =============================================
                        // 3. Calcular monto total de la devolución
                        // =============================================
                        decimal montoTotal = 0;
                        foreach (var item in request.Productos)
                        {
                            string getPrecioQuery = @"
                                SELECT 
                                    CASE 
                                        WHEN EstabaEnOferta = 1 AND PrecioOferta IS NOT NULL THEN PrecioOferta
                                        ELSE PrecioUnitario
                                    END AS PrecioFinal
                                FROM DetalleCompra
                                WHERE CompraId = @CompraId AND ProductoId = @ProductoId";

                            using (var cmd = new SqlCommand(getPrecioQuery, conn, transaction))
                            {
                                cmd.Parameters.AddWithValue("@CompraId", request.CompraId);
                                cmd.Parameters.AddWithValue("@ProductoId", item.ProductoId);
                                var precioFinal = (decimal)cmd.ExecuteScalar();
                                montoTotal += item.Cantidad * precioFinal;
                            }
                        }

                        // =============================================
                        // 4. Insertar encabezado de devolución
                        // =============================================
                        string insertEncabezado = @"
                            INSERT INTO EncabezadoDevolucion (
                                CompraId, EmpleadoId, NumeroDocCompra, FechaCompraRef,
                                TeniaProductosEnOferta, FechaDevolucion, Motivo,
                                TipoDevolucion, MontoTotal, Estado, Activo, FechaCreacion)
                            VALUES (
                                @CompraId, @EmpleadoId, @NumeroDocCompra, @FechaCompraRef,
                                @TeniaProductosEnOferta, GETDATE(), @Motivo,
                                @TipoDevolucion, @MontoTotal, 'Completada', 1, GETDATE());
                            SELECT SCOPE_IDENTITY();";

                        int devolucionId;
                        using (var cmd = new SqlCommand(insertEncabezado, conn, transaction))
                        {
                            cmd.Parameters.AddWithValue("@CompraId", request.CompraId);
                            cmd.Parameters.AddWithValue("@EmpleadoId", request.EmpleadoId);
                            cmd.Parameters.AddWithValue("@NumeroDocCompra", numeroDocCompra);
                            cmd.Parameters.AddWithValue("@FechaCompraRef", fechaCompra);
                            cmd.Parameters.AddWithValue("@TeniaProductosEnOferta", teniaProductosEnOferta);
                            cmd.Parameters.AddWithValue("@Motivo", request.Motivo);
                            cmd.Parameters.AddWithValue("@TipoDevolucion", request.TipoDevolucion);
                            cmd.Parameters.AddWithValue("@MontoTotal", montoTotal);
                            devolucionId = Convert.ToInt32(cmd.ExecuteScalar());
                        }

                        // =============================================
                        // 5. Insertar detalles y actualizar stock
                        // =============================================
                        foreach (var item in request.Productos)
                        {
                            // Obtener detalles del producto de la compra
                            string getDetalleCompra = @"
                                SELECT 
                                    CASE 
                                        WHEN EstabaEnOferta = 1 AND PrecioOferta IS NOT NULL THEN PrecioOferta
                                        ELSE PrecioUnitario
                                    END AS PrecioReferencia,
                                    EstabaEnOferta,
                                    PrecioOferta
                                FROM DetalleCompra
                                WHERE CompraId = @CompraId AND ProductoId = @ProductoId";

                            decimal precioReferencia = 0;
                            bool estabaEnOferta = false;
                            decimal? precioOfertaRef = null;

                            using (var cmd = new SqlCommand(getDetalleCompra, conn, transaction))
                            {
                                cmd.Parameters.AddWithValue("@CompraId", request.CompraId);
                                cmd.Parameters.AddWithValue("@ProductoId", item.ProductoId);
                                using (var reader = cmd.ExecuteReader())
                                {
                                    if (reader.Read())
                                    {
                                        precioReferencia = (decimal)reader["PrecioReferencia"];
                                        estabaEnOferta = (bool)reader["EstabaEnOferta"];
                                        precioOfertaRef = reader["PrecioOferta"] as decimal?;
                                    }
                                }
                            }

                            decimal subtotal = item.Cantidad * precioReferencia;

                            // Insertar detalle de devolución
                            string insertDetalle = @"
                                INSERT INTO DetalleDevolucion (
                                    DevolucionId, ProductoId, Cantidad, PrecioReferencia, 
                                    Subtotal, EstabaEnOferta, PrecioOfertaRef)
                                VALUES (
                                    @DevolucionId, @ProductoId, @Cantidad, @PrecioReferencia, 
                                    @Subtotal, @EstabaEnOferta, @PrecioOfertaRef)";

                            using (var cmd = new SqlCommand(insertDetalle, conn, transaction))
                            {
                                cmd.Parameters.AddWithValue("@DevolucionId", devolucionId);
                                cmd.Parameters.AddWithValue("@ProductoId", item.ProductoId);
                                cmd.Parameters.AddWithValue("@Cantidad", item.Cantidad);
                                cmd.Parameters.AddWithValue("@PrecioReferencia", precioReferencia);
                                cmd.Parameters.AddWithValue("@Subtotal", subtotal);
                                cmd.Parameters.AddWithValue("@EstabaEnOferta", estabaEnOferta);
                                cmd.Parameters.AddWithValue("@PrecioOfertaRef", precioOfertaRef.HasValue ? (object)precioOfertaRef.Value : DBNull.Value);
                                cmd.ExecuteNonQuery();
                            }

                            // Actualizar CantidadDevuelta en DetalleCompra
                            string updateDetalleCompra = @"
                                UPDATE DetalleCompra 
                                SET CantidadDevuelta = CantidadDevuelta + @Cantidad
                                WHERE CompraId = @CompraId AND ProductoId = @ProductoId";

                            using (var cmd = new SqlCommand(updateDetalleCompra, conn, transaction))
                            {
                                cmd.Parameters.AddWithValue("@CompraId", request.CompraId);
                                cmd.Parameters.AddWithValue("@ProductoId", item.ProductoId);
                                cmd.Parameters.AddWithValue("@Cantidad", item.Cantidad);
                                int rowsAffected = cmd.ExecuteNonQuery();

                                if (rowsAffected == 0)
                                {
                                    throw new Exception($"No se encontró el producto {item.ProductoId} en la compra");
                                }
                            }

                            // =============================================
                            // 6. DESCONTAR STOCK de ProductoCompra
                            // =============================================
                            string updateStock = @"
                                UPDATE ProductoCompra 
                                SET StockActual = StockActual - @Cantidad
                                WHERE ProductoCompraId = @ProductoId";

                            using (var cmd = new SqlCommand(updateStock, conn, transaction))
                            {
                                cmd.Parameters.AddWithValue("@ProductoId", item.ProductoId);
                                cmd.Parameters.AddWithValue("@Cantidad", item.Cantidad);
                                int rowsAffected = cmd.ExecuteNonQuery();

                                if (rowsAffected == 0)
                                {
                                    throw new Exception($"No se encontró el producto con ID {item.ProductoId} en el inventario");
                                }
                            }
                        }

                        // =============================================
                        // 7. Marcar compra como Cerrada
                        // =============================================
                        string updateCompra = @"
                            UPDATE EncabezadoCompra 
                            SET Estado = 'Cerrada' 
                            WHERE CompraId = @CompraId AND Estado = 'Aprobada'";

                        using (var cmd = new SqlCommand(updateCompra, conn, transaction))
                        {
                            cmd.Parameters.AddWithValue("@CompraId", request.CompraId);
                            cmd.ExecuteNonQuery();
                        }

                        transaction.Commit();
                        return devolucionId;
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        throw new Exception($"Error al registrar devolución: {ex.Message}", ex);
                    }
                }
            }
        }

        public List<object> ObtenerProductosDisponiblesParaDevolver(int compraId)
        {
            var productos = new List<object>();

            string query = @"
                SELECT 
                    dc.ProductoId,
                    pc.Codigo AS CodigoProducto,
                    pc.Nombre AS NombreProducto,
                    ISNULL(pc.UnidadMedida, 'Unidad') AS Presentacion,
                    dc.Cantidad AS CantidadComprada,
                    ISNULL(dc.CantidadDevuelta, 0) AS CantidadYaDevuelta,
                    (dc.Cantidad - ISNULL(dc.CantidadDevuelta, 0)) AS Disponible,
                    dc.PrecioUnitario,
                    dc.EstabaEnOferta,
                    CASE 
                        WHEN dc.EstabaEnOferta = 1 THEN 'No disponible (producto en oferta)'
                        ELSE 'Disponible'
                    END AS MensajeEstado
                FROM DetalleCompra dc
                INNER JOIN ProductoCompra pc ON dc.ProductoId = pc.ProductoCompraId
                WHERE dc.CompraId = @CompraId
                  AND (dc.Cantidad - ISNULL(dc.CantidadDevuelta, 0)) > 0
                  AND dc.EstabaEnOferta = 0
                ORDER BY pc.Nombre";

            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@CompraId", compraId);
                conn.Open();

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        productos.Add(new
                        {
                            ProductoId = reader.GetInt32(reader.GetOrdinal("ProductoId")),
                            CodigoProducto = reader.GetString(reader.GetOrdinal("CodigoProducto")),
                            NombreProducto = reader.GetString(reader.GetOrdinal("NombreProducto")),
                            Presentacion = reader.GetString(reader.GetOrdinal("Presentacion")),
                            CantidadComprada = reader.GetDecimal(reader.GetOrdinal("CantidadComprada")),
                            CantidadYaDevuelta = reader.GetDecimal(reader.GetOrdinal("CantidadYaDevuelta")),
                            Disponible = reader.GetDecimal(reader.GetOrdinal("Disponible")),
                            PrecioUnitario = reader.GetDecimal(reader.GetOrdinal("PrecioUnitario")),
                            EstaEnOferta = reader.GetBoolean(reader.GetOrdinal("EstabaEnOferta")),
                            MensajeEstado = reader.GetString(reader.GetOrdinal("MensajeEstado"))
                        });
                    }
                }
            }

            return productos;
        }
    }
}