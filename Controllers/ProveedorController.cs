using LaMediaCancha.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;
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
                    p.PoliticaDevolucion,
                    p.DiasMaximosDevolucion,
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
                            Nit = reader["Nit"]?.ToString() ?? "",
                            RazonSocial = reader["RazonSocial"]?.ToString() ?? "",
                            Contacto = reader["Contacto"]?.ToString(),
                            Telefono = reader["Telefono"]?.ToString(),
                            Correo = reader["Correo"]?.ToString(),
                            Direccion = reader["Direccion"]?.ToString(),
                            Activo = reader["Activo"] != DBNull.Value && (bool)reader["Activo"],
                            MantenimientoId = reader["MantenimientoId"] != DBNull.Value ? (int?)reader["MantenimientoId"] : null,
                            PoliticaNombre = reader["PoliticaNombre"]?.ToString(),
                            PoliticaDevolucion = reader["PoliticaDevolucion"]?.ToString(),
                            DiasMaximosDevolucion = reader["DiasMaximosDevolucion"] != DBNull.Value ? Convert.ToInt32(reader["DiasMaximosDevolucion"]) : 0,
                            Nombres = reader["Nombres"]?.ToString() ?? "",
                            Apellidos = reader["Apellidos"]?.ToString() ?? ""
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
        public ActionResult Crear(Proveedor proveedor, int? DiasPersonalizados)
        {
            if (Session["UserRol"] == null)
            {
                return RedirectToAction("Login", "Account");
            }

            // Validaciones
            if (string.IsNullOrEmpty(proveedor.Nit))
                ModelState.AddModelError("Nit", "El NIT es requerido");
            if (string.IsNullOrEmpty(proveedor.RazonSocial))
                ModelState.AddModelError("RazonSocial", "La razón social es requerida");
            if (string.IsNullOrEmpty(proveedor.Nombres))
                ModelState.AddModelError("Nombres", "Los nombres son requeridos");
            if (string.IsNullOrEmpty(proveedor.Apellidos))
                ModelState.AddModelError("Apellidos", "Los apellidos son requeridos");

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

                // Si se ingresaron días personalizados
                if (DiasPersonalizados.HasValue && DiasPersonalizados.Value > 0)
                {
                    proveedor.PoliticaDevolucion = $"Se aceptan devoluciones hasta {DiasPersonalizados.Value} días después de la compra";
                    proveedor.DiasMaximosDevolucion = DiasPersonalizados.Value;
                }

                int personaId;

                // Verificar si la persona ya existe
                string checkPersonaQuery = @"
                    SELECT PersonaId FROM Persona 
                    WHERE Nombres = @Nombres AND Apellidos = @Apellidos";

                using (var conn = new SqlConnection(_connectionString))
                {
                    conn.Open();
                    using (var cmd = new SqlCommand(checkPersonaQuery, conn))
                    {
                        cmd.Parameters.AddWithValue("@Nombres", proveedor.Nombres);
                        cmd.Parameters.AddWithValue("@Apellidos", proveedor.Apellidos);
                        var result = cmd.ExecuteScalar();

                        if (result != null && result != DBNull.Value)
                        {
                            personaId = Convert.ToInt32(result);
                        }
                        else
                        {
                            string insertPersona = @"
                                INSERT INTO Persona (Nombres, Apellidos, Telefono, Correo, Direccion, Activo)
                                VALUES (@Nombres, @Apellidos, @Telefono, @Correo, @Direccion, 1);
                                SELECT SCOPE_IDENTITY();";

                            using (var cmdInsert = new SqlCommand(insertPersona, conn))
                            {
                                cmdInsert.Parameters.AddWithValue("@Nombres", proveedor.Nombres);
                                cmdInsert.Parameters.AddWithValue("@Apellidos", proveedor.Apellidos);
                                cmdInsert.Parameters.AddWithValue("@Telefono", (object)proveedor.Telefono ?? DBNull.Value);
                                cmdInsert.Parameters.AddWithValue("@Correo", (object)proveedor.Correo ?? DBNull.Value);
                                cmdInsert.Parameters.AddWithValue("@Direccion", (object)proveedor.Direccion ?? DBNull.Value);
                                personaId = Convert.ToInt32(cmdInsert.ExecuteScalar());
                            }
                        }
                    }
                }

                string queryProveedor = @"
                    INSERT INTO Proveedor (PersonaId, Nit, RazonSocial, Contacto, Telefono, Correo, Direccion, MantenimientoId, PoliticaDevolucion, DiasMaximosDevolucion, Activo)
                    VALUES (@PersonaId, @Nit, @RazonSocial, @Contacto, @Telefono, @Correo, @Direccion, @MantenimientoId, @Politica, @DiasMaximos, 1)";

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
                    cmd.Parameters.AddWithValue("@DiasMaximos", proveedor.DiasMaximosDevolucion > 0 ? (object)proveedor.DiasMaximosDevolucion : DBNull.Value);
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
                    p.DiasMaximosDevolucion,
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
                            Nit = reader["Nit"]?.ToString() ?? "",
                            RazonSocial = reader["RazonSocial"]?.ToString() ?? "",
                            Contacto = reader["Contacto"]?.ToString(),
                            Telefono = reader["Telefono"]?.ToString(),
                            Correo = reader["Correo"]?.ToString(),
                            Direccion = reader["Direccion"]?.ToString(),
                            Activo = reader["Activo"] != DBNull.Value && (bool)reader["Activo"],
                            MantenimientoId = reader["MantenimientoId"] != DBNull.Value ? (int?)reader["MantenimientoId"] : null,
                            PoliticaDevolucion = reader["PoliticaDevolucion"]?.ToString(),
                            DiasMaximosDevolucion = reader["DiasMaximosDevolucion"] != DBNull.Value ? Convert.ToInt32(reader["DiasMaximosDevolucion"]) : 0,
                            Nombres = reader["Nombres"]?.ToString() ?? "",
                            Apellidos = reader["Apellidos"]?.ToString() ?? ""
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

        // POST: Proveedor/Editar
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Editar(Proveedor proveedor, int? DiasPersonalizados)
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

                // Si se ingresaron días personalizados
                if (DiasPersonalizados.HasValue && DiasPersonalizados.Value > 0)
                {
                    proveedor.PoliticaDevolucion = $"Se aceptan devoluciones hasta {DiasPersonalizados.Value} días después de la compra";
                    proveedor.DiasMaximosDevolucion = DiasPersonalizados.Value;
                }

                // Actualizar Persona
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

                // Actualizar Proveedor
                string updateProveedor = @"
                    UPDATE Proveedor 
                    SET Nit = @Nit,
                        RazonSocial = @RazonSocial,
                        Contacto = @Contacto,
                        Telefono = @Telefono,
                        Correo = @Correo,
                        Direccion = @Direccion,
                        MantenimientoId = @MantenimientoId,
                        PoliticaDevolucion = @Politica,
                        DiasMaximosDevolucion = @DiasMaximos
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
                    cmd.Parameters.AddWithValue("@DiasMaximos", proveedor.DiasMaximosDevolucion > 0 ? (object)proveedor.DiasMaximosDevolucion : DBNull.Value);
                    conn.Open();
                    cmd.ExecuteNonQuery();
                }

                TempData["Success"] = "Proveedor actualizado exitosamente";
                return RedirectToAction("Index");
            }

            ViewBag.Politicas = ObtenerPoliticas(proveedor.MantenimientoId);
            return View(proveedor);
        }

        // POST: Proveedor/CambiarEstado
        [HttpPost]
        public JsonResult CambiarEstado(int id, bool activo)
        {
            try
            {
                string query = "UPDATE Proveedor SET Activo = @Activo WHERE ProveedorId = @ProveedorId";
                using (var conn = new SqlConnection(_connectionString))
                using (var cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@ProveedorId", id);
                    cmd.Parameters.AddWithValue("@Activo", activo);
                    conn.Open();
                    cmd.ExecuteNonQuery();
                }

                string mensaje = activo ? "Proveedor activado exitosamente" : "Proveedor inactivado exitosamente";
                return Json(new { success = true, message = mensaje });
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
                    p.PoliticaDevolucion,
                    p.DiasMaximosDevolucion,
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
                            Nit = reader["Nit"]?.ToString() ?? "",
                            RazonSocial = reader["RazonSocial"]?.ToString() ?? "",
                            Contacto = reader["Contacto"]?.ToString(),
                            Telefono = reader["Telefono"]?.ToString(),
                            Correo = reader["Correo"]?.ToString(),
                            Direccion = reader["Direccion"]?.ToString(),
                            Activo = reader["Activo"] != DBNull.Value && (bool)reader["Activo"],
                            MantenimientoId = reader["MantenimientoId"] != DBNull.Value ? (int?)reader["MantenimientoId"] : null,
                            PoliticaNombre = reader["PoliticaNombre"]?.ToString(),
                            PoliticaDevolucion = reader["PoliticaDevolucion"]?.ToString(),
                            DiasMaximosDevolucion = reader["DiasMaximosDevolucion"] != DBNull.Value ? Convert.ToInt32(reader["DiasMaximosDevolucion"]) : 0,
                            Nombres = reader["Nombres"]?.ToString() ?? "",
                            Apellidos = reader["Apellidos"]?.ToString() ?? ""
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
            string query = "SELECT MantenimientoId, Nombre FROM Mantenimiento WHERE Activo = 1 ORDER BY Nombre";

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
                            Text = reader["Nombre"].ToString()
                        });
                    }
                }
            }

            return new SelectList(politicas, "Value", "Text", selectedValue);
        }
    }
}