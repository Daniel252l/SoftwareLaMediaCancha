using LaMediaCancha.Models.ViewModels;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;
using System.Web.Mvc;

namespace LaMediaCancha.Controllers
{
    public class OfertaController : Controller
    {
        private readonly string _connectionString;

        public OfertaController()
        {
            _connectionString = ConfigurationManager.ConnectionStrings["LaMediaCanchaDB"].ConnectionString;
        }

        public ActionResult Index()
        {
            if (Session["UserRol"] == null)
                return RedirectToAction("Login", "Account");

            var ofertas = new List<OfertaViewModel>();

            string query = @"
                SELECT 
                    o.OfertaId, o.Nombre, o.Descripcion, o.ProductoId, 
                    p.Nombre AS ProductoNombre, p.PrecioVenta AS PrecioOriginal,
                    o.DescuentoPorcentaje,
                    (p.PrecioVenta - (p.PrecioVenta * o.DescuentoPorcentaje / 100)) AS PrecioOferta,
                    o.FechaInicio, o.FechaFin,
                    DATEDIFF(DAY, GETDATE(), o.FechaFin) AS DiasRestantes
                FROM Oferta o
                INNER JOIN Producto p ON o.ProductoId = p.ProductoId
                WHERE o.Activo = 1
                ORDER BY o.FechaFin ASC";

            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand(query, conn))
            {
                conn.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var fechaFin = (DateTime)reader["FechaFin"];
                        var fechaInicio = (DateTime)reader["FechaInicio"];

                        ofertas.Add(new OfertaViewModel
                        {
                            OfertaId = (int)reader["OfertaId"],
                            Nombre = reader["Nombre"].ToString(),
                            Descripcion = reader["Descripcion"]?.ToString(),
                            ProductoId = (int)reader["ProductoId"],
                            ProductoNombre = reader["ProductoNombre"].ToString(),
                            PrecioOriginal = (decimal)reader["PrecioOriginal"],
                            PrecioOferta = (decimal)reader["PrecioOferta"],
                            DescuentoPorcentaje = (decimal)reader["DescuentoPorcentaje"],
                            FechaInicio = fechaInicio,
                            FechaFin = fechaFin,
                            DiasRestantes = reader["DiasRestantes"] != DBNull.Value ? (int)reader["DiasRestantes"] : 0,
                        });
                    }
                }
            }

            return View(ofertas);
        }

        public ActionResult Crear()
        {
            if (Session["UserRol"] == null)
                return RedirectToAction("Login", "Account");

            CargarProductos();
            return View(new OfertaViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Crear(OfertaViewModel oferta)
        {
            if (Session["UserRol"] == null)
                return RedirectToAction("Login", "Account");

            if (oferta.DescuentoPorcentaje < 0 || oferta.DescuentoPorcentaje > 100)
                ModelState.AddModelError("DescuentoPorcentaje", "El descuento debe estar entre 0% y 100%");

            if (oferta.FechaInicio > oferta.FechaFin)
                ModelState.AddModelError("FechaFin", "La fecha de fin debe ser mayor a la fecha de inicio");

            if (ModelState.IsValid)
            {
                string query = @"
                    INSERT INTO Oferta (Nombre, Descripcion, ProductoId, DescuentoPorcentaje, 
                                       FechaInicio, FechaFin, Activo)
                    VALUES (@Nombre, @Descripcion, @ProductoId, @DescuentoPorcentaje, 
                            @FechaInicio, @FechaFin, 1);
                    SELECT SCOPE_IDENTITY();";

                using (var conn = new SqlConnection(_connectionString))
                using (var cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@Nombre", oferta.Nombre);
                    cmd.Parameters.AddWithValue("@Descripcion", oferta.Descripcion ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@ProductoId", oferta.ProductoId);
                    cmd.Parameters.AddWithValue("@DescuentoPorcentaje", oferta.DescuentoPorcentaje);
                    cmd.Parameters.AddWithValue("@FechaInicio", oferta.FechaInicio);
                    cmd.Parameters.AddWithValue("@FechaFin", oferta.FechaFin);

                    conn.Open();
                    cmd.ExecuteScalar();
                }

                TempData["Success"] = "Oferta creada exitosamente";
                return RedirectToAction("Index");
            }

            CargarProductos(oferta.ProductoId);
            return View(oferta);
        }

        // GET: Oferta/Editar/5
        public ActionResult Editar(int id)
        {
            if (Session["UserRol"] == null)
                return RedirectToAction("Login", "Account");

            OfertaViewModel oferta = null;

            string query = @"
                SELECT o.OfertaId, o.Nombre, o.Descripcion, o.ProductoId, 
                       p.Nombre AS ProductoNombre, p.PrecioVenta AS PrecioOriginal,
                       o.DescuentoPorcentaje, o.FechaInicio, o.FechaFin
                FROM Oferta o
                INNER JOIN Producto p ON o.ProductoId = p.ProductoId
                WHERE o.OfertaId = @OfertaId";

            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@OfertaId", id);
                conn.Open();

                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        var precioOriginal = (decimal)reader["PrecioOriginal"];
                        var descuento = (decimal)reader["DescuentoPorcentaje"];

                        oferta = new OfertaViewModel
                        {
                            OfertaId = (int)reader["OfertaId"],
                            Nombre = reader["Nombre"].ToString(),
                            Descripcion = reader["Descripcion"]?.ToString(),
                            ProductoId = (int)reader["ProductoId"],
                            ProductoNombre = reader["ProductoNombre"].ToString(),
                            PrecioOriginal = precioOriginal,
                            PrecioOferta = precioOriginal - (precioOriginal * descuento / 100),
                            DescuentoPorcentaje = descuento,
                            FechaInicio = (DateTime)reader["FechaInicio"],
                            FechaFin = (DateTime)reader["FechaFin"]
                        };
                    }
                }
            }

            if (oferta == null)
                return HttpNotFound();

            CargarProductos(oferta.ProductoId);
            return View(oferta);
        }

        // POST: Oferta/Editar/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Editar(OfertaViewModel oferta)
        {
            if (Session["UserRol"] == null)
                return RedirectToAction("Login", "Account");

            if (oferta.DescuentoPorcentaje < 0 || oferta.DescuentoPorcentaje > 100)
                ModelState.AddModelError("DescuentoPorcentaje", "El descuento debe estar entre 0% y 100%");

            if (oferta.FechaInicio > oferta.FechaFin)
                ModelState.AddModelError("FechaFin", "La fecha de fin debe ser mayor a la fecha de inicio");

            if (ModelState.IsValid)
            {
                string query = @"
                    UPDATE Oferta 
                    SET Nombre = @Nombre,
                        Descripcion = @Descripcion,
                        ProductoId = @ProductoId,
                        DescuentoPorcentaje = @DescuentoPorcentaje,
                        FechaInicio = @FechaInicio,
                        FechaFin = @FechaFin
                    WHERE OfertaId = @OfertaId";

                using (var conn = new SqlConnection(_connectionString))
                using (var cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@OfertaId", oferta.OfertaId);
                    cmd.Parameters.AddWithValue("@Nombre", oferta.Nombre);
                    cmd.Parameters.AddWithValue("@Descripcion", oferta.Descripcion ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@ProductoId", oferta.ProductoId);
                    cmd.Parameters.AddWithValue("@DescuentoPorcentaje", oferta.DescuentoPorcentaje);
                    cmd.Parameters.AddWithValue("@FechaInicio", oferta.FechaInicio);
                    cmd.Parameters.AddWithValue("@FechaFin", oferta.FechaFin);

                    conn.Open();
                    cmd.ExecuteNonQuery();
                }

                TempData["Success"] = "Oferta actualizada exitosamente";
                return RedirectToAction("Index");
            }

            CargarProductos(oferta.ProductoId);
            return View(oferta);
        }

        // POST: Oferta/Eliminar
        [HttpPost]
        public JsonResult Eliminar(int id)
        {
            try
            {
                string query = "DELETE FROM Oferta WHERE OfertaId = @OfertaId";
                using (var conn = new SqlConnection(_connectionString))
                using (var cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@OfertaId", id);
                    conn.Open();
                    cmd.ExecuteNonQuery();
                }
                return Json(new { success = true, message = "Oferta eliminada exitosamente" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // GET: Oferta/GetPrecioProducto (para AJAX)
        [HttpGet]
        public JsonResult GetPrecioProducto(int productoId)
        {
            string query = "SELECT PrecioVenta FROM Producto WHERE ProductoId = @ProductoId";
            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@ProductoId", productoId);
                conn.Open();
                var precio = cmd.ExecuteScalar();
                if (precio != null)
                    return Json(new { success = true, precio = Convert.ToDecimal(precio) }, JsonRequestBehavior.AllowGet);

                return Json(new { success = false }, JsonRequestBehavior.AllowGet);
            }
        }

        // ── Métodos auxiliares ─────────────────────────────────────────────────

        private void CargarProductos(int? productoId = null)
        {
            var productos = new List<SelectListItem>();
            string query = "SELECT ProductoId, Nombre, PrecioVenta FROM Producto WHERE Activo = 1 ORDER BY Nombre";

            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand(query, conn))
            {
                conn.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        productos.Add(new SelectListItem
                        {
                            Value = reader["ProductoId"].ToString(),
                            Text = $"{reader["Nombre"]} - {(decimal)reader["PrecioVenta"]:C}",
                            Selected = productoId.HasValue && reader["ProductoId"].ToString() == productoId.Value.ToString()
                        });
                    }
                }
            }

            ViewBag.Productos = new SelectList(productos, "Value", "Text", productoId?.ToString());
        }
    }
}