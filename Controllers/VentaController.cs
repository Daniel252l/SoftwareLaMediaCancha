using LaMediaCancha.Models.ViewModels;
using LaMediaCancha.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;

namespace LaMediaCancha.Controllers
{
    public class VentaController : Controller
    {
        private readonly VentaService _ventaService;

        public VentaController()
        {
            _ventaService = new VentaService();
        }

        public ActionResult Index()
        {
            if (Session["UserRol"] == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var productos = _ventaService.ObtenerProductosConLotes();
            return View(productos);
        }

        public ActionResult Carrito()
        {
            if (Session["UserRol"] == null)
            {
                return RedirectToAction("Login", "Account");
            }
            return View();
        }

        public ActionResult Facturar()
        {
            if (Session["UserRol"] == null)
            {
                return RedirectToAction("Login", "Account");
            }
            return View();
        }
        [HttpPost]
        public JsonResult AplicarFIFO(int productoId, decimal cantidad)
        {
            if (Session["UserRol"] == null)
            {
                return Json(new { success = false, message = "Sesión expirada" });
            }

            try
            {
                var lotes = _ventaService.AplicarFIFO(productoId, cantidad);
                return Json(new { success = true, lotes = lotes });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public JsonResult ProcesarVenta(VentaViewModel model)
        {
            if (Session["UserRol"] == null)
            {
                return Json(new { success = false, message = "Sesión expirada" });
            }

            try
            {
                int usuarioId = (int)Session["UserId"];
                string usuarioNombre = Session["UserNombre"]?.ToString() ?? "Usuario";

                int ventaId = _ventaService.RegistrarVenta(model, usuarioId, usuarioNombre);

                return Json(new { success = true, message = "Venta registrada exitosamente", ventaId = ventaId });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // Ver detalle de factura (sin impresión automática)
        public ActionResult Factura(int id)
        {
            if (Session["UserRol"] == null)
                return RedirectToAction("Login", "Account");

            var venta = _ventaService.ObtenerVentaPorId(id);
            if (venta == null)
                return HttpNotFound();

            return View(venta);
        }

        // Imprimir factura (con impresión automática)
        public ActionResult ImprimirFactura(int id)
        {
            if (Session["UserRol"] == null)
                return RedirectToAction("Login", "Account");

            var venta = _ventaService.ObtenerVentaPorId(id);
            if (venta == null)
                return HttpNotFound();

            return View("ImprimirFactura", venta);
        }

        // Ver detalle de venta (sin impresión automática)
        public ActionResult DetalleVenta(int id)
        {
            if (Session["UserRol"] == null)
                return RedirectToAction("Login", "Account");

            var venta = _ventaService.ObtenerVentaPorId(id);
            if (venta == null)
                return HttpNotFound();

            return View(venta);
        }

        

    }
}