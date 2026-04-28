using LaMediaCancha.Models.ViewModels;
using LaMediaCancha.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;

namespace LaMediaCancha.Controllers
{
    public class FacturaController : Controller
    {
        private readonly FacturaService _facturaService;

        public FacturaController()
        {
            _facturaService = new FacturaService();
        }

        // GET: Factura/Index
        public ActionResult Index()
        {
            if (Session["UserRol"] == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var model = new BuscarFacturaViewModel
            {
                Estados = new List<SelectListItem>
                {
                    new SelectListItem { Value = "", Text = "Todos" },
                    new SelectListItem { Value = "Vigente", Text = "Vigente" },
                    new SelectListItem { Value = "Anulada", Text = "Anulada" }
                }
            };

            return View(model);
        }

        // POST: Factura/Buscar
        [HttpPost]
        public ActionResult Buscar(string NumeroFactura, string NumeroDocumento, DateTime? FechaInicio, DateTime? FechaFin, string Estado)
        {
            if (Session["UserRol"] == null)
            {
                return Json(new { success = false, message = "Sesión expirada" });
            }

            var filtro = new BuscarFacturaViewModel
            {
                NumeroFactura = NumeroFactura,
                NumeroDocumento = NumeroDocumento,
                FechaInicio = FechaInicio,
                FechaFin = FechaFin,
                Estado = Estado
            };

            var facturas = _facturaService.ObtenerFacturas(filtro);

            if (facturas == null || !facturas.Any())
            {
                return Content("<tr><td colspan='8' class='text-center'>No se encontraron facturas con los criterios de búsqueda</td</tr");
            }

            var html = new System.Text.StringBuilder();

            foreach (var factura in facturas)
            {
                string rowClass = factura.Estado == "Anulada" ? "factura-anulada" : "";
                string estadoBadge = factura.Estado == "Vigente"
                    ? "<span class='badge-vigente'>Vigente</span>"
                    : "<span class='badge-anulada'>Anulada</span>";

                string notaCredito = !string.IsNullOrEmpty(factura.NumeroNotaCredito)
                    ? $"<span class='badge-info'>{factura.NumeroNotaCredito}</span>"
                    : "<span style='color:#999;'>—</span>";

                string acciones = $"<a href='{Url.Action("Detalle", new { id = factura.FacturaId })}' class='btn-ver' title='Ver detalle'><i class='fas fa-eye'></i></a>" +
                                 $"<a href='{Url.Action("Imprimir", new { id = factura.FacturaId })}' class='btn-imprimir' title='Imprimir' target='_blank'><i class='fas fa-print'></i></a>";

                if (factura.Estado == "Vigente")
                {
                    acciones += $"<a href='javascript:void(0)' onclick='anularFactura({factura.FacturaId}, \"{factura.NumeroFactura}\")' class='btn-anular' title='Anular'><i class='fas fa-ban'></i></a>";
                }
                else if (factura.Estado == "Anulada" && factura.NotaCreditoId.HasValue)
                {
                    acciones += $"<a href='javascript:void(0)' onclick='verNotaCredito({factura.NotaCreditoId})' class='btn-ver-nc' title='Ver Nota de Crédito'><i class='fas fa-file-invoice'></i> NC</a>";
                }

                html.Append($@"
                    <tr class='{rowClass}'>
                        <td><strong>{factura.NumeroFactura}</strong></td>
                        <td>{factura.FechaEmision:dd/MM/yyyy HH:mm}</td>
                        <td>{factura.ClienteNombre}</td>
                        <td>{factura.ClienteDocumento ?? "—"}</td>
                        <td class='text-right'>{factura.Total:N2}</td>
                        <td>{estadoBadge}</td>
                        <td class='text-center'>{notaCredito}</td>
                        <td class='text-center'>{acciones}</td>
                    </tr>");
            }

            return Content(html.ToString());
        }

        // GET: Factura/Detalle/5
        public ActionResult Detalle(int id)
        {
            if (Session["UserRol"] == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var factura = _facturaService.ObtenerFacturaPorId(id);
            if (factura == null)
            {
                return HttpNotFound();
            }

            return View(factura);
        }

        // GET: Factura/Imprimir/5
        public ActionResult Imprimir(int id)
        {
            if (Session["UserRol"] == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var factura = _facturaService.ObtenerFacturaPorId(id);
            if (factura == null)
            {
                return HttpNotFound();
            }

            return View(factura);
        }

        // POST: Factura/Anular
        [HttpPost]
        public JsonResult Anular(int FacturaId, string MotivoAnulacion)
        {
            if (Session["UserRol"] == null)
            {
                return Json(new { success = false, message = "Sesión expirada" });
            }

            if (string.IsNullOrEmpty(MotivoAnulacion))
            {
                return Json(new { success = false, message = "Debe seleccionar un motivo de anulación" });
            }

            try
            {
                // Obtener datos del usuario de la sesión
                int usuarioId = (int)Session["UserId"];
                string usuarioNombre = Session["UserNombre"]?.ToString() ?? "Usuario";
                string usuarioRol = Session["UserRol"]?.ToString() ?? "Usuario";

                // Combinar nombre y rol para mejor identificación
                string usuarioCompleto = $"{usuarioNombre} ({usuarioRol})";

                bool resultado = _facturaService.AnularFactura(FacturaId, MotivoAnulacion, usuarioId, usuarioCompleto);

                if (resultado)
                {
                    return Json(new { success = true, message = "Factura anulada correctamente. Se ha generado la Nota de Crédito correspondiente." });
                }
                else
                {
                    return Json(new { success = false, message = "Error al anular la factura" });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error: " + ex.Message });
            }
        }

        // GET: Factura/ObtenerNotaCredito
        public JsonResult ObtenerNotaCredito(int id)
        {
            if (Session["UserRol"] == null)
            {
                return Json(new { success = false, message = "Sesión expirada" }, JsonRequestBehavior.AllowGet);
            }

            try
            {
                var notaCredito = _facturaService.ObtenerNotaCreditoPorId(id);
                if (notaCredito == null)
                {
                    return Json(new { success = false, message = "Nota de crédito no encontrada" }, JsonRequestBehavior.AllowGet);
                }

                return Json(new
                {
                    success = true,
                    NumeroNotaCredito = notaCredito.NumeroNotaCredito,
                    FechaEmision = notaCredito.FechaEmision.ToString("dd/MM/yyyy HH:mm"),
                    MontoTotal = notaCredito.MontoTotal,
                    Motivo = notaCredito.Motivo,
                    Estado = notaCredito.Estado,
                    UsuarioNombre = notaCredito.UsuarioNombre,
                    FacturaOriginalNumero = notaCredito.FacturaOriginalNumero,
                    ClienteNombre = notaCredito.ClienteNombre,
                    ClienteDocumento = notaCredito.ClienteDocumento
                }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }
    }
}