using LaMediaCancha.Models;
using LaMediaCancha.Models.ViewModels;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;
using static LaMediaCancha.Models.OrdenModels;

namespace LaMediaCancha.Services
{
    public class OrdenService
    {
        private readonly string _connectionString;

        public OrdenService()
        {
            _connectionString = ConfigurationManager.ConnectionStrings["LaMediaCanchaDB"].ConnectionString;
        }

        // ==================== MESAS Y SILLAS ====================

        public List<Models.OrdenModels.Mesa> ObtenerTodasLasMesasConSillas()
        {
            var mesas = new List<Models.OrdenModels.Mesa>();

            string queryMesas = "SELECT MesaId, NumeroMesa, Capacidad, Ubicacion, Estado FROM Mesa WHERE Activo = 1 ORDER BY NumeroMesa";

            using (var conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                using (var cmd = new SqlCommand(queryMesas, conn))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var mesa = new Models.OrdenModels.Mesa
                        {
                            MesaId = Convert.ToInt32(reader["MesaId"]),
                            NumeroMesa = Convert.ToInt32(reader["NumeroMesa"]),
                            Capacidad = Convert.ToInt32(reader["Capacidad"]),
                            Ubicacion = reader["Ubicacion"].ToString(),
                            Estado = reader["Estado"].ToString(),
                            Sillas = new List<Models.OrdenModels.Silla>(),
                            TotalMonto = 0
                        };
                        mesas.Add(mesa);
                    }
                }
            }

            foreach (var mesa in mesas)
            {
                mesa.Sillas = ObtenerSillasPorMesa(mesa.MesaId);

                if (mesa.Sillas.Count == 0 && mesa.Capacidad > 0)
                {
                    CrearSillasParaMesa(mesa.MesaId, mesa.Capacidad);
                    mesa.Sillas = ObtenerSillasPorMesa(mesa.MesaId);
                }

                decimal totalMesa = 0;

                foreach (var silla in mesa.Sillas)
                {
                    var ordenPersona = ObtenerOrdenActivaPorSilla(silla.SillaId);
                    if (ordenPersona != null && ordenPersona.OrdenPersonaId > 0)
                    {
                        var personaCompleta = ObtenerOrdenPersonaCompleta(ordenPersona.OrdenPersonaId.Value);
                        if (personaCompleta != null && !personaCompleta.Pagado)
                        {
                            silla.Estado = "Ocupada";
                            silla.OrdenPersonaId = personaCompleta.OrdenPersonaId;
                            silla.Total = personaCompleta.Total;
                            silla.NombreCliente = personaCompleta.NombreCliente;
                            totalMesa += personaCompleta.Total;
                        }
                        else
                        {
                            silla.Estado = "Disponible";
                            silla.Total = 0;
                        }
                    }
                    else
                    {
                        silla.Estado = "Disponible";
                        silla.Total = 0;
                    }
                }
                mesa.TotalMonto = totalMesa;
                mesa.Estado = mesa.Sillas.Any(s => s.Estado == "Ocupada") ? "Ocupada" : "Disponible";
            }

