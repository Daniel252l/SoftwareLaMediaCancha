using System.Web.Mvc;

namespace LaMediaCancha.Controllers
{
    [Authorize]
    public class ReporteController : Controller
    {
        // GET: Reporte/Index
        public ActionResult Index()
        {
            return View();
        }

        // GET: Reporte/BajoStock
        public ActionResult BajoStock()
        {
            return View();
        }

        // GET: Reporte/Movimientos
        public ActionResult Movimientos()
        {
            return View();
        }

        // GET: Reporte/DevolucionesPorProducto
        public ActionResult DevolucionesPorProducto()
        {
            return View();
        }
    }
}