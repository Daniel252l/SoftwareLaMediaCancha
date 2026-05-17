using LaMediaCancha.Models.ViewModels;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;

namespace LaMediaCancha.Services
{
    public class ReporteService
    {
        private readonly string _connectionString;

        public ReporteService()
        {
            _connectionString = ConfigurationManager.ConnectionStrings["LaMediaCanchaDB"].ConnectionString;
        }

        // ==================== DATASETS PARA RDLC ====================

        public DataTable ObtenerDatasetVentas(DateTime fechaInicio, DateTime fechaFin, string formaPago = null)
        {
            var dt = new DataTable();
            dt.Columns.Add("VentaId", typeof(int));
            dt.Columns.Add("NumeroFactura", typeof(string));
            dt.Columns.Add("FechaVenta", typeof(DateTime));
            dt.Columns.Add("ClienteNombre", typeof(string));
            dt.Columns.Add("ClienteDocumento", typeof(string));
            dt.Columns.Add("TipoPago", typeof(string));
            dt.Columns.Add("Subtotal", typeof(decimal));
            dt.Columns.Add("Impuesto", typeof(decimal));
            dt.Columns.Add("Descuento", typeof(decimal));
            dt.Columns.Add("Total", typeof(decimal));
            dt.Columns.Add("Estado", typeof(string));
            dt.Columns.Add("UsuarioNombre", typeof(string));

            string query = @"
                SELECT 
                    v.VentaId,
                    v.NumeroFactura,
                    v.FechaVenta,
                    v.ClienteNombre,
                    v.ClienteDocumento,
                    v.TipoPago,
                    v.Subtotal,
                    v.Impuesto,
                    v.Descuento,
                    v.Total,
                    v.Estado,
                    u.Nombre AS UsuarioNombre
                FROM Venta v
                LEFT JOIN Usuario u ON v.UsuarioId = u.UsuarioId
                WHERE CAST(v.FechaVenta AS DATE) BETWEEN @FechaInicio AND @FechaFin
                  AND (@FormaPago IS NULL OR v.TipoPago = @FormaPago)
                  AND v.Estado IN ('Completada', 'Anulada')
                ORDER BY v.FechaVenta DESC";

            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@FechaInicio", fechaInicio);
                cmd.Parameters.AddWithValue("@FechaFin", fechaFin);
                cmd.Parameters.AddWithValue("@FormaPago", string.IsNullOrEmpty(formaPago) ? (object)DBNull.Value : formaPago);

                conn.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    dt.Load(reader);
                }
            }

