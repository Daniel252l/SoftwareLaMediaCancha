using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using LaMediaCancha.Models;
using System.Data.SqlClient;

namespace LaMediaCancha.Services
{
    public class LoteService
    {
        private readonly string _connectionString;

        public LoteService()
        {
            _connectionString = ConfigurationManager.ConnectionStrings["LaMediaCanchaDB"].ConnectionString;
        }

        public void CrearLotesDesdeCompra(int compraId)
        {
            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand("sp_CrearLotesDesdeCompra", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@CompraId", compraId);
                conn.Open();
                cmd.ExecuteNonQuery();
            }
        }

        public List<LoteViewModel> ObtenerLotesPorProducto(int productoId)
        {
            var lotes = new List<LoteViewModel>();

            string query = @"
        SELECT 
            l.LoteId,
            l.NumeroLoteInterno,
            l.NumeroLoteProveedor,
            l.FechaIngreso,
            l.FechaVencimiento,
            l.CantidadInicial,
            l.CantidadActual,
            l.PrecioUnitario,
            l.Estado,
            l.CompraId,
            ec.NumeroDocumento AS NumeroCompra,
            p.RazonSocial AS ProveedorNombre,
            CASE 
                WHEN l.FechaVencimiento IS NULL THEN NULL
                WHEN l.FechaVencimiento < GETDATE() THEN -1
                ELSE DATEDIFF(DAY, GETDATE(), l.FechaVencimiento)
            END AS DiasParaVencer
        FROM Lote l
        LEFT JOIN EncabezadoCompra ec ON l.CompraId = ec.CompraId
        LEFT JOIN Proveedor p ON l.ProveedorId = p.ProveedorId
        WHERE l.ProductoId = @ProductoId
          AND l.Activo = 1
        ORDER BY l.FechaIngreso ASC";

            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@ProductoId", productoId);
                conn.Open();

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var lote = new LoteViewModel
                        {
                            LoteId = Convert.ToInt32(reader["LoteId"]),
                            NumeroLoteInterno = reader["NumeroLoteInterno"]?.ToString() ?? "",
                            NumeroLoteProveedor = reader["NumeroLoteProveedor"] != DBNull.Value ? reader["NumeroLoteProveedor"].ToString() : null,
                            FechaIngreso = Convert.ToDateTime(reader["FechaIngreso"]),
                            FechaVencimiento = reader["FechaVencimiento"] as DateTime?,
                            CantidadInicial = reader["CantidadInicial"] != DBNull.Value ? Convert.ToDecimal(reader["CantidadInicial"]) : 0,
                            CantidadActual = reader["CantidadActual"] != DBNull.Value ? Convert.ToDecimal(reader["CantidadActual"]) : 0,
                            PrecioUnitario = reader["PrecioUnitario"] != DBNull.Value ? Convert.ToDecimal(reader["PrecioUnitario"]) : 0,
                            Estado = reader["Estado"]?.ToString() ?? "Activo",
                            NumeroCompra = reader["NumeroCompra"]?.ToString() ?? "N/A",
                            ProveedorNombre = reader["ProveedorNombre"]?.ToString() ?? "N/A"
                        };

                        if (reader["DiasParaVencer"] != DBNull.Value)
                        {
                            lote.DiasParaVencer = Convert.ToInt32(reader["DiasParaVencer"]);
                        }

                        lotes.Add(lote);
                    }
                }
            }

            return lotes;
        }

        public List<LoteViewModel> ObtenerLotesProximosAVencer(int diasAlerta = 7)
        {
            var lotes = new List<LoteViewModel>();

            string query = @"
                SELECT 
                    l.LoteId,
                    l.NumeroLoteInterno,
                    l.NumeroLoteProveedor,
                    l.FechaVencimiento,
                    l.CantidadActual,
                    l.Estado,
                    l.PrecioUnitario,
                    pr.Nombre AS ProductoNombre,
                    pr.Codigo AS ProductoCodigo,
                    p.RazonSocial AS ProveedorNombre,
                    DATEDIFF(DAY, GETDATE(), l.FechaVencimiento) AS DiasParaVencer
                FROM Lote l
                INNER JOIN Producto pr ON l.ProductoId = pr.ProductoId
                LEFT JOIN Proveedor p ON l.ProveedorId = p.ProveedorId
                WHERE l.Estado = 'Activo'
                  AND l.FechaVencimiento IS NOT NULL
                  AND l.FechaVencimiento <= DATEADD(DAY, @DiasAlerta, GETDATE())
                  AND l.FechaVencimiento >= GETDATE()
                  AND l.CantidadActual > 0
                  AND l.Activo = 1
                ORDER BY l.FechaVencimiento ASC";

            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@DiasAlerta", diasAlerta);
                conn.Open();

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var lote = new LoteViewModel
                        {
                            LoteId = (int)reader["LoteId"],
                            NumeroLoteInterno = reader["NumeroLoteInterno"].ToString(),
                            NumeroLoteProveedor = reader["NumeroLoteProveedor"] != DBNull.Value ? reader["NumeroLoteProveedor"].ToString() : null,
                            FechaVencimiento = reader["FechaVencimiento"] as DateTime?,
                            CantidadActual = (decimal)reader["CantidadActual"],
                            Estado = reader["Estado"].ToString(),
                            PrecioUnitario = (decimal)reader["PrecioUnitario"],
                            ProductoNombre = reader["ProductoNombre"].ToString(),
                            ProductoCodigo = reader["ProductoCodigo"].ToString(),
                            ProveedorNombre = reader["ProveedorNombre"]?.ToString() ?? "N/A",
                            DiasParaVencer = (int)reader["DiasParaVencer"]
                        };
                        lotes.Add(lote);
                    }
                }
            }

            return lotes;
        }

        public bool ActualizarFechaVencimiento(int loteId, DateTime fechaVencimiento, string numeroLoteProveedor)
        {
            string query = @"
        UPDATE Lote 
        SET FechaVencimiento = @FechaVencimiento,
            NumeroLoteProveedor = @NumeroLoteProveedor,
            Estado = CASE 
                        WHEN @FechaVencimiento < GETDATE() THEN 'Vencido'
                        ELSE Estado
                     END
        WHERE LoteId = @LoteId";

            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@LoteId", loteId);
                cmd.Parameters.AddWithValue("@FechaVencimiento", fechaVencimiento);
                cmd.Parameters.AddWithValue("@NumeroLoteProveedor", (object)numeroLoteProveedor ?? DBNull.Value);
                conn.Open();
                return cmd.ExecuteNonQuery() > 0;
            }
        }
        public bool CrearLote(int productoId, string numeroLoteInterno, string numeroLoteProveedor,
                      decimal cantidad, decimal precioUnitario, string fechaIngreso, string fechaVencimiento)
        {
            string query = @"
        INSERT INTO Lote (ProductoId, NumeroLoteInterno, NumeroLoteProveedor, 
                         CantidadInicial, CantidadActual, PrecioUnitario, 
                         FechaIngreso, FechaVencimiento, Estado, Activo, FechaCreacion)
        VALUES (@ProductoId, @NumeroLoteInterno, @NumeroLoteProveedor,
                @Cantidad, @Cantidad, @PrecioUnitario,
                @FechaIngreso, @FechaVencimiento, 'Activo', 1, GETDATE())";

            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@ProductoId", productoId);
                cmd.Parameters.AddWithValue("@NumeroLoteInterno", numeroLoteInterno);
                cmd.Parameters.AddWithValue("@NumeroLoteProveedor", (object)numeroLoteProveedor ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Cantidad", cantidad);
                cmd.Parameters.AddWithValue("@PrecioUnitario", precioUnitario);
                cmd.Parameters.AddWithValue("@FechaIngreso", string.IsNullOrEmpty(fechaIngreso) ? DateTime.Now : DateTime.Parse(fechaIngreso));
                cmd.Parameters.AddWithValue("@FechaVencimiento", string.IsNullOrEmpty(fechaVencimiento) ? DBNull.Value : (object)DateTime.Parse(fechaVencimiento));
                conn.Open();
                return cmd.ExecuteNonQuery() > 0;
            }
        }
    }
}