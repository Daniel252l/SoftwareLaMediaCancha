using System.Web.Mvc;

namespace LaMediaCancha.Controllers
{
    public class HomeController : Controller
    {
        // Página de bienvenida pública (sin sesión)
        public ActionResult Index()
        {
            // Si ya está logueado, redirigir al Dashboard
            if (Session["UserRol"] != null)
            {
                return RedirectToAction("Dashboard", "Home");
            }
            return View();
        }

        // Dashboard (solo con sesión activa)
        public ActionResult Dashboard()
        {
            // Verificar sesión
            if (Session["UserRol"] == null)
            {
                return RedirectToAction("Login", "Account");
            }
            return View();
        }

        public ActionResult Denegado()
        {
            return View();
        }
    }
}