            return dt;
        }

        public DataTable ObtenerDatasetDetalleVenta(int ventaId)
        {
            var dt = new DataTable();
            dt.Columns.Add("ProductoNombre", typeof(string));
            dt.Columns.Add("ProductoCodigo", typeof(string));
            dt.Columns.Add("Cantidad", typeof(decimal));
            dt.Columns.Add("PrecioUnitario", typeof(decimal));
            dt.Columns.Add("Subtotal", typeof(decimal));

            string query = @"
                SELECT 
                    dv.ProductoNombre,
                    dv.ProductoCodigo,
                    dv.Cantidad,
                    dv.PrecioUnitario,
                    dv.Subtotal
                FROM DetalleVenta dv
                WHERE dv.VentaId = @VentaId";

            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@VentaId", ventaId);
                conn.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    dt.Load(reader);
                }
            }

            return dt;
        }

        public DataTable ObtenerDatasetProductosMasVendidos(DateTime fechaInicio, DateTime fechaFin, int topN = 10)
        {
            var dt = new DataTable();
            dt.Columns.Add("Codigo", typeof(string));
            dt.Columns.Add("ProductoNombre", typeof(string));
            dt.Columns.Add("CantidadVendida", typeof(decimal));
            dt.Columns.Add("TotalVenta", typeof(decimal));
            dt.Columns.Add("NumeroVentas", typeof(int));
            dt.Columns.Add("PorcentajeParticipacion", typeof(decimal));

            string query = @"
                SELECT TOP (@TopN)
                    p.Codigo,
                    p.Nombre AS ProductoNombre,
                    ISNULL(SUM(dv.Cantidad), 0) AS CantidadVendida,
                    ISNULL(SUM(dv.Subtotal), 0) AS TotalVenta,
                    COUNT(DISTINCT dv.VentaId) AS NumeroVentas,
                    (ISNULL(SUM(dv.Subtotal), 0) * 100.0 / NULLIF((SELECT SUM(Total) FROM Venta WHERE CAST(FechaVenta AS DATE) BETWEEN @FechaInicio AND @FechaFin AND Estado = 'Completada'), 0)) AS PorcentajeParticipacion
                FROM DetalleVenta dv
                INNER JOIN Producto p ON dv.ProductoId = p.ProductoId
                INNER JOIN Venta v ON dv.VentaId = v.VentaId
                WHERE CAST(v.FechaVenta AS DATE) BETWEEN @FechaInicio AND @FechaFin
                  AND v.Estado = 'Completada'
                GROUP BY p.Codigo, p.Nombre
                ORDER BY CantidadVendida DESC";

            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@FechaInicio", fechaInicio);
                cmd.Parameters.AddWithValue("@FechaFin", fechaFin);
                cmd.Parameters.AddWithValue("@TopN", topN);

                conn.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    dt.Load(reader);
                }
            }

            return dt;
        }

        public DataTable ObtenerDatasetInventario()
        {
            var dt = new DataTable();
            dt.Columns.Add("Codigo", typeof(string));
            dt.Columns.Add("ProductoNombre", typeof(string));
            dt.Columns.Add("StockActual", typeof(int));
            dt.Columns.Add("StockMinimo", typeof(int));
            dt.Columns.Add("StockMaximo", typeof(int));
            dt.Columns.Add("PorcentajeStock", typeof(decimal));
            dt.Columns.Add("NivelStock", typeof(string));

            string query = @"
                SELECT 
                    p.Codigo,
                    p.Nombre AS ProductoNombre,
                    ISNULL(i.ExistenciaActual, 0) AS StockActual,
                    ISNULL(i.StockMinimo, 10) AS StockMinimo,
                    ISNULL(i.StockMaximo, 100) AS StockMaximo,
                    CASE 
                        WHEN ISNULL(i.StockMaximo, 100) > 0 
                        THEN (ISNULL(i.ExistenciaActual, 0) * 100.0 / ISNULL(i.StockMaximo, 100))
                        ELSE 0
                    END AS PorcentajeStock,
                    CASE 
                        WHEN ISNULL(i.ExistenciaActual, 0) <= ISNULL(i.StockMinimo, 10) THEN 'CRÍTICO'
                        WHEN ISNULL(i.ExistenciaActual, 0) <= (ISNULL(i.StockMaximo, 100) * 0.2) THEN 'BAJO'
                        WHEN ISNULL(i.ExistenciaActual, 0) >= (ISNULL(i.StockMaximo, 100) * 0.8) THEN 'ALTO'
                        ELSE 'NORMAL'
                    END AS NivelStock
                FROM Producto p
                LEFT JOIN Inventario i ON p.ProductoId = i.ProductoId
                WHERE p.Activo = 1
                ORDER BY 
                    CASE 
                        WHEN ISNULL(i.ExistenciaActual, 0) <= ISNULL(i.StockMinimo, 10) THEN 1
                        ELSE 2
                    END,
                    p.Nombre";

            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand(query, conn))
            {
                conn.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    dt.Load(reader);
                }
            }

            return dt;
        }

        public DataTable ObtenerDatasetCajaDiaria(DateTime fecha)
        {
            var dt = new DataTable();
            dt.Columns.Add("TipoPago", typeof(string));
            dt.Columns.Add("Cantidad", typeof(int));
            dt.Columns.Add("Monto", typeof(decimal));
            dt.Columns.Add("Porcentaje", typeof(decimal));

            string query = @"
                SELECT 
                    TipoPago,
                    COUNT(*) AS Cantidad,
                    ISNULL(SUM(Total), 0) AS Monto,
                    (ISNULL(SUM(Total), 0) * 100.0 / NULLIF((SELECT SUM(Total) FROM Venta WHERE CAST(FechaVenta AS DATE) = @Fecha AND Estado = 'Completada'), 0)) AS Porcentaje
                FROM Venta
                WHERE CAST(FechaVenta AS DATE) = @Fecha
                  AND Estado = 'Completada'
                GROUP BY TipoPago";

            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@Fecha", fecha);
                conn.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    dt.Load(reader);
                }
            }

            return dt;
        }

        public DataTable ObtenerDatasetUtilidad(DateTime fechaInicio, DateTime fechaFin)
        {
            var dt = new DataTable();
            dt.Columns.Add("Codigo", typeof(string));
            dt.Columns.Add("ProductoNombre", typeof(string));
            dt.Columns.Add("CantidadVendida", typeof(decimal));
            dt.Columns.Add("CostoUnitario", typeof(decimal));
            dt.Columns.Add("PrecioVenta", typeof(decimal));
            dt.Columns.Add("CostoTotal", typeof(decimal));
            dt.Columns.Add("VentaTotal", typeof(decimal));
            dt.Columns.Add("Utilidad", typeof(decimal));
            dt.Columns.Add("PorcentajeUtilidad", typeof(decimal));

            string query = @"
                SELECT 
                    p.Codigo,
                    p.Nombre AS ProductoNombre,
                    SUM(dv.Cantidad) AS CantidadVendida,
                    ISNULL(p.PrecioCompra, 0) AS CostoUnitario,
                    p.PrecioVenta,
                    SUM(dv.Cantidad * ISNULL(p.PrecioCompra, 0)) AS CostoTotal,
                    SUM(dv.Subtotal) AS VentaTotal,
                    SUM(dv.Subtotal) - SUM(dv.Cantidad * ISNULL(p.PrecioCompra, 0)) AS Utilidad,
                    CASE 
                        WHEN SUM(dv.Cantidad * ISNULL(p.PrecioCompra, 0)) > 0 
                        THEN ((SUM(dv.Subtotal) - SUM(dv.Cantidad * ISNULL(p.PrecioCompra, 0))) * 100.0 / SUM(dv.Cantidad * ISNULL(p.PrecioCompra, 0)))
                        ELSE 0
                    END AS PorcentajeUtilidad
                FROM DetalleVenta dv
                INNER JOIN Producto p ON dv.ProductoId = p.ProductoId
                INNER JOIN Venta v ON dv.VentaId = v.VentaId
                WHERE CAST(v.FechaVenta AS DATE) BETWEEN @FechaInicio AND @FechaFin
                  AND v.Estado = 'Completada'
                GROUP BY p.Codigo, p.Nombre, p.PrecioCompra, p.PrecioVenta
                ORDER BY Utilidad DESC";

            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@FechaInicio", fechaInicio);
                cmd.Parameters.AddWithValue("@FechaFin", fechaFin);

                conn.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    dt.Load(reader);
                }
            }

            return dt;
        }

        public DataTable ObtenerDatasetResumenVentas(DateTime fechaInicio, DateTime fechaFin)
        {
            var dt = new DataTable();
            dt.Columns.Add("TotalFacturas", typeof(int));
            dt.Columns.Add("TotalSubtotal", typeof(decimal));
            dt.Columns.Add("TotalImpuesto", typeof(decimal));
            dt.Columns.Add("TotalDescuento", typeof(decimal));
            dt.Columns.Add("TotalVentas", typeof(decimal));
            dt.Columns.Add("TicketPromedio", typeof(decimal));

            string query = @"
                SELECT 
                    COUNT(*) AS TotalFacturas,
                    ISNULL(SUM(Subtotal), 0) AS TotalSubtotal,
                    ISNULL(SUM(Impuesto), 0) AS TotalImpuesto,
                    ISNULL(SUM(Descuento), 0) AS TotalDescuento,
                    ISNULL(SUM(Total), 0) AS TotalVentas,
                    ISNULL(AVG(Total), 0) AS TicketPromedio
                FROM Venta
                WHERE CAST(FechaVenta AS DATE) BETWEEN @FechaInicio AND @FechaFin
                  AND Estado = 'Completada'";

            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@FechaInicio", fechaInicio);
                cmd.Parameters.AddWithValue("@FechaFin", fechaFin);

                conn.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    dt.Load(reader);
                }
            }

            return dt;
        }
    }
}