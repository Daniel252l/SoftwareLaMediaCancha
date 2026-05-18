using LaMediaCancha.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;

namespace LaMediaCancha.Services
{
    public class InventarioService
    {
        private readonly string _connectionString;

        public InventarioService()
        {
            _connectionString = ConfigurationManager.ConnectionStrings["LaMediaCanchaDB"].ConnectionString;
        }

        public List<ProductoModels.InventarioCompra> ObtenerInventario()
        {
            var inventario = new List<ProductoModels.InventarioCompra>();

            string query = @"
                SELECT 
                    pc.ProductoCompraId,
                    pc.Codigo,
                    pc.Nombre,
                    pc.UnidadMedida,
                    pc.PrecioCompra,
                    pc.StockActual,
                    pc.StockMinimo,
                    ISNULL(cc.Nombre, 'Sin categoría') AS Categoria,
                    pc.Activo,
                    (SELECT COUNT(*) FROM LoteCompra WHERE ProductoCompraId = pc.ProductoCompraId AND Activo = 1 AND CantidadActual > 0) AS LotesActivos,
                    CASE 
                        WHEN pc.StockActual <= pc.StockMinimo THEN 'Bajo'
                        WHEN pc.StockActual <= pc.StockMinimo * 1.5 THEN 'Alerta'
                        ELSE 'Normal'
                    END AS EstadoStock
                FROM ProductoCompra pc
                LEFT JOIN CategoriaCompra cc ON pc.CategoriaId = cc.CategoriaCompraId
                ORDER BY pc.Nombre";

            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand(query, conn))
            {
                conn.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        inventario.Add(new ProductoModels.InventarioCompra
                        {
                            ProductoCompraId = (int)reader["ProductoCompraId"],
                            Codigo = reader["Codigo"].ToString(),
                            Nombre = reader["Nombre"].ToString(),
                            UnidadMedida = reader["UnidadMedida"].ToString(),
                            PrecioCompra = (decimal)reader["PrecioCompra"],
                            StockActual = (int)reader["StockActual"],
                            StockMinimo = (int)reader["StockMinimo"],
                            Categoria = reader["Categoria"].ToString(),
                            Activo = (bool)reader["Activo"],
                            EstadoStock = reader["EstadoStock"].ToString(),
                            LotesActivos = (int)reader["LotesActivos"]
                        });
                    }
                }
            }

            return inventario;
        }

        public List<ProductoModels.InventarioCompra> ObtenerProductosStockBajo()
        {
            var productos = new List<ProductoModels.InventarioCompra>();

            string query = @"
                SELECT 
                    pc.ProductoCompraId,
                    pc.Codigo,
                    pc.Nombre,
                    pc.UnidadMedida,
                    pc.PrecioCompra,
                    pc.StockActual,
                    pc.StockMinimo,
                    ISNULL(cc.Nombre, 'Sin categoría') AS Categoria,
                    pc.Activo,
                    (SELECT COUNT(*) FROM LoteCompra WHERE ProductoCompraId = pc.ProductoCompraId AND Activo = 1 AND CantidadActual > 0) AS LotesActivos,
                    'Bajo' AS EstadoStock
                FROM ProductoCompra pc
                LEFT JOIN CategoriaCompra cc ON pc.CategoriaId = cc.CategoriaCompraId
                WHERE pc.Activo = 1 AND pc.StockActual <= pc.StockMinimo
                ORDER BY pc.Nombre";

            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand(query, conn))
            {
                conn.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        productos.Add(new ProductoModels.InventarioCompra
                        {
                            ProductoCompraId = (int)reader["ProductoCompraId"],
                            Codigo = reader["Codigo"].ToString(),
                            Nombre = reader["Nombre"].ToString(),
                            UnidadMedida = reader["UnidadMedida"].ToString(),
                            PrecioCompra = (decimal)reader["PrecioCompra"],
                            StockActual = (int)reader["StockActual"],
                            StockMinimo = (int)reader["StockMinimo"],
                            Categoria = reader["Categoria"].ToString(),
                            Activo = (bool)reader["Activo"],
                            EstadoStock = reader["EstadoStock"].ToString(),
                            LotesActivos = (int)reader["LotesActivos"]
                        });
                    }
                }
            }

            return productos;
        }

        public List<ProductoModels.MovimientoInventario> ObtenerMovimientosPorProducto(int productoCompraId)
        {
            var movimientos = new List<ProductoModels.MovimientoInventario>();

            string query = @"
                SELECT 
                    m.MovimientoId,
                    m.ProductoCompraId,
                    pc.Nombre AS ProductoNombre,
                    m.LoteCompraId,
                    l.NumeroLote,
                    m.TipoMovimiento,
                    m.Cantidad,
                    m.PrecioUnitario,
                    m.Motivo,
                    m.ReferenciaId,
                    m.FechaMovimiento,
                    ISNULL(u.Nombre, 'Sistema') AS UsuarioNombre,
                    ISNULL(prov.RazonSocial, 'N/A') AS ProveedorNombre
                FROM MovimientoInventario m
                LEFT JOIN ProductoCompra pc ON m.ProductoCompraId = pc.ProductoCompraId
                LEFT JOIN LoteCompra l ON m.LoteCompraId = l.LoteCompraId
                LEFT JOIN Proveedor prov ON l.ProveedorId = prov.ProveedorId
                LEFT JOIN Usuario u ON m.UsuarioId = u.UsuarioId
                WHERE m.ProductoCompraId = @ProductoCompraId
                ORDER BY m.FechaMovimiento DESC";

            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@ProductoCompraId", productoCompraId);
                conn.Open();

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        movimientos.Add(new ProductoModels.MovimientoInventario
                        {
                            MovimientoId = (int)reader["MovimientoId"],
                            ProductoCompraId = (int)reader["ProductoCompraId"],
                            ProductoNombre = reader["ProductoNombre"]?.ToString() ?? "",
                            LoteCompraId = reader["LoteCompraId"] as int?,
                            NumeroLote = reader["NumeroLote"]?.ToString(),
                            TipoMovimiento = reader["TipoMovimiento"].ToString(),
                            Cantidad = (int)reader["Cantidad"],
                            PrecioUnitario = (decimal)reader["PrecioUnitario"],
                            Motivo = reader["Motivo"]?.ToString(),
                            ReferenciaId = reader["ReferenciaId"] as int?,
                            FechaMovimiento = (DateTime)reader["FechaMovimiento"],
                            UsuarioNombre = reader["UsuarioNombre"]?.ToString() ?? "Sistema",
                            ProveedorNombre = reader["ProveedorNombre"]?.ToString() ?? "N/A"
                        });
                    }
                }
            }

            return movimientos;
        }

        public bool DescontarStockFIFO(int productoCompraId, int cantidad, int referenciaId, string motivo, int usuarioId)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                using (var transaction = conn.BeginTransaction())
                {
                    try
                    {
                        int cantidadRestante = cantidad;

                        string getLotes = @"
                            SELECT LoteCompraId, CantidadActual, PrecioUnitario
                            FROM LoteCompra
                            WHERE ProductoCompraId = @ProductoCompraId 
                              AND Activo = 1 
                              AND CantidadActual > 0
                              AND (FechaVencimiento IS NULL OR FechaVencimiento >= GETDATE())
                            ORDER BY ISNULL(FechaVencimiento, '9999-12-31') ASC, FechaIngreso ASC";

                        var lotes = new List<dynamic>();

                        using (var cmd = new SqlCommand(getLotes, conn, transaction))
                        {
                            cmd.Parameters.AddWithValue("@ProductoCompraId", productoCompraId);
                            using (var reader = cmd.ExecuteReader())
                            {
                                while (reader.Read())
                                {
                                    lotes.Add(new
                                    {
                                        LoteCompraId = (int)reader["LoteCompraId"],
                                        CantidadActual = (int)reader["CantidadActual"],
                                        PrecioUnitario = (decimal)reader["PrecioUnitario"]
                                    });
                                }
                            }
                        }

                        if (lotes.Count == 0)
                        {
                            throw new Exception($"No hay stock disponible");
                        }

                        int totalDescontado = 0;

                        foreach (var lote in lotes)
                        {
                            if (cantidadRestante <= 0) break;

                            int cantidadADescontar = Math.Min(cantidadRestante, lote.CantidadActual);

                            string updateLote = @"
                                UPDATE LoteCompra 
                                SET CantidadActual = CantidadActual - @Cantidad
                                WHERE LoteCompraId = @LoteCompraId";

                            using (var cmd = new SqlCommand(updateLote, conn, transaction))
                            {
                                cmd.Parameters.AddWithValue("@LoteCompraId", lote.LoteCompraId);
                                cmd.Parameters.AddWithValue("@Cantidad", cantidadADescontar);
                                cmd.ExecuteNonQuery();
                            }

                            string insertMovimiento = @"
                                INSERT INTO MovimientoInventario (ProductoCompraId, LoteCompraId, TipoMovimiento, Cantidad, PrecioUnitario, Motivo, ReferenciaId, UsuarioId)
                                VALUES (@ProductoCompraId, @LoteCompraId, 'SALIDA', @Cantidad, @PrecioUnitario, @Motivo, @ReferenciaId, @UsuarioId)";

                            using (var cmd = new SqlCommand(insertMovimiento, conn, transaction))
                            {
                                cmd.Parameters.AddWithValue("@ProductoCompraId", productoCompraId);
                                cmd.Parameters.AddWithValue("@LoteCompraId", lote.LoteCompraId);
                                cmd.Parameters.AddWithValue("@Cantidad", cantidadADescontar);
                                cmd.Parameters.AddWithValue("@PrecioUnitario", lote.PrecioUnitario);
                                cmd.Parameters.AddWithValue("@Motivo", motivo);
                                cmd.Parameters.AddWithValue("@ReferenciaId", referenciaId);
                                cmd.Parameters.AddWithValue("@UsuarioId", usuarioId);
                                cmd.ExecuteNonQuery();
                            }

                            cantidadRestante -= cantidadADescontar;
                            totalDescontado += cantidadADescontar;
                        }

                        if (cantidadRestante > 0)
                        {
                            throw new Exception($"Stock insuficiente. Faltan {cantidadRestante} unidades");
                        }

                        string updateStock = @"
                            UPDATE ProductoCompra 
                            SET StockActual = StockActual - @Cantidad
                            WHERE ProductoCompraId = @ProductoCompraId";

                        using (var cmd = new SqlCommand(updateStock, conn, transaction))
                        {
                            cmd.Parameters.AddWithValue("@ProductoCompraId", productoCompraId);
                            cmd.Parameters.AddWithValue("@Cantidad", totalDescontado);
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