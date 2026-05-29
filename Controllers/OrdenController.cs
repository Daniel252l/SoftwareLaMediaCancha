using LaMediaCancha.Models;
using LaMediaCancha.Models.ViewModels;
using LaMediaCancha.Services;
using System;
using System.Collections.Generic;
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

            var mesas = _ordenService.ObtenerTodasLasMesasConSillas();
            return View(mesas);
        }

        public ActionResult SillasPorMesa(int mesaId, int numeroMesa)
        {
            if (Session["UserRol"] == null)
                return RedirectToAction("Login", "Account");

            var mesa = _ordenService.ObtenerMesaConSillasYPedidos(mesaId);
            if (mesa == null)
                return RedirectToAction("Index");

            ViewBag.NumeroMesa = numeroMesa;
            return View(mesa);
        }

        public ActionResult TomarPedido(int mesaId, int sillaId, int numeroSilla)
        {
            if (Session["UserRol"] == null)
                return RedirectToAction("Login", "Account");

            var mesa = _ordenService.ObtenerMesaPorId(mesaId);
            if (mesa == null)
                return RedirectToAction("Index");

            var ordenExistente = _ordenService.ObtenerOrdenActivaPorSilla(sillaId);

            if (ordenExistente == null)
            {
                int usuarioId = (int)Session["UserId"];
                string usuarioNombre = Session["UserNombre"]?.ToString() ?? "Usuario";
                int ordenPersonaId = _ordenService.CrearOrdenParaSilla(sillaId, mesaId, usuarioId, usuarioNombre);

                var model = new OrdenTomaPedidoAvanzadaViewModel
                {
                    MesaId = mesa.MesaId,
                    NumeroMesa = mesa.NumeroMesa,
                    NumeroSilla = numeroSilla,
                    SillaId = sillaId,
                    OrdenPersonaId = ordenPersonaId,
                    Ubicacion = mesa.Ubicacion,
                    Productos = new List<OrdenProductoViewModel>(),
                    Combos = new List<ComboSeleccionadoViewModel>(),
                    CuentasPorPersona = new List<CuentaPersonaViewModel>(),
                    UsarCuentasSeparadas = false
                };

                ViewBag.Productos = _ordenService.ObtenerProductosActivos();
                ViewBag.Combos = _ordenService.ObtenerCombosActivos();
                ViewBag.Ofertas = _ordenService.ObtenerOfertasActivas();

                return View("Ordenar", model);
            }
            else
            {
                var model = new OrdenTomaPedidoAvanzadaViewModel
                {
                    MesaId = mesa.MesaId,
                    NumeroMesa = mesa.NumeroMesa,
                    NumeroSilla = numeroSilla,
                    SillaId = sillaId,
                    OrdenPersonaId = ordenExistente.OrdenPersonaId,
                    Ubicacion = mesa.Ubicacion,
                    Productos = new List<OrdenProductoViewModel>(),
                    Combos = new List<ComboSeleccionadoViewModel>(),
                    CuentasPorPersona = new List<CuentaPersonaViewModel>(),
                    UsarCuentasSeparadas = false
                };

                var detalles = _ordenService.ObtenerDetallesPorOrdenPersona(ordenExistente.OrdenPersonaId.Value);
                foreach (var detalle in detalles)
                {
                    model.Productos.Add(new OrdenProductoViewModel
                    {
                        ProductoId = detalle.ProductoId,
                        NombreProducto = detalle.ProductoNombre,
                        Cantidad = detalle.Cantidad,
                        PrecioUnitario = detalle.PrecioUnitario,
                        Nota = detalle.Nota,
                        EsOferta = false
                    });
                }

                ViewBag.Productos = _ordenService.ObtenerProductosActivos();
                ViewBag.Combos = _ordenService.ObtenerCombosActivos();
                ViewBag.Ofertas = _ordenService.ObtenerOfertasActivas();

                return View("Ordenar", model);
            }
        }

        [HttpPost]
        public JsonResult GuardarPedidoSilla(OrdenTomaPedidoAvanzadaViewModel model)
        {
            if (Session["UserRol"] == null)
                return Json(new { success = false, message = "Sesión expirada" });

            try
            {
                int usuarioId = (int)Session["UserId"];
                string usuarioNombre = Session["UserNombre"]?.ToString() ?? "Usuario";

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

                if (model.OrdenPersonaId.HasValue && model.OrdenPersonaId.Value > 0)
                {
                    _ordenService.LimpiarDetallesOrdenPersona(model.OrdenPersonaId.Value);

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
                        _ordenService.AgregarDetalleOrdenPersona(model.OrdenPersonaId.Value, detalle);
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
                                _ordenService.AgregarDetalleOrdenPersona(model.OrdenPersonaId.Value, detalle);
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
                                _ordenService.AgregarDetalleOrdenPersona(model.OrdenPersonaId.Value, detalle);
                            }
                        }
                    }

                    _ordenService.ActualizarTotalesOrdenPersona(model.OrdenPersonaId.Value, subtotal, impuesto, total);
                    var ordenId = _ordenService.ObtenerOrdenIdPorPersona(model.OrdenPersonaId.Value);
                    _ordenService.RecalcularTotalesOrden(ordenId);
                }
                else
                {
                    string numeroOrden = _ordenService.GenerarNumeroOrden();

                    var ordenPrincipal = new OrdenModels.Orden
                    {
                        NumeroOrden = numeroOrden,
                        MesaId = model.MesaId,
                        ClienteNombre = $"Silla {model.NumeroSilla}",
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
                    int ordenId = _ordenService.CrearOrden(ordenPrincipal);

                    int ordenPersonaId = _ordenService.CrearOrdenPersonaConSilla(ordenId, model.SillaId.Value, $"Silla {model.NumeroSilla}");

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
                        _ordenService.AgregarDetalleOrdenPersona(ordenPersonaId, detalle);
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
                                _ordenService.AgregarDetalleOrdenPersona(ordenPersonaId, detalle);
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
                                _ordenService.AgregarDetalleOrdenPersona(ordenPersonaId, detalle);
                            }
                        }
                    }

                    _ordenService.ActualizarTotalesOrdenPersona(ordenPersonaId, subtotal, impuesto, total);
                }

                _ordenService.ActualizarEstadoMesa(model.MesaId, "Ocupada");
                _ordenService.ActualizarEstadoSilla(model.SillaId.Value, "Ocupada");

                return Json(new
                {
                    success = true,
                    message = "Pedido guardado exitosamente",
                    redirigir = true,
                    url = Url.Action("SillasPorMesa", "Orden", new { mesaId = model.MesaId, numeroMesa = model.NumeroMesa })
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public JsonResult CerrarCuentaSilla(int ordenPersonaId)
        {
            if (Session["UserRol"] == null)
                return Json(new { success = false, message = "Sesión expirada" });

            try
            {
                // Obtener el ticket antes de cerrar la cuenta
                var ordenPersona = _ordenService.ObtenerOrdenPersonaCompleta(ordenPersonaId);
                if (ordenPersona == null)
                    return Json(new { success = false, message = "No se encontró la orden" });

                var ordenId = ordenPersona.OrdenId;
                var ticket = _ordenService.GenerarTicket(ordenId, ordenPersonaId);

                // Cerrar la cuenta de la silla
                _ordenService.CerrarCuentaSilla(ordenPersonaId);

                return Json(new
                {
                    success = true,
                    message = "Cuenta de silla cerrada exitosamente",
                    mostrarTicket = true,
                    ticket = ticket
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public JsonResult CerrarCuentaMesaCompleta(int mesaId)
        {
            if (Session["UserRol"] == null)
                return Json(new { success = false, message = "Sesión expirada" });

            try
            {
                var ordenActiva = _ordenService.ObtenerOrdenActivaPorMesa(mesaId);

                if (ordenActiva == null)
                {
                    return Json(new { success = false, message = "No hay una orden activa para esta mesa" });
                }

                var ordenId = ordenActiva.OrdenId;
                var ticket = _ordenService.GenerarTicket(ordenId);

                _ordenService.CerrarCuentaMesaCompleta(mesaId);

                return Json(new
                {
                    success = true,
                    message = "Cuenta de mesa cerrada exitosamente",
                    mostrarTicket = true,
                    ticket = ticket
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public JsonResult AgregarSillaTemporal(int mesaId, int numeroSilla, string nombreCliente)
        {
            if (Session["UserRol"] == null)
                return Json(new { success = false, message = "Sesión expirada" });

            try
            {
                int sillaId = _ordenService.AgregarSillaTemporal(mesaId, numeroSilla, nombreCliente);
                return Json(new { success = true, message = "Silla temporal agregada exitosamente", sillaId = sillaId });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public JsonResult EliminarSillaTemporal(int sillaId)
        {
            if (Session["UserRol"] == null)
                return Json(new { success = false, message = "Sesión expirada" });

            try
            {
                _ordenService.EliminarSillaTemporal(sillaId);
                return Json(new { success = true, message = "Silla temporal eliminada exitosamente" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // ==================== HISTORIAL DE ÓRDENES ====================

        public ActionResult Historial(DateTime? fechaInicio, DateTime? fechaFin, int? numeroMesa, string estado, string buscar)
        {
            if (Session["UserRol"] == null)
                return RedirectToAction("Login", "Account");

            // Valores por defecto: últimos 7 días
            if (!fechaInicio.HasValue)
                fechaInicio = DateTime.Now.AddDays(-7);
            if (!fechaFin.HasValue)
                fechaFin = DateTime.Now;

            var ordenes = _ordenService.ObtenerHistorialOrdenes(fechaInicio, fechaFin, numeroMesa, estado, buscar);
            var resumen = _ordenService.ObtenerResumenEstadisticas(fechaInicio, fechaFin);

            ViewBag.FechaInicio = fechaInicio.Value.ToString("yyyy-MM-dd");
            ViewBag.FechaFin = fechaFin.Value.ToString("yyyy-MM-dd");
            ViewBag.NumeroMesa = numeroMesa;
            ViewBag.Estado = estado;
            ViewBag.Buscar = buscar;
            ViewBag.Resumen = resumen;

            return View(ordenes);
        }

        [HttpGet]
        public ActionResult DetalleHistorial(int ordenId)
        {
            if (Session["UserRol"] == null)
                return Json(new { success = false, message = "Sesión expirada" }, JsonRequestBehavior.AllowGet);

            try
            {
                var orden = _ordenService.ObtenerHistorialOrdenPorId(ordenId);

                if (orden == null)
                    return Json(new { success = false, message = "Orden no encontrada" }, JsonRequestBehavior.AllowGet);

                return Json(new
                {
                    success = true,
                    OrdenId = orden.OrdenId,
                    NumeroOrden = orden.NumeroOrden,
                    NumeroMesa = orden.NumeroMesa,
                    ClienteNombre = orden.ClienteNombre,
                    Subtotal = orden.Subtotal,
                    Impuesto = orden.Impuesto,
                    Total = orden.Total,
                    FechaApertura = orden.FechaApertura.ToString("dd/MM/yyyy HH:mm:ss"), // <- Fecha formateada
                    Detalles = orden.Detalles.Select(d => new
                    {
                        d.ProductoNombre,
                        Cantidad = d.Cantidad,
                        PrecioUnitario = d.PrecioUnitario,
                        Subtotal = d.Subtotal,
                        EsDeCombo = d.EsDeCombo,
                        Nota = d.Nota
                    }),
                    Sillas = orden.Sillas.Select(s => new
                    {
                        s.NumeroSilla,
                        s.NombreCliente,
                        s.Total,
                        s.Pagado
                    })
                }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        public JsonResult ReimprimirTicket(int ordenId, int? ordenPersonaId = null)
        {
            if (Session["UserRol"] == null)
                return Json(new { success = false, message = "Sesión expirada" });

            try
            {
                var ticket = _ordenService.GenerarTicket(ordenId, ordenPersonaId);
                return Json(new { success = true, ticket = ticket });
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
    }
}