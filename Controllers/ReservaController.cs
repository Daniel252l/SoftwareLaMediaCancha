using LaMediaCancha.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;
using System.Web.Mvc;

namespace LaMediaCancha.Controllers
{
    public class ReservaController : Controller
    {
        private readonly string _connectionString;

        public ReservaController()
        {
            _connectionString = ConfigurationManager.ConnectionStrings["LaMediaCanchaDB"].ConnectionString;
        }

        // GET: Reserva/Index
        public ActionResult Index(DateTime? fecha, string estado = "")
        {
            if (Session["UserRol"] == null)
                return RedirectToAction("Login", "Account");

            if (!fecha.HasValue)
                fecha = DateTime.Now.Date;

            ViewBag.FechaSeleccionada = fecha.Value.ToString("yyyy-MM-dd");
            ViewBag.EstadoSeleccionado = estado;

            var reservas = new List<ReservaModels.Reserva>();

            string query = @"
                SELECT 
                    r.ReservaId,
                    r.CodigoReserva,
                    r.ClienteNombre,
                    r.ClienteTelefono,
                    r.ClienteEmail,
                    r.FechaReserva,
                    r.HoraReserva,
                    r.NumeroPersonas,
                    r.MesaAsignadaId,
                    m.NumeroMesa AS MesaNumero,
                    r.Observaciones,
                    r.Estado,
                    r.FechaCreacion,
                    u.Nombre AS UsuarioNombre
                FROM Reserva r
                LEFT JOIN Mesa m ON r.MesaAsignadaId = m.MesaId
                LEFT JOIN Usuario u ON r.UsuarioId = u.UsuarioId
                WHERE CAST(r.FechaReserva AS DATE) = @Fecha
                  AND (@Estado = '' OR r.Estado = @Estado)
                ORDER BY r.HoraReserva ASC";

            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@Fecha", fecha.Value);
                cmd.Parameters.AddWithValue("@Estado", estado);
                conn.Open();

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        reservas.Add(new ReservaModels.Reserva
                        {
                            ReservaId = (int)reader["ReservaId"],
                            CodigoReserva = reader["CodigoReserva"].ToString(),
                            ClienteNombre = reader["ClienteNombre"].ToString(),
                            ClienteTelefono = reader["ClienteTelefono"]?.ToString(),
                            ClienteEmail = reader["ClienteEmail"]?.ToString(),
                            FechaReserva = (DateTime)reader["FechaReserva"],
                            HoraReserva = reader["HoraReserva"] != DBNull.Value ? (TimeSpan)reader["HoraReserva"] : TimeSpan.Zero,
                            NumeroPersonas = (int)reader["NumeroPersonas"],
                            MesaAsignadaId = reader["MesaAsignadaId"] as int?,
                            MesaNumero = reader["MesaNumero"] as int?,
                            Observaciones = reader["Observaciones"]?.ToString(),
                            Estado = reader["Estado"].ToString(),
                            FechaCreacion = (DateTime)reader["FechaCreacion"],
                            UsuarioNombre = reader["UsuarioNombre"]?.ToString()
                        });
                    }
                }
            }

            return View(reservas);
        }

        // GET: Reserva/Detalle/5
        public ActionResult Detalle(int id)
        {
            if (Session["UserRol"] == null)
                return RedirectToAction("Login", "Account");

            ReservaModels.Reserva reserva = null;

            string query = @"
                SELECT 
                    r.ReservaId,
                    r.CodigoReserva,
                    r.ClienteNombre,
                    r.ClienteTelefono,
                    r.ClienteEmail,
                    r.FechaReserva,
                    r.HoraReserva,
                    r.NumeroPersonas,
                    r.MesaAsignadaId,
                    m.NumeroMesa AS MesaNumero,
                    r.Observaciones,
                    r.Estado,
                    r.FechaCreacion,
                    u.Nombre AS UsuarioNombre
                FROM Reserva r
                LEFT JOIN Mesa m ON r.MesaAsignadaId = m.MesaId
                LEFT JOIN Usuario u ON r.UsuarioId = u.UsuarioId
                WHERE r.ReservaId = @ReservaId";

            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@ReservaId", id);
                conn.Open();

                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        reserva = new ReservaModels.Reserva
                        {
                            ReservaId = (int)reader["ReservaId"],
                            CodigoReserva = reader["CodigoReserva"].ToString(),
                            ClienteNombre = reader["ClienteNombre"].ToString(),
                            ClienteTelefono = reader["ClienteTelefono"]?.ToString(),
                            ClienteEmail = reader["ClienteEmail"]?.ToString(),
                            FechaReserva = (DateTime)reader["FechaReserva"],
                            HoraReserva = reader["HoraReserva"] != DBNull.Value ? (TimeSpan)reader["HoraReserva"] : TimeSpan.Zero,
                            NumeroPersonas = (int)reader["NumeroPersonas"],
                            MesaAsignadaId = reader["MesaAsignadaId"] as int?,
                            MesaNumero = reader["MesaNumero"] as int?,
                            Observaciones = reader["Observaciones"]?.ToString(),
                            Estado = reader["Estado"].ToString(),
                            FechaCreacion = (DateTime)reader["FechaCreacion"],
                            UsuarioNombre = reader["UsuarioNombre"]?.ToString()
                        };
                    }
                }
            }

            if (reserva == null)
                return HttpNotFound();

            return View(reserva);
        }

        // POST: Reserva/CambiarEstado
        [HttpPost]
        public JsonResult CambiarEstado(int id, string estado)
        {
            if (Session["UserRol"] == null)
                return Json(new { success = false, message = "Sesión expirada" });

            try
            {
                string query = "UPDATE Reserva SET Estado = @Estado WHERE ReservaId = @ReservaId";
                using (var conn = new SqlConnection(_connectionString))
                using (var cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@ReservaId", id);
                    cmd.Parameters.AddWithValue("@Estado", estado);
                    conn.Open();
                    cmd.ExecuteNonQuery();
                }

                string mensaje = estado == "Confirmada" ? "Reserva confirmada exitosamente" :
                                estado == "Cancelada" ? "Reserva cancelada exitosamente" :
                                "Estado actualizado correctamente";

                return Json(new { success = true, message = mensaje });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
    }
}