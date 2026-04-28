using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Configuration;
using System.Web.Mvc;
using LaMediaCancha.Models;

namespace LaMediaCancha.Controllers
{
    public class BitacoraController : Controller
    {
        private readonly string _connectionString;

        public BitacoraController()
        {
            _connectionString = ConfigurationManager.ConnectionStrings["LaMediaCanchaDB"].ConnectionString;
        }

        public ActionResult Index()
        {
            if (Session["UserRol"] == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var registros = new List<Bitacora>();

            string query = @"
                SELECT BitacoraId, UsuarioId, UsuarioNombre, Accion, Tabla, Detalle, Fecha
                FROM Bitacora
                ORDER BY Fecha DESC";

            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand(query, conn))
            {
                conn.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        registros.Add(new Bitacora
                        {
                            BitacoraId = (int)reader["BitacoraId"],
                            UsuarioId = reader["UsuarioId"] != DBNull.Value ? (int)reader["UsuarioId"] : 0,
                            UsuarioNombre = reader["UsuarioNombre"].ToString(),
                            Accion = reader["Accion"].ToString(),
                            Tabla = reader["Tabla"].ToString(),
                            Detalle = reader["Detalle"]?.ToString(),
                            Fecha = (DateTime)reader["Fecha"]
                        });
                    }
                }
            }

            return View(registros);
        }

        public ActionResult Detalle(int id)
        {
            if (Session["UserRol"] == null)
            {
                return RedirectToAction("Login", "Account");
            }

            Bitacora registro = null;

            string query = "SELECT BitacoraId, UsuarioId, UsuarioNombre, Accion, Tabla, Detalle, Fecha FROM Bitacora WHERE BitacoraId = @Id";

            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@Id", id);
                conn.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        registro = new Bitacora
                        {
                            BitacoraId = (int)reader["BitacoraId"],
                            UsuarioId = reader["UsuarioId"] != DBNull.Value ? (int)reader["UsuarioId"] : 0,
                            UsuarioNombre = reader["UsuarioNombre"].ToString(),
                            Accion = reader["Accion"].ToString(),
                            Tabla = reader["Tabla"].ToString(),
                            Detalle = reader["Detalle"]?.ToString(),
                            Fecha = (DateTime)reader["Fecha"]
                        };
                    }
                }
            }

            if (registro == null)
            {
                return HttpNotFound();
            }

            return View(registro);
        }
    }
}