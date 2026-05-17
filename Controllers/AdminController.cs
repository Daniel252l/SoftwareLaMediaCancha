using LaMediaCancha.Filters;
using LaMediaCancha.Models;
using LaMediaCancha.Models.ViewModels;
using LaMediaCancha.Services;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace LaMediaCancha.Controllers
{
    [RolAuthorize("Soporte")]
    public class AdminController : Controller
    {
        private readonly string _connectionStringSeguridad;
        private readonly string _connectionStringPrincipal;
        private readonly EncriptacionService _encriptacion;
        private readonly EmailService _emailService;

        public AdminController()
        {
            _connectionStringSeguridad = ConfigurationManager.ConnectionStrings["LaMediaCanchaSeguridad"].ConnectionString;
            _connectionStringPrincipal = ConfigurationManager.ConnectionStrings["LaMediaCanchaDB"].ConnectionString;
            _encriptacion = new EncriptacionService();
            _emailService = new EmailService();
        }

        // GET: Admin/ListaUsuarios
        public ActionResult ListaUsuarios()
        {
            if (Session["UserRol"] == null)
                return RedirectToAction("Login", "Account");

            var usuarios = new List<Usuario>();

            string query = @"
                SELECT u.UsuarioId, u.RolId, u.NombreCompleto, u.Email, u.Activo, 
                       u.Bloqueado, u.IntentosFallidos, u.FechaUltimoAcceso,
                       r.Nombre AS RolNombre
                FROM Usuarios u
                INNER JOIN Roles r ON u.RolId = r.RolId
                ORDER BY u.UsuarioId DESC";

            using (var conn = new SqlConnection(_connectionStringSeguridad))
            using (var cmd = new SqlCommand(query, conn))
            {
                conn.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        usuarios.Add(new Usuario
                        {
                            UsuarioId = (int)reader["UsuarioId"],
                            RolId = (int)reader["RolId"],
                            Rol = new Rol { RolId = (int)reader["RolId"], Nombre = reader["RolNombre"].ToString() },
                            NombreCompleto = reader["NombreCompleto"].ToString(),
                            Email = reader["Email"].ToString(),
                            Activo = (bool)reader["Activo"],
                            Bloqueado = (bool)reader["Bloqueado"],
                            IntentosFallidos = (int)reader["IntentosFallidos"],
                            FechaUltimoAcceso = reader["FechaUltimoAcceso"] as DateTime?
                        });
                    }
                }
            }

            return View(usuarios);
        }

        // GET: Admin/CrearUsuario
        public ActionResult CrearUsuario()
        {
            if (Session["UserRol"] == null)
                return RedirectToAction("Login", "Account");

            var model = new CrearUsuarioViewModel
            {
                Roles = ObtenerRoles()
            };
            return View(model);
        }

        // POST: Admin/CrearUsuario
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> CrearUsuario(CrearUsuarioViewModel model)
        {
            if (Session["UserRol"] == null)
                return RedirectToAction("Login", "Account");

            if (!ModelState.IsValid)
            {
                model.Roles = ObtenerRoles();
                return View(model);
            }

            // Verificar email duplicado en la base de seguridad
            string checkQuery = "SELECT COUNT(1) FROM Usuarios WHERE Email = @Email";
            using (var conn = new SqlConnection(_connectionStringSeguridad))
            using (var cmd = new SqlCommand(checkQuery, conn))
            {
                cmd.Parameters.AddWithValue("@Email", model.Email);
                conn.Open();
                int existe = (int)cmd.ExecuteScalar();
                if (existe > 0)
                {
                    ModelState.AddModelError("Email", "Ya existe un usuario con ese correo.");
                    model.Roles = ObtenerRoles();
                    return View(model);
                }
            }

            // Generar contraseña temporal
            string passwordTemporal = GenerarPasswordTemporal();
            string salt = _encriptacion.GenerarSalt();
            string hash = _encriptacion.HashPassword(passwordTemporal, salt);
            string soloLetras = new string(passwordTemporal.Where(char.IsLetter).ToArray()).ToUpper();
            string numeros = new string(passwordTemporal.Where(c => !char.IsLetter(c)).ToArray());

            string insertQuery = @"
                INSERT INTO Usuarios (RolId, NombreCompleto, Email, Salt, PasswordHash, 
                                      SoundexPassword, NumerosSimbolosPassword, LongitudPassword, 
                                      EsPasswordTemporal, FechaPasswordTemporal, Activo, Bloqueado, 
                                      IntentosFallidos, FechaCreacion)
                VALUES (@RolId, @NombreCompleto, @Email, @Salt, @PasswordHash, 
                        @SoundexPassword, @NumerosSimbolosPassword, @LongitudPassword, 
                        1, GETDATE(), 1, 0, 0, GETDATE());
                SELECT SCOPE_IDENTITY();";

            using (var conn = new SqlConnection(_connectionStringSeguridad))
            using (var cmd = new SqlCommand(insertQuery, conn))
            {
                cmd.Parameters.AddWithValue("@RolId", model.RolId);
                cmd.Parameters.AddWithValue("@NombreCompleto", model.NombreCompleto);
                cmd.Parameters.AddWithValue("@Email", model.Email);
                cmd.Parameters.AddWithValue("@Salt", salt);
                cmd.Parameters.AddWithValue("@PasswordHash", hash);
                cmd.Parameters.AddWithValue("@SoundexPassword", soloLetras);
                cmd.Parameters.AddWithValue("@NumerosSimbolosPassword", numeros);
                cmd.Parameters.AddWithValue("@LongitudPassword", passwordTemporal.Length);
                conn.Open();
                int newId = Convert.ToInt32(cmd.ExecuteScalar());
            }

            // Enviar email
            string cuerpo = $@"
                <div style='font-family: Arial, sans-serif;'>
                    <h2 style='color: #c95a00;'>La Media Cancha Asadera</h2>
                    <p>Hola <b>{model.NombreCompleto}</b>,</p>
                    <p>Su cuenta ha sido creada exitosamente.</p>
                    <p>Sus credenciales de acceso son:</p>
                    <ul>
                        <li><b>Correo:</b> {model.Email}</li>
                        <li><b>Contraseña temporal:</b> {passwordTemporal}</li>
                    </ul>
                    <p><i>Por seguridad, deberá cambiar su contraseña en el primer inicio de sesión.</i></p>
                    <hr>
                    <p style='font-size: 12px; color: #666;'>La Media Cancha Asadera</p>
                </div>";

            await _emailService.EnviarEmailAsync(model.Email, "Bienvenido - La Media Cancha", cuerpo);

            TempData["Success"] = $"Usuario '{model.NombreCompleto}' creado exitosamente. Se envió la contraseña a {model.Email}.";
            return RedirectToAction("ListaUsuarios");
        }

        // POST: Admin/CambiarEstadoUsuario
        [HttpPost]
        public JsonResult CambiarEstadoUsuario(int id, bool activo)
        {
            try
            {
                string query = "UPDATE Usuarios SET Activo = @Activo WHERE UsuarioId = @UsuarioId";
                using (var conn = new SqlConnection(_connectionStringSeguridad))
                using (var cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@UsuarioId", id);
                    cmd.Parameters.AddWithValue("@Activo", activo);
                    conn.Open();
                    cmd.ExecuteNonQuery();
                }

                string mensaje = activo ? "Usuario activado exitosamente" : "Usuario inactivado exitosamente";
                return Json(new { success = true, message = mensaje });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // Métodos auxiliares
        private SelectList ObtenerRoles()
        {
            var roles = new List<SelectListItem>();
            string query = "SELECT RolId, Nombre FROM Roles WHERE Activo = 1 ORDER BY Nombre";

            using (var conn = new SqlConnection(_connectionStringSeguridad))
            using (var cmd = new SqlCommand(query, conn))
            {
                conn.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        roles.Add(new SelectListItem
                        {
                            Value = reader["RolId"].ToString(),
                            Text = reader["Nombre"].ToString()
                        });
                    }
                }
            }

            return new SelectList(roles, "Value", "Text");
        }

        private string GenerarPasswordTemporal()
        {
            var rng = new System.Security.Cryptography.RNGCryptoServiceProvider();
            var bytes = new byte[4];
            rng.GetBytes(bytes);
            int num = Math.Abs(BitConverter.ToInt32(bytes, 0)) % 9000 + 1000;
            return $"Lmc{num}!";
        }
    }
}