using LaMediaCancha.App_Data;
using LaMediaCancha.Models;
using LaMediaCancha.Models.ViewModels;
using LaMediaCancha.Services;
using System;
using System.Configuration;
using System.Data.Entity;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace LaMediaCancha.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly EncriptacionService _encriptacion;
        private readonly SoundexService _soundex;
        private readonly EmailService _emailService;
        private readonly string _connectionString;

        public AccountController()
        {
            _connectionString = ConfigurationManager.ConnectionStrings["LaMediaCanchaSeguridad"].ConnectionString;
            _context = new ApplicationDbContext();
            _encriptacion = new EncriptacionService();
            _soundex = new SoundexService();
            _emailService = new EmailService();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) _context.Dispose();
            base.Dispose(disposing);
        }


        [HttpGet]
        public ActionResult Login()
        {
            if (Session["UserEmail"] != null)
                return RedirectToAction("Dashboard", "Home");
            return View(new LoginViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Login(LoginViewModel model)
        {
            if (string.IsNullOrWhiteSpace(model.Email))
            {
                TempData["Error"] = "Debe ingresar un correo electrónico.";
                return RedirectToAction("Login");
            }

            if (string.IsNullOrWhiteSpace(model.Password))
            {
                TempData["Error"] = "Debe ingresar una contraseña.";
                return RedirectToAction("Login");
            }

            try
            {
                var usuario = _context.Usuarios
                    .Include(u => u.Rol)
                    .FirstOrDefault(u => u.Email == model.Email && u.Activo);

                if (usuario == null)
                {
                    TempData["Error"] = "Credenciales incorrectas. Verifique su correo electrónico y contraseña.";
                    return RedirectToAction("Login");
                }

                if (usuario.Bloqueado)
                {
                    TempData["Error"] = "Su cuenta ha sido suspendida por multiples intentos fallidos. " +
                        "Fecha de bloqueo: " + (usuario.FechaBloqueo?.ToString("dd/MM/yyyy HH:mm") ?? "No registrada") +
                        ". Utilice la opcion de recuperar contraseña para reactivarla.";
                    return RedirectToAction("Login");
                }

                bool passwordValida = _encriptacion.VerificarPassword(
                    model.Password, usuario.Salt, usuario.PasswordHash);

                if (!passwordValida)
                {
                    usuario.IntentosFallidos++;
                    int intentosRestantes = 3 - usuario.IntentosFallidos;

                    if (usuario.IntentosFallidos >= 3)
                    {
                        usuario.Bloqueado = true;
                        usuario.FechaBloqueo = DateTime.Now;
                        _context.SaveChanges();
                        TempData["Error"] = "Cuenta bloqueada por 3 intentos fallidos consecutivos. " +
                            "Fecha: " + DateTime.Now.ToString("dd/MM/yyyy HH:mm") +
                            ". Use la opcion de recuperar contraseña para reactivar el acceso.";
                        return RedirectToAction("Login");
                    }

                    _context.SaveChanges();
                    TempData["Error"] = "Credenciales incorrectas. Le quedan " + intentosRestantes +
                        " intento(s) antes de que su cuenta sea suspendida.";
                    return RedirectToAction("Login");
                }

                usuario.IntentosFallidos = 0;
                usuario.FechaUltimoAcceso = DateTime.Now;
                _context.SaveChanges();

                Session["UserId"] = usuario.UsuarioId;
                Session["UserEmail"] = usuario.Email;
                Session["UserNombre"] = usuario.NombreCompleto;
                Session["UserRol"] = usuario.Rol.Nombre;

                if (usuario.EsPasswordTemporal)
                {
                    TempData["MensajeInfo"] = "Bienvenido. Debe cambiar su contraseña temporal antes de continuar.";
                    return RedirectToAction("CambiarPassword", "Account");
                }

                TempData["Exito"] = "Bienvenido " + usuario.NombreCompleto;
                return RedirectToAction("Dashboard", "Home");
            }
            catch
            {
                TempData["Error"] = "Error al procesar la solicitud. Intente nuevamente.";
                return RedirectToAction("Login");
            }
        }

        [HttpGet]
        public ActionResult Logout()
        {
            Session.Clear();
            Session.Abandon();
            TempData["Exito"] = "Sesión cerrada correctamente. Gracias por utilizar nuestros servicios.";
            return RedirectToAction("Login");
        }

        [HttpGet]
        public ActionResult CambiarPassword()
        {
            if (Session["UserId"] == null)
            {
                TempData["Error"] = "Debe iniciar sesión para acceder a esta pagina.";
                return RedirectToAction("Login");
            }

            if (TempData["MensajeInfo"] != null)
                ViewBag.MensajeInfo = TempData["MensajeInfo"];

            return View(new CambiarPasswordViewModel());
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CambiarPassword(CambiarPasswordViewModel model)
        {
            // Verificar sesión
            if (Session["UserId"] == null)
            {
                TempData["Error"] = "Su sesión ha expirado. Por favor, inicie sesión nuevamente.";
                return RedirectToAction("Login", "Account");
            }

            // Verificar modelo
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                int usuarioId = (int)Session["UserId"];

                // Verificar que la cadena de conexión no sea nula
                if (string.IsNullOrEmpty(_connectionString))
                {
                    TempData["Error"] = "Error de configuración de base de datos.";
                    return View(model);
                }

                // Verificar que las contraseñas coincidan
                if (model.NuevaPassword != model.ConfirmarPassword)
                {
                    ModelState.AddModelError("ConfirmarPassword", "Las contraseñas no coinciden");
                    return View(model);
                }

                // Actualizar la contraseña directamente (sin validaciones adicionales)
                string updateQuery = "UPDATE Usuarios SET PasswordHash = @PasswordHash WHERE UsuarioId = @UsuarioId";

                using (var conn = new SqlConnection(_connectionString))
                {
                    conn.Open();
                    using (var cmd = new SqlCommand(updateQuery, conn))
                    {
                        cmd.Parameters.AddWithValue("@PasswordHash", model.NuevaPassword);
                        cmd.Parameters.AddWithValue("@UsuarioId", usuarioId);
                        int rowsAffected = cmd.ExecuteNonQuery();

                        if (rowsAffected == 0)
                        {
                            TempData["Error"] = "No se encontró el usuario.";
                            return View(model);
                        }
                    }
                }

                // Limpiar sesión
                Session.Clear();
                Session.Abandon();

                // Eliminar cookie de sesión
                if (Request.Cookies["ASP.NET_SessionId"] != null)
                {
                    var cookie = new HttpCookie("ASP.NET_SessionId");
                    cookie.Expires = DateTime.Now.AddDays(-1);
                    Response.Cookies.Add(cookie);
                }

                TempData["Exito"] = "Contraseña actualizada exitosamente. Por favor, inicie sesión con su nueva contraseña.";
                return RedirectToAction("Login", "Account");
            }
            catch (SqlException ex)
            {
                System.Diagnostics.Debug.WriteLine("SQL Error: " + ex.Message);
                TempData["Error"] = "Error de base de datos: " + ex.Message;
                return View(model);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error: " + ex.Message);
                TempData["Error"] = "Error: " + ex.Message;
                return View(model);
            }
        }

        [HttpGet]
        public ActionResult RecuperarPassword()
            => View(new RecuperarPasswordViewModel());

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> RecuperarPassword(RecuperarPasswordViewModel model)
        {
            if (string.IsNullOrWhiteSpace(model.Email))
            {
                TempData["Error"] = "Debe ingresar un correo electronico.";
                return RedirectToAction("Login");
            }

            try
            {
                var usuario = _context.Usuarios
                    .FirstOrDefault(u => u.Email == model.Email && u.Activo);

                if (usuario != null)
                {
                    usuario.TokenRecuperacion = Guid.NewGuid().ToString("N");
                    usuario.ExpiracionToken = DateTime.Now.AddHours(24);
                    await _context.SaveChangesAsync();

                    string enlace = Url.Action("ResetPassword", "Account",
                        new { token = usuario.TokenRecuperacion, email = usuario.Email },
                        Request.Url.Scheme);

                    string cuerpo = $@"
<!DOCTYPE html>
<html lang='es'>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <style>
        body {{ margin:0; padding:0; background-color:#FDF5E6; font-family:'Segoe UI',Arial,sans-serif; }}
        .container {{ max-width:600px; margin:20px auto; background:#FFFFFF; border-radius:8px; overflow:hidden; box-shadow:0 4px 12px rgba(0,0,0,0.1); }}
        .header {{ background:linear-gradient(135deg,#8B4513 0%,#D35400 100%); padding:32px 24px; text-align:center; }}
        .header h1 {{ color:#FFFFFF; font-size:28px; margin:0; }}
        .header p {{ color:rgba(255,255,255,0.9); margin:8px 0 0; font-size:14px; }}
        .content {{ padding:40px 32px; }}
        .greeting {{ font-size:16px; color:#2C3E50; margin-bottom:20px; }}
        .message {{ color:#4A5568; line-height:1.6; margin-bottom:24px; }}
        .button-container {{ text-align:center; margin:32px 0; }}
        .button {{ display:inline-block; background:#D35400; color:#FFFFFF; text-decoration:none; padding:12px 32px; border-radius:4px; font-weight:600; }}
        .info-box {{ background:#F8F9FA; border-left:3px solid #D35400; padding:16px 20px; margin:24px 0; }}
        .info-box p {{ margin:8px 0; color:#4A5568; font-size:13px; }}
        .footer {{ background:#F8F9FA; padding:24px; text-align:center; border-top:1px solid #E2E8F0; }}
        .footer p {{ margin:8px 0; color:#718096; font-size:12px; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>La Media Cancha</h1>
            <p>Asadera · Sistema de Gestión</p>
        </div>
        <div class='content'>
            <div class='greeting'>Estimado(a) <strong>{usuario.NombreCompleto}</strong>,</div>
            <div class='message'>Hemos recibido una solicitud para restablecer la contraseña de su cuenta.
            Para continuar, utilice el siguiente enlace:</div>
            <div class='button-container'>
                <a href='{enlace}' class='button'>Restablecer Contraseña</a>
            </div>
            <div class='info-box'>
                <p><strong>Informacion importante:</strong></p>
                <p>Este enlace sera valido por 24 horas.</p>
                <p>Si no solicito este cambio, ignore este mensaje.</p>
                <p>Su contraseña actual permanece activa hasta que complete el proceso.</p>
            </div>
        </div>
        <div class='footer'>
            <p>La Media Cancha - Sistema de Gestion</p>
            <p>Mensaje automatico, no responda a este correo.</p>
            <p>© {DateTime.Now.Year} La Media Cancha. Todos los derechos reservados.</p>
        </div>
    </div>
</body>
</html>";

                    await _emailService.EnviarEmailAsync(
                        usuario.Email, "Recuperacion de Contraseña - La Media Cancha", cuerpo);
                }
            }
            catch { }

            TempData["MensajeInfo"] = "El correo a sido enviado con las instrucciones de restablecimiento  " +
                "para restablecer su contraseña.";
            return RedirectToAction("Login");
        }

        [HttpGet]
        public ActionResult ResetPassword(string token, string email)
        {
            var usuario = _context.Usuarios.FirstOrDefault(u =>
                u.Email == email &&
                u.TokenRecuperacion == token &&
                u.ExpiracionToken > DateTime.Now);

            if (usuario == null)
            {
                TempData["Error"] = "El enlace no es válido o ha expirado. " +
                    "Solicite un nuevo restablecimiento de contraseña.";
                return RedirectToAction("Login");
            }

            return View(new ResetPasswordViewModel { Token = token, Email = email });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Por favor, complete todos los campos correctamente.";
                return RedirectToAction("ResetPassword",
                    new { token = model.Token, email = model.Email });
            }

            try
            {
                var usuario = _context.Usuarios.FirstOrDefault(u =>
                    u.Email == model.Email &&
                    u.TokenRecuperacion == model.Token &&
                    u.ExpiracionToken > DateTime.Now);

                if (usuario == null)
                {
                    TempData["Error"] = "El enlace no es válido o ha expirado.";
                    return RedirectToAction("Login");
                }

                if (model.NuevaPassword != model.ConfirmarPassword)
                {
                    TempData["Error"] = "Las contraseñas no coinciden.";
                    return RedirectToAction("ResetPassword",
                        new { token = model.Token, email = model.Email });
                }

                if (!_encriptacion.ValidarFortalezaPassword(model.NuevaPassword))
                {
                    TempData["Error"] = "La contraseña debe tener minimo 8 caracteres, " +
                        "una mayuscula, una minuscula, un numero y un simbolo. Sin espacios.";
                    return RedirectToAction("ResetPassword",
                        new { token = model.Token, email = model.Email });
                }

                GuardarNuevaPassword(usuario, model.NuevaPassword);
                usuario.TokenRecuperacion = null;
                usuario.ExpiracionToken = null;
                usuario.Bloqueado = false;
                usuario.IntentosFallidos = 0;
                _context.SaveChanges();

                TempData["Exito"] = "Contraseña restablecida exitosamente. " +
                    "Ya puede iniciar sesión con su nueva contraseña.";
                return RedirectToAction("Login");
            }
            catch
            {
                TempData["Error"] = "Error al restablecer la contraseña. Intente nuevamente.";
                return RedirectToAction("Login");
            }
        }

        private void GuardarNuevaPassword(Usuario usuario, string nuevaPassword)
        {
            string soloLetras = new string(nuevaPassword.Where(char.IsLetter).ToArray()).ToUpper();
            string numerosSimb = new string(nuevaPassword.Where(c => !char.IsLetter(c)).ToArray());

            usuario.Salt = _encriptacion.GenerarSalt();
            usuario.PasswordHash = _encriptacion.HashPassword(nuevaPassword, usuario.Salt);
            usuario.SoundexPassword = _soundex.CalcularSoundex(soloLetras);
            usuario.NumerosSimbolosPassword = numerosSimb;
            usuario.LongitudPassword = nuevaPassword.Length;
            usuario.EsPasswordTemporal = false;
            usuario.FechaPasswordTemporal = null;
            usuario.FechaModificacion = DateTime.Now;
        }
    }
}