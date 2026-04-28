using LaMediaCancha.Models;
using LaMediaCancha.Services;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Web.Mvc;

namespace LaMediaCancha.Controllers
{
    public class ProveedorController : Controller
    {
        private readonly string _connectionString;

        public ProveedorController()
        {
            _connectionString = ConfigurationManager.ConnectionStrings["LaMediaCanchaDB"].ConnectionString;
        }

        // GET: Proveedor/Index
        public ActionResult Index()
        {
            if (Session["UserRol"] == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var proveedores = new List<Proveedor>();

            string query = @"
                SELECT 
                    p.ProveedorId,
                    p.Nit,
                    p.RazonSocial,
                    p.Contacto,
                    p.Telefono,
                    p.Correo,
                    p.Direccion,
                    p.Activo,
                    p.MantenimientoId,
                    m.Nombre AS PoliticaNombre,
                    m.Valor AS DiasMaximosDevolucion,
                    p.PoliticaDevolucion,
                    pe.Nombres,
                    pe.Apellidos
                FROM Proveedor p
                INNER JOIN Persona pe ON p.PersonaId = pe.PersonaId
                LEFT JOIN Mantenimiento m ON p.MantenimientoId = m.MantenimientoId AND m.Activo = 1
                ORDER BY p.RazonSocial";

            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand(query, conn))
            {
                conn.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        proveedores.Add(new Proveedor
                        {
                            ProveedorId = (int)reader["ProveedorId"],
                            Nit = reader["Nit"].ToString(),
                            RazonSocial = reader["RazonSocial"].ToString(),
                            Contacto = reader["Contacto"]?.ToString(),
                            Telefono = reader["Telefono"]?.ToString(),
                            Correo = reader["Correo"]?.ToString(),
                            Direccion = reader["Direccion"]?.ToString(),
                            Activo = (bool)reader["Activo"],
                            MantenimientoId = reader["MantenimientoId"] != DBNull.Value ? (int?)reader["MantenimientoId"] : null,
                            PoliticaNombre = reader["PoliticaNombre"]?.ToString(),
                            DiasMaximosDevolucion = reader["DiasMaximosDevolucion"] != DBNull.Value ? (int)reader["DiasMaximosDevolucion"] : 10,
                            PoliticaDevolucion = reader["PoliticaDevolucion"]?.ToString(),
                            Nombres = reader["Nombres"].ToString(),
                            Apellidos = reader["Apellidos"].ToString()
                        });
                    }
                }
            }

