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

        // ==================== MESAS ====================
        public List<Mesa> ObtenerMesasActivas()
        {
            var mesas = new List<Mesa>();
            string query = @"
                SELECT MesaId, NumeroMesa, Capacidad, Ubicacion, Estado 
                FROM Mesa 
                WHERE Activo = 1 
                ORDER BY NumeroMesa";

            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand(query, conn))
            {
                conn.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        mesas.Add(new Mesa
                        {
                            MesaId = (int)reader["MesaId"],
                            NumeroMesa = (int)reader["NumeroMesa"],
                            Capacidad = (int)reader["Capacidad"],
                            Ubicacion = reader["Ubicacion"]?.ToString() ?? "",
                            Estado = reader["Estado"]?.ToString() ?? "Disponible"
                        });
                    }
                }
            }
            return mesas;
        }

        public Mesa ObtenerMesaPorId(int mesaId)
        {
            string query = "SELECT MesaId, NumeroMesa, Ubicacion, Estado FROM Mesa WHERE MesaId = @MesaId";
            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@MesaId", mesaId);
                conn.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        return new Mesa
                        {
                            MesaId = (int)reader["MesaId"],
                            NumeroMesa = (int)reader["NumeroMesa"],
                            Ubicacion = reader["Ubicacion"]?.ToString() ?? "",
                            Estado = reader["Estado"]?.ToString() ?? "Disponible"
                        };
                    }
                }
            }
            return null;
        }

        public void ActualizarEstadoMesa(int mesaId, string estado, SqlConnection conn = null, SqlTransaction transaction = null)
        {
            string query = "UPDATE Mesa SET Estado = @Estado WHERE MesaId = @MesaId";
            Action<SqlConnection, SqlTransaction> action = (connection, transactionParam) =>
            {
                using (var cmd = new SqlCommand(query, connection, transactionParam))
                {
                    cmd.Parameters.AddWithValue("@MesaId", mesaId);
                    cmd.Parameters.AddWithValue("@Estado", estado);
                    cmd.ExecuteNonQuery();
                }
            };

            if (conn != null && transaction != null)
            {
                action(conn, transaction);
            }
            else
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    connection.Open();
                    action(connection, null);
                }
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

        // ==================== ÓRDENES ====================
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

        public Orden ObtenerOrdenActivaPorMesa(int mesaId)
        {
            string query = @"
                SELECT TOP 1 OrdenId, NumeroOrden, ClienteNombre, FechaApertura, Subtotal, Impuesto, Total, Observaciones
                FROM Orden 
                WHERE MesaId = @MesaId AND Estado = 'Abierta'
                ORDER BY OrdenId DESC";

            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@MesaId", mesaId);
                conn.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        return new Orden
                        {
                            OrdenId = (int)reader["OrdenId"],
                            NumeroOrden = reader["NumeroOrden"]?.ToString(),
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

        public void CerrarOrden(int ordenId)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                using (var transaction = conn.BeginTransaction())
                {
                    try
                    {
                        int mesaId = 0;
                        string getMesaQuery = "SELECT MesaId FROM Orden WHERE OrdenId = @OrdenId";
                        using (var cmd = new SqlCommand(getMesaQuery, conn, transaction))
                        {
                            cmd.Parameters.AddWithValue("@OrdenId", ordenId);
                            var result = cmd.ExecuteScalar();
                            if (result != null) mesaId = (int)result;
                        }

                        string updateOrden = @"
                            UPDATE Orden 
                            SET Estado = 'Cerrada', FechaCierre = GETDATE() 
                            WHERE OrdenId = @OrdenId";
                        using (var cmd = new SqlCommand(updateOrden, conn, transaction))
                        {
                            cmd.Parameters.AddWithValue("@OrdenId", ordenId);
                            cmd.ExecuteNonQuery();
                        }

                        ActualizarEstadoMesa(mesaId, "Disponible", conn, transaction);
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

        // ==================== CUENTAS SEPARADAS ====================
        public int CrearOrdenPersona(int ordenId, string nombreCliente)
        {
            string query = @"
                INSERT INTO OrdenPersona (OrdenId, NombreCliente, Subtotal, Impuesto, Total, Pagado, FechaCreacion)
                VALUES (@OrdenId, @NombreCliente, 0, 0, 0, 0, GETDATE());
                SELECT SCOPE_IDENTITY();";

            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@OrdenId", ordenId);
                cmd.Parameters.AddWithValue("@NombreCliente", nombreCliente ?? (object)DBNull.Value);
                conn.Open();
                return Convert.ToInt32(cmd.ExecuteScalar());
            }
        }

        public void AgregarDetalleOrdenPersona(int ordenPersonaId, OrdenModels.DetalleOrden detalle)
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
                int rowsAffected = cmd.ExecuteNonQuery();

                System.Diagnostics.Debug.WriteLine($"Agregado detalle - PersonaId: {ordenPersonaId}, Producto: {detalle.ProductoNombre}, Subtotal: {detalle.Subtotal}, Filas: {rowsAffected}");
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

        public List<OrdenPersona> ObtenerCuentasPorOrden(int ordenId)
        {
            var cuentas = new List<OrdenPersona>();
            string query = "SELECT OrdenPersonaId, NombreCliente, Subtotal, Impuesto, Total, Pagado FROM OrdenPersona WHERE OrdenId = @OrdenId";

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

        private List<OrdenModels.DetalleOrdenPersona> ObtenerDetallesPorOrdenPersona(int ordenPersonaId)
        {
            var detalles = new List<OrdenModels.DetalleOrdenPersona>();
            string query = @"
        SELECT ProductoId, ProductoNombre, Cantidad, PrecioUnitario, Subtotal, EsDeCombo, ComboId, Nota
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
                        detalles.Add(new OrdenModels.DetalleOrdenPersona
                        {
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

        // ==================== TICKETS ====================
        public TicketViewModel GenerarTicket(int ordenId, int? ordenPersonaId = null)
        {
            var ticket = new TicketViewModel
            {
                Detalles = new List<TicketDetalleViewModel>()
            };

            try
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    conn.Open();

                    if (ordenPersonaId.HasValue && ordenPersonaId.Value > 0)
                    {
                        // ========== CUENTA SEPARADA ==========
                        ticket.EsCuentaSeparada = true;

                        // Obtener datos de la persona y orden
                        string queryPersona = @"
                    SELECT 
                        op.NombreCliente,
                        op.Subtotal,
                        op.Impuesto,
                        op.Total,
                        o.NumeroOrden,
                        o.MesaId,
                        o.ClienteNombre,
                        o.FechaApertura as Fecha,
                        m.NumeroMesa
                    FROM OrdenPersona op
                    INNER JOIN Orden o ON op.OrdenId = o.OrdenId
                    INNER JOIN Mesa m ON o.MesaId = m.MesaId
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
                                    ticket.Fecha = (DateTime)reader["Fecha"];
                                    ticket.NombrePersona = reader["NombreCliente"]?.ToString() ?? "";
                                    ticket.Subtotal = (decimal)reader["Subtotal"];
                                    ticket.Impuesto = (decimal)reader["Impuesto"];
                                    ticket.Total = (decimal)reader["Total"];
                                }
                            }
                        }

                        // Obtener detalles de la persona
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
                        // ========== ORDEN NORMAL ==========
                        ticket.EsCuentaSeparada = false;

                        string queryOrden = @"
                    SELECT 
                        o.NumeroOrden,
                        o.MesaId,
                        o.ClienteNombre,
                        o.FechaApertura as Fecha,
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
                                    ticket.Fecha = (DateTime)reader["Fecha"];
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
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error en GenerarTicket: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"StackTrace: {ex.StackTrace}");
            }

            return ticket;
        }



        // ==================== MÉTODOS DE UTILIDAD ====================
        public string GenerarNumeroOrden()
        {
            return $"ORD-{DateTime.Now:yyyyMMddHHmmss}";
        }

        public List<VentaModels.LoteDisponible> ObtenerLotesPorProducto(int productoId)
        {
            string query = @"
                SELECT 
                    l.LoteId,
                    ISNULL(l.NumeroLoteInterno, '') AS NumeroLote,
                    ISNULL(l.CantidadActual, 0) AS Cantidad,
                    ISNULL(l.PrecioUnitario, 0) AS PrecioUnitario,
                    l.FechaIngreso,
                    l.FechaVencimiento
                FROM Lote l
                WHERE l.ProductoId = @ProductoId
                  AND l.Activo = 1
                  AND l.Estado = 'Activo'
                  AND l.CantidadActual > 0
                ORDER BY l.FechaIngreso ASC";

            var lotes = new List<VentaModels.LoteDisponible>();

            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@ProductoId", productoId);
                conn.Open();

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        lotes.Add(new VentaModels.LoteDisponible
                        {
                            LoteId = Convert.ToInt32(reader["LoteId"]),
                            NumeroLote = reader["NumeroLote"]?.ToString()?.Trim() ?? "",
                            Cantidad = reader["Cantidad"] != DBNull.Value ? Convert.ToDecimal(reader["Cantidad"]) : 0,
                            PrecioUnitario = reader["PrecioUnitario"] != DBNull.Value ? Convert.ToDecimal(reader["PrecioUnitario"]) : 0,
                            FechaIngreso = reader["FechaIngreso"] != DBNull.Value ? Convert.ToDateTime(reader["FechaIngreso"]) : DateTime.Now,
                            FechaVencimiento = reader["FechaVencimiento"] != DBNull.Value ? (DateTime?)reader["FechaVencimiento"] : null
                        });
                    }
                }
            }

            return lotes;
        }
    }
}