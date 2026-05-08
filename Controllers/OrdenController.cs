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

                // Verificar si es cuentas separadas
                if (model.UsarCuentasSeparadas && model.CuentasPorPersona != null && model.CuentasPorPersona.Any())
                {
                    // Crear orden principal
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
                        decimal subtotalPersona = 0;

                        // Agregar productos individuales de la persona
                        if (persona.Productos != null)
                        {
                            foreach (var producto in persona.Productos)
                            {
                                decimal subtotalProducto = producto.Cantidad * producto.PrecioUnitario;
                                var detalle = new OrdenModels.DetalleOrden
                                {
                                    ProductoId = producto.ProductoId,
                                    ProductoNombre = producto.NombreProducto,
                                    Cantidad = producto.Cantidad,
                                    PrecioUnitario = producto.PrecioUnitario,
                                    Subtotal = subtotalProducto,
                                    Nota = producto.Nota,
                                    EsDeCombo = false,
                                    ComboId = null
                                };
                                _ordenService.AgregarDetalleOrdenPersona(ordenPersonaId, detalle);
                                subtotalPersona += subtotalProducto;

                                // Debug
                                System.Diagnostics.Debug.WriteLine($"Producto guardado: {producto.NombreProducto} - Cantidad: {producto.Cantidad} - Subtotal: {subtotalProducto}");
                            }
                        }

                        // Agregar combos de la persona
                        if (persona.Combos != null)
                        {
                            foreach (var combo in persona.Combos)
                            {
                                if (combo.VenderPorSeparado && combo.ProductosSeparados != null)
                                {
                                    foreach (var producto in combo.ProductosSeparados)
                                    {
                                        decimal subtotalProducto = producto.Cantidad * producto.PrecioUnitario;
                                        var detalle = new OrdenModels.DetalleOrden
                                        {
                                            ProductoId = producto.ProductoId,
                                            ProductoNombre = producto.NombreProducto,
                                            Cantidad = producto.Cantidad,
                                            PrecioUnitario = producto.PrecioUnitario,
                                            Subtotal = subtotalProducto,
                                            Nota = $"Combo {combo.NombreCombo} - {producto.Nota}",
                                            EsDeCombo = true,
                                            ComboId = combo.ComboId
                                        };
                                        _ordenService.AgregarDetalleOrdenPersona(ordenPersonaId, detalle);
                                        subtotalPersona += subtotalProducto;
                                    }
                                }
                                else
                                {
                                    var productosDelCombo = _ordenService.ObtenerProductosPorCombo(combo.ComboId);
                                    foreach (var productoCombo in productosDelCombo)
                                    {
                                        decimal cantidadTotal = productoCombo.CantidadIncluida * combo.Cantidad;
                                        decimal subtotalProducto = cantidadTotal * productoCombo.PrecioIndividual;

                                        var detalle = new OrdenModels.DetalleOrden
                                        {
                                            ProductoId = productoCombo.ProductoId,
                                            ProductoNombre = productoCombo.ProductoNombre,
                                            Cantidad = cantidadTotal,
                                            PrecioUnitario = productoCombo.PrecioIndividual,
                                            Subtotal = subtotalProducto,
                                            Nota = $"Combo: {combo.NombreCombo}",
                                            EsDeCombo = true,
                                            ComboId = combo.ComboId
                                        };
                                        _ordenService.AgregarDetalleOrdenPersona(ordenPersonaId, detalle);
                                        subtotalPersona += subtotalProducto;
                                    }
                                }
                            }
                        }

                        decimal impuestoPersona = subtotalPersona * 0.12m;
                        decimal totalPersona = subtotalPersona + impuestoPersona;
                        _ordenService.ActualizarTotalesOrdenPersona(ordenPersonaId, subtotalPersona, impuestoPersona, totalPersona);

                        subtotalGlobal += subtotalPersona;
                        impuestoGlobal += impuestoPersona;
                        totalGlobal += totalPersona;
                    }

                    ActualizarTotalesOrden(ordenId, subtotalGlobal, impuestoGlobal, totalGlobal);
                }
                else
                {
                    // Modo normal
                    if (model.Productos == null)
                        model.Productos = new List<OrdenProductoViewModel>();
                    if (model.Combos == null)
                        model.Combos = new List<ComboSeleccionadoViewModel>();

                    // Calcular subtotal de productos individuales
                    decimal subtotal = 0;
                    foreach (var producto in model.Productos)
                    {
                        subtotal += producto.Cantidad * producto.PrecioUnitario;
                    }

                    // Calcular subtotal de combos (expandiendo a productos individuales)
                    foreach (var combo in model.Combos)
                    {
                        if (combo.VenderPorSeparado && combo.ProductosSeparados != null)
                        {
                            foreach (var producto in combo.ProductosSeparados)
                            {
                                subtotal += producto.Cantidad * producto.PrecioUnitario;
                            }
                        }
                        else
                        {
                            var productosDelCombo = _ordenService.ObtenerProductosPorCombo(combo.ComboId);
                            foreach (var productoCombo in productosDelCombo)
                            {
                                subtotal += productoCombo.CantidadIncluida * combo.Cantidad * productoCombo.PrecioIndividual;
                            }
                        }
                    }

                    decimal impuesto = subtotal * 0.12m;
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

                    // Guardar productos individuales
                    if (model.Productos != null)
                    {
                        foreach (var producto in model.Productos)
                        {
                            decimal subtotalProducto = producto.Cantidad * producto.PrecioUnitario;
                            var detalle = new OrdenModels.DetalleOrden
                            {
                                ProductoId = producto.ProductoId,
                                ProductoNombre = producto.NombreProducto,
                                Cantidad = producto.Cantidad,
                                PrecioUnitario = producto.PrecioUnitario,
                                Subtotal = subtotalProducto,
                                Nota = producto.Nota,
                                EsDeCombo = false,
                                ComboId = null
                            };
                            _ordenService.AgregarDetalleOrden(ordenId, detalle);

                            // Debug
                            System.Diagnostics.Debug.WriteLine($"Producto guardado: {producto.NombreProducto} - Cantidad: {producto.Cantidad} - Subtotal: {subtotalProducto}");
                        }
                    }

                    // Guardar combos (expandiendo a productos individuales)
                    if (model.Combos != null)
                    {
                        foreach (var combo in model.Combos)
                        {
                            if (combo.VenderPorSeparado && combo.ProductosSeparados != null)
                            {
                                foreach (var producto in combo.ProductosSeparados)
                                {
                                    decimal subtotalProducto = producto.Cantidad * producto.PrecioUnitario;
                                    var detalle = new OrdenModels.DetalleOrden
                                    {
                                        ProductoId = producto.ProductoId,
                                        ProductoNombre = producto.NombreProducto,
                                        Cantidad = producto.Cantidad,
                                        PrecioUnitario = producto.PrecioUnitario,
                                        Subtotal = subtotalProducto,
                                        Nota = $"Combo {combo.NombreCombo} - {producto.Nota}",
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
                                    decimal cantidadTotal = productoCombo.CantidadIncluida * combo.Cantidad;
                                    decimal subtotalProducto = cantidadTotal * productoCombo.PrecioIndividual;

                                    var detalle = new OrdenModels.DetalleOrden
                                    {
                                        ProductoId = productoCombo.ProductoId,
                                        ProductoNombre = productoCombo.ProductoNombre,
                                        Cantidad = cantidadTotal,
                                        PrecioUnitario = productoCombo.PrecioIndividual,
                                        Subtotal = subtotalProducto,
                                        Nota = $"Combo: {combo.NombreCombo}",
                                        EsDeCombo = true,
                                        ComboId = combo.ComboId
                                    };
                                    _ordenService.AgregarDetalleOrden(ordenId, detalle);
                                }
                            }
                        }
                    }
                }

                _ordenService.ActualizarEstadoMesa(model.MesaId, "Ocupada");

                // Verificar que los detalles se guardaron correctamente
                var detallesVerificacion = _ordenService.ObtenerDetallesOrden(ordenId);
                decimal subtotalVerificacion = detallesVerificacion.Sum(d => d.Subtotal);
                System.Diagnostics.Debug.WriteLine($"=== VERIFICACIÓN FINAL ===");
                System.Diagnostics.Debug.WriteLine($"OrdenId: {ordenId}");
                System.Diagnostics.Debug.WriteLine($"Total calculado en orden: {totalGlobal}");
                System.Diagnostics.Debug.WriteLine($"Subtotal de detalles: {subtotalVerificacion}");
                System.Diagnostics.Debug.WriteLine($"Cantidad de detalles: {detallesVerificacion.Count}");

                var ticket = _ordenService.GenerarTicket(ordenId);

                return Json(new
                {
                    success = true,
                    message = "Pedido guardado exitosamente",
                    ordenId = ordenId,
                    ticket = ticket
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ERROR: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"STACK TRACE: {ex.StackTrace}");
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

            var ticket = _ordenService.GenerarTicket(ordenId, ordenPersonaId);

            if (Request.IsAjaxRequest())
            {
                return PartialView("_TicketPartial", ticket);
            }

            return View(ticket);
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