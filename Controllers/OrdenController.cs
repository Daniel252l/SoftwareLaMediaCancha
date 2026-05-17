using LaMediaCancha.Models;
using LaMediaCancha.Models.ViewModels;
using LaMediaCancha.Services;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Web.Mvc;

namespace LaMediaCancha.Controllers
{
    public class OrdenController : Controller
    {
        private readonly OrdenService _ordenService;

        public OrdenController()
        {
            _ordenService = new OrdenService();
        }

        public ActionResult Index()
        {
            if (Session["UserRol"] == null)
                return RedirectToAction("Login", "Account");

            var mesas = _ordenService.ObtenerMesasActivas();
            return View(mesas);
        }

        public ActionResult Ordenar(int mesaId)
        {
            if (Session["UserRol"] == null)
                return RedirectToAction("Login", "Account");

            var mesa = _ordenService.ObtenerMesaPorId(mesaId);
            if (mesa == null)
                return RedirectToAction("Index");

            var model = new OrdenTomaPedidoAvanzadaViewModel
            {
                MesaId = mesa.MesaId,
                NumeroMesa = mesa.NumeroMesa,
                Ubicacion = mesa.Ubicacion,
                Productos = new List<OrdenProductoViewModel>(),
                Combos = new List<ComboSeleccionadoViewModel>(),
                CuentasPorPersona = new List<CuentaPersonaViewModel>(),
                UsarCuentasSeparadas = false
            };

            ViewBag.Productos = _ordenService.ObtenerProductosActivos();
            ViewBag.Combos = _ordenService.ObtenerCombosActivos();
            ViewBag.Ofertas = _ordenService.ObtenerOfertasActivas();

            return View(model);
        }

        public ActionResult Cuenta(int mesaId)
        {
            if (Session["UserRol"] == null)
                return RedirectToAction("Login", "Account");

            var orden = _ordenService.ObtenerOrdenActivaPorMesa(mesaId);
            if (orden == null)
            {
                TempData["Error"] = "No hay una orden activa para esta mesa";
                return RedirectToAction("Index");
            }

            var cuentasSeparadas = _ordenService.ObtenerCuentasPorOrden(orden.OrdenId);

            var model = new OrdenCuentaViewModel
            {
                OrdenId = orden.OrdenId,
                NumeroOrden = orden.NumeroOrden,
                MesaId = mesaId,
                ClienteNombre = orden.ClienteNombre,
                FechaApertura = orden.FechaApertura,
                Subtotal = orden.Subtotal,
                Impuesto = orden.Impuesto,
                Total = orden.Total,
                Observaciones = orden.Observaciones,
                Detalles = orden.Detalles.Select(d => new OrdenDetalleViewModel
                {
                    ProductoNombre = d.ProductoNombre,
                    Cantidad = d.Cantidad,
                    PrecioUnitario = d.PrecioUnitario,
                    Subtotal = d.Subtotal,
                    Nota = d.Nota
                }).ToList(),
                CuentasSeparadas = cuentasSeparadas
            };

            return View(model);
        }