            return View(proveedores);
        }

        // GET: Proveedor/Crear
        public ActionResult Crear()
        {
            if (Session["UserRol"] == null)
            {
                return RedirectToAction("Login", "Account");
            }

            ViewBag.Politicas = ObtenerPoliticas();
            return View(new Proveedor());
        }

        // POST: Proveedor/Crear
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Crear(Proveedor proveedor)
        {
            if (Session["UserRol"] == null)
            {
                return RedirectToAction("Login", "Account");
            }

            if (ModelState.IsValid)
            {
                // Verificar si el NIT ya existe
                string checkQuery = "SELECT COUNT(*) FROM Proveedor WHERE Nit = @Nit";
                using (var conn = new SqlConnection(_connectionString))
                using (var cmd = new SqlCommand(checkQuery, conn))
                {
                    cmd.Parameters.AddWithValue("@Nit", proveedor.Nit);
                    conn.Open();
                    int existe = (int)cmd.ExecuteScalar();
                    if (existe > 0)
                    {
                        ModelState.AddModelError("Nit", "Ya existe un proveedor con este NIT");
                        ViewBag.Politicas = ObtenerPoliticas();
                        return View(proveedor);
                    }
                }

                string query = @"
                    INSERT INTO Persona (Nombres, Apellidos, Telefono, Correo, Direccion, Activo)
                    VALUES (@Nombres, @Apellidos, @Telefono, @Correo, @Direccion, 1);
                    SELECT SCOPE_IDENTITY();";

                int personaId;
                using (var conn = new SqlConnection(_connectionString))
                using (var cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@Nombres", proveedor.Nombres);
                    cmd.Parameters.AddWithValue("@Apellidos", proveedor.Apellidos);
                    cmd.Parameters.AddWithValue("@Telefono", (object)proveedor.Telefono ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Correo", (object)proveedor.Correo ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Direccion", (object)proveedor.Direccion ?? DBNull.Value);
                    conn.Open();
                    personaId = Convert.ToInt32(cmd.ExecuteScalar());
                }

                string queryProveedor = @"
                    INSERT INTO Proveedor (PersonaId, Nit, RazonSocial, Contacto, Telefono, Correo, Direccion, MantenimientoId, PoliticaDevolucion, Activo)
                    VALUES (@PersonaId, @Nit, @RazonSocial, @Contacto, @Telefono, @Correo, @Direccion, @MantenimientoId, @Politica, 1)";

                using (var conn = new SqlConnection(_connectionString))
                using (var cmd = new SqlCommand(queryProveedor, conn))
                {
                    cmd.Parameters.AddWithValue("@PersonaId", personaId);
                    cmd.Parameters.AddWithValue("@Nit", proveedor.Nit);
                    cmd.Parameters.AddWithValue("@RazonSocial", proveedor.RazonSocial);
                    cmd.Parameters.AddWithValue("@Contacto", (object)proveedor.Contacto ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Telefono", (object)proveedor.Telefono ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Correo", (object)proveedor.Correo ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Direccion", (object)proveedor.Direccion ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@MantenimientoId", proveedor.MantenimientoId.HasValue ? (object)proveedor.MantenimientoId.Value : DBNull.Value);
                    cmd.Parameters.AddWithValue("@Politica", (object)proveedor.PoliticaDevolucion ?? DBNull.Value);
                    conn.Open();
                    cmd.ExecuteNonQuery();
                }

                TempData["Success"] = "Proveedor creado exitosamente";
                return RedirectToAction("Index");
            }

            ViewBag.Politicas = ObtenerPoliticas();
            return View(proveedor);
        }

        // GET: Proveedor/Editar/5
        public ActionResult Editar(int id)
        {
            if (Session["UserRol"] == null)
            {
                return RedirectToAction("Login", "Account");
            }

            Proveedor proveedor = null;

            string query = @"
                SELECT 
                    p.ProveedorId,
                    p.Nit,
                    p.RazonSocial,
                    p.Contacto,
                    p.Telefono,
                    p.Correo,
                    p.Direccion,
                    p.Activo,
                    p.MantenimientoId,
                    p.PoliticaDevolucion,
                    pe.PersonaId,
                    pe.Nombres,
                    pe.Apellidos
                FROM Proveedor p
                INNER JOIN Persona pe ON p.PersonaId = pe.PersonaId
                WHERE p.ProveedorId = @ProveedorId";

            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@ProveedorId", id);
                conn.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        proveedor = new Proveedor
                        {
                            ProveedorId = (int)reader["ProveedorId"],
                            PersonaId = (int)reader["PersonaId"],
                            Nit = reader["Nit"].ToString(),
                            RazonSocial = reader["RazonSocial"].ToString(),
                            Contacto = reader["Contacto"]?.ToString(),
                            Telefono = reader["Telefono"]?.ToString(),
                            Correo = reader["Correo"]?.ToString(),
                            Direccion = reader["Direccion"]?.ToString(),
                            Activo = (bool)reader["Activo"],
                            MantenimientoId = reader["MantenimientoId"] != DBNull.Value ? (int?)reader["MantenimientoId"] : null,
                            PoliticaDevolucion = reader["PoliticaDevolucion"]?.ToString(),
                            Nombres = reader["Nombres"].ToString(),
                            Apellidos = reader["Apellidos"].ToString()
                        };
                    }
                }
            }

            if (proveedor == null)
            {
                return HttpNotFound();
            }

            ViewBag.Politicas = ObtenerPoliticas(proveedor.MantenimientoId);
            return View(proveedor);
        }

        // POST: Proveedor/Editar/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Editar(Proveedor proveedor)
        {
            if (Session["UserRol"] == null)
            {
                return RedirectToAction("Login", "Account");
            }

            if (ModelState.IsValid)
            {
                // Verificar si el NIT ya existe (excluyendo el actual)
                string checkQuery = "SELECT COUNT(*) FROM Proveedor WHERE Nit = @Nit AND ProveedorId != @ProveedorId";
                using (var conn = new SqlConnection(_connectionString))
                using (var cmd = new SqlCommand(checkQuery, conn))
                {
                    cmd.Parameters.AddWithValue("@Nit", proveedor.Nit);
                    cmd.Parameters.AddWithValue("@ProveedorId", proveedor.ProveedorId);
                    conn.Open();
                    int existe = (int)cmd.ExecuteScalar();
                    if (existe > 0)
                    {
                        ModelState.AddModelError("Nit", "Ya existe otro proveedor con este NIT");
                        ViewBag.Politicas = ObtenerPoliticas(proveedor.MantenimientoId);
                        return View(proveedor);
                    }
                }

                string updatePersona = @"
                    UPDATE Persona 
                    SET Nombres = @Nombres, 
                        Apellidos = @Apellidos, 
                        Telefono = @Telefono, 
                        Correo = @Correo, 
                        Direccion = @Direccion
                    WHERE PersonaId = @PersonaId";

                using (var conn = new SqlConnection(_connectionString))
                using (var cmd = new SqlCommand(updatePersona, conn))
                {
                    cmd.Parameters.AddWithValue("@PersonaId", proveedor.PersonaId);
                    cmd.Parameters.AddWithValue("@Nombres", proveedor.Nombres);
                    cmd.Parameters.AddWithValue("@Apellidos", proveedor.Apellidos);
                    cmd.Parameters.AddWithValue("@Telefono", (object)proveedor.Telefono ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Correo", (object)proveedor.Correo ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Direccion", (object)proveedor.Direccion ?? DBNull.Value);
                    conn.Open();
                    cmd.ExecuteNonQuery();
                }

                string updateProveedor = @"
                    UPDATE Proveedor 
                    SET Nit = @Nit,
                        RazonSocial = @RazonSocial,
                        Contacto = @Contacto,
                        Telefono = @Telefono,
                        Correo = @Correo,
                        Direccion = @Direccion,
                        MantenimientoId = @MantenimientoId,
                        PoliticaDevolucion = @Politica
                    WHERE ProveedorId = @ProveedorId";

                using (var conn = new SqlConnection(_connectionString))
                using (var cmd = new SqlCommand(updateProveedor, conn))
                {
                    cmd.Parameters.AddWithValue("@ProveedorId", proveedor.ProveedorId);
                    cmd.Parameters.AddWithValue("@Nit", proveedor.Nit);
                    cmd.Parameters.AddWithValue("@RazonSocial", proveedor.RazonSocial);
                    cmd.Parameters.AddWithValue("@Contacto", (object)proveedor.Contacto ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Telefono", (object)proveedor.Telefono ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Correo", (object)proveedor.Correo ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Direccion", (object)proveedor.Direccion ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@MantenimientoId", proveedor.MantenimientoId.HasValue ? (object)proveedor.MantenimientoId.Value : DBNull.Value);
                    cmd.Parameters.AddWithValue("@Politica", (object)proveedor.PoliticaDevolucion ?? DBNull.Value);
                    conn.Open();
                    cmd.ExecuteNonQuery();
                }

                TempData["Success"] = "Proveedor actualizado exitosamente";
                return RedirectToAction("Index");
            }

            ViewBag.Politicas = ObtenerPoliticas(proveedor.MantenimientoId);
            return View(proveedor);
        }

        // POST: Proveedor/Inactivar
        [HttpPost]
        public JsonResult Inactivar(int id)
        {
            try
            {
                string query = "UPDATE Proveedor SET Activo = 0 WHERE ProveedorId = @ProveedorId";
                using (var conn = new SqlConnection(_connectionString))
                using (var cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@ProveedorId", id);
                    conn.Open();
                    cmd.ExecuteNonQuery();
                }

                return Json(new { success = true, message = "Proveedor inactivado exitosamente" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // GET: Proveedor/Detalle/5
        public ActionResult Detalle(int id)
        {
            if (Session["UserRol"] == null)
            {
                return RedirectToAction("Login", "Account");
            }

            Proveedor proveedor = null;

            string query = @"
                SELECT 
                    p.ProveedorId,
                    p.Nit,
                    p.RazonSocial,
                    p.Contacto,
                    p.Telefono,
                    p.Correo,
                    p.Direccion,
                    p.Activo,
                    p.MantenimientoId,
                    m.Nombre AS PoliticaNombre,
                    m.Valor AS DiasMaximosDevolucion,
                    p.PoliticaDevolucion,
                    pe.Nombres,
                    pe.Apellidos
                FROM Proveedor p
                INNER JOIN Persona pe ON p.PersonaId = pe.PersonaId
                LEFT JOIN Mantenimiento m ON p.MantenimientoId = m.MantenimientoId
                WHERE p.ProveedorId = @ProveedorId";

            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@ProveedorId", id);
                conn.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        proveedor = new Proveedor
                        {
                            ProveedorId = (int)reader["ProveedorId"],
                            Nit = reader["Nit"].ToString(),
                            RazonSocial = reader["RazonSocial"].ToString(),
                            Contacto = reader["Contacto"]?.ToString(),
                            Telefono = reader["Telefono"]?.ToString(),
                            Correo = reader["Correo"]?.ToString(),
                            Direccion = reader["Direccion"]?.ToString(),
                            Activo = (bool)reader["Activo"],
                            MantenimientoId = reader["MantenimientoId"] != DBNull.Value ? (int?)reader["MantenimientoId"] : null,
                            PoliticaNombre = reader["PoliticaNombre"]?.ToString(),
                            DiasMaximosDevolucion = reader["DiasMaximosDevolucion"] != DBNull.Value ? (int)reader["DiasMaximosDevolucion"] : 10,
                            PoliticaDevolucion = reader["PoliticaDevolucion"]?.ToString(),
                            Nombres = reader["Nombres"].ToString(),
                            Apellidos = reader["Apellidos"].ToString()
                        };
                    }
                }
            }

            if (proveedor == null)
            {
                return HttpNotFound();
            }

            return View(proveedor);
        }

        private SelectList ObtenerPoliticas(int? selectedValue = null)
        {
            var politicas = new List<SelectListItem>();
            string query = "SELECT MantenimientoId, Nombre, Valor FROM Mantenimiento WHERE Activo = 1 ORDER BY Valor";

            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand(query, conn))
            {
                conn.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        politicas.Add(new SelectListItem
                        {
                            Value = reader["MantenimientoId"].ToString(),
                            Text = $"{reader["Nombre"]} ({reader["Valor"]} días)"
                        });
                    }
                }
            }

            return new SelectList(politicas, "Value", "Text", selectedValue);
        }
    }
}