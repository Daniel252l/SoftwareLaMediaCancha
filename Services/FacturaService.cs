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

        public List<FacturaViewModel> ObtenerFacturas(BuscarFacturaViewModel filtro)
        {
            var facturas = new List<FacturaViewModel>();

            string query = @"
                SELECT 
                    f.FacturaId,
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
                        facturas.Add(new FacturaViewModel
                        {
                            FacturaId = (int)reader["FacturaId"],
                            NumeroFactura = reader["NumeroFactura"].ToString(),
                            NumeroDocumento = reader["NumeroDocumento"]?.ToString(),
                            FechaEmision = (DateTime)reader["FechaEmision"],
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
                            MotivoAnulacion = reader["MotivoAnulacion"]?.ToString(),
                            UsuarioAnulacion = reader["UsuarioAnulacion"]?.ToString(),
                            FechaAnulacion = reader["FechaAnulacion"] as DateTime?,
                            NotaCreditoId = reader["NotaCreditoId"] as int?,
                            NumeroNotaCredito = reader["NumeroNotaCredito"]?.ToString()
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
                        factura = new FacturaViewModel
                        {
                            FacturaId = (int)reader["FacturaId"],
                            NumeroFactura = reader["NumeroFactura"].ToString(),
                            NumeroDocumento = reader["NumeroDocumento"]?.ToString(),
                            FechaEmision = (DateTime)reader["FechaEmision"],
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
                            MotivoAnulacion = reader["MotivoAnulacion"]?.ToString(),
                            UsuarioAnulacion = reader["UsuarioAnulacion"]?.ToString(),
                            FechaAnulacion = reader["FechaAnulacion"] as DateTime?,
                            NotaCreditoId = reader["NotaCreditoId"] as int?,
                            NumeroNotaCredito = reader["NumeroNotaCredito"]?.ToString(),
                            Detalles = ObtenerDetallesFactura(facturaId)
                        };
                    }
                }
            }

            return factura;
        }

        public List<DetalleFacturaViewModel> ObtenerDetallesFactura(int facturaId)
        {
            var detalles = new List<DetalleFacturaViewModel>();

            string query = @"
                SELECT 
                    ProductoId,
                    ProductoCodigo,
                    ProductoNombre,
                    Cantidad,
                    PrecioUnitario,
                    Descuento,
                    Subtotal
                FROM DetalleFactura
                WHERE FacturaId = @FacturaId";

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
                            ProductoId = (int)reader["ProductoId"],
                            ProductoCodigo = reader["ProductoCodigo"].ToString(),
                            ProductoNombre = reader["ProductoNombre"].ToString(),
                            Cantidad = (decimal)reader["Cantidad"],
                            PrecioUnitario = (decimal)reader["PrecioUnitario"],
                            Descuento = (decimal)reader["Descuento"],
                            Subtotal = (decimal)reader["Subtotal"]
                        });
                    }
                }
            }

            return detalles;
        }

        public bool AnularFactura(int facturaId, string motivoAnulacion, int usuarioId, string usuarioNombre)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                using (var transaction = conn.BeginTransaction())
                {
                    try
                    {
                        // 1. Verificar que la factura existe y está vigente
                        string checkQuery = "SELECT Estado FROM Factura WHERE FacturaId = @FacturaId";
                        string estado = "";
                        using (var cmd = new SqlCommand(checkQuery, conn, transaction))
                        {
                            cmd.Parameters.AddWithValue("@FacturaId", facturaId);
                            var result = cmd.ExecuteScalar();
                            if (result == null)
                            {
                                throw new Exception("Factura no encontrada");
                            }
                            estado = result.ToString();
                            if (estado != "Vigente")
                            {
                                throw new Exception("La factura ya está anulada");
                            }
                        }

                        // 2. Obtener datos de la factura
                        string getFacturaQuery = "SELECT NumeroFactura, Total FROM Factura WHERE FacturaId = @FacturaId";
                        string numeroFactura = "";
                        decimal total = 0;

                        using (var cmd = new SqlCommand(getFacturaQuery, conn, transaction))
                        {
                            cmd.Parameters.AddWithValue("@FacturaId", facturaId);
                            using (var reader = cmd.ExecuteReader())
                            {
                                if (reader.Read())
                                {
                                    numeroFactura = reader["NumeroFactura"].ToString();
                                    total = (decimal)reader["Total"];
                                }
                                reader.Close();
                            }
                        }

                        // 3. Crear Nota de Crédito
                        string numeroNotaCredito = $"NC-{numeroFactura}-{DateTime.Now:yyyyMMddHHmmss}";
                        int notaCreditoId = 0;

                        string insertNotaQuery = @"
                    INSERT INTO NotaCredito (FacturaOriginalId, NumeroNotaCredito, FechaEmision, MontoTotal, Motivo, Estado, UsuarioId, UsuarioNombre)
                    VALUES (@FacturaOriginalId, @NumeroNotaCredito, GETDATE(), @MontoTotal, @Motivo, 'Activa', @UsuarioId, @UsuarioNombre);
                    SELECT SCOPE_IDENTITY();";

                        using (var cmd = new SqlCommand(insertNotaQuery, conn, transaction))
                        {
                            cmd.Parameters.AddWithValue("@FacturaOriginalId", facturaId);
                            cmd.Parameters.AddWithValue("@NumeroNotaCredito", numeroNotaCredito);
                            cmd.Parameters.AddWithValue("@MontoTotal", total);
                            cmd.Parameters.AddWithValue("@Motivo", motivoAnulacion);
                            cmd.Parameters.AddWithValue("@UsuarioId", usuarioId);
                            cmd.Parameters.AddWithValue("@UsuarioNombre", usuarioNombre);
                            notaCreditoId = Convert.ToInt32(cmd.ExecuteScalar());
                        }

                        // 4. Actualizar factura como anulada
                        string updateFacturaQuery = @"
                    UPDATE Factura 
                    SET Estado = 'Anulada',
                        FechaAnulacion = GETDATE(),
                        MotivoAnulacion = @MotivoAnulacion,
                        NotaCreditoId = @NotaCreditoId,
                        UsuarioAnulacion = @UsuarioAnulacion
                    WHERE FacturaId = @FacturaId";

                        using (var cmd = new SqlCommand(updateFacturaQuery, conn, transaction))
                        {
                            cmd.Parameters.AddWithValue("@FacturaId", facturaId);
                            cmd.Parameters.AddWithValue("@MotivoAnulacion", motivoAnulacion);
                            cmd.Parameters.AddWithValue("@NotaCreditoId", notaCreditoId);
                            cmd.Parameters.AddWithValue("@UsuarioAnulacion", usuarioNombre);
                            cmd.ExecuteNonQuery();
                        }

                        transaction.Commit();
                        return true;
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        throw new Exception("Error al anular la factura: " + ex.Message);
                    }
                }
            }
        }
        public NotaCreditoViewModel ObtenerNotaCreditoPorId(int notaCreditoId)
        {
            NotaCreditoViewModel notaCredito = null;

            string query = @"
        SELECT 
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
                            NumeroNotaCredito = reader["NumeroNotaCredito"].ToString(),
                            FechaEmision = (DateTime)reader["FechaEmision"],
                            MontoTotal = (decimal)reader["MontoTotal"],
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
    }
}