            return mesas;
        }

        public void CrearSillasParaMesa(int mesaId, int cantidad)
        {
            for (int i = 1; i <= cantidad; i++)
            {
                string query = @"INSERT INTO Silla (MesaId, NumeroSilla, Estado, Activo, EsTemporal)
                                 VALUES (@MesaId, @NumeroSilla, 'Disponible', 1, 0)";

                using (var conn = new SqlConnection(_connectionString))
                {
                    conn.Open();
                    using (var cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@MesaId", mesaId);
                        cmd.Parameters.AddWithValue("@NumeroSilla", i);
                        cmd.ExecuteNonQuery();
                    }
                }
            }
        }

        public List<Models.OrdenModels.Silla> ObtenerSillasPorMesa(int mesaId)
        {
            var sillas = new List<Models.OrdenModels.Silla>();
            string query = "SELECT SillaId, MesaId, NumeroSilla, Estado, Activo, ISNULL(EsTemporal, 0) as EsTemporal FROM Silla WHERE MesaId = @MesaId AND Activo = 1 ORDER BY NumeroSilla";

            using (var conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                using (var cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@MesaId", mesaId);
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            sillas.Add(new Models.OrdenModels.Silla
                            {
                                SillaId = Convert.ToInt32(reader["SillaId"]),
                                MesaId = Convert.ToInt32(reader["MesaId"]),
                                NumeroSilla = Convert.ToInt32(reader["NumeroSilla"]),
                                Estado = reader["Estado"].ToString(),
                                Activo = Convert.ToBoolean(reader["Activo"]),
                                EsTemporal = reader["EsTemporal"] != DBNull.Value && Convert.ToBoolean(reader["EsTemporal"]),
                                Total = 0,
                                NombreCliente = null
                            });
                        }
                    }
                }
            }
            return sillas;
        }

        public Models.OrdenModels.Orden ObtenerOrdenActivaPorSilla(int sillaId)
        {
            string query = @"
                SELECT TOP 1 
                    o.OrdenId, 
                    o.NumeroOrden, 
                    o.MesaId, 
                    o.FechaApertura,
                    op.OrdenPersonaId, 
                    op.NombreCliente, 
                    op.Total,
                    op.Pagado,
                    m.NumeroMesa
                FROM OrdenPersona op
                INNER JOIN Orden o ON op.OrdenId = o.OrdenId
                INNER JOIN Silla s ON op.SillaId = s.SillaId
                INNER JOIN Mesa m ON o.MesaId = m.MesaId
                WHERE op.SillaId = @SillaId AND o.Estado = 'Abierta' AND op.Pagado = 0";

            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@SillaId", sillaId);
                conn.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        return new Models.OrdenModels.Orden
                        {
                            OrdenId = (int)reader["OrdenId"],
                            NumeroOrden = reader["NumeroOrden"]?.ToString(),
                            MesaId = (int)reader["MesaId"],
                            NumeroMesa = (int)reader["NumeroMesa"],
                            FechaApertura = (DateTime)reader["FechaApertura"],
                            OrdenPersonaId = (int)reader["OrdenPersonaId"],
                            ClienteNombre = reader["NombreCliente"]?.ToString(),
                            Total = (decimal)reader["Total"]
                        };
                    }
                }
            }
            return null;
        }

        public OrdenPersona ObtenerOrdenPersonaCompleta(int ordenPersonaId)
        {
            string query = @"SELECT TOP 1 
                                OrdenPersonaId, OrdenId, SillaId, NombreCliente, 
                                Subtotal, Impuesto, Total, Pagado, FechaCreacion
                             FROM OrdenPersona 
                             WHERE OrdenPersonaId = @OrdenPersonaId";

            using (var conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                using (var cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@OrdenPersonaId", ordenPersonaId);
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return new OrdenPersona
                            {
                                OrdenPersonaId = Convert.ToInt32(reader["OrdenPersonaId"]),
                                OrdenId = Convert.ToInt32(reader["OrdenId"]),
                                SillaId = reader["SillaId"] != DBNull.Value ? Convert.ToInt32(reader["SillaId"]) : (int?)null,
                                NombreCliente = reader["NombreCliente"]?.ToString() ?? "",
                                Subtotal = Convert.ToDecimal(reader["Subtotal"]),
                                Impuesto = Convert.ToDecimal(reader["Impuesto"]),
                                Total = Convert.ToDecimal(reader["Total"]),
                                Pagado = Convert.ToBoolean(reader["Pagado"]),
                                FechaCreacion = Convert.ToDateTime(reader["FechaCreacion"])
                            };
                        }
                    }
                }
            }
            return null;
        }

        public int CrearOrdenParaSilla(int sillaId, int mesaId, int usuarioId, string usuarioNombre)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                using (var transaction = conn.BeginTransaction())
                {
                    try
                    {
                        string checkOrden = "SELECT OrdenId FROM Orden WHERE MesaId = @MesaId AND Estado = 'Abierta'";
                        int ordenId = 0;

                        using (var cmd = new SqlCommand(checkOrden, conn, transaction))
                        {
                            cmd.Parameters.AddWithValue("@MesaId", mesaId);
                            var result = cmd.ExecuteScalar();
                            if (result != null)
                            {
                                ordenId = (int)result;
                            }
                            else
                            {
                                string insertOrden = @"
                                    INSERT INTO Orden (NumeroOrden, MesaId, FechaApertura, Subtotal, Impuesto, Total, Estado, UsuarioId, UsuarioNombre)
                                    VALUES (@NumeroOrden, @MesaId, GETDATE(), 0, 0, 0, 'Abierta', @UsuarioId, @UsuarioNombre);
                                    SELECT SCOPE_IDENTITY();";

                                using (var cmd2 = new SqlCommand(insertOrden, conn, transaction))
                                {
                                    cmd2.Parameters.AddWithValue("@NumeroOrden", GenerarNumeroOrden());
                                    cmd2.Parameters.AddWithValue("@MesaId", mesaId);
                                    cmd2.Parameters.AddWithValue("@UsuarioId", usuarioId);
                                    cmd2.Parameters.AddWithValue("@UsuarioNombre", usuarioNombre);
                                    ordenId = Convert.ToInt32(cmd2.ExecuteScalar());
                                }
                            }
                        }

                        string insertPersona = @"
                            INSERT INTO OrdenPersona (OrdenId, SillaId, NombreCliente, Subtotal, Impuesto, Total, Pagado, FechaCreacion)
                            VALUES (@OrdenId, @SillaId, @NombreCliente, 0, 0, 0, 0, GETDATE());
                            SELECT SCOPE_IDENTITY();";

                        int ordenPersonaId;
                        using (var cmd = new SqlCommand(insertPersona, conn, transaction))
                        {
                            cmd.Parameters.AddWithValue("@OrdenId", ordenId);
                            cmd.Parameters.AddWithValue("@SillaId", sillaId);
                            cmd.Parameters.AddWithValue("@NombreCliente", $"Silla {sillaId}");
                            ordenPersonaId = Convert.ToInt32(cmd.ExecuteScalar());
                        }

                        string updateSilla = "UPDATE Silla SET Estado = 'Ocupada' WHERE SillaId = @SillaId";
                        using (var cmd = new SqlCommand(updateSilla, conn, transaction))
                        {
                            cmd.Parameters.AddWithValue("@SillaId", sillaId);
                            cmd.ExecuteNonQuery();
                        }

                        transaction.Commit();
                        return ordenPersonaId;
                    }
                    catch
                    {
                        transaction.Rollback();
                        throw;
                    }
                }
            }
        }

        public int AgregarSillaTemporal(int mesaId, int numeroSilla, string nombreCliente)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                conn.Open();

                string checkQuery = "SELECT COUNT(*) FROM Silla WHERE MesaId = @MesaId AND NumeroSilla = @NumeroSilla";
                using (var cmd = new SqlCommand(checkQuery, conn))
                {
                    cmd.Parameters.AddWithValue("@MesaId", mesaId);
                    cmd.Parameters.AddWithValue("@NumeroSilla", numeroSilla);
                    int existe = (int)cmd.ExecuteScalar();

                    if (existe > 0)
                    {
                        throw new Exception($"La silla {numeroSilla} ya existe en esta mesa");
                    }
                }

                string insertQuery = @"
                    INSERT INTO Silla (MesaId, NumeroSilla, Estado, Activo, EsTemporal)
                    VALUES (@MesaId, @NumeroSilla, 'Disponible', 1, 1);
                    SELECT SCOPE_IDENTITY();";

                using (var cmd = new SqlCommand(insertQuery, conn))
                {
                    cmd.Parameters.AddWithValue("@MesaId", mesaId);
                    cmd.Parameters.AddWithValue("@NumeroSilla", numeroSilla);
                    return Convert.ToInt32(cmd.ExecuteScalar());
                }
            }
        }

        public void EliminarSillaTemporal(int sillaId)
        {
            string query = "DELETE FROM Silla WHERE SillaId = @SillaId AND EsTemporal = 1";
            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@SillaId", sillaId);
                conn.Open();
                cmd.ExecuteNonQuery();
            }
        }

        public Models.OrdenModels.Mesa ObtenerMesaPorId(int mesaId)
        {
            string query = "SELECT MesaId, NumeroMesa, Capacidad, Ubicacion, Estado FROM Mesa WHERE MesaId = @MesaId";
            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@MesaId", mesaId);
                conn.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        return new Models.OrdenModels.Mesa
                        {
                            MesaId = (int)reader["MesaId"],
                            NumeroMesa = (int)reader["NumeroMesa"],
                            Capacidad = (int)reader["Capacidad"],
                            Ubicacion = reader["Ubicacion"]?.ToString() ?? "",
                            Estado = reader["Estado"]?.ToString() ?? "Disponible"
                        };
                    }
                }
            }
            return null;
        }

        public Models.OrdenModels.Mesa ObtenerMesaConSillasYPedidos(int mesaId)
        {
            var mesa = ObtenerMesaPorId(mesaId);
            if (mesa == null) return null;

            var sillas = ObtenerSillasPorMesa(mesaId);
            decimal totalMesa = 0;

            foreach (var silla in sillas)
            {
                var ordenPersona = ObtenerOrdenActivaPorSilla(silla.SillaId);
                if (ordenPersona != null && ordenPersona.OrdenPersonaId.HasValue)
                {
                    var personaCompleta = ObtenerOrdenPersonaCompleta(ordenPersona.OrdenPersonaId.Value);
                    if (personaCompleta != null && !personaCompleta.Pagado)
                    {
                        silla.Estado = "Ocupada";
                        silla.OrdenPersonaId = personaCompleta.OrdenPersonaId;
                        silla.Total = personaCompleta.Total;
                        silla.NombreCliente = personaCompleta.NombreCliente;
                        totalMesa += personaCompleta.Total;
                    }
                    else
                    {
                        silla.Estado = "Disponible";
                        silla.Total = 0;
                    }
                }
                else
                {
                    silla.Estado = "Disponible";
                    silla.Total = 0;
                }
            }

            mesa.Sillas = sillas;
            mesa.TotalMonto = totalMesa;
            mesa.Estado = sillas.Any(s => s.Estado == "Ocupada") ? "Ocupada" : "Disponible";

            return mesa;
        }

        // ==================== ÓRDENES ====================

        public string GenerarNumeroOrden()
        {
            return $"ORD-{DateTime.Now:yyyyMMddHHmmss}";
        }

        public int CrearOrden(Orden orden)
        {
            string query = @"
                INSERT INTO Orden (NumeroOrden, MesaId, ClienteNombre, ClienteTelefono, FechaApertura, 
                                  Subtotal, Impuesto, Total, Estado, Observaciones, UsuarioId, UsuarioNombre)
                VALUES (@NumeroOrden, @MesaId, @ClienteNombre, @ClienteTelefono, @FechaApertura, 
                        @Subtotal, @Impuesto, @Total, @Estado, @Observaciones, @UsuarioId, @UsuarioNombre);
                SELECT SCOPE_IDENTITY();";

            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@NumeroOrden", orden.NumeroOrden);
                cmd.Parameters.AddWithValue("@MesaId", orden.MesaId);
                cmd.Parameters.AddWithValue("@ClienteNombre", string.IsNullOrEmpty(orden.ClienteNombre) ? DBNull.Value : (object)orden.ClienteNombre);
                cmd.Parameters.AddWithValue("@ClienteTelefono", string.IsNullOrEmpty(orden.ClienteTelefono) ? DBNull.Value : (object)orden.ClienteTelefono);
                cmd.Parameters.AddWithValue("@FechaApertura", orden.FechaApertura);
                cmd.Parameters.AddWithValue("@Subtotal", orden.Subtotal);
                cmd.Parameters.AddWithValue("@Impuesto", orden.Impuesto);
                cmd.Parameters.AddWithValue("@Total", orden.Total);
                cmd.Parameters.AddWithValue("@Estado", orden.Estado);
                cmd.Parameters.AddWithValue("@Observaciones", string.IsNullOrEmpty(orden.Observaciones) ? DBNull.Value : (object)orden.Observaciones);
                cmd.Parameters.AddWithValue("@UsuarioId", orden.UsuarioId);
                cmd.Parameters.AddWithValue("@UsuarioNombre", orden.UsuarioNombre);
                conn.Open();
                return Convert.ToInt32(cmd.ExecuteScalar());
            }
        }

        public void AgregarDetalleOrden(int ordenId, DetalleOrden detalle)
        {
            string query = @"
                INSERT INTO DetalleOrden (OrdenId, ProductoId, ProductoCodigo, ProductoNombre, 
                                         Cantidad, PrecioUnitario, Subtotal, Nota, EsDeCombo, ComboId)
                VALUES (@OrdenId, @ProductoId, @ProductoCodigo, @ProductoNombre, 
                        @Cantidad, @PrecioUnitario, @Subtotal, @Nota, @EsDeCombo, @ComboId)";

            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@OrdenId", ordenId);
                cmd.Parameters.AddWithValue("@ProductoId", detalle.ProductoId);
                cmd.Parameters.AddWithValue("@ProductoCodigo", detalle.ProductoCodigo ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@ProductoNombre", detalle.ProductoNombre);
                cmd.Parameters.AddWithValue("@Cantidad", detalle.Cantidad);
                cmd.Parameters.AddWithValue("@PrecioUnitario", detalle.PrecioUnitario);
                cmd.Parameters.AddWithValue("@Subtotal", detalle.Subtotal);
                cmd.Parameters.AddWithValue("@Nota", detalle.Nota ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@EsDeCombo", detalle.EsDeCombo);
                cmd.Parameters.AddWithValue("@ComboId", detalle.ComboId ?? (object)DBNull.Value);
                conn.Open();
                cmd.ExecuteNonQuery();
            }
        }

        public Models.OrdenModels.Orden ObtenerOrdenActivaPorMesa(int mesaId)
        {
            string query = @"
                SELECT TOP 1 
                    o.OrdenId, 
                    o.NumeroOrden, 
                    o.ClienteNombre, 
                    o.FechaApertura, 
                    o.Subtotal, 
                    o.Impuesto, 
                    o.Total, 
                    o.Observaciones,
                    m.NumeroMesa
                FROM Orden o
                INNER JOIN Mesa m ON o.MesaId = m.MesaId
                WHERE o.MesaId = @MesaId AND o.Estado = 'Abierta'
                ORDER BY o.OrdenId DESC";

            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@MesaId", mesaId);
                conn.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        return new Models.OrdenModels.Orden
                        {
                            OrdenId = (int)reader["OrdenId"],
                            NumeroOrden = reader["NumeroOrden"]?.ToString(),
                            NumeroMesa = (int)reader["NumeroMesa"],
                            ClienteNombre = reader["ClienteNombre"]?.ToString(),
                            FechaApertura = (DateTime)reader["FechaApertura"],
                            Subtotal = (decimal)reader["Subtotal"],
                            Impuesto = (decimal)reader["Impuesto"],
                            Total = (decimal)reader["Total"],
                            Observaciones = reader["Observaciones"]?.ToString(),
                            Detalles = ObtenerDetallesOrden((int)reader["OrdenId"])
                        };
                    }
                }
            }
            return null;
        }

        public List<DetalleOrden> ObtenerDetallesOrden(int ordenId)
        {
            var detalles = new List<DetalleOrden>();
            string query = @"
                SELECT DetalleOrdenId, ProductoId, ProductoCodigo, ProductoNombre, 
                       Cantidad, PrecioUnitario, Subtotal, Nota, EsDeCombo, ComboId
                FROM DetalleOrden
                WHERE OrdenId = @OrdenId";

            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@OrdenId", ordenId);
                conn.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        detalles.Add(new DetalleOrden
                        {
                            DetalleOrdenId = (int)reader["DetalleOrdenId"],
                            ProductoId = (int)reader["ProductoId"],
                            ProductoCodigo = reader["ProductoCodigo"]?.ToString(),
                            ProductoNombre = reader["ProductoNombre"]?.ToString(),
                            Cantidad = (decimal)reader["Cantidad"],
                            PrecioUnitario = (decimal)reader["PrecioUnitario"],
                            Subtotal = (decimal)reader["Subtotal"],
                            Nota = reader["Nota"]?.ToString(),
                            EsDeCombo = (bool)reader["EsDeCombo"],
                            ComboId = reader["ComboId"] as int?
                        });
                    }
                }
            }
            return detalles;
        }

        public List<DetalleOrdenPersona> ObtenerDetallesPorOrdenPersona(int ordenPersonaId)
        {
            var detalles = new List<DetalleOrdenPersona>();
            string query = @"
                SELECT DetalleOrdenPersonaId, ProductoId, ProductoNombre, Cantidad, 
                       PrecioUnitario, Subtotal, EsDeCombo, ComboId, Nota
                FROM DetalleOrdenPersona
                WHERE OrdenPersonaId = @OrdenPersonaId";

            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@OrdenPersonaId", ordenPersonaId);
                conn.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        detalles.Add(new DetalleOrdenPersona
                        {
                            DetalleOrdenPersonaId = (int)reader["DetalleOrdenPersonaId"],
                            ProductoId = (int)reader["ProductoId"],
                            ProductoNombre = reader["ProductoNombre"]?.ToString(),
                            Cantidad = (decimal)reader["Cantidad"],
                            PrecioUnitario = (decimal)reader["PrecioUnitario"],
                            Subtotal = (decimal)reader["Subtotal"],
                            EsDeCombo = (bool)reader["EsDeCombo"],
                            ComboId = reader["ComboId"] as int?,
                            Nota = reader["Nota"]?.ToString()
                        });
                    }
                }
            }
            return detalles;
        }

        public void LimpiarDetallesOrdenPersona(int ordenPersonaId)
        {
            string query = "DELETE FROM DetalleOrdenPersona WHERE OrdenPersonaId = @OrdenPersonaId";
            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@OrdenPersonaId", ordenPersonaId);
                conn.Open();
                cmd.ExecuteNonQuery();
            }
        }

        public void AgregarDetalleOrdenPersona(int ordenPersonaId, DetalleOrden detalle)
        {
            string query = @"
                INSERT INTO DetalleOrdenPersona (OrdenPersonaId, ProductoId, ProductoNombre, 
                                                Cantidad, PrecioUnitario, Subtotal, EsDeCombo, ComboId, Nota)
                VALUES (@OrdenPersonaId, @ProductoId, @ProductoNombre, 
                        @Cantidad, @PrecioUnitario, @Subtotal, @EsDeCombo, @ComboId, @Nota)";

            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@OrdenPersonaId", ordenPersonaId);
                cmd.Parameters.AddWithValue("@ProductoId", detalle.ProductoId);
                cmd.Parameters.AddWithValue("@ProductoNombre", detalle.ProductoNombre ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@Cantidad", detalle.Cantidad);
                cmd.Parameters.AddWithValue("@PrecioUnitario", detalle.PrecioUnitario);
                cmd.Parameters.AddWithValue("@Subtotal", detalle.Subtotal);
                cmd.Parameters.AddWithValue("@EsDeCombo", detalle.EsDeCombo);
                cmd.Parameters.AddWithValue("@ComboId", detalle.ComboId ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@Nota", detalle.Nota ?? (object)DBNull.Value);
                conn.Open();
                cmd.ExecuteNonQuery();
            }
        }

        public void ActualizarTotalesOrdenPersona(int ordenPersonaId, decimal subtotal, decimal impuesto, decimal total)
        {
            string query = "UPDATE OrdenPersona SET Subtotal = @Subtotal, Impuesto = @Impuesto, Total = @Total WHERE OrdenPersonaId = @OrdenPersonaId";
            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@OrdenPersonaId", ordenPersonaId);
                cmd.Parameters.AddWithValue("@Subtotal", subtotal);
                cmd.Parameters.AddWithValue("@Impuesto", impuesto);
                cmd.Parameters.AddWithValue("@Total", total);
                conn.Open();
                cmd.ExecuteNonQuery();
            }
        }

        public int ObtenerOrdenIdPorPersona(int ordenPersonaId)
        {
            string query = "SELECT OrdenId FROM OrdenPersona WHERE OrdenPersonaId = @OrdenPersonaId";
            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@OrdenPersonaId", ordenPersonaId);
                conn.Open();
                var result = cmd.ExecuteScalar();
                return result != null ? (int)result : 0;
            }
        }

        public void RecalcularTotalesOrden(int ordenId)
        {
            string query = @"SELECT SUM(Subtotal) as Subtotal, SUM(Impuesto) as Impuesto, SUM(Total) as Total 
                     FROM OrdenPersona WHERE OrdenId = @OrdenId AND Pagado = 0";

            decimal subtotal = 0, impuesto = 0, total = 0;

            using (var conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                using (var cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@OrdenId", ordenId);
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            subtotal = reader["Subtotal"] != DBNull.Value ? Convert.ToDecimal(reader["Subtotal"]) : 0;
                            impuesto = reader["Impuesto"] != DBNull.Value ? Convert.ToDecimal(reader["Impuesto"]) : 0;
                            total = reader["Total"] != DBNull.Value ? Convert.ToDecimal(reader["Total"]) : 0;
                        }
                    }
                }
            }

            string updateQuery = "UPDATE Orden SET Subtotal = @Subtotal, Impuesto = @Impuesto, Total = @Total WHERE OrdenId = @OrdenId";
            using (var conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                using (var cmd = new SqlCommand(updateQuery, conn))
                {
                    cmd.Parameters.AddWithValue("@OrdenId", ordenId);
                    cmd.Parameters.AddWithValue("@Subtotal", subtotal);
                    cmd.Parameters.AddWithValue("@Impuesto", impuesto);
                    cmd.Parameters.AddWithValue("@Total", total);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public int CrearOrdenPersonaConSilla(int ordenId, int sillaId, string nombreCliente)
        {
            string query = @"INSERT INTO OrdenPersona (OrdenId, SillaId, NombreCliente, Subtotal, Impuesto, Total, Pagado, FechaCreacion)
                     VALUES (@OrdenId, @SillaId, @NombreCliente, 0, 0, 0, 0, GETDATE());
                     SELECT SCOPE_IDENTITY();";

            using (var conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                using (var cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@OrdenId", ordenId);
                    cmd.Parameters.AddWithValue("@SillaId", sillaId);
                    cmd.Parameters.AddWithValue("@NombreCliente", nombreCliente);
                    return Convert.ToInt32(cmd.ExecuteScalar());
                }
            }
        }

        public void CerrarCuentaSilla(int ordenPersonaId)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                using (var transaction = conn.BeginTransaction())
                {
                    try
                    {
                        string updatePersona = "UPDATE OrdenPersona SET Pagado = 1 WHERE OrdenPersonaId = @OrdenPersonaId";
                        using (var cmd = new SqlCommand(updatePersona, conn, transaction))
                        {
                            cmd.Parameters.AddWithValue("@OrdenPersonaId", ordenPersonaId);
                            cmd.ExecuteNonQuery();
                        }

                        int sillaId = 0;
                        int ordenId = 0;
                        string getSilla = "SELECT SillaId, OrdenId FROM OrdenPersona WHERE OrdenPersonaId = @OrdenPersonaId";
                        using (var cmd = new SqlCommand(getSilla, conn, transaction))
                        {
                            cmd.Parameters.AddWithValue("@OrdenPersonaId", ordenPersonaId);
                            using (var reader = cmd.ExecuteReader())
                            {
                                if (reader.Read())
                                {
                                    sillaId = reader["SillaId"] != DBNull.Value ? (int)reader["SillaId"] : 0;
                                    ordenId = (int)reader["OrdenId"];
                                }
                            }
                        }

                        if (sillaId > 0)
                        {
                            string updateSilla = "UPDATE Silla SET Estado = 'Disponible' WHERE SillaId = @SillaId";
                            using (var cmd = new SqlCommand(updateSilla, conn, transaction))
                            {
                                cmd.Parameters.AddWithValue("@SillaId", sillaId);
                                cmd.ExecuteNonQuery();
                            }
                        }

                        string checkSillas = @"
                            SELECT COUNT(*) FROM OrdenPersona 
                            WHERE OrdenId = @OrdenId AND Pagado = 0";
                        using (var cmd = new SqlCommand(checkSillas, conn, transaction))
                        {
                            cmd.Parameters.AddWithValue("@OrdenId", ordenId);
                            int pendientes = (int)cmd.ExecuteScalar();

                            if (pendientes == 0)
                            {
                                string closeOrden = "UPDATE Orden SET Estado = 'Cerrada', FechaCierre = GETDATE() WHERE OrdenId = @OrdenId";
                                using (var cmd2 = new SqlCommand(closeOrden, conn, transaction))
                                {
                                    cmd2.Parameters.AddWithValue("@OrdenId", ordenId);
                                    cmd2.ExecuteNonQuery();
                                }

                                int mesaId = 0;
                                string getMesa = "SELECT MesaId FROM Orden WHERE OrdenId = @OrdenId";
                                using (var cmd2 = new SqlCommand(getMesa, conn, transaction))
                                {
                                    cmd2.Parameters.AddWithValue("@OrdenId", ordenId);
                                    var result = cmd2.ExecuteScalar();
                                    if (result != null) mesaId = (int)result;
                                }

                                if (mesaId > 0)
                                {
                                    ActualizarEstadoMesa(mesaId, "Disponible", conn, transaction);
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
        }

        public void CerrarCuentaMesaCompleta(int mesaId)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                using (var transaction = conn.BeginTransaction())
                {
                    try
                    {
                        string getOrdenQuery = "SELECT OrdenId FROM Orden WHERE MesaId = @MesaId AND Estado = 'Abierta'";
                        int ordenId = 0;

                        using (var cmd = new SqlCommand(getOrdenQuery, conn, transaction))
                        {
                            cmd.Parameters.AddWithValue("@MesaId", mesaId);
                            var result = cmd.ExecuteScalar();
                            if (result != null)
                                ordenId = (int)result;
                        }

                        if (ordenId > 0)
                        {
                            string updatePersonasQuery = "UPDATE OrdenPersona SET Pagado = 1 WHERE OrdenId = @OrdenId";
                            using (var cmd = new SqlCommand(updatePersonasQuery, conn, transaction))
                            {
                                cmd.Parameters.AddWithValue("@OrdenId", ordenId);
                                cmd.ExecuteNonQuery();
                            }

                            string closeOrdenQuery = "UPDATE Orden SET Estado = 'Cerrada', FechaCierre = GETDATE() WHERE OrdenId = @OrdenId";
                            using (var cmd = new SqlCommand(closeOrdenQuery, conn, transaction))
                            {
                                cmd.Parameters.AddWithValue("@OrdenId", ordenId);
                                cmd.ExecuteNonQuery();
                            }
                        }

                        string liberarSillasQuery = "UPDATE Silla SET Estado = 'Disponible' WHERE MesaId = @MesaId";
                        using (var cmd = new SqlCommand(liberarSillasQuery, conn, transaction))
                        {
                            cmd.Parameters.AddWithValue("@MesaId", mesaId);
                            cmd.ExecuteNonQuery();
                        }

                        string updateMesaQuery = "UPDATE Mesa SET Estado = 'Disponible' WHERE MesaId = @MesaId";
                        using (var cmd = new SqlCommand(updateMesaQuery, conn, transaction))
                        {
                            cmd.Parameters.AddWithValue("@MesaId", mesaId);
                            cmd.ExecuteNonQuery();
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
        }

        public void ActualizarEstadoMesa(int mesaId, string estado, SqlConnection conn = null, SqlTransaction transaction = null)
        {
            string query = "UPDATE Mesa SET Estado = @Estado WHERE MesaId = @MesaId";

            if (conn != null && transaction != null)
            {
                using (var cmd = new SqlCommand(query, conn, transaction))
                {
                    cmd.Parameters.AddWithValue("@MesaId", mesaId);
                    cmd.Parameters.AddWithValue("@Estado", estado);
                    cmd.ExecuteNonQuery();
                }
            }
            else
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    connection.Open();
                    using (var cmd = new SqlCommand(query, connection))
                    {
                        cmd.Parameters.AddWithValue("@MesaId", mesaId);
                        cmd.Parameters.AddWithValue("@Estado", estado);
                        cmd.ExecuteNonQuery();
                    }
                }
            }
        }

        public void ActualizarEstadoSilla(int sillaId, string estado)
        {
            string query = "UPDATE Silla SET Estado = @Estado WHERE SillaId = @SillaId";
            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@SillaId", sillaId);
                cmd.Parameters.AddWithValue("@Estado", estado);
                conn.Open();
                cmd.ExecuteNonQuery();
            }
        }

        // ==================== PRODUCTOS ====================

        public List<ProductoModels.Producto> ObtenerProductosActivos()
        {
            var productos = new List<ProductoModels.Producto>();
            string query = @"
                SELECT ProductoId, Codigo, Nombre, PrecioVenta
                FROM Producto 
                WHERE Activo = 1 
                ORDER BY Nombre";

            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand(query, conn))
            {
                conn.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        productos.Add(new ProductoModels.Producto
                        {
                            ProductoId = (int)reader["ProductoId"],
                            Codigo = reader["Codigo"]?.ToString(),
                            Nombre = reader["Nombre"]?.ToString(),
                            PrecioVenta = (decimal)reader["PrecioVenta"]
                        });
                    }
                }
            }
            return productos;
        }

        // ==================== COMBOS ====================

        public List<Combo> ObtenerCombosActivos()
        {
            var combos = new List<Combo>();
            string query = @"
                SELECT c.ComboId, c.Nombre, c.Descripcion, c.PrecioCombo, c.PrecioRegularTotal,
                       cd.ProductoId, p.Nombre AS ProductoNombre, p.PrecioVenta AS PrecioIndividual, cd.CantidadIncluida
                FROM Combo c
                INNER JOIN ComboDetalle cd ON c.ComboId = cd.ComboId
                INNER JOIN Producto p ON cd.ProductoId = p.ProductoId
                WHERE c.Activo = 1
                ORDER BY c.ComboId";

            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand(query, conn))
            {
                conn.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    var combosDict = new Dictionary<int, Combo>();
                    while (reader.Read())
                    {
                        int comboId = (int)reader["ComboId"];
                        if (!combosDict.ContainsKey(comboId))
                        {
                            combosDict[comboId] = new Combo
                            {
                                ComboId = comboId,
                                Nombre = reader["Nombre"].ToString(),
                                Descripcion = reader["Descripcion"]?.ToString(),
                                PrecioCombo = (decimal)reader["PrecioCombo"],
                                PrecioRegularTotal = (decimal)reader["PrecioRegularTotal"],
                                Productos = new List<ComboDetalle>()
                            };
                        }

                        combosDict[comboId].Productos.Add(new ComboDetalle
                        {
                            ProductoId = (int)reader["ProductoId"],
                            ProductoNombre = reader["ProductoNombre"].ToString(),
                            PrecioIndividual = (decimal)reader["PrecioIndividual"],
                            CantidadIncluida = (int)reader["CantidadIncluida"]
                        });
                    }
                    combos = combosDict.Values.ToList();
                }
            }
            return combos;
        }

        public List<ComboDetalle> ObtenerProductosPorCombo(int comboId)
        {
            var productos = new List<ComboDetalle>();
            string query = @"
                SELECT cd.ProductoId, p.Nombre AS ProductoNombre, p.PrecioVenta AS PrecioIndividual, cd.CantidadIncluida
                FROM ComboDetalle cd
                INNER JOIN Producto p ON cd.ProductoId = p.ProductoId
                WHERE cd.ComboId = @ComboId";

            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@ComboId", comboId);
                conn.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        productos.Add(new ComboDetalle
                        {
                            ProductoId = (int)reader["ProductoId"],
                            ProductoNombre = reader["ProductoNombre"].ToString(),
                            PrecioIndividual = (decimal)reader["PrecioIndividual"],
                            CantidadIncluida = (int)reader["CantidadIncluida"]
                        });
                    }
                }
            }
            return productos;
        }

        // ==================== OFERTAS ====================

        public List<OfertaViewModel> ObtenerOfertasActivas()
        {
            var ofertas = new List<OfertaViewModel>();
            string query = @"
                SELECT 
                    o.OfertaId, 
                    o.Nombre, 
                    o.Descripcion, 
                    o.ProductoId, 
                    p.Nombre AS ProductoNombre,
                    p.PrecioVenta AS PrecioOriginal,
                    o.DescuentoPorcentaje,
                    (p.PrecioVenta - (p.PrecioVenta * o.DescuentoPorcentaje / 100)) AS PrecioOferta,
                    o.FechaInicio, 
                    o.FechaFin,
                    DATEDIFF(DAY, GETDATE(), o.FechaFin) AS DiasRestantes
                FROM Oferta o
                INNER JOIN Producto p ON o.ProductoId = p.ProductoId
                WHERE o.Activo = 1 
                AND GETDATE() BETWEEN o.FechaInicio AND o.FechaFin
                ORDER BY o.FechaFin ASC";

            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand(query, conn))
            {
                conn.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        ofertas.Add(new OfertaViewModel
                        {
                            OfertaId = (int)reader["OfertaId"],
                            Nombre = reader["Nombre"].ToString(),
                            Descripcion = reader["Descripcion"]?.ToString(),
                            ProductoId = (int)reader["ProductoId"],
                            ProductoNombre = reader["ProductoNombre"].ToString(),
                            PrecioOriginal = (decimal)reader["PrecioOriginal"],
                            PrecioOferta = (decimal)reader["PrecioOferta"],
                            DescuentoPorcentaje = (decimal)reader["DescuentoPorcentaje"],
                            FechaInicio = (DateTime)reader["FechaInicio"],
                            FechaFin = (DateTime)reader["FechaFin"],
                            DiasRestantes = reader["DiasRestantes"] != DBNull.Value ? (int)reader["DiasRestantes"] : 0
                        });
                    }
                }
            }
            return ofertas;
        }

        public TicketViewModel GenerarTicket(int ordenId, int? ordenPersonaId = null)
        {
            var ticket = new TicketViewModel
            {
                Detalles = new List<TicketDetalleViewModel>()
            };

            using (var conn = new SqlConnection(_connectionString))
            {
                conn.Open();

                if (ordenPersonaId.HasValue && ordenPersonaId.Value > 0)
                {
                    ticket.EsCuentaSeparada = true;

                    string queryPersona = @"
                SELECT 
                    op.NombreCliente,
                    op.Subtotal,
                    op.Impuesto,
                    op.Total,
                    o.NumeroOrden,
                    o.MesaId,
                    o.ClienteNombre,
                    o.FechaApertura,
                    m.NumeroMesa,
                    s.NumeroSilla
                FROM OrdenPersona op
                INNER JOIN Orden o ON op.OrdenId = o.OrdenId
                INNER JOIN Mesa m ON o.MesaId = m.MesaId
                LEFT JOIN Silla s ON op.SillaId = s.SillaId
                WHERE op.OrdenPersonaId = @OrdenPersonaId";

                    using (var cmd = new SqlCommand(queryPersona, conn))
                    {
                        cmd.Parameters.AddWithValue("@OrdenPersonaId", ordenPersonaId.Value);
                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                ticket.OrdenId = ordenId;
                                ticket.NumeroOrden = reader["NumeroOrden"]?.ToString() ?? "";
                                ticket.MesaId = (int)reader["MesaId"];
                                ticket.NumeroMesa = (int)reader["NumeroMesa"];
                                ticket.ClienteNombre = reader["ClienteNombre"]?.ToString() ?? "";
                                var fecha = (DateTime)reader["FechaApertura"];
                                ticket.FechaStr = fecha.ToString("dd/MM/yyyy HH:mm:ss");
                                ticket.NombrePersona = reader["NombreCliente"]?.ToString() ?? "";
                                ticket.NumeroSilla = reader["NumeroSilla"] != DBNull.Value ? (int)reader["NumeroSilla"] : 0;
                                ticket.Subtotal = (decimal)reader["Subtotal"];
                                ticket.Impuesto = (decimal)reader["Impuesto"];
                                ticket.Total = (decimal)reader["Total"];
                            }
                        }
                    }

                    string queryDetalles = @"
                SELECT 
                    ProductoNombre,
                    Cantidad,
                    PrecioUnitario,
                    Subtotal,
                    Nota,
                    EsDeCombo,
                    ComboId
                FROM DetalleOrdenPersona
                WHERE OrdenPersonaId = @OrdenPersonaId
                ORDER BY DetalleOrdenPersonaId ASC";

                    using (var cmd = new SqlCommand(queryDetalles, conn))
                    {
                        cmd.Parameters.AddWithValue("@OrdenPersonaId", ordenPersonaId.Value);
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                ticket.Detalles.Add(new TicketDetalleViewModel
                                {
                                    ProductoNombre = reader["ProductoNombre"]?.ToString() ?? "Producto",
                                    Cantidad = Convert.ToInt32(reader["Cantidad"]),
                                    PrecioUnitario = (decimal)reader["PrecioUnitario"],
                                    Subtotal = (decimal)reader["Subtotal"],
                                    Nota = reader["Nota"]?.ToString(),
                                    EsDeCombo = (bool)reader["EsDeCombo"],
                                    ComboNombre = null
                                });
                            }
                        }
                    }
                }
                else
                {
                    ticket.EsCuentaSeparada = false;

                    string queryOrden = @"
                SELECT 
                    o.NumeroOrden,
                    o.MesaId,
                    o.ClienteNombre,
                    o.FechaApertura,
                    o.Subtotal,
                    o.Impuesto,
                    o.Total,
                    m.NumeroMesa
                FROM Orden o
                INNER JOIN Mesa m ON o.MesaId = m.MesaId
                WHERE o.OrdenId = @OrdenId";

                    using (var cmd = new SqlCommand(queryOrden, conn))
                    {
                        cmd.Parameters.AddWithValue("@OrdenId", ordenId);
                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                ticket.OrdenId = ordenId;
                                ticket.NumeroOrden = reader["NumeroOrden"]?.ToString() ?? "";
                                ticket.MesaId = (int)reader["MesaId"];
                                ticket.NumeroMesa = (int)reader["NumeroMesa"];
                                ticket.ClienteNombre = reader["ClienteNombre"]?.ToString() ?? "";
                                var fecha = (DateTime)reader["FechaApertura"];
                                ticket.FechaStr = fecha.ToString("dd/MM/yyyy HH:mm:ss");
                                ticket.Subtotal = (decimal)reader["Subtotal"];
                                ticket.Impuesto = (decimal)reader["Impuesto"];
                                ticket.Total = (decimal)reader["Total"];
                                ticket.NumeroSilla = 0;
                            }
                        }
                    }

                    // Primero buscar en DetalleOrden
                    string queryDetalles = @"
                SELECT 
                    ProductoNombre,
                    Cantidad,
                    PrecioUnitario,
                    Subtotal,
                    Nota,
                    EsDeCombo,
                    ComboId
                FROM DetalleOrden
                WHERE OrdenId = @OrdenId
                ORDER BY DetalleOrdenId ASC";

                    using (var cmd = new SqlCommand(queryDetalles, conn))
                    {
                        cmd.Parameters.AddWithValue("@OrdenId", ordenId);
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                ticket.Detalles.Add(new TicketDetalleViewModel
                                {
                                    ProductoNombre = reader["ProductoNombre"]?.ToString() ?? "Producto",
                                    Cantidad = Convert.ToInt32(reader["Cantidad"]),
                                    PrecioUnitario = (decimal)reader["PrecioUnitario"],
                                    Subtotal = (decimal)reader["Subtotal"],
                                    Nota = reader["Nota"]?.ToString(),
                                    EsDeCombo = (bool)reader["EsDeCombo"],
                                    ComboNombre = null
                                });
                            }
                        }
                    }

                    // Si no hay detalles en DetalleOrden, buscar en DetalleOrdenPersona
                    if (ticket.Detalles.Count == 0)
                    {
                        string queryDetallesPersona = @"
                    SELECT 
                        dop.ProductoNombre,
                        dop.Cantidad,
                        dop.PrecioUnitario,
                        dop.Subtotal,
                        dop.Nota,
                        dop.EsDeCombo,
                        dop.ComboId
                    FROM DetalleOrdenPersona dop
                    INNER JOIN OrdenPersona op ON dop.OrdenPersonaId = op.OrdenPersonaId
                    WHERE op.OrdenId = @OrdenId
                    ORDER BY dop.DetalleOrdenPersonaId ASC";

                        using (var cmd = new SqlCommand(queryDetallesPersona, conn))
                        {
                            cmd.Parameters.AddWithValue("@OrdenId", ordenId);
                            using (var reader = cmd.ExecuteReader())
                            {
                                while (reader.Read())
                                {
                                    ticket.Detalles.Add(new TicketDetalleViewModel
                                    {
                                        ProductoNombre = reader["ProductoNombre"]?.ToString() ?? "Producto",
                                        Cantidad = Convert.ToInt32(reader["Cantidad"]),
                                        PrecioUnitario = (decimal)reader["PrecioUnitario"],
                                        Subtotal = (decimal)reader["Subtotal"],
                                        Nota = reader["Nota"]?.ToString(),
                                        EsDeCombo = (bool)reader["EsDeCombo"],
                                        ComboNombre = null
                                    });
                                }
                            }
                        }
                    }
                }
            }

            return ticket;
        }

        public List<OrdenPersona> ObtenerCuentasPorOrden(int ordenId)
        {
            var cuentas = new List<OrdenPersona>();
            string query = @"
                SELECT op.OrdenPersonaId, op.SillaId, op.NombreCliente, op.Subtotal, op.Impuesto, op.Total, op.Pagado,
                       s.NumeroSilla
                FROM OrdenPersona op
                LEFT JOIN Silla s ON op.SillaId = s.SillaId
                WHERE op.OrdenId = @OrdenId";

            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@OrdenId", ordenId);
                conn.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var ordenPersonaId = (int)reader["OrdenPersonaId"];
                        var persona = new OrdenPersona
                        {
                            OrdenPersonaId = ordenPersonaId,
                            SillaId = reader["SillaId"] as int?,
                            NumeroSilla = reader["NumeroSilla"] != DBNull.Value ? (int)reader["NumeroSilla"] : 0,
                            NombreCliente = reader["NombreCliente"]?.ToString(),
                            Subtotal = (decimal)reader["Subtotal"],
                            Impuesto = (decimal)reader["Impuesto"],
                            Total = (decimal)reader["Total"],
                            Pagado = (bool)reader["Pagado"],
                            Detalles = ObtenerDetallesPorOrdenPersona(ordenPersonaId)
                        };
                        cuentas.Add(persona);
                    }
                }
            }
            return cuentas;
        }


        // ==================== HISTORIAL DE ÓRDENES ====================

        public List<HistorialOrdenViewModel> ObtenerHistorialOrdenes(DateTime? fechaInicio = null, DateTime? fechaFin = null, int? numeroMesa = null, string estado = null, string buscar = null)
        {
            var ordenes = new List<HistorialOrdenViewModel>();

            string query = @"
        SELECT 
            o.OrdenId,
            o.NumeroOrden,
            o.MesaId,
            m.NumeroMesa,
            m.Ubicacion,
            o.ClienteNombre,
            o.FechaApertura,
            o.FechaCierre,
            o.Subtotal,
            o.Impuesto,
            o.Total,
            o.Estado,
            o.UsuarioNombre,
            (SELECT COUNT(*) FROM DetalleOrden WHERE OrdenId = o.OrdenId) as CantidadProductos
        FROM Orden o
        INNER JOIN Mesa m ON o.MesaId = m.MesaId
        WHERE o.Estado IN ('Cerrada', 'Pagada')";

            var condiciones = new List<string>();
            var parametros = new List<SqlParameter>();

            if (fechaInicio.HasValue)
            {
                condiciones.Add("o.FechaApertura >= @FechaInicio");
                parametros.Add(new SqlParameter("@FechaInicio", fechaInicio.Value));
            }

            if (fechaFin.HasValue)
            {
                condiciones.Add("o.FechaApertura <= @FechaFin");
                parametros.Add(new SqlParameter("@FechaFin", fechaFin.Value.AddDays(1)));
            }

            if (numeroMesa.HasValue && numeroMesa.Value > 0)
            {
                condiciones.Add("m.NumeroMesa = @NumeroMesa");
                parametros.Add(new SqlParameter("@NumeroMesa", numeroMesa.Value));
            }

            if (!string.IsNullOrEmpty(estado))
            {
                condiciones.Add("o.Estado = @Estado");
                parametros.Add(new SqlParameter("@Estado", estado));
            }

            if (!string.IsNullOrEmpty(buscar))
            {
                condiciones.Add("(o.NumeroOrden LIKE @Buscar OR o.ClienteNombre LIKE @Buscar OR CAST(m.NumeroMesa AS VARCHAR) LIKE @Buscar)");
                parametros.Add(new SqlParameter("@Buscar", "%" + buscar + "%"));
            }

            if (condiciones.Any())
            {
                query += " AND " + string.Join(" AND ", condiciones);
            }

            query += " ORDER BY o.FechaApertura DESC";

            using (var conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                using (var cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddRange(parametros.ToArray());
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var orden = new HistorialOrdenViewModel
                            {
                                OrdenId = (int)reader["OrdenId"],
                                NumeroOrden = reader["NumeroOrden"].ToString(),
                                NumeroMesa = (int)reader["NumeroMesa"],
                                UbicacionMesa = reader["Ubicacion"].ToString(),
                                ClienteNombre = reader["ClienteNombre"]?.ToString() ?? "Consumo en mesa",
                                FechaApertura = (DateTime)reader["FechaApertura"],
                                FechaCierre = reader["FechaCierre"] != DBNull.Value ? (DateTime?)reader["FechaCierre"] : null,
                                Subtotal = (decimal)reader["Subtotal"],
                                Impuesto = (decimal)reader["Impuesto"],
                                Total = (decimal)reader["Total"],
                                Estado = reader["Estado"].ToString(),
                                UsuarioNombre = reader["UsuarioNombre"]?.ToString() ?? "",
                                CantidadProductos = (int)reader["CantidadProductos"],
                                Detalles = new List<HistorialDetalleViewModel>(),
                                Sillas = new List<HistorialSillaViewModel>()
                            };
                            ordenes.Add(orden);
                        }
                    }
                }

                // Cargar detalles y sillas para cada orden
                foreach (var orden in ordenes)
                {
                    orden.Detalles = ObtenerDetallesHistorial(orden.OrdenId);
                    orden.Sillas = ObtenerSillasHistorial(orden.OrdenId);
                }
            }

            return ordenes;
        }

        public List<HistorialDetalleViewModel> ObtenerDetallesHistorial(int ordenId)
        {
            var detalles = new List<HistorialDetalleViewModel>();

            // Primero buscar en DetalleOrden (para órdenes normales)
            string queryDetalleOrden = @"
        SELECT 
            ProductoNombre,
            Cantidad,
            PrecioUnitario,
            Subtotal,
            Nota,
            EsDeCombo,
            ComboId
        FROM DetalleOrden
        WHERE OrdenId = @OrdenId
        ORDER BY DetalleOrdenId ASC";

            using (var conn = new SqlConnection(_connectionString))
            {
                conn.Open();

                // Buscar en DetalleOrden
                using (var cmd = new SqlCommand(queryDetalleOrden, conn))
                {
                    cmd.Parameters.AddWithValue("@OrdenId", ordenId);
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            detalles.Add(new HistorialDetalleViewModel
                            {
                                ProductoNombre = reader["ProductoNombre"]?.ToString() ?? "Producto sin nombre",
                                Cantidad = (decimal)reader["Cantidad"],
                                PrecioUnitario = (decimal)reader["PrecioUnitario"],
                                Subtotal = (decimal)reader["Subtotal"],
                                Nota = reader["Nota"]?.ToString(),
                                EsDeCombo = (bool)reader["EsDeCombo"],
                                ComboNombre = null
                            });
                        }
                    }
                }

                // Si no hay detalles en DetalleOrden, buscar en DetalleOrdenPersona
                if (detalles.Count == 0)
                {
                    string queryDetallePersona = @"
                SELECT 
                    dop.ProductoNombre,
                    dop.Cantidad,
                    dop.PrecioUnitario,
                    dop.Subtotal,
                    dop.Nota,
                    dop.EsDeCombo,
                    dop.ComboId
                FROM DetalleOrdenPersona dop
                INNER JOIN OrdenPersona op ON dop.OrdenPersonaId = op.OrdenPersonaId
                WHERE op.OrdenId = @OrdenId
                ORDER BY dop.DetalleOrdenPersonaId ASC";

                    using (var cmd = new SqlCommand(queryDetallePersona, conn))
                    {
                        cmd.Parameters.AddWithValue("@OrdenId", ordenId);
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                detalles.Add(new HistorialDetalleViewModel
                                {
                                    ProductoNombre = reader["ProductoNombre"]?.ToString() ?? "Producto sin nombre",
                                    Cantidad = (decimal)reader["Cantidad"],
                                    PrecioUnitario = (decimal)reader["PrecioUnitario"],
                                    Subtotal = (decimal)reader["Subtotal"],
                                    Nota = reader["Nota"]?.ToString(),
                                    EsDeCombo = (bool)reader["EsDeCombo"],
                                    ComboNombre = null
                                });
                            }
                        }
                    }
                }
            }

            return detalles;
        }

        public List<HistorialSillaViewModel> ObtenerSillasHistorial(int ordenId)
        {
            var sillas = new List<HistorialSillaViewModel>();
            string query = @"
        SELECT 
            s.NumeroSilla,
            op.NombreCliente,
            op.Total,
            op.Pagado
        FROM OrdenPersona op
        LEFT JOIN Silla s ON op.SillaId = s.SillaId
        WHERE op.OrdenId = @OrdenId
        ORDER BY s.NumeroSilla ASC";

            using (var conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                using (var cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@OrdenId", ordenId);
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            sillas.Add(new HistorialSillaViewModel
                            {
                                NumeroSilla = reader["NumeroSilla"] != DBNull.Value ? (int)reader["NumeroSilla"] : 0,
                                NombreCliente = reader["NombreCliente"]?.ToString() ?? "",
                                Total = (decimal)reader["Total"],
                                Pagado = (bool)reader["Pagado"]
                            });
                        }
                    }
                }
            }
            return sillas;
        }

        public ResumenEstadisticasViewModel ObtenerResumenEstadisticas(DateTime? fechaInicio = null, DateTime? fechaFin = null)
        {
            var resumen = new ResumenEstadisticasViewModel();

            using (var conn = new SqlConnection(_connectionString))
            {
                conn.Open();

                // Construir condiciones de fecha
                string fechaCondition = "";
                if (fechaInicio.HasValue)
                {
                    fechaCondition += " AND FechaApertura >= '" + fechaInicio.Value.ToString("yyyy-MM-dd") + "'";
                }
                if (fechaFin.HasValue)
                {
                    fechaCondition += " AND FechaApertura <= '" + fechaFin.Value.AddDays(1).ToString("yyyy-MM-dd") + "'";
                }

                // Consulta para obtener todas las estadísticas en una sola ejecución
                string query = @"
            SELECT 
                (SELECT COUNT(*) FROM Orden WHERE Estado IN ('Cerrada', 'Pagada')" + fechaCondition + @") as TotalOrdenes,
                (SELECT ISNULL(SUM(Total), 0) FROM Orden WHERE Estado IN ('Cerrada', 'Pagada')" + fechaCondition + @") as TotalVentas,
                (SELECT ISNULL(AVG(Total), 0) FROM Orden WHERE Estado IN ('Cerrada', 'Pagada')" + fechaCondition + @") as PromedioVenta,
                (SELECT COUNT(DISTINCT MesaId) FROM Orden WHERE Estado IN ('Cerrada', 'Pagada')" + fechaCondition + @") as MesasAtendidas,
                (SELECT COUNT(*) FROM DetalleOrden d INNER JOIN Orden o ON d.OrdenId = o.OrdenId WHERE o.Estado IN ('Cerrada', 'Pagada')" + fechaCondition + @") as TotalProductosVendidos";

                using (var cmd = new SqlCommand(query, conn))
                {
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            resumen.TotalOrdenes = reader["TotalOrdenes"] != DBNull.Value ? Convert.ToInt32(reader["TotalOrdenes"]) : 0;
                            resumen.TotalVentas = reader["TotalVentas"] != DBNull.Value ? Convert.ToDecimal(reader["TotalVentas"]) : 0;
                            resumen.PromedioVenta = reader["PromedioVenta"] != DBNull.Value ? Convert.ToDecimal(reader["PromedioVenta"]) : 0;
                            resumen.MesasAtendidas = reader["MesasAtendidas"] != DBNull.Value ? Convert.ToInt32(reader["MesasAtendidas"]) : 0;
                            resumen.TotalProductosVendidos = reader["TotalProductosVendidos"] != DBNull.Value ? Convert.ToInt32(reader["TotalProductosVendidos"]) : 0;
                        }
                    }
                }
            }

            return resumen;
        }

        public HistorialOrdenViewModel ObtenerHistorialOrdenPorId(int ordenId)
        {
            var orden = new HistorialOrdenViewModel();

            string query = @"
        SELECT 
            o.OrdenId,
            o.NumeroOrden,
            o.MesaId,
            m.NumeroMesa,
            m.Ubicacion,
            o.ClienteNombre,
            o.FechaApertura,
            o.FechaCierre,
            o.Subtotal,
            o.Impuesto,
            o.Total,
            o.Estado,
            o.UsuarioNombre
        FROM Orden o
        INNER JOIN Mesa m ON o.MesaId = m.MesaId
        WHERE o.OrdenId = @OrdenId";

            using (var conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                using (var cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@OrdenId", ordenId);
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            orden = new HistorialOrdenViewModel
                            {
                                OrdenId = (int)reader["OrdenId"],
                                NumeroOrden = reader["NumeroOrden"].ToString(),
                                NumeroMesa = (int)reader["NumeroMesa"],
                                UbicacionMesa = reader["Ubicacion"].ToString(),
                                ClienteNombre = reader["ClienteNombre"]?.ToString() ?? "Consumo en mesa",
                                FechaApertura = (DateTime)reader["FechaApertura"],
                                FechaCierre = reader["FechaCierre"] != DBNull.Value ? (DateTime?)reader["FechaCierre"] : null,
                                Subtotal = (decimal)reader["Subtotal"],
                                Impuesto = (decimal)reader["Impuesto"],
                                Total = (decimal)reader["Total"],
                                Estado = reader["Estado"].ToString(),
                                UsuarioNombre = reader["UsuarioNombre"]?.ToString() ?? "",
                                Detalles = new List<HistorialDetalleViewModel>(),
                                Sillas = new List<HistorialSillaViewModel>()
                            };
                        }
                    }
                }

                // Cargar detalles (buscará en ambas tablas)
                orden.Detalles = ObtenerDetallesHistorial(ordenId);

                // Calcular cantidad de productos
                orden.CantidadProductos = orden.Detalles.Sum(d => (int)d.Cantidad);

                // Cargar sillas
                orden.Sillas = ObtenerSillasHistorial(ordenId);
            }

            return orden;
        }
    }
}