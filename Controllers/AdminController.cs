using LaMediaCancha.App_Data;
using LaMediaCancha.Filters;
using LaMediaCancha.Models;
using LaMediaCancha.Models.ViewModels;
using LaMediaCancha.Services;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace LaMediaCancha.Controllers
{
    [RolAuthorize("Soporte")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly EncriptacionService _encriptacion;
        private readonly SoundexService _soundex;
        private readonly EmailService _emailService;

        public AdminController()
        {
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
        public ActionResult CrearUsuario()
        {
            var model = new CrearUsuarioViewModel
            {
                Roles = _context.Roles
                    .Where(r => r.Activo)
                    .Select(r => new SelectListItem
                    {
                        Value = r.RolId.ToString(),
                        Text = r.Nombre
                    }).ToList()
            };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> CrearUsuario(CrearUsuarioViewModel model)
        {
            // Recargar roles si hay error de validación
            if (!ModelState.IsValid)
            {
                model.Roles = _context.Roles
                    .Where(r => r.Activo)
                    .Select(r => new SelectListItem
                    {
                        Value = r.RolId.ToString(),
                        Text = r.Nombre
                    }).ToList();
                return View(model);
            }

            // Verificar email duplicado
            if (_context.Usuarios.Any(u => u.Email == model.Email))
            {
                ModelState.AddModelError("Email", "Ya existe un usuario con ese correo.");
                model.Roles = _context.Roles
                    .Where(r => r.Activo)
                    .Select(r => new SelectListItem
                    {
                        Value = r.RolId.ToString(),
                        Text = r.Nombre
                    }).ToList();
                return View(model);
            }

            // Generar contraseña temporal aleatoria
            string passwordTemporal = GenerarPasswordTemporal();
            string salt = _encriptacion.GenerarSalt();
            string hash = _encriptacion.HashPassword(passwordTemporal, salt);
            string soloLetras = new string(passwordTemporal.Where(char.IsLetter).ToArray()).ToUpper();
            string numeros = new string(passwordTemporal.Where(c => !char.IsLetter(c)).ToArray());

            var usuario = new Usuario
            {
                RolId = model.RolId,
                NombreCompleto = model.NombreCompleto,
                Email = model.Email,
                Salt = salt,
                PasswordHash = hash,
                SoundexPassword = _soundex.CalcularSoundex(soloLetras),
                NumerosSimbolosPassword = numeros,
                LongitudPassword = passwordTemporal.Length,
                EsPasswordTemporal = true,
                FechaPasswordTemporal = DateTime.Now,
                Activo = true,
                Bloqueado = false,
                IntentosFallidos = 0
            };

            _context.Usuarios.Add(usuario);
            _context.SaveChanges();

            // Enviar contraseña temporal por email
            string cuerpo = $@"
                <p>Hola <b>{usuario.NombreCompleto}</b>,</p>
                <p>Tu cuenta en <b>La Media Cancha</b> ha sido creada.</p>
                <p>Tus credenciales de acceso son:</p>
                <ul>
                    <li><b>Correo:</b> {usuario.Email}</li>
                    <li><b>Contraseña temporal:</b> {passwordTemporal}</li>
                </ul>
                <p>Esta contraseña es válida por <b>6 meses</b>. 
                   Al ingresar se te pedirá cambiarla.</p>";

            await _emailService.EnviarEmailAsync(usuario.Email, "Bienvenido - La Media Cancha", cuerpo);

            TempData["Exito"] = $"Usuario '{usuario.NombreCompleto}' creado. " +
                                $"Se envió la contraseña temporal a {usuario.Email}.";

            return RedirectToAction("CrearUsuario");
        }

        // Genera una contraseña temporal segura que cumple las reglas
        private string GenerarPasswordTemporal()
        {
            var rng = new System.Security.Cryptography.RNGCryptoServiceProvider();
            var bytes = new byte[4];
            rng.GetBytes(bytes);
            int num = Math.Abs(BitConverter.ToInt32(bytes, 0)) % 9000 + 1000; // 4 dígitos
            return $"Lmc{num}!";  // Ej: Lmc4821! — cumple todas las reglas
        }
    }
}