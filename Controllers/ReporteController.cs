using LaMediaCancha.Models.ViewModels;
using LaMediaCancha.Services;
using Microsoft.Reporting.WebForms;
using System;
using System.Collections.Generic;
using System.Web.Mvc;

namespace LaMediaCancha.Controllers
{
    public class ReporteController : Controller
    {
        private readonly ReporteService _reporteService;

        public ReporteController()
        {
            _reporteService = new ReporteService();
        }

        public ActionResult Index()
        {
            if (Session["UserRol"] == null)
                return RedirectToAction("Login", "Account");

            var model = new ReporteFiltrosViewModel
            {
                FechaInicio = DateTime.Now.AddDays(-30),
                FechaFin = DateTime.Now
            };

            return View(model);
        }

        [HttpPost]
        public ActionResult GenerarReporte(ReporteFiltrosViewModel filtros)
        {
            if (Session["UserRol"] == null)
                return RedirectToAction("Login", "Account");

            ViewBag.TipoReporte = ObtenerNombreReporte(filtros.TipoReporte);
            ViewBag.FechaInicio = filtros.FechaInicio.ToString("dd/MM/yyyy");
            ViewBag.FechaFin = filtros.FechaFin.ToString("dd/MM/yyyy");
            ViewBag.TipoReporteCodigo = filtros.TipoReporte;

            return View("VerReporte");
        }

        [HttpGet]
        public ActionResult RenderReport(string tipo, DateTime fechaInicio, DateTime fechaFin)
        {
            if (Session["UserRol"] == null)
                return RedirectToAction("Login", "Account");

            try
            {
                byte[] reportBytes = GenerarReportePDF(tipo, fechaInicio, fechaFin);
                return File(reportBytes, "application/pdf", $"Reporte_{tipo}_{DateTime.Now:yyyyMMddHHmmss}.pdf");
            }
            catch (Exception ex)
            {
                return Content($"Error al generar el reporte: {ex.Message}");
            }
        }

        private byte[] GenerarReportePDF(string tipo, DateTime fechaInicio, DateTime fechaFin)
        {
            var reportViewer = new ReportViewer();
            reportViewer.ProcessingMode = ProcessingMode.Local;
            reportViewer.LocalReport.ReportEmbeddedResource = $"LaMediaCancha.Reports.{tipo}.rdlc";

            // Configurar parámetros
            var parameters = new List<ReportParameter>
            {
                new ReportParameter("FechaInicio", fechaInicio.ToString("dd/MM/yyyy")),
                new ReportParameter("FechaFin", fechaFin.ToString("dd/MM/yyyy")),
                new ReportParameter("Usuario", Session["UserNombre"]?.ToString() ?? "Usuario"),
                new ReportParameter("FechaGeneracion", DateTime.Now.ToString("dd/MM/yyyy HH:mm"))
            };

            // Cargar datos según el tipo de reporte
            switch (tipo)
            {
                case "Ventas":
                    var ventas = _reporteService.ObtenerDatasetVentas(fechaInicio, fechaFin);
                    reportViewer.LocalReport.DataSources.Add(new ReportDataSource("dsVentas", ventas));
                    break;

                case "ProductosMasVendidos":
                    var productos = _reporteService.ObtenerDatasetProductosMasVendidos(fechaInicio, fechaFin);
                    reportViewer.LocalReport.DataSources.Add(new ReportDataSource("dsProductos", productos));
                    break;

                case "Inventario":
                    var inventario = _reporteService.ObtenerDatasetInventario();
                    reportViewer.LocalReport.DataSources.Add(new ReportDataSource("dsInventario", inventario));
                    break;

                case "CajaDiaria":
                    var caja = _reporteService.ObtenerDatasetCajaDiaria(fechaInicio);
                    reportViewer.LocalReport.DataSources.Add(new ReportDataSource("dsCajaDiaria", caja));
                    break;

                case "Utilidad":
                    var utilidad = _reporteService.ObtenerDatasetUtilidad(fechaInicio, fechaFin);
                    reportViewer.LocalReport.DataSources.Add(new ReportDataSource("dsUtilidad", utilidad));
                    break;

                default:
                    throw new Exception("Tipo de reporte no válido");
            }

            reportViewer.LocalReport.SetParameters(parameters);
            reportViewer.LocalReport.Refresh();

            // Renderizar a PDF
            string mimeType, encoding, extension;
            string[] streamids;
            Warning[] warnings;
            byte[] reportBytes = reportViewer.LocalReport.Render("PDF", null, out mimeType, out encoding, out extension, out streamids, out warnings);

            return reportBytes;
        }

        private string ObtenerNombreReporte(string tipo)
        {
            switch (tipo)
            {
                case "Ventas": return "Ventas por período";
                case "ProductosMasVendidos": return "Productos más vendidos";
                case "Inventario": return "Inventario actual";
                case "CajaDiaria": return "Caja diaria";
                case "Utilidad": return "Utilidad por período";
                default: return tipo;
            }
        }
    }
}