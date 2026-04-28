using LaMediaCancha.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using static LaMediaCancha.Models.CompraModels;

namespace LaMediaCancha.Services
{
    public class CompraService : ICompraService
    {
        private readonly string _connectionString;

        public CompraService()
        {
            _connectionString = ConfigurationManager.ConnectionStrings["LaMediaCanchaDB"].ConnectionString;
        }

        public EncabezadoCompra ObtenerCompraPorId(int compraId)
        {
            EncabezadoCompra compra = null;

            string query = @"
                SELECT 
                    ec.*,
                    p.RazonSocial AS ProveedorNombre,
                    e.CodigoEmpleado
                FROM EncabezadoCompra ec
                INNER JOIN Proveedor p ON ec.ProveedorId = p.ProveedorId
                INNER JOIN Empleado e ON ec.EmpleadoId = e.EmpleadoId
                WHERE ec.CompraId = @CompraId";

            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@CompraId", compraId);
                conn.Open();

                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        compra = new EncabezadoCompra
                        {
                            CompraId = (int)reader["CompraId"],
                            EmpleadoId = (int)reader["EmpleadoId"],
                            ProveedorId = (int)reader["ProveedorId"],
                            ProveedorNombre = reader["ProveedorNombre"].ToString(),
                            NumeroDocumento = reader["NumeroDocumento"].ToString(),
                            NumeroFactura = reader["NumeroFactura"]?.ToString(),
                            FechaCompra = (DateTime)reader["FechaCompra"],
                            Subtotal = (decimal)reader["Subtotal"],
                            Impuesto = (decimal)reader["Impuesto"],
                            Descuento = (decimal)reader["Descuento"],
                            Total = (decimal)reader["Total"],
                            Estado = reader["Estado"].ToString(),
                            Observaciones = reader["Observaciones"]?.ToString()
                        };
                    }
                }
            }

            return compra;
        }

        public List<EncabezadoCompra> ObtenerTodasCompras()
        {
            var compras = new List<EncabezadoCompra>();

            string query = @"
                SELECT 
                    ec.*,
                    p.RazonSocial AS ProveedorNombre
                FROM EncabezadoCompra ec
                INNER JOIN Proveedor p ON ec.ProveedorId = p.ProveedorId
                ORDER BY ec.FechaCompra DESC";

            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand(query, conn))
            {
                conn.Open();

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        compras.Add(new EncabezadoCompra
                        {
                            CompraId = (int)reader["CompraId"],
                            ProveedorNombre = reader["ProveedorNombre"].ToString(),
                            NumeroDocumento = reader["NumeroDocumento"].ToString(),
                            FechaCompra = (DateTime)reader["FechaCompra"],
                            Total = (decimal)reader["Total"],
                            Estado = reader["Estado"].ToString()
                        });
                    }
                }
            }

            return compras;
        }

        public int RegistrarCompra(RegistrarCompraRequest request)
        {
            // Implementar según necesidad
            throw new NotImplementedException();
        }

        public bool CancelarCompra(int compraId, string motivo)
        {
            // Implementar según necesidad
            throw new NotImplementedException();
        }
    }
}