        [HttpPost]
        public JsonResult GuardarOrdenAvanzada(OrdenTomaPedidoAvanzadaViewModel model)
        {
            if (Session["UserRol"] == null)
                return Json(new { success = false, message = "Sesión expirada" });

            try
            {
                int usuarioId = (int)Session["UserId"];
                string usuarioNombre = Session["UserNombre"]?.ToString() ?? "Usuario";
                string numeroOrden = _ordenService.GenerarNumeroOrden();

                int ordenId = 0;
                decimal subtotalGlobal = 0, impuestoGlobal = 0, totalGlobal = 0;
                List<int> ordenesPersonaIds = new List<int>();

                if (model.UsarCuentasSeparadas && model.CuentasPorPersona != null && model.CuentasPorPersona.Any())
                {
                    var ordenPrincipal = new OrdenModels.Orden
                    {
                        NumeroOrden = numeroOrden,
                        MesaId = model.MesaId,
                        ClienteNombre = "Cuentas Separadas",
                        ClienteTelefono = "",
                        FechaApertura = DateTime.Now,
                        Subtotal = 0,
                        Impuesto = 0,
                        Total = 0,
                        Estado = "Abierta",
                        Observaciones = model.Observaciones,
                        UsuarioId = usuarioId,
                        UsuarioNombre = usuarioNombre
                    };
                    ordenId = _ordenService.CrearOrden(ordenPrincipal);

                    foreach (var persona in model.CuentasPorPersona)
                    {
                        int ordenPersonaId = _ordenService.CrearOrdenPersona(ordenId, persona.NombreCliente);
                        ordenesPersonaIds.Add(ordenPersonaId);

                        decimal subtotalPersonaNormal = 0;
                        decimal subtotalPersonaOfertas = 0;

                        if (persona.Productos != null)
                        {
                            foreach (var producto in persona.Productos)
                            {
                                decimal montoProducto = producto.Cantidad * producto.PrecioUnitario;
                                var detalle = new OrdenModels.DetalleOrden
                                {
                                    ProductoId = producto.ProductoId,
                                    ProductoCodigo = "",
                                    ProductoNombre = producto.NombreProducto,
                                    Cantidad = producto.Cantidad,
                                    PrecioUnitario = producto.PrecioUnitario,
                                    Subtotal = montoProducto,
                                    Nota = producto.Nota,
                                    EsDeCombo = false,
                                    ComboId = null
                                };
                                _ordenService.AgregarDetalleOrdenPersona(ordenPersonaId, detalle);

                                if (producto.EsOferta)
                                    subtotalPersonaOfertas += montoProducto;
                                else
                                    subtotalPersonaNormal += montoProducto;
                            }
                        }

                        if (persona.Combos != null)
                        {
                            foreach (var combo in persona.Combos)
                            {
                                if (combo.VenderPorSeparado && combo.ProductosSeparados != null)
                                {
                                    foreach (var producto in combo.ProductosSeparados)
                                    {
                                        decimal montoProducto = producto.Cantidad * producto.PrecioUnitario;
                                        var detalle = new OrdenModels.DetalleOrden
                                        {
                                            ProductoId = producto.ProductoId,
                                            ProductoCodigo = "",
                                            ProductoNombre = producto.NombreProducto,
                                            Cantidad = producto.Cantidad,
                                            PrecioUnitario = producto.PrecioUnitario,
                                            Subtotal = montoProducto,
                                            Nota = producto.Nota,
                                            EsDeCombo = true,
                                            ComboId = combo.ComboId
                                        };
                                        _ordenService.AgregarDetalleOrdenPersona(ordenPersonaId, detalle);
                                        subtotalPersonaNormal += montoProducto;
                                    }
                                }
                                else
                                {
                                    var productosDelCombo = _ordenService.ObtenerProductosPorCombo(combo.ComboId);
                                    foreach (var productoCombo in productosDelCombo)
                                    {
                                        decimal cantidadTotal = productoCombo.CantidadIncluida * combo.Cantidad;
                                        decimal montoProducto = cantidadTotal * productoCombo.PrecioIndividual;
                                        var detalle = new OrdenModels.DetalleOrden
                                        {
                                            ProductoId = productoCombo.ProductoId,
                                            ProductoCodigo = "",
                                            ProductoNombre = productoCombo.ProductoNombre,
                                            Cantidad = cantidadTotal,
                                            PrecioUnitario = productoCombo.PrecioIndividual,
                                            Subtotal = montoProducto,
                                            Nota = $"Combo: {combo.NombreCombo}",
                                            EsDeCombo = true,
                                            ComboId = combo.ComboId
                                        };
                                        _ordenService.AgregarDetalleOrdenPersona(ordenPersonaId, detalle);
                                        subtotalPersonaNormal += montoProducto;
                                    }
                                }
                            }
                        }

                        decimal subtotalPersona = subtotalPersonaNormal + subtotalPersonaOfertas;
                        decimal impuestoPersona = subtotalPersonaNormal * 0.12m;
                        decimal totalPersona = subtotalPersona + impuestoPersona;

                        _ordenService.ActualizarTotalesOrdenPersona(ordenPersonaId, subtotalPersona, impuestoPersona, totalPersona);

                        subtotalGlobal += subtotalPersona;
                        impuestoGlobal += impuestoPersona;
                        totalGlobal += totalPersona;
                    }

                    ActualizarTotalesOrden(ordenId, subtotalGlobal, impuestoGlobal, totalGlobal);
                    _ordenService.ActualizarEstadoMesa(model.MesaId, "Ocupada");

                    // CORREGIDO: Generar tickets para cada persona
                    var tickets = new List<TicketViewModel>();
                    foreach (var personaId in ordenesPersonaIds)
                    {
                        var ticket = _ordenService.GenerarTicket(ordenId, personaId);
                        tickets.Add(ticket);
                    }

                    return Json(new
                    {
                        success = true,
                        message = "Pedido guardado exitosamente",
                        ordenId = ordenId,
                        esCuentasSeparadas = true,
                        tickets = tickets,
                        ordenesPersonaIds = ordenesPersonaIds
                    });
                }
                else
                {
                    if (model.Productos == null)
                        model.Productos = new List<OrdenProductoViewModel>();
                    if (model.Combos == null)
                        model.Combos = new List<ComboSeleccionadoViewModel>();

                    decimal subtotalNormal = 0;
                    decimal subtotalOfertas = 0;

                    foreach (var producto in model.Productos)
                    {
                        decimal monto = producto.Cantidad * producto.PrecioUnitario;
                        if (producto.EsOferta)
                            subtotalOfertas += monto;
                        else
                            subtotalNormal += monto;
                    }

                    foreach (var combo in model.Combos)
                    {
                        if (combo.VenderPorSeparado && combo.ProductosSeparados != null)
                        {
                            foreach (var producto in combo.ProductosSeparados)
                                subtotalNormal += producto.Cantidad * producto.PrecioUnitario;
                        }
                        else
                        {
                            var productosDelCombo = _ordenService.ObtenerProductosPorCombo(combo.ComboId);
                            foreach (var pc in productosDelCombo)
                                subtotalNormal += pc.CantidadIncluida * combo.Cantidad * pc.PrecioIndividual;
                        }
                    }

                    decimal subtotal = subtotalNormal + subtotalOfertas;
                    decimal impuesto = subtotalNormal * 0.12m;
                    decimal total = subtotal + impuesto;

                    var orden = new OrdenModels.Orden
                    {
                        NumeroOrden = numeroOrden,
                        MesaId = model.MesaId,
                        ClienteNombre = string.IsNullOrEmpty(model.ClienteNombre) ? "Cliente" : model.ClienteNombre,
                        ClienteTelefono = "",
                        FechaApertura = DateTime.Now,
                        Subtotal = subtotal,
                        Impuesto = impuesto,
                        Total = total,
                        Estado = "Abierta",
                        Observaciones = model.Observaciones,
                        UsuarioId = usuarioId,
                        UsuarioNombre = usuarioNombre
                    };
                    ordenId = _ordenService.CrearOrden(orden);

                    foreach (var producto in model.Productos)
                    {
                        var detalle = new OrdenModels.DetalleOrden
                        {
                            ProductoId = producto.ProductoId,
                            ProductoCodigo = "",
                            ProductoNombre = producto.NombreProducto,
                            Cantidad = producto.Cantidad,
                            PrecioUnitario = producto.PrecioUnitario,
                            Subtotal = producto.Cantidad * producto.PrecioUnitario,
                            Nota = producto.Nota,
                            EsDeCombo = false,
                            ComboId = null
                        };
                        _ordenService.AgregarDetalleOrden(ordenId, detalle);
                    }

                    foreach (var combo in model.Combos)
                    {
                        if (combo.VenderPorSeparado && combo.ProductosSeparados != null)
                        {
                            foreach (var producto in combo.ProductosSeparados)
                            {
                                var detalle = new OrdenModels.DetalleOrden
                                {
                                    ProductoId = producto.ProductoId,
                                    ProductoCodigo = "",
                                    ProductoNombre = producto.NombreProducto,
                                    Cantidad = producto.Cantidad,
                                    PrecioUnitario = producto.PrecioUnitario,
                                    Subtotal = producto.Cantidad * producto.PrecioUnitario,
                                    Nota = producto.Nota,
                                    EsDeCombo = true,
                                    ComboId = combo.ComboId
                                };
                                _ordenService.AgregarDetalleOrden(ordenId, detalle);
                            }
                        }
                        else
                        {
                            var productosDelCombo = _ordenService.ObtenerProductosPorCombo(combo.ComboId);
                            foreach (var productoCombo in productosDelCombo)
                            {
                                var detalle = new OrdenModels.DetalleOrden
                                {
                                    ProductoId = productoCombo.ProductoId,
                                    ProductoCodigo = "",
                                    ProductoNombre = productoCombo.ProductoNombre,
                                    Cantidad = productoCombo.CantidadIncluida * combo.Cantidad,
                                    PrecioUnitario = productoCombo.PrecioIndividual,
                                    Subtotal = productoCombo.CantidadIncluida * combo.Cantidad * productoCombo.PrecioIndividual,
                                    Nota = $"Combo: {combo.NombreCombo}",
                                    EsDeCombo = true,
                                    ComboId = combo.ComboId
                                };
                                _ordenService.AgregarDetalleOrden(ordenId, detalle);
                            }
                        }
                    }

                    _ordenService.ActualizarEstadoMesa(model.MesaId, "Ocupada");
                    var ticket = _ordenService.GenerarTicket(ordenId);

                    return Json(new
                    {
                        success = true,
                        message = "Pedido guardado exitosamente",
                        ordenId = ordenId,
                        esCuentasSeparadas = false,
                        ticket = ticket
                    });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public JsonResult CerrarOrden(int ordenId)
        {
            if (Session["UserRol"] == null)
                return Json(new { success = false, message = "Sesión expirada" });

            try
            {
                _ordenService.CerrarOrden(ordenId);
                return Json(new { success = true, message = "Cuenta cerrada exitosamente" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        public JsonResult GetProductos()
        {
            var productos = _ordenService.ObtenerProductosActivos();
            return Json(productos, JsonRequestBehavior.AllowGet);
        }

        public JsonResult GetCombos()
        {
            var combos = _ordenService.ObtenerCombosActivos();
            return Json(combos, JsonRequestBehavior.AllowGet);
        }

        public JsonResult GetOfertas()
        {
            var ofertas = _ordenService.ObtenerOfertasActivas();
            return Json(ofertas, JsonRequestBehavior.AllowGet);
        }

        public ActionResult GenerarTicket(int ordenId, int? ordenPersonaId = null)
        {
            if (Session["UserRol"] == null)
                return RedirectToAction("Login", "Account");

            try
            {
                var ticket = _ordenService.GenerarTicket(ordenId, ordenPersonaId);
                ViewBag.FormaPago = "Efectivo";
                return PartialView("_TicketPartial", ticket);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error GenerarTicket: {ex.Message}");
                return Content($"Error al generar el ticket: {ex.Message}");
            }
        }

        private void ActualizarTotalesOrden(int ordenId, decimal subtotal, decimal impuesto, decimal total)
        {
            string query = "UPDATE Orden SET Subtotal = @Subtotal, Impuesto = @Impuesto, Total = @Total WHERE OrdenId = @OrdenId";
            using (var conn = new System.Data.SqlClient.SqlConnection(
                System.Configuration.ConfigurationManager.ConnectionStrings["LaMediaCanchaDB"].ConnectionString))
            {
                conn.Open();
                using (var cmd = new System.Data.SqlClient.SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@OrdenId", ordenId);
                    cmd.Parameters.AddWithValue("@Subtotal", subtotal);
                    cmd.Parameters.AddWithValue("@Impuesto", impuesto);
                    cmd.Parameters.AddWithValue("@Total", total);
                    cmd.ExecuteNonQuery();
                }
            }
        }
    }
}