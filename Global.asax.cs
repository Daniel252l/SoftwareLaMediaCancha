using System;
using System.Linq;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;
using LaMediaCancha.App_Data;
using LaMediaCancha.Models;
using LaMediaCancha.Services;

namespace LaMediaCancha
{
    public class MvcApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);

            SeedUsuarios();
        }

        private void SeedUsuarios()
        {
            using (var db = new ApplicationDbContext())
            {
                var rolSoporte = db.Roles.FirstOrDefault(r => r.Nombre == "Soporte");
                var rolAdmin = db.Roles.FirstOrDefault(r => r.Nombre == "Administrativo");
                var rolFinal = db.Roles.FirstOrDefault(r => r.Nombre == "UsuarioFinal");
                var empresa = db.Empresas.FirstOrDefault();

                if (rolSoporte == null || rolAdmin == null || rolFinal == null || empresa == null)
                    return;

                if (!db.Usuarios.Any(u => u.Email == "soporte@lamediacancha.com"))
                    CrearUsuario(db, rolSoporte.RolId, empresa.EmpresaId,
                        "Soporte Técnico", "soporte@lamediacancha.com", "Soporte123!");

                if (!db.Usuarios.Any(u => u.Email == "admin@lamediacancha.com"))
                    CrearUsuario(db, rolAdmin.RolId, empresa.EmpresaId,
                        "Administrador", "admin@lamediacancha.com", "Admin123!");

                // ── Nuevo usuario final ────────────────────────────────────
                if (!db.Usuarios.Any(u => u.Email == "usuario@lamediacancha.com"))
                    CrearUsuario(db, rolFinal.RolId, empresa.EmpresaId,
                        "Usuario Final", "usuario@lamediacancha.com", "Usuario123!");
            }
        }
        private static void CrearUsuario(ApplicationDbContext db, int rolId,
            int empresaId, string nombre, string email, string password)
        {
            var enc = new EncriptacionService();
            var soundex = new SoundexService();

            string salt = enc.GenerarSalt();
            string hash = enc.HashPassword(password, salt);
            string letras = new string(password.Where(char.IsLetter).ToArray()).ToUpper();
            string numeros = new string(password.Where(c => !char.IsLetter(c)).ToArray());

            var usuario = new Usuario
            {
                RolId = rolId,
                NombreCompleto = nombre,
                Email = email,
                Salt = salt,
                PasswordHash = hash,
                SoundexPassword = soundex.CalcularSoundex(letras),
                NumerosSimbolosPassword = numeros,
                LongitudPassword = password.Length,
                EsPasswordTemporal = true,
                FechaPasswordTemporal = DateTime.Now,
                Activo = true,
                Bloqueado = false,
                IntentosFallidos = 0
            };

            db.Usuarios.Add(usuario);
            db.SaveChanges();

            // Asignar a la empresa
            db.EmpresaUsuarios.Add(new EmpresaUsuario
            {
                EmpresaId = empresaId,
                UsuarioId = usuario.UsuarioId,
                Activo = true
            });
            db.SaveChanges();
        }
    }
}