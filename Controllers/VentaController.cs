using LaMediaCancha.Models;
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
                return RedirectToAction("Login", "Account");

            var productos = _ventaService.ObtenerProductosConLotes();
            return View(productos);
        }

        public ActionResult Carrito()
        {
            if (Session["UserRol"] == null)
                return RedirectToAction("Login", "Account");
            return View();
        }
        public ActionResult Facturar()
        {
            if (Session["UserRol"] == null)
                return RedirectToAction("Login", "Account");

            return View();
        }

        public ActionResult Factura(int id)
        {
            if (Session["UserRol"] == null)
                return RedirectToAction("Login", "Account");

            var venta = _ventaService.ObtenerVentaPorId(id);
            if (venta == null)
                return HttpNotFound();

            return View(venta);
        }

        [HttpPost]
        public JsonResult AplicarFIFO(int productoId, decimal cantidad)
        {
            if (Session["UserRol"] == null)
                return Json(new { success = false, message = "Sesión expirada" });

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
                return Json(new { success = false, message = "Sesión expirada" });

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

        [HttpPost]
        public JsonResult VerificarStockParaVenta(int productoId, decimal cantidad)
        {
            if (Session["UserRol"] == null)
                return Json(new { success = false, message = "Sesión expirada" });

            try
            {
                var verificacion = _ventaService.VerificarStockParaVenta(productoId, cantidad);

                return Json(new
                {
                    success = true,
                    hayStock = verificacion.HayStock,
                    mensaje = verificacion.Mensaje,
                    esProductoSimple = verificacion.EsProductoSimple,
                    detalles = verificacion.Detalles.Select(d => new
                    {
                        d.ProductoCompraId,
                        d.ProductoCompraNombre,
                        d.CantidadNecesaria,
                        d.CantidadTotal,
                        d.UnidadMedida,
                        d.StockDisponible,
                        d.Suficiente
                    })
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public JsonResult AgregarProducto(string nombre, string codigo, decimal precio)
        {
            if (Session["UserRol"] == null)
                return Json(new { success = false, message = "Sesión expirada" });

            try
            {
                if (string.IsNullOrEmpty(nombre))
                    return Json(new { success = false, message = "El nombre del producto es requerido" });

                if (string.IsNullOrEmpty(codigo))
                    return Json(new { success = false, message = "El código del producto es requerido" });

                if (precio <= 0)
                    return Json(new { success = false, message = "El precio debe ser mayor a cero" });

                if (_ventaService.ExisteCodigoProducto(codigo))
                    return Json(new { success = false, message = "Ya existe un producto con este código" });

                int nuevoId = _ventaService.AgregarProducto(nombre, codigo, precio);
                return Json(new { success = true, message = "Producto agregado exitosamente", productoId = nuevoId });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public JsonResult EditarProducto(int productoId, string nombre, string codigo, decimal precio)
        {
            if (Session["UserRol"] == null)
                return Json(new { success = false, message = "Sesión expirada" });

            try
            {
                if (productoId <= 0)
                    return Json(new { success = false, message = "ID de producto inválido" });

                if (string.IsNullOrEmpty(nombre))
                    return Json(new { success = false, message = "El nombre del producto es requerido" });

                if (string.IsNullOrEmpty(codigo))
                    return Json(new { success = false, message = "El código del producto es requerido" });

                if (precio <= 0)
                    return Json(new { success = false, message = "El precio debe ser mayor a cero" });

                if (_ventaService.ExisteCodigoProducto(codigo, productoId))
                    return Json(new { success = false, message = "Ya existe otro producto con este código" });

                bool editado = _ventaService.EditarProducto(productoId, nombre, codigo, precio);
                if (editado)
                    return Json(new { success = true, message = "Producto actualizado exitosamente" });
                else
                    return Json(new { success = false, message = "No se encontró el producto" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public JsonResult EliminarProducto(int productoId)
        {
            if (Session["UserRol"] == null)
                return Json(new { success = false, message = "Sesión expirada" });

            try
            {
                if (productoId <= 0)
                    return Json(new { success = false, message = "ID de producto inválido" });

                bool eliminado = _ventaService.EliminarProducto(productoId);
                if (eliminado)
                    return Json(new { success = true, message = "Producto eliminado correctamente" });
                else
                    return Json(new { success = false, message = "No se encontró el producto" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public JsonResult GetMateriasPrimas()
        {
            if (Session["UserRol"] == null)
                return Json(new { success = false, message = "Sesión expirada" }, JsonRequestBehavior.AllowGet);

            try
            {
                var materiasPrimas = _ventaService.ObtenerMateriasPrimas();
                return Json(materiasPrimas, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        public JsonResult AgregarProductoCompleto()
        {
            if (Session["UserRol"] == null)
                return Json(new { success = false, message = "Sesión expirada" });

            try
            {
                // Leer el cuerpo de la solicitud como string
                string jsonString = "";
                using (var reader = new System.IO.StreamReader(Request.InputStream))
                {
                    jsonString = reader.ReadToEnd();
                }

                // Deserializar usando Newtonsoft.Json
                var data = Newtonsoft.Json.Linq.JObject.Parse(jsonString);

                string nombre = data["nombre"]?.ToString() ?? "";
                string codigo = data["codigo"]?.ToString() ?? "";

                decimal precio = 0;
                if (data["precio"] != null)
                {
                    decimal.TryParse(data["precio"].ToString(), System.Globalization.NumberStyles.Any,
                        System.Globalization.CultureInfo.InvariantCulture, out precio);
                }

                decimal rendimiento = 1;
                if (data["rendimiento"] != null)
                {
                    decimal.TryParse(data["rendimiento"].ToString(), System.Globalization.NumberStyles.Any,
                        System.Globalization.CultureInfo.InvariantCulture, out rendimiento);
                }

                var ingredientes = new List<object>();

                var ingredientesArray = data["ingredientes"] as Newtonsoft.Json.Linq.JArray;
                if (ingredientesArray != null)
                {
                    foreach (var item in ingredientesArray)
                    {
                        string codigoMP = item["codigo"]?.ToString() ?? "";
                        decimal cantidad = 0;

                        if (item["cantidad"] != null)
                        {
                            decimal.TryParse(item["cantidad"].ToString(), System.Globalization.NumberStyles.Any,
                                System.Globalization.CultureInfo.InvariantCulture, out cantidad);
                        }

                        if (!string.IsNullOrEmpty(codigoMP) && cantidad > 0)
                        {
                            ingredientes.Add(new { codigo = codigoMP, cantidad = cantidad });
                        }
                    }
                }

                // Validaciones
                if (string.IsNullOrEmpty(nombre))
                    return Json(new { success = false, message = "El nombre del producto es requerido" });

                if (string.IsNullOrEmpty(codigo))
                    return Json(new { success = false, message = "El código del producto es requerido" });

                if (precio <= 0)
                    return Json(new { success = false, message = "El precio debe ser mayor a cero" });

                if (ingredientes.Count == 0)
                    return Json(new { success = false, message = "Debe agregar al menos un ingrediente" });

                if (_ventaService.ExisteCodigoProducto(codigo))
                    return Json(new { success = false, message = "Ya existe un producto con este código" });

                int productoId = _ventaService.AgregarProductoCompleto(nombre, codigo, precio, rendimiento, ingredientes);
                return Json(new { success = true, message = "Producto agregado exitosamente", productoId = productoId });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
    }
}
    