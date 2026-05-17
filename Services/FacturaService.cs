using LaMediaCancha.Models;
using LaMediaCancha.Models.ViewModels;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;

namespace LaMediaCancha.Services
{
    public class FacturaService
    {
        private readonly string _connectionString;

        public FacturaService()
        {
            _connectionString = ConfigurationManager.ConnectionStrings["LaMediaCanchaDB"].ConnectionString;
        }

        private class ProductoVentaInfo
        {
            public int ProductoId { get; set; }
            public decimal Cantidad { get; set; }
            public decimal PrecioUnitario { get; set; }
        }

        public class BuscarVentaResult
        {
            public int VentaId { get; set; }
            public string NumeroFactura { get; set; }
            public DateTime FechaVenta { get; set; }
            public string ClienteNombre { get; set; }
            public string ClienteDocumento { get; set; }
            public decimal Total { get; set; }
            public string Estado { get; set; }
        }

        // ==================== OBTENER FACTURAS ====================

        public List<FacturaViewModel> ObtenerFacturas(BuscarFacturaViewModel filtro)
        {
            var facturas = new List<FacturaViewModel>();

            string query = @"
        SELECT 
            f.FacturaId,
            f.CompraId,
            f.NumeroFactura,
            f.NumeroDocumento,
            f.FechaEmision,
            f.ClienteNombre,
            f.ClienteDocumento,
            f.ClienteTelefono,
            f.TipoPago,
            f.Subtotal,
            f.Impuesto,
            f.Descuento,
            f.Total,
            f.Estado,
            f.Observaciones,
            f.MotivoAnulacion,
            f.UsuarioAnulacion,
            f.FechaAnulacion,
            f.NotaCreditoId,
            f.NumeroNotaCredito       AS NumeroNCProveedor,
            nc.NumeroNotaCredito      AS NumeroNCVenta
        FROM Factura f
        LEFT JOIN NotaCredito nc ON f.NotaCreditoId = nc.NotaCreditoId
        WHERE 1=1";

            if (!string.IsNullOrEmpty(filtro.NumeroFactura))
                query += " AND f.NumeroFactura LIKE @NumeroFactura";
            if (!string.IsNullOrEmpty(filtro.NumeroDocumento))
                query += " AND f.NumeroDocumento LIKE @NumeroDocumento";
            if (filtro.FechaInicio.HasValue)
                query += " AND CAST(f.FechaEmision AS DATE) >= @FechaInicio";
            if (filtro.FechaFin.HasValue)
                query += " AND CAST(f.FechaEmision AS DATE) <= @FechaFin";
            if (!string.IsNullOrEmpty(filtro.Estado))
                query += " AND f.Estado = @Estado";

            query += " ORDER BY f.FechaEmision DESC";

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
                        // Toma NC del proveedor si existe, sino la de venta
                        string numeroNC = reader["NumeroNCProveedor"] != DBNull.Value
                            ? reader["NumeroNCProveedor"].ToString()
                            : reader["NumeroNCVenta"]?.ToString();

                        facturas.Add(new FacturaViewModel
                        {
                            FacturaId = Convert.ToInt32(reader["FacturaId"]),
                            CompraId = reader["CompraId"] != DBNull.Value ? Convert.ToInt32(reader["CompraId"]) : 0,
                            NumeroFactura = reader["NumeroFactura"].ToString(),
                            NumeroDocumento = reader["NumeroDocumento"]?.ToString(),
                            FechaEmision = Convert.ToDateTime(reader["FechaEmision"]),
                            ClienteNombre = reader["ClienteNombre"].ToString(),
                            ClienteDocumento = reader["ClienteDocumento"]?.ToString(),
                            ClienteTelefono = reader["ClienteTelefono"]?.ToString(),
                            TipoPago = reader["TipoPago"].ToString(),
                            Subtotal = Convert.ToDecimal(reader["Subtotal"]),
                            Impuesto = Convert.ToDecimal(reader["Impuesto"]),
                            Descuento = Convert.ToDecimal(reader["Descuento"]),
                            Total = Convert.ToDecimal(reader["Total"]),
                            Estado = reader["Estado"].ToString(),
                            Observaciones = reader["Observaciones"]?.ToString(),
                            MotivoAnulacion = reader["MotivoAnulacion"]?.ToString(),
                            UsuarioAnulacion = reader["UsuarioAnulacion"]?.ToString(),
                            FechaAnulacion = reader["FechaAnulacion"] as DateTime?,
                            NotaCreditoId = reader["NotaCreditoId"] as int?,
                            NumeroNotaCredito = numeroNC
                        });
                    }
                }
            }

            return facturas;
        }

        public FacturaViewModel ObtenerFacturaPorId(int facturaId)
        {
            FacturaViewModel factura = null;

            string query = @"
                SELECT 
                    f.FacturaId,
                    f.CompraId,
                    f.NumeroFactura,
                    f.NumeroDocumento,
                    f.FechaEmision,
                    f.ClienteNombre,
                    f.ClienteDocumento,
                    f.ClienteTelefono,
                    f.TipoPago,
                    f.Subtotal,
                    f.Impuesto,
                    f.Descuento,
                    f.Total,
                    f.Estado,
                    f.Observaciones,
                    f.MotivoAnulacion,
                    f.UsuarioAnulacion,
                    f.FechaAnulacion,
                    f.NotaCreditoId,
                    nc.NumeroNotaCredito
                FROM Factura f
                LEFT JOIN NotaCredito nc ON f.NotaCreditoId = nc.NotaCreditoId
                WHERE f.FacturaId = @FacturaId";

            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@FacturaId", facturaId);
                conn.Open();

                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        int compraId = reader["CompraId"] != DBNull.Value
                            ? Convert.ToInt32(reader["CompraId"]) : 0;

                        factura = new FacturaViewModel
                        {
                            FacturaId = Convert.ToInt32(reader["FacturaId"]),
                            CompraId = compraId,
                            NumeroFactura = reader["NumeroFactura"].ToString(),
                            NumeroDocumento = reader["NumeroDocumento"]?.ToString(),
                            FechaEmision = Convert.ToDateTime(reader["FechaEmision"]),
                            ClienteNombre = reader["ClienteNombre"].ToString(),
                            ClienteDocumento = reader["ClienteDocumento"]?.ToString(),
                            ClienteTelefono = reader["ClienteTelefono"]?.ToString(),
                            TipoPago = reader["TipoPago"].ToString(),
                            Subtotal = Convert.ToDecimal(reader["Subtotal"]),
                            Impuesto = Convert.ToDecimal(reader["Impuesto"]),
                            Descuento = Convert.ToDecimal(reader["Descuento"]),
                            Total = Convert.ToDecimal(reader["Total"]),
                            Estado = reader["Estado"].ToString(),
                            Observaciones = reader["Observaciones"]?.ToString(),
                            MotivoAnulacion = reader["MotivoAnulacion"]?.ToString(),
                            UsuarioAnulacion = reader["UsuarioAnulacion"]?.ToString(),
                            FechaAnulacion = reader["FechaAnulacion"] as DateTime?,
                            NotaCreditoId = reader["NotaCreditoId"] as int?,
                            NumeroNotaCredito = reader["NumeroNotaCredito"]?.ToString(),
                            Detalles = compraId > 0
                                ? ObtenerDetallesFacturaPorCompra(compraId)
                                : new List<DetalleFacturaViewModel>()
                        };
                    }
                }
            }

            return factura;
        }

        // Detalles desde DetalleFactura (ventas) — se mantiene por compatibilidad
        public List<DetalleFacturaViewModel> ObtenerDetallesFactura(int facturaId)
        {
            var detalles = new List<DetalleFacturaViewModel>();

            string query = @"
                SELECT 
                    df.ProductoId,
                    p.Codigo AS ProductoCodigo,
                    p.Nombre AS ProductoNombre,
                    df.Cantidad,
                    df.PrecioUnitario,
                    df.Descuento,
                    df.Subtotal
                FROM DetalleFactura df
                INNER JOIN Producto p ON df.ProductoId = p.ProductoId
                WHERE df.FacturaId = @FacturaId";

            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@FacturaId", facturaId);
                conn.Open();

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        detalles.Add(new DetalleFacturaViewModel
                        {
                            ProductoId = Convert.ToInt32(reader["ProductoId"]),
                            ProductoCodigo = reader["ProductoCodigo"].ToString(),
                            ProductoNombre = reader["ProductoNombre"].ToString(),
                            Cantidad = Convert.ToDecimal(reader["Cantidad"]),
                            PrecioUnitario = Convert.ToDecimal(reader["PrecioUnitario"]),
                            Descuento = Convert.ToDecimal(reader["Descuento"]),
                            Subtotal = Convert.ToDecimal(reader["Subtotal"])
                        });
                    }
                }
            }

            return detalles;
        }

        // Detalles desde DetalleCompra (compras)
        public List<DetalleFacturaViewModel> ObtenerDetallesFacturaPorCompra(int compraId)
        {
            var detalles = new List<DetalleFacturaViewModel>();

            string query = @"
                SELECT 
                    dc.ProductoId,
                    p.Codigo AS ProductoCodigo,
                    p.Nombre AS ProductoNombre,
                    dc.Cantidad,
                    dc.PrecioUnitario,
                    dc.Descuento,
                    dc.Subtotal
                FROM DetalleCompra dc
                INNER JOIN Producto p ON dc.ProductoId = p.ProductoId
                WHERE dc.CompraId = @CompraId";

            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@CompraId", compraId);
                conn.Open();

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        detalles.Add(new DetalleFacturaViewModel
                        {
                            ProductoId = Convert.ToInt32(reader["ProductoId"]),
                            ProductoCodigo = reader["ProductoCodigo"].ToString(),
                            ProductoNombre = reader["ProductoNombre"].ToString(),
                            Cantidad = Convert.ToDecimal(reader["Cantidad"]),
                            PrecioUnitario = Convert.ToDecimal(reader["PrecioUnitario"]),
                            Descuento = Convert.ToDecimal(reader["Descuento"]),
                            Subtotal = Convert.ToDecimal(reader["Subtotal"])
                        });
                    }
                }
            }

            return detalles;
        }

        // ==================== BUSCAR VENTAS ====================

        public List<BuscarVentaResult> BuscarVentas(BuscarVentaViewModel filtro)
        {
            var ventas = new List<BuscarVentaResult>();

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
                        ventas.Add(new BuscarVentaResult
                        {
                            VentaId = Convert.ToInt32(reader["VentaId"]),
                            NumeroFactura = reader["NumeroFactura"].ToString(),
                            FechaVenta = Convert.ToDateTime(reader["FechaVenta"]),
                            ClienteNombre = reader["ClienteNombre"].ToString(),
                            ClienteDocumento = reader["ClienteDocumento"]?.ToString(),
                            Total = Convert.ToDecimal(reader["Total"]),
                            Estado = reader["Estado"].ToString()
                        });
                    }
                }
            }

            return ventas;
        }

        // ==================== NOTA DE CRÉDITO ====================

        public NotaCreditoViewModel ObtenerNotaCreditoPorId(int notaCreditoId)
        {
            NotaCreditoViewModel notaCredito = null;

            string query = @"
                SELECT 
                    nc.NotaCreditoId,
                    nc.NumeroNotaCredito,
                    nc.FechaEmision,
                    nc.MontoTotal,
                    nc.Motivo,
                    nc.Estado,
                    nc.UsuarioNombre,
                    f.NumeroFactura AS FacturaOriginalNumero,
                    f.ClienteNombre,
                    f.ClienteDocumento
                FROM NotaCredito nc
                INNER JOIN Factura f ON nc.FacturaOriginalId = f.FacturaId
                WHERE nc.NotaCreditoId = @NotaCreditoId";

            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@NotaCreditoId", notaCreditoId);
                conn.Open();

                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        notaCredito = new NotaCreditoViewModel
                        {
                            NotaCreditoId = Convert.ToInt32(reader["NotaCreditoId"]),
                            NumeroNotaCredito = reader["NumeroNotaCredito"].ToString(),
                            FechaEmision = Convert.ToDateTime(reader["FechaEmision"]),
                            MontoTotal = Convert.ToDecimal(reader["MontoTotal"]),
                            Motivo = reader["Motivo"].ToString(),
                            Estado = reader["Estado"].ToString(),
                            UsuarioNombre = reader["UsuarioNombre"].ToString(),
                            FacturaOriginalNumero = reader["FacturaOriginalNumero"].ToString(),
                            ClienteNombre = reader["ClienteNombre"].ToString(),
                            ClienteDocumento = reader["ClienteDocumento"]?.ToString()
                        };
                    }
                }
            }

            return notaCredito;
        }

        // ==================== NOTA DE CRÉDITO DEL PROVEEDOR ====================

        public int RegistrarNotaCreditoProveedor(NotaCreditoProveedorViewModel model,
                                  int usuarioId, string usuarioNombre,
                                  string documentoRuta, string documentoNombre)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                using (var transaction = conn.BeginTransaction())
                {
                    try
                    {
                        // 1. Insertar la Nota de Crédito del Proveedor
                        string insertNC = @"
                    INSERT INTO NotaCreditoProveedor 
                        (CompraId, NumeroNCProveedor, FechaEmision, MontoTotal, 
                         Motivo, Estado, DocumentoRuta, DocumentoNombre, 
                         UsuarioId, UsuarioNombre, FechaCreacion)
                    VALUES 
                        (@CompraId, @NumeroNCProveedor, @FechaEmision, @MontoTotal,
                         @Motivo, 'Activa', @DocumentoRuta, @DocumentoNombre,
                         @UsuarioId, @UsuarioNombre, GETDATE());
                    SELECT SCOPE_IDENTITY();";

                        int ncProveedorId = 0;
                        using (var cmd = new SqlCommand(insertNC, conn, transaction))
                        {
                            cmd.Parameters.AddWithValue("@CompraId", model.CompraId);
                            cmd.Parameters.AddWithValue("@NumeroNCProveedor", model.NumeroNCProveedor);
                            cmd.Parameters.AddWithValue("@FechaEmision", model.FechaEmision);
                            cmd.Parameters.AddWithValue("@MontoTotal", model.MontoTotal);
                            cmd.Parameters.AddWithValue("@Motivo", model.Motivo);
                            cmd.Parameters.AddWithValue("@DocumentoRuta", documentoRuta ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@DocumentoNombre", documentoNombre ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@UsuarioId", usuarioId);
                            cmd.Parameters.AddWithValue("@UsuarioNombre", usuarioNombre);
                            ncProveedorId = Convert.ToInt32(cmd.ExecuteScalar());
                        }

                        // 2. Actualizar estado de la Factura a Anulada
                        string updateFactura = @"
                    UPDATE Factura 
                    SET Estado            = 'Anulada',
                        MotivoAnulacion   = @Motivo,
                        UsuarioAnulacion  = @UsuarioNombre,
                        FechaAnulacion    = GETDATE(),
                        NumeroNotaCredito = @NumeroNCProveedor
                    WHERE CompraId = @CompraId";

                        using (var cmd = new SqlCommand(updateFactura, conn, transaction))
                        {
                            cmd.Parameters.AddWithValue("@Motivo", model.Motivo);
                            cmd.Parameters.AddWithValue("@UsuarioNombre", usuarioNombre);
                            cmd.Parameters.AddWithValue("@NumeroNCProveedor", model.NumeroNCProveedor);
                            cmd.Parameters.AddWithValue("@CompraId", model.CompraId);
                            cmd.ExecuteNonQuery();
                        }

                        // 3. Actualizar estado de EncabezadoCompra a Anulada
                        string updateCompra = @"
                    UPDATE EncabezadoCompra 
                    SET Estado = 'Anulada'
                    WHERE CompraId = @CompraId";

                        using (var cmd = new SqlCommand(updateCompra, conn, transaction))
                        {
                            cmd.Parameters.AddWithValue("@CompraId", model.CompraId);
                            cmd.ExecuteNonQuery();
                        }

                        // 4. Restar stock del Inventario
                        string updateStock = @"
                    UPDATE i
                    SET i.ExistenciaActual = i.ExistenciaActual - dc.Cantidad
                    FROM Inventario i
                    INNER JOIN DetalleCompra dc ON i.ProductoId = dc.ProductoId
                    WHERE dc.CompraId = @CompraId";

                        using (var cmd = new SqlCommand(updateStock, conn, transaction))
                        {
                            cmd.Parameters.AddWithValue("@CompraId", model.CompraId);
                            cmd.ExecuteNonQuery();
                        }

                        transaction.Commit();
                        return ncProveedorId;
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        throw new Exception("Error en RegistrarNCProveedor: " + ex.Message);
                    }
                }
            }
        }

        public NotaCreditoProveedorViewModel ObtenerNCProveedorPorCompra(int compraId)
        {
            NotaCreditoProveedorViewModel nc = null;

            string query = @"
                SELECT ncp.*, ec.NumeroFactura
                FROM NotaCreditoProveedor ncp
                INNER JOIN EncabezadoCompra ec ON ncp.CompraId = ec.CompraId
                WHERE ncp.CompraId = @CompraId";

            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@CompraId", compraId);
                conn.Open();

                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        nc = new NotaCreditoProveedorViewModel
                        {
                            CompraId = Convert.ToInt32(reader["CompraId"]),
                            NumeroNCProveedor = reader["NumeroNCProveedor"].ToString(),
                            FechaEmision = Convert.ToDateTime(reader["FechaEmision"]),
                            MontoTotal = Convert.ToDecimal(reader["MontoTotal"]),
                            Motivo = reader["Motivo"].ToString(),
                            NumeroFacturaCompra = reader["NumeroFactura"].ToString(),
                            DocumentoRuta = reader["DocumentoRuta"]?.ToString(),
                            DocumentoNombre = reader["DocumentoNombre"]?.ToString()
                        };
                    }
                }
            }

            return nc;
        }

        // ==================== ANULAR VENTA ====================
        public bool AnularVenta(int ventaId, string motivo, int usuarioId, string usuarioNombre)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                using (var transaction = conn.BeginTransaction())
                {
                    try
                    {
                        // 1. Obtener datos de la venta
                        decimal totalVenta = 0;
                        string queryVenta = "SELECT Total FROM Venta WHERE VentaId = @VentaId";
                        using (var cmd = new SqlCommand(queryVenta, conn, transaction))
                        {
                            cmd.Parameters.AddWithValue("@VentaId", ventaId);
                            totalVenta = (decimal)cmd.ExecuteScalar();
                        }

                        // 2. Generar número de nota de crédito
                        string numeroNotaCredito = $"NCV-{DateTime.Now:yyyyMMddHHmmss}";

                        // 3. Insertar Nota de Crédito
                        string insertNC = @"
                    INSERT INTO NotaCreditoVenta (VentaId, NumeroNotaCredito, FechaEmision, Motivo, MontoTotal, Estado, UsuarioId, UsuarioNombre)
                    VALUES (@VentaId, @NumeroNotaCredito, GETDATE(), @Motivo, @MontoTotal, 'Activa', @UsuarioId, @UsuarioNombre);
                    SELECT SCOPE_IDENTITY();";

                        int notaCreditoId = 0;
                        using (var cmd = new SqlCommand(insertNC, conn, transaction))
                        {
                            cmd.Parameters.AddWithValue("@VentaId", ventaId);
                            cmd.Parameters.AddWithValue("@NumeroNotaCredito", numeroNotaCredito);
                            cmd.Parameters.AddWithValue("@Motivo", motivo);
                            cmd.Parameters.AddWithValue("@MontoTotal", totalVenta);
                            cmd.Parameters.AddWithValue("@UsuarioId", usuarioId);
                            cmd.Parameters.AddWithValue("@UsuarioNombre", usuarioNombre);
                            notaCreditoId = Convert.ToInt32(cmd.ExecuteScalar());
                        }

                        // 4. Actualizar estado de la venta
                        string updateVenta = @"
                    UPDATE Venta 
                    SET Estado = 'Anulada', 
                        FechaAnulacion = GETDATE(),
                        UsuarioAnulacion = @UsuarioNombre,
                        MotivoAnulacion = @Motivo,
                        NotaCreditoId = @NotaCreditoId
                    WHERE VentaId = @VentaId";
                        using (var cmd = new SqlCommand(updateVenta, conn, transaction))
                        {
                            cmd.Parameters.AddWithValue("@VentaId", ventaId);
                            cmd.Parameters.AddWithValue("@UsuarioNombre", usuarioNombre);
                            cmd.Parameters.AddWithValue("@Motivo", motivo);
                            cmd.Parameters.AddWithValue("@NotaCreditoId", notaCreditoId);
                            cmd.ExecuteNonQuery();
                        }

                        transaction.Commit();
                        return true;
                    }
                    catch
                    {
                        transaction.Rollback();
                        throw;
                    }
                }
            }
        }
    }
}