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
        private readonly InventarioService _inventarioService;

        public DevolucionService()
        {
            _connectionString = ConfigurationManager.ConnectionStrings["LaMediaCanchaDB"].ConnectionString;
            _inventarioService = new InventarioService();
        }

        // ==================== DEVOLUCIÓN A PROVEEDOR (MATERIA PRIMA) ====================

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
                        // 2. Validar plazo de devolución del proveedor
                        // =============================================
                        string checkPlazoQuery = @"
                            SELECT DATEDIFF(DAY, ec.FechaCompra, GETDATE()) AS DiasTranscurridos,
                                   ISNULL(p.DiasMaximosDevolucion, 10) AS DiasMaximos
                            FROM EncabezadoCompra ec
                            INNER JOIN Proveedor p ON ec.ProveedorId = p.ProveedorId
                            WHERE ec.CompraId = @CompraId";

                        int diasTranscurridos = 0;
                        int diasMaximos = 0;

                        using (var cmd = new SqlCommand(checkPlazoQuery, conn, transaction))
                        {
                            cmd.Parameters.AddWithValue("@CompraId", request.CompraId);
                            using (var reader = cmd.ExecuteReader())
                            {
                                if (reader.Read())
                                {
                                    diasTranscurridos = (int)reader["DiasTranscurridos"];
                                    diasMaximos = (int)reader["DiasMaximos"];
                                }
                            }
                        }

                        if (diasTranscurridos > diasMaximos)
                        {
                            throw new Exception($"Fuera de plazo de devolución. Máximo {diasMaximos} días.");
                        }

                        // =============================================
                        // 3. Obtener datos de la compra
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
                        // 4. Calcular monto total de la devolución
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
                        // 5. Insertar encabezado de devolución
                        // =============================================
                        string numeroDevolucion = $"DEV-PROV-{DateTime.Now:yyyyMMddHHmmss}";

                        string insertEncabezado = @"
                            INSERT INTO EncabezadoDevolucion (
                                CompraId, EmpleadoId, NumeroDocCompra, FechaCompraRef,
                                TeniaProductosEnOferta, FechaDevolucion, Motivo,
                                TipoDevolucion, MontoTotal, Estado, Activo, FechaCreacion,
                                NumeroDevolucion, Tipo)
                            VALUES (
                                @CompraId, @EmpleadoId, @NumeroDocCompra, @FechaCompraRef,
                                @TeniaProductosEnOferta, GETDATE(), @Motivo,
                                @TipoDevolucion, @MontoTotal, 'Pendiente', 1, GETDATE(),
                                @NumeroDevolucion, 'Proveedor')";

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
                            cmd.Parameters.AddWithValue("@NumeroDevolucion", numeroDevolucion);
                            devolucionId = Convert.ToInt32(cmd.ExecuteScalar());
                        }

                        // =============================================
                        // 6. Insertar detalles y actualizar stock
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

                            string insertDetalle = @"
                                INSERT INTO DetalleDevolucion (
                                    DevolucionId, ProductoId, Cantidad, PrecioReferencia, 
                                    Subtotal, EstabaEnOferta, PrecioOfertaRef, Tipo)
                                VALUES (
                                    @DevolucionId, @ProductoId, @Cantidad, @PrecioReferencia, 
                                    @Subtotal, @EstabaEnOferta, @PrecioOfertaRef, 'Proveedor')";

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

                            // DESCONTAR STOCK de ProductoCompra
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

                        // Marcar compra como Cerrada
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

        // ==================== DEVOLUCIÓN DE CLIENTE (VENTA) ====================

        public int RegistrarDevolucionCliente(RegistrarDevolucionClienteRequest request)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                using (var transaction = conn.BeginTransaction())
                {
                    try
                    {
                        string numeroDevolucion = $"DEV-CLI-{DateTime.Now:yyyyMMddHHmmss}";
                        decimal montoTotal = 0;
                        bool requiereAutorizacion = false;

                        // Obtener información de la orden
                        int? ordenId = null;
                        string clienteNombre = "";
                        int empleadoId = request.EmpleadoId;

                        if (request.OrdenId.HasValue)
                        {
                            string getOrdenQuery = @"
                                SELECT o.OrdenId, o.ClienteNombre, o.Total
                                FROM Orden o
                                WHERE o.OrdenId = @OrdenId";

                            using (var cmd = new SqlCommand(getOrdenQuery, conn, transaction))
                            {
                                cmd.Parameters.AddWithValue("@OrdenId", request.OrdenId.Value);
                                using (var reader = cmd.ExecuteReader())
                                {
                                    if (reader.Read())
                                    {
                                        ordenId = (int)reader["OrdenId"];
                                        clienteNombre = reader["ClienteNombre"]?.ToString() ?? "Cliente";
                                    }
                                }
                            }
                        }

                        // Procesar cada producto a devolver
                        foreach (var item in request.Productos)
                        {
                            // Obtener detalles del detalle de orden
                            string getDetalleOrden = @"
                                SELECT 
                                    d.ProductoId,
                                    d.ProductoNombre,
                                    d.Cantidad,
                                    d.PrecioUnitario,
                                    d.EstabaEnOferta,
                                    d.PrecioOferta,
                                    d.OrdenId
                                FROM DetalleOrden d
                                WHERE d.DetalleOrdenId = @DetalleOrdenId";

                            int productoId = 0;
                            string productoNombre = "";
                            decimal cantidadVendida = 0;
                            decimal precioUnitario = 0;
                            bool estabaEnOferta = false;
                            decimal? precioOferta = null;
                            int ordenIdFromDetalle = 0;

                            using (var cmd = new SqlCommand(getDetalleOrden, conn, transaction))
                            {
                                cmd.Parameters.AddWithValue("@DetalleOrdenId", item.DetalleOrdenId);
                                using (var reader = cmd.ExecuteReader())
                                {
                                    if (reader.Read())
                                    {
                                        productoId = (int)reader["ProductoId"];
                                        productoNombre = reader["ProductoNombre"].ToString();
                                        cantidadVendida = (decimal)reader["Cantidad"];
                                        precioUnitario = (decimal)reader["PrecioUnitario"];
                                        estabaEnOferta = (bool)reader["EstabaEnOferta"];
                                        precioOferta = reader["PrecioOferta"] as decimal?;
                                        ordenIdFromDetalle = (int)reader["OrdenId"];
                                        ordenId = ordenIdFromDetalle;
                                    }
                                }
                            }

                            if (item.Cantidad > cantidadVendida)
                            {
                                throw new Exception($"La cantidad a devolver no puede ser mayor a la cantidad vendida de {productoNombre}");
                            }

                            decimal precioFinal = estabaEnOferta && precioOferta.HasValue ? precioOferta.Value : precioUnitario;
                            decimal subtotal = item.Cantidad * precioFinal;
                            montoTotal += subtotal;

                            // Verificar si requiere autorización (producto en oferta)
                            if (estabaEnOferta && item.DestinoStock == "DevolucionStock")
                            {
                                requiereAutorizacion = true;
                            }
                        }

                        // Insertar encabezado de devolución de cliente
                        string estado = requiereAutorizacion ? "PendienteAutorizacion" : "Completada";

                        string insertEncabezado = @"
                            INSERT INTO EncabezadoDevolucion (
                                OrdenId, EmpleadoId, ClienteNombre, FechaDevolucion, Motivo,
                                TipoDevolucion, MontoTotal, Estado, Activo, FechaCreacion,
                                NumeroDevolucion, Tipo, FormaCompensacion)
                            VALUES (
                                @OrdenId, @EmpleadoId, @ClienteNombre, GETDATE(), @Motivo,
                                @TipoDevolucion, @MontoTotal, @Estado, 1, GETDATE(),
                                @NumeroDevolucion, 'Cliente', @FormaCompensacion);
                            SELECT SCOPE_IDENTITY();";

                        int devolucionId;
                        using (var cmd = new SqlCommand(insertEncabezado, conn, transaction))
                        {
                            cmd.Parameters.AddWithValue("@OrdenId", ordenId.HasValue ? (object)ordenId.Value : DBNull.Value);
                            cmd.Parameters.AddWithValue("@EmpleadoId", empleadoId);
                            cmd.Parameters.AddWithValue("@ClienteNombre", clienteNombre);
                            cmd.Parameters.AddWithValue("@Motivo", request.Motivo);
                            cmd.Parameters.AddWithValue("@TipoDevolucion", request.TipoDevolucion);
                            cmd.Parameters.AddWithValue("@MontoTotal", montoTotal);
                            cmd.Parameters.AddWithValue("@Estado", estado);
                            cmd.Parameters.AddWithValue("@NumeroDevolucion", numeroDevolucion);
                            cmd.Parameters.AddWithValue("@FormaCompensacion", request.FormaCompensacion);
                            devolucionId = Convert.ToInt32(cmd.ExecuteScalar());
                        }

                        // Insertar detalles con el DevolucionId correcto
                        foreach (var item in request.Productos)
                        {
                            // Obtener detalles nuevamente
                            string getDetalleOrden = @"
                                SELECT 
                                    d.ProductoId,
                                    d.ProductoNombre,
                                    d.PrecioUnitario,
                                    d.EstabaEnOferta,
                                    d.PrecioOferta
                                FROM DetalleOrden d
                                WHERE d.DetalleOrdenId = @DetalleOrdenId";

                            decimal precioUnitario = 0;
                            bool estabaEnOferta = false;
                            decimal? precioOferta = null;

                            using (var cmd = new SqlCommand(getDetalleOrden, conn, transaction))
                            {
                                cmd.Parameters.AddWithValue("@DetalleOrdenId", item.DetalleOrdenId);
                                using (var reader = cmd.ExecuteReader())
                                {
                                    if (reader.Read())
                                    {
                                        precioUnitario = (decimal)reader["PrecioUnitario"];
                                        estabaEnOferta = (bool)reader["EstabaEnOferta"];
                                        precioOferta = reader["PrecioOferta"] as decimal?;
                                    }
                                }
                            }

                            decimal precioFinal = estabaEnOferta && precioOferta.HasValue ? precioOferta.Value : precioUnitario;
                            decimal subtotal = item.Cantidad * precioFinal;

                            string insertDetalle = @"
                                INSERT INTO DetalleDevolucion (
                                    DevolucionId, ProductoId, Cantidad, PrecioReferencia, 
                                    Subtotal, EstabaEnOferta, PrecioOfertaRef, Tipo,
                                    DetalleOrdenId, DestinoStock, Autorizado)
                                VALUES (
                                    @DevolucionId, @ProductoId, @Cantidad, @PrecioReferencia, 
                                    @Subtotal, @EstabaEnOferta, @PrecioOfertaRef, 'Cliente',
                                    @DetalleOrdenId, @DestinoStock, @Autorizado)";

                            using (var cmd = new SqlCommand(insertDetalle, conn, transaction))
                            {
                                cmd.Parameters.AddWithValue("@DevolucionId", devolucionId);
                                cmd.Parameters.AddWithValue("@ProductoId", item.ProductoId);
                                cmd.Parameters.AddWithValue("@Cantidad", item.Cantidad);
                                cmd.Parameters.AddWithValue("@PrecioReferencia", precioFinal);
                                cmd.Parameters.AddWithValue("@Subtotal", subtotal);
                                cmd.Parameters.AddWithValue("@EstabaEnOferta", estabaEnOferta);
                                cmd.Parameters.AddWithValue("@PrecioOfertaRef", precioOferta.HasValue ? (object)precioOferta.Value : DBNull.Value);
                                cmd.Parameters.AddWithValue("@DetalleOrdenId", item.DetalleOrdenId);
                                cmd.Parameters.AddWithValue("@DestinoStock", item.DestinoStock);
                                cmd.Parameters.AddWithValue("@Autorizado", item.Autorizado);
                                cmd.ExecuteNonQuery();
                            }

                            // Actualizar CantidadDevuelta en DetalleOrden
                            string updateDetalleOrden = @"
                                UPDATE DetalleOrden 
                                SET CantidadDevuelta = ISNULL(CantidadDevuelta, 0) + @Cantidad
                                WHERE DetalleOrdenId = @DetalleOrdenId";

                            using (var cmd = new SqlCommand(updateDetalleOrden, conn, transaction))
                            {
                                cmd.Parameters.AddWithValue("@DetalleOrdenId", item.DetalleOrdenId);
                                cmd.Parameters.AddWithValue("@Cantidad", item.Cantidad);
                                cmd.ExecuteNonQuery();
                            }

                            // Si el destino es Merma, descontar stock
                            if (item.DestinoStock == "Merma")
                            {
                                // Obtener productoId
                                int productoId = 0;
                                string getProducto = "SELECT ProductoId FROM DetalleOrden WHERE DetalleOrdenId = @DetalleOrdenId";
                                using (var cmd = new SqlCommand(getProducto, conn, transaction))
                                {
                                    cmd.Parameters.AddWithValue("@DetalleOrdenId", item.DetalleOrdenId);
                                    productoId = (int)cmd.ExecuteScalar();
                                }

                                // Convertir decimal a int
                                int cantidadInt = Convert.ToInt32(item.Cantidad);

                                // Descontar stock usando FIFO
                                try
                                {
                                    _inventarioService.DescontarStockFIFO(productoId, cantidadInt, devolucionId, $"Devolución cliente - {request.Motivo}", empleadoId);
                                }
                                catch (Exception ex)
                                {
                                    // Si no es materia prima, no hay problema
                                    System.Diagnostics.Debug.WriteLine($"No se pudo descontar stock: {ex.Message}");
                                }
                            }
                        }

                        transaction.Commit();
                        return devolucionId;
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        throw new Exception($"Error al registrar devolución de cliente: {ex.Message}", ex);
                    }
                }
            }
        }

        // ==================== AUTORIZACIÓN DE DEVOLUCIÓN ====================

        public bool AutorizarDevolucionCliente(int devolucionId, int empleadoId, string motivo)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                using (var transaction = conn.BeginTransaction())
                {
                    try
                    {
                        // Obtener nombre del empleado
                        string empleadoNombre = "";
                        string getEmpleado = @"
                            SELECT p.Nombres + ' ' + p.Apellidos 
                            FROM Empleado e 
                            INNER JOIN Persona p ON e.PersonaId = p.PersonaId 
                            WHERE e.EmpleadoId = @EmpleadoId";

                        using (var cmd = new SqlCommand(getEmpleado, conn, transaction))
                        {
                            cmd.Parameters.AddWithValue("@EmpleadoId", empleadoId);
                            var result = cmd.ExecuteScalar();
                            if (result != null)
                                empleadoNombre = result.ToString();
                        }

                        // Actualizar detalles no autorizados
                        string updateDetalles = @"
                            UPDATE DetalleDevolucion 
                            SET Autorizado = 1 
                            WHERE DevolucionId = @DevolucionId AND Autorizado = 0";

                        using (var cmd = new SqlCommand(updateDetalles, conn, transaction))
                        {
                            cmd.Parameters.AddWithValue("@DevolucionId", devolucionId);
                            cmd.ExecuteNonQuery();
                        }

                        // Actualizar encabezado
                        string updateEncabezado = @"
                            UPDATE EncabezadoDevolucion 
                            SET Estado = 'Completada', 
                                AutorizadoPor = @AutorizadoPor,
                                FechaModificacion = GETDATE()
                            WHERE DevolucionId = @DevolucionId";

                        using (var cmd = new SqlCommand(updateEncabezado, conn, transaction))
                        {
                            cmd.Parameters.AddWithValue("@DevolucionId", devolucionId);
                            cmd.Parameters.AddWithValue("@AutorizadoPor", empleadoId);
                            cmd.ExecuteNonQuery();
                        }

                        // Registrar en bitácora
                        string insertBitacora = @"
                            INSERT INTO BitacoraAutorizaciones (Tipo, ReferenciaId, EmpleadoId, EmpleadoNombre, Motivo, FechaAutorizacion)
                            VALUES ('DevolucionCliente', @ReferenciaId, @EmpleadoId, @EmpleadoNombre, @Motivo, GETDATE())";

                        using (var cmd = new SqlCommand(insertBitacora, conn, transaction))
                        {
                            cmd.Parameters.AddWithValue("@ReferenciaId", devolucionId);
                            cmd.Parameters.AddWithValue("@EmpleadoId", empleadoId);
                            cmd.Parameters.AddWithValue("@EmpleadoNombre", empleadoNombre);
                            cmd.Parameters.AddWithValue("@Motivo", motivo);
                            cmd.ExecuteNonQuery();
                        }

                        transaction.Commit();
                        return true;
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        throw new Exception($"Error al autorizar devolución: {ex.Message}", ex);
                    }
                }
            }
        }

        // ==================== ACTUALIZAR NOTA DE CRÉDITO ====================

        public bool ActualizarNotaCredito(int devolucionId, string numeroNotaCredito)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                string query = @"
                    UPDATE EncabezadoDevolucion 
                    SET NumeroNotaCredito = @NumeroNotaCredito, 
                        Estado = 'Completada',
                        FechaModificacion = GETDATE()
                    WHERE DevolucionId = @DevolucionId";

                using (var cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@DevolucionId", devolucionId);
                    cmd.Parameters.AddWithValue("@NumeroNotaCredito", numeroNotaCredito);
                    conn.Open();
                    int rows = cmd.ExecuteNonQuery();
                    return rows > 0;
                }
            }
        }

        // ==================== MÉTODOS DE CONSULTA ====================

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

        public List<object> ObtenerProductosDisponiblesParaDevolverCliente(int ordenId)
        {
            var productos = new List<object>();

            string query = @"
                SELECT 
                    d.DetalleOrdenId,
                    d.ProductoId,
                    d.ProductoNombre,
                    d.Cantidad AS CantidadVendida,
                    ISNULL(d.CantidadDevuelta, 0) AS CantidadDevuelta,
                    (d.Cantidad - ISNULL(d.CantidadDevuelta, 0)) AS Disponible,
                    d.PrecioUnitario,
                    d.EstabaEnOferta,
                    d.PrecioOferta,
                    p.Codigo AS ProductoCodigo,
                    CASE 
                        WHEN d.EstabaEnOferta = 1 THEN 'Producto en oferta - requiere autorización'
                        ELSE 'Disponible para devolver'
                    END AS MensajeEstado,
                    CASE 
                        WHEN d.EstabaEnOferta = 1 THEN 1 
                        ELSE 0 
                    END AS RequiereAutorizacion
                FROM DetalleOrden d
                INNER JOIN Producto p ON d.ProductoId = p.ProductoId
                WHERE d.OrdenId = @OrdenId
                  AND (d.Cantidad - ISNULL(d.CantidadDevuelta, 0)) > 0
                ORDER BY d.ProductoNombre";

            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@OrdenId", ordenId);
                conn.Open();

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        productos.Add(new
                        {
                            DetalleOrdenId = reader.GetInt32(reader.GetOrdinal("DetalleOrdenId")),
                            ProductoId = reader.GetInt32(reader.GetOrdinal("ProductoId")),
                            ProductoCodigo = reader.GetString(reader.GetOrdinal("ProductoCodigo")),
                            ProductoNombre = reader.GetString(reader.GetOrdinal("ProductoNombre")),
                            CantidadVendida = reader.GetDecimal(reader.GetOrdinal("CantidadVendida")),
                            CantidadDevuelta = reader.GetDecimal(reader.GetOrdinal("CantidadDevuelta")),
                            Disponible = reader.GetDecimal(reader.GetOrdinal("Disponible")),
                            PrecioUnitario = reader.GetDecimal(reader.GetOrdinal("PrecioUnitario")),
                            EstabaEnOferta = reader.GetBoolean(reader.GetOrdinal("EstabaEnOferta")),
                            PrecioOferta = reader["PrecioOferta"] as decimal?,
                            MensajeEstado = reader.GetString(reader.GetOrdinal("MensajeEstado")),
                            RequiereAutorizacion = reader.GetBoolean(reader.GetOrdinal("RequiereAutorizacion"))
                        });
                    }
                }
            }

            return productos;
        }

        public List<EncabezadoDevolucion> ObtenerDevolucionesPorTipo(string tipo)
        {
            var devoluciones = new List<EncabezadoDevolucion>();

            string query = @"
                SELECT 
                    d.DevolucionId,
                    d.NumeroDevolucion,
                    d.NumeroDocCompra,
                    d.FechaDevolucion,
                    d.Motivo,
                    d.TipoDevolucion,
                    d.MontoTotal,
                    d.Estado,
                    d.Tipo,
                    d.FormaCompensacion,
                    d.NumeroNotaCredito,
                    d.ClienteNombre,
                    e.CodigoEmpleado AS EmpleadoNombre,
                    p.RazonSocial AS ProveedorNombre
                FROM EncabezadoDevolucion d
                LEFT JOIN Empleado e ON d.EmpleadoId = e.EmpleadoId
                LEFT JOIN Proveedor p ON d.ProveedorId = p.ProveedorId
                WHERE (@Tipo = '' OR d.Tipo = @Tipo)
                ORDER BY d.FechaDevolucion DESC";

            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@Tipo", tipo ?? "");
                conn.Open();

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        devoluciones.Add(new EncabezadoDevolucion
                        {
                            DevolucionId = (int)reader["DevolucionId"],
                            NumeroDevolucion = reader["NumeroDevolucion"]?.ToString(),
                            NumeroDocCompra = reader["NumeroDocCompra"]?.ToString(),
                            FechaDevolucion = (DateTime)reader["FechaDevolucion"],
                            Motivo = reader["Motivo"].ToString(),
                            TipoDevolucion = reader["TipoDevolucion"].ToString(),
                            MontoTotal = (decimal)reader["MontoTotal"],
                            Estado = reader["Estado"].ToString(),
                            Tipo = reader["Tipo"]?.ToString(),
                            FormaCompensacion = reader["FormaCompensacion"]?.ToString(),
                            NumeroNotaCredito = reader["NumeroNotaCredito"]?.ToString(),
                            ClienteNombre = reader["ClienteNombre"]?.ToString(),
                            EmpleadoNombre = reader["EmpleadoNombre"]?.ToString(),
                            ProveedorNombre = reader["ProveedorNombre"]?.ToString()
                        });
                    }
                }
            }

            return devoluciones;
        }
    }
}