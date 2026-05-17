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
                return Content("<tr><td colspan='8' class='text-center text-muted py-4'><i class='fas fa-info-circle fa-2x mb-2 d-block'></i>No se encontraron facturas de compras</td></tr>");

            var html = new System.Text.StringBuilder();

            foreach (var factura in facturas)
            {
                string rowClass = factura.Estado == "Anulada" ? "factura-anulada" : "";
                string estadoBadge = factura.Estado == "Vigente"
                    ? "<span class='badge-vigente'>Vigente</span>"
                    : "<span class='badge-anulada'>Anulada</span>";

                string notaCredito = !string.IsNullOrEmpty(factura.NumeroNotaCredito)
                    ? $"<span class='badge-nc'>{factura.NumeroNotaCredito}</span>"
                    : "<span class='badge-nc-vacio'>—</span>";

                // ── Botones de acción ──────────────────────────────────────────
                string btnVer = $"<a href='{Url.Action("Detalle", new { id = factura.FacturaId })}' class='btn-ver' title='Ver detalle'><i class='fas fa-eye'></i> Ver</a>";
                string btnImprimir = $"<a href='{Url.Action("Imprimir", new { id = factura.FacturaId })}' class='btn-imprimir' title='Imprimir' target='_blank'><i class='fas fa-print'></i> Imprimir</a>";

                string btnNC = "";
                if (factura.Estado == "Vigente")
                {
                    // Factura vigente → registrar NC del proveedor
                    btnNC = $"<a href='javascript:void(0)' onclick='abrirModalNCProveedor({factura.FacturaId}, \"{factura.NumeroFactura}\")' class='btn-nc' title='Registrar NC Proveedor'><i class='fas fa-file-invoice'></i> NC</a>";
                }
                else if (factura.Estado == "Anulada" && factura.NotaCreditoId.HasValue)
                {
                    // Factura anulada con NC → ver la NC
                    btnNC = $"<a href='javascript:void(0)' onclick='verNotaCredito({factura.NotaCreditoId})' class='btn-nc' title='Ver NC'><i class='fas fa-file-invoice'></i> NC</a>";
                }

                string acciones = $"<div class='acciones-btns'>{btnVer}{btnImprimir}{btnNC}</div>";

                html.Append($@"
                    <tr class='{rowClass}'>
                        <td><strong>{factura.NumeroFactura}</strong></td>
                        <td>{factura.FechaEmision:dd/MM/yyyy HH:mm}</td>
                        <td>{factura.ClienteNombre}</td>
                        <td>{factura.NumeroDocumento ?? "—"}</td>
                        <td class='text-end'>{factura.Total:N2}</td>
                        <td class='text-center'>{estadoBadge}</td>
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
                return Content("<tr><td colspan='8' class='text-center text-muted py-4'><i class='fas fa-info-circle fa-2x mb-2 d-block'></i>No se encontraron facturas de ventas</td></tr>");

            var html = new System.Text.StringBuilder();

            foreach (var venta in ventas)
            {
                string rowClass = venta.Estado == "Anulada" ? "factura-anulada" : "";
                string estadoBadge = venta.Estado == "Completada"
                    ? "<span class='badge-vigente'>Completada</span>"
                    : "<span class='badge-anulada'>Anulada</span>";

                string notaCredito = "<span class='badge-nc-vacio'>—</span>";

                // Botón Ver – DetalleVenta en VentaController
                string btnVer = $"<a href='{Url.Action("DetalleVenta", "Venta", new { id = venta.VentaId })}' class='btn-ver' title='Ver detalle'><i class='fas fa-eye'></i> Ver</a>";

                // Botón Imprimir – ImprimirFactura en VentaController
                string btnImprimir = $"<a href='{Url.Action("Factura", "Venta", new { id = venta.VentaId })}' class='btn-imprimir' title='Imprimir' target='_blank'><i class='fas fa-print'></i> Imprimir</a>";

                // Botón Anular – solo si está Completada
                string btnAnular = "";
                if (venta.Estado == "Completada")
                {
                    btnAnular = $"<a href='javascript:void(0)' onclick='anularFacturaVenta({venta.VentaId}, \"{venta.NumeroFactura}\")' class='btn-anular' title='Anular'><i class='fas fa-ban'></i> Anular</a>";
                }

                string acciones = $"<div class='acciones-btns'>{btnVer}{btnImprimir}{btnAnular}</div>";

                html.Append($@"
                    <tr class='{rowClass}'>
                        <td><strong>{venta.NumeroFactura}</strong></td>
                        <td>{venta.FechaVenta:dd/MM/yyyy HH:mm}</td>
                        <td>{venta.ClienteNombre}</td>
                        <td>{venta.ClienteDocumento ?? "—"}</td>
                        <td class='text-end fw-bold'>{venta.Total:N2}</td>
                        <td class='text-center'>{estadoBadge}</td>
                        <td class='text-center'>{notaCredito}</td>
                        <td class='text-center'>{acciones}</td>
                    </tr>
                ");
            }

            return Content(html.ToString());
        }

        // Detalle para compras
        public ActionResult Detalle(int id)
        {
            if (Session["UserRol"] == null)
                return RedirectToAction("Login", "Account");

            var factura = _facturaService.ObtenerFacturaPorId(id);
            if (factura == null) return HttpNotFound();

            return View(factura);
        }

        // Imprimir para compras
        public ActionResult Imprimir(int id)
        {
            if (Session["UserRol"] == null)
                return RedirectToAction("Login", "Account");

            var factura = _facturaService.ObtenerFacturaPorId(id);
            if (factura == null) return HttpNotFound();

            return View("ImprimirFactura", factura);
        }

        // Anular Venta
        [HttpPost]
        public JsonResult AnularVenta(int facturaId, string motivoAnulacion)
        {
            if (Session["UserRol"] == null)
                return Json(new { success = false, message = "Sesión expirada" });

            try
            {
                int usuarioId = (int)Session["UserId"];
                string usuarioNombre = Session["UserNombre"]?.ToString() ?? "Usuario";

                bool resultado = _facturaService.AnularVenta(facturaId, motivoAnulacion, usuarioId, usuarioNombre);
                if (resultado)
                    return Json(new { success = true, message = "Factura anulada exitosamente" });
                else
                    return Json(new { success = false, message = "Error al anular la factura" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
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
            try
            {
                int facturaId = Convert.ToInt32(Request.Form["CompraId"]);

                // Obtener el CompraId real desde la Factura
                int compraId = 0;
                using (var conn = new System.Data.SqlClient.SqlConnection(
                    System.Configuration.ConfigurationManager.ConnectionStrings["LaMediaCanchaDB"].ConnectionString))
                {
                    conn.Open();
                    var cmd = new System.Data.SqlClient.SqlCommand(
                        "SELECT ISNULL(CompraId, 0) FROM Factura WHERE FacturaId = @FacturaId", conn);
                    cmd.Parameters.AddWithValue("@FacturaId", facturaId);
                    compraId = Convert.ToInt32(cmd.ExecuteScalar());
                }

                if (compraId == 0)
                    return Json(new { success = false, message = "No se encontró la compra asociada a esta factura" });

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

                return Json(new { success = true, message = "Nota de Crédito registrada correctamente.", ncId = ncId });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error: " + ex.Message });
            }
        }
    }
}