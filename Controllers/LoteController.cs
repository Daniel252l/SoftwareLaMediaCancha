using LaMediaCancha.Services;
using System;
using System.Web.Mvc;

namespace LaMediaCancha.Controllers
{
    public class LoteController : Controller
    {
        private readonly LoteService _loteService;

        public LoteController()
        {
            _loteService = new LoteService();
        }

        public ActionResult PorProducto(int productoId)
        {
            if (Session["UserRol"] == null)
                return RedirectToAction("Login", "Account");

            var lotes = _loteService.ObtenerLotesPorProducto(productoId);
            ViewBag.ProductoId = productoId;
            return View(lotes);
        }

        public ActionResult ProximosAVencer(int dias = 7)
        {
            if (Session["UserRol"] == null)
                return RedirectToAction("Login", "Account");

            var lotes = _loteService.ObtenerLotesProximosAVencer(dias);
            ViewBag.DiasAlerta = dias;
            return View(lotes);
        }

        [HttpPost]
        public JsonResult ActualizarVencimiento(int loteId, string fechaVencimiento,
                                                string numeroLoteProveedor)
        {
            try
            {
                if (!DateTime.TryParse(fechaVencimiento, out DateTime fecha))
                    return Json(new { success = false, message = "Fecha inválida" });

                bool ok = _loteService.ActualizarFechaVencimiento(loteId, fecha,
                                                                   numeroLoteProveedor);
                return Json(new
                {
                    success = ok,
                    message = ok ? "Lote actualizado" : "Error al actualizar"
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        public JsonResult ObtenerLotesJson(int productoId)
        {
            var lotes = _loteService.ObtenerLotesPorProducto(productoId);
            return Json(lotes, JsonRequestBehavior.AllowGet);
        }
        [HttpPost]
        public JsonResult CrearLote(int productoId, string numeroLoteInterno, string numeroLoteProveedor,
                            decimal cantidad, decimal precioUnitario, string fechaIngreso, string fechaVencimiento)
        {
            try
            {
                if (string.IsNullOrEmpty(numeroLoteInterno))
                    return Json(new { success = false, message = "El número de lote interno es requerido" });

                if (cantidad <= 0)
                    return Json(new { success = false, message = "La cantidad debe ser mayor a 0" });

                if (precioUnitario <= 0)
                    return Json(new { success = false, message = "El precio unitario debe ser mayor a 0" });

                bool resultado = _loteService.CrearLote(productoId, numeroLoteInterno, numeroLoteProveedor,
                                                        cantidad, precioUnitario, fechaIngreso, fechaVencimiento);

                if (resultado)
                    return Json(new { success = true, message = "Lote creado exitosamente" });
                else
                    return Json(new { success = false, message = "Error al crear el lote" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
    }
}