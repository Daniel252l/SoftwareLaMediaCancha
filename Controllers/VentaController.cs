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

        // Ver detalle de factura
        public ActionResult Factura(int id)
        {
            if (Session["UserRol"] == null)
                return RedirectToAction("Login", "Account");

            var venta = _ventaService.ObtenerVentaPorId(id);
            if (venta == null)
                return HttpNotFound();

            return View(venta);
        }

        // Imprimir factura
        public ActionResult ImprimirFactura(int id)
        {
            if (Session["UserRol"] == null)
                return RedirectToAction("Login", "Account");

            var venta = _ventaService.ObtenerVentaPorId(id);
            if (venta == null)
                return HttpNotFound();

            return View("ImprimirFactura", venta);
        }

        // Ver detalle de venta
        public ActionResult DetalleVenta(int id)
        {
            if (Session["UserRol"] == null)
                return RedirectToAction("Login", "Account");

            var venta = _ventaService.ObtenerVentaPorId(id);
            if (venta == null)
                return HttpNotFound();

            return View(venta);
        }

        // ==================== MÉTODOS PARA AGREGAR, EDITAR Y ELIMINAR PRODUCTOS ====================

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

                // Verificar si el código ya existe
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

                // Verificar si el código ya existe (excluyendo el producto actual)
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

        [HttpPost]
        public JsonResult EliminarProductoDuplicado(int productoId)
        {
            if (Session["UserRol"] == null)
                return Json(new { success = false, message = "Sesión expirada" });

            try
            {
                bool eliminado = _ventaService.EliminarProductoFisico(productoId);
                if (eliminado)
                    return Json(new { success = true, message = "Producto duplicado eliminado correctamente" });
                else
                    return Json(new { success = false, message = "No se encontró el producto" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
    }
}