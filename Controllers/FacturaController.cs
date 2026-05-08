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
        private readonly VentaService _ventaService;

        public FacturaController()
        {
            _facturaService = new FacturaService();
            _ventaService = new VentaService();
        }

        public ActionResult Index()
        {
            if (Session["UserRol"] == null)
                return RedirectToAction("Login", "Account");

            var model = new BuscarFacturaViewModel
            {
                Estados = new List<SelectListItem>
                {
                    new SelectListItem { Value = "",         Text = "Todos"   },
                    new SelectListItem { Value = "Vigente",  Text = "Vigente" },
                    new SelectListItem { Value = "Anulada",  Text = "Anulada" }
                }
            };

            return View(model);
        }

        [HttpPost]
        public ActionResult Buscar(string NumeroFactura, string NumeroDocumento,
                                   DateTime? FechaInicio, DateTime? FechaFin, string Estado)
        {
            if (Session["UserRol"] == null)
                return Json(new { success = false, message = "Sesión expirada" });

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
                return Content("<tr><td colspan='8' class='text-center'>No se encontraron facturas de compras</td></tr>");

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

                string acciones =
                    $"<a href='{Url.Action("Detalle", new { id = factura.FacturaId })}' class='btn-ver' title='Ver detalle'><i class='fas fa-eye'></i></a>" +
                    $"<a href='{Url.Action("Imprimir", new { id = factura.FacturaId })}' class='btn-imprimir' title='Imprimir' target='_blank'><i class='fas fa-print'></i></a>";

                if (factura.Estado == "Vigente")
                    acciones += $"<a href='javascript:void(0)' onclick='abrirModalNCProveedor({factura.CompraId}, \"{factura.NumeroFactura}\")' class='btn-nc-proveedor' title='Registrar NC Proveedor'><i class='fas fa-file-invoice'></i> NC</a>";
                else if (factura.Estado == "Anulada" && factura.NotaCreditoId.HasValue)
                    acciones += $"<a href='javascript:void(0)' onclick='verNotaCredito({factura.NotaCreditoId})' class='btn-ver-nc' title='Ver NC'><i class='fas fa-file-invoice'></i> NC</a>";

                html.Append($@"
                    <tr class='{rowClass}'>
                        <td><strong>{factura.NumeroFactura}</strong></td>
                        <td>{factura.FechaEmision:dd/MM/yyyy HH:mm}</td>
                        <td>{factura.ClienteNombre}</td>
                        <td>{factura.NumeroDocumento ?? "—"}</td>
                        <td class='text-right'>{factura.Total:N2}</td>
                        <td>{estadoBadge}</td>
                        <td class='text-center'>{notaCredito}</td>
                        <td class='text-center'>{acciones}</td>
                    </tr>
                ");
            }

            return Content(html.ToString());
        }

        [HttpPost]
        public ActionResult BuscarVentas(string NumeroFactura, string NumeroDocumento,
                                         DateTime? FechaInicio, DateTime? FechaFin, string Estado)
        {
            if (Session["UserRol"] == null)
                return Json(new { success = false, message = "Sesión expirada" });

            var filtro = new BuscarVentaViewModel
            {
                NumeroFactura = NumeroFactura,
                NumeroDocumento = NumeroDocumento,
                FechaInicio = FechaInicio,
                FechaFin = FechaFin,
                Estado = Estado
            };

            var ventas = _facturaService.BuscarVentas(filtro);

            if (ventas == null || !ventas.Any())
                return Content("<tr><td colspan='8' class='text-center'>No se encontraron facturas de ventas</td></tr>");

            var html = new System.Text.StringBuilder();

            foreach (var venta in ventas)
            {
                string rowClass = venta.Estado == "Anulada" ? "factura-anulada" : "";

                string estadoBadge = venta.Estado == "Completada"
                    ? "<span class='badge-vigente'>Completada</span>"
                    : "<span class='badge-anulada'>Anulada</span>";

                string acciones =
                    $"<a href='{Url.Action("Factura", "Venta", new { id = venta.VentaId })}' class='btn-ver' title='Ver detalle' target='_blank'><i class='fas fa-eye'></i></a>";

                if (venta.Estado == "Completada")
                    acciones += $"<a href='javascript:void(0)' onclick='anularFactura({venta.VentaId}, \"{venta.NumeroFactura}\", \"venta\")' class='btn-anular' title='Anular'><i class='fas fa-ban'></i></a>";

                html.Append($@"
                    <tr class='{rowClass}'>
                        <td><strong>{venta.NumeroFactura}</strong></td>
                        <td>{venta.FechaVenta:dd/MM/yyyy HH:mm}</td>
                        <td>{venta.ClienteNombre}</td>
                        <td>{venta.ClienteDocumento ?? "—"}</td>
                        <td class='text-right'>{venta.Total:N2}</td>
                        <td>{estadoBadge}</td>
                        <td class='text-center'><span style='color:#999;'>—</span></td>
                        <td class='text-center'>{acciones}</td>
                    </tr>
                ");
            }

            return Content(html.ToString());
        }

        public ActionResult Detalle(int id)
        {
            if (Session["UserRol"] == null)
                return RedirectToAction("Login", "Account");

            var factura = _facturaService.ObtenerFacturaPorId(id);
            if (factura == null) return HttpNotFound();

            return View(factura);
        }

        public ActionResult Imprimir(int id)
        {
            if (Session["UserRol"] == null)
                return RedirectToAction("Login", "Account");

            var factura = _facturaService.ObtenerFacturaPorId(id);
            if (factura == null) return HttpNotFound();

            return View(factura);
        }

        public JsonResult ObtenerNotaCredito(int id)
        {
            if (Session["UserRol"] == null)
                return Json(new { success = false, message = "Sesión expirada" }, JsonRequestBehavior.AllowGet);

            try
            {
                var nc = _facturaService.ObtenerNotaCreditoPorId(id);
                if (nc == null)
                    return Json(new { success = false, message = "Nota de crédito no encontrada" }, JsonRequestBehavior.AllowGet);

                return Json(new
                {
                    success = true,
                    NumeroNotaCredito = nc.NumeroNotaCredito,
                    FechaEmision = nc.FechaEmision.ToString("dd/MM/yyyy HH:mm"),
                    MontoTotal = nc.MontoTotal,
                    Motivo = nc.Motivo,
                    Estado = nc.Estado,
                    UsuarioNombre = nc.UsuarioNombre,
                    FacturaOriginalNumero = nc.FacturaOriginalNumero,
                    ClienteNombre = nc.ClienteNombre,
                    ClienteDocumento = nc.ClienteDocumento
                }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        public JsonResult RegistrarNCProveedor()
        {
            if (Session["UserRol"] == null)
                return Json(new { success = false, message = "Sesión expirada" });

            try
            {
                int compraId = Convert.ToInt32(Request.Form["CompraId"]);
                string numeroNC = Request.Form["NumeroNCProveedor"];
                DateTime fechaEmision = Convert.ToDateTime(Request.Form["FechaEmision"]);
                decimal montoTotal = Convert.ToDecimal(Request.Form["MontoTotal"]);
                string motivo = Request.Form["Motivo"];

                int usuarioId = (int)Session["UserId"];
                string usuarioNombre = Session["UserNombre"]?.ToString() ?? "Usuario";

                string documentoRuta = null;
                string documentoNombre = null;

                if (Request.Files.Count > 0 && Request.Files[0].ContentLength > 0)
                {
                    var archivo = Request.Files[0];
                    string extension = System.IO.Path.GetExtension(archivo.FileName).ToLower();

                    if (extension != ".pdf" && extension != ".jpg" && extension != ".jpeg" && extension != ".png")
                        return Json(new { success = false, message = "Solo se permiten archivos PDF, JPG o PNG" });

                    if (archivo.ContentLength > 5 * 1024 * 1024)
                        return Json(new { success = false, message = "El archivo no puede superar 5MB" });

                    string carpeta = Server.MapPath("~/Uploads/NotasCredito/");
                    if (!System.IO.Directory.Exists(carpeta))
                        System.IO.Directory.CreateDirectory(carpeta);

                    documentoNombre = $"NC-{compraId}-{DateTime.Now:yyyyMMddHHmmss}{extension}";
                    documentoRuta = $"/Uploads/NotasCredito/{documentoNombre}";
                    archivo.SaveAs(carpeta + documentoNombre);
                }

                var model = new NotaCreditoProveedorViewModel
                {
                    CompraId = compraId,
                    NumeroNCProveedor = numeroNC,
                    FechaEmision = fechaEmision,
                    MontoTotal = montoTotal,
                    Motivo = motivo
                };

                int ncId = _facturaService.RegistrarNotaCreditoProveedor(
                    model, usuarioId, usuarioNombre, documentoRuta, documentoNombre);

                return Json(new { success = true, message = "Nota de Crédito del proveedor registrada correctamente.", ncId = ncId });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error: " + ex.Message });
            }
        }

        public JsonResult ObtenerNCProveedor(int compraId)
        {
            if (Session["UserRol"] == null)
                return Json(new { success = false, message = "Sesión expirada" }, JsonRequestBehavior.AllowGet);

            try
            {
                var nc = _facturaService.ObtenerNCProveedorPorCompra(compraId);
                if (nc == null)
                    return Json(new { success = false, message = "No se encontró NC para esta compra" }, JsonRequestBehavior.AllowGet);

                return Json(new
                {
                    success = true,
                    NumeroNCProveedor = nc.NumeroNCProveedor,
                    FechaEmision = nc.FechaEmision.ToString("dd/MM/yyyy"),
                    MontoTotal = nc.MontoTotal,
                    Motivo = nc.Motivo,
                    NumeroFactura = nc.NumeroFacturaCompra,
                    DocumentoRuta = nc.DocumentoRuta,
                    DocumentoNombre = nc.DocumentoNombre
                }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }
    }
}