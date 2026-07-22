using LaMediaCancha.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;
using System.Web.Mvc;

namespace LaMediaCancha.Controllers
{
    public class ComboController : Controller
    {
        private readonly string _connectionString;

        public ComboController()
        {
            _connectionString = ConfigurationManager.ConnectionStrings["LaMediaCanchaDB"].ConnectionString;
        }

        // GET: Combo/Index
        public ActionResult Index()
        {
            if (Session["UserRol"] == null)
                return RedirectToAction("Login", "Account");

            var combos = new List<ComboModels.ComboViewModel>();

            string query = @"
                SELECT 
                    c.ComboId,
                    c.Nombre,
                    c.Descripcion,
                    c.PrecioCombo,
                    c.PrecioRegularTotal,
                    c.Activo,
                    c.FechaCreacion
                FROM Combo c
                ORDER BY c.Activo DESC, c.Nombre";

            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand(query, conn))
            {
                conn.Open();

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var productos = ObtenerProductosDelCombo((int)reader["ComboId"]);
                        var totalProductos = productos?.Sum(p => p.CantidadIncluida) ?? 0;

                        var combo = new ComboModels.ComboViewModel
                        {
                            ComboId = (int)reader["ComboId"],
                            Nombre = reader["Nombre"].ToString(),
                            Descripcion = reader["Descripcion"]?.ToString(),
                            PrecioCombo = (decimal)reader["PrecioCombo"],
                            PrecioRegularTotal = productos?.Sum(p => p.CantidadIncluida * p.PrecioIndividual) ?? (decimal)reader["PrecioRegularTotal"],
                            Activo = (bool)reader["Activo"],
                            FechaCreacion = (DateTime)reader["FechaCreacion"],
                            Productos = productos,
                            TotalProductos = totalProductos
                        };
                        combos.Add(combo);
                    }
                }
            }

            return View(combos);
        }

        // GET: Combo/Detalle/5
        public ActionResult Detalle(int id)
        {
            if (Session["UserRol"] == null)
                return RedirectToAction("Login", "Account");

            ComboModels.ComboViewModel combo = null;

            string query = @"
                SELECT 
                    c.ComboId,
                    c.Nombre,
                    c.Descripcion,
                    c.PrecioCombo,
                    c.PrecioRegularTotal,
                    c.Activo,
                    c.FechaCreacion
                FROM Combo c
                WHERE c.ComboId = @ComboId";

            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@ComboId", id);
                conn.Open();

                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        var productos = ObtenerProductosDelCombo(id);
                        var precioRegularCalculado = productos?.Sum(p => p.CantidadIncluida * p.PrecioIndividual) ?? (decimal)reader["PrecioRegularTotal"];

                        combo = new ComboModels.ComboViewModel
                        {
                            ComboId = (int)reader["ComboId"],
                            Nombre = reader["Nombre"].ToString(),
                            Descripcion = reader["Descripcion"]?.ToString(),
                            PrecioCombo = (decimal)reader["PrecioCombo"],
                            PrecioRegularTotal = precioRegularCalculado,
                            Activo = (bool)reader["Activo"],
                            FechaCreacion = (DateTime)reader["FechaCreacion"],
                            Productos = productos,
                            TotalProductos = productos?.Sum(p => p.CantidadIncluida) ?? 0
                        };
                    }
                }
            }

            if (combo == null)
                return HttpNotFound();

            return View(combo);
        }

        // GET: Combo/Crear
        public ActionResult Crear()
        {
            if (Session["UserRol"] == null)
                return RedirectToAction("Login", "Account");

            var model = new ComboModels.CrearComboViewModel
            {
                Productos = new List<ComboModels.ComboProductoItem>(),
                ProductosDisponibles = ObtenerProductosDisponibles(),
                Activo = true,
                PrecioCombo = 0
            };

            return View(model);
        }

        // POST: Combo/Crear
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Crear(ComboModels.CrearComboViewModel model)
        {
            if (Session["UserRol"] == null)
                return RedirectToAction("Login", "Account");

            if (model.Productos == null || model.Productos.Count == 0)
            {
                ModelState.AddModelError("", "Debe agregar al menos un producto al combo");
                model.ProductosDisponibles = ObtenerProductosDisponibles();
                return View(model);
            }

            try
            {
                // Calcular el precio regular total sumando los productos
                decimal precioRegularTotal = model.Productos.Sum(p => p.Cantidad * p.PrecioVenta);

                using (var conn = new SqlConnection(_connectionString))
                {
                    conn.Open();
                    using (var transaction = conn.BeginTransaction())
                    {
                        try
                        {
                            string insertCombo = @"
                                INSERT INTO Combo (Nombre, Descripcion, PrecioCombo, PrecioRegularTotal, Activo, FechaCreacion)
                                VALUES (@Nombre, @Descripcion, @PrecioCombo, @PrecioRegularTotal, @Activo, GETDATE());
                                SELECT SCOPE_IDENTITY();";

                            int comboId;
                            using (var cmd = new SqlCommand(insertCombo, conn, transaction))
                            {
                                cmd.Parameters.AddWithValue("@Nombre", model.Nombre);
                                cmd.Parameters.AddWithValue("@Descripcion", string.IsNullOrEmpty(model.Descripcion) ? (object)DBNull.Value : model.Descripcion);
                                cmd.Parameters.AddWithValue("@PrecioCombo", model.PrecioCombo);
                                cmd.Parameters.AddWithValue("@PrecioRegularTotal", precioRegularTotal);
                                cmd.Parameters.AddWithValue("@Activo", model.Activo);
                                comboId = Convert.ToInt32(cmd.ExecuteScalar());
                            }

                            foreach (var item in model.Productos)
                            {
                                string insertDetalle = @"
                                    INSERT INTO ComboDetalle (ComboId, ProductoId, CantidadIncluida)
                                    VALUES (@ComboId, @ProductoId, @Cantidad)";

                                using (var cmd = new SqlCommand(insertDetalle, conn, transaction))
                                {
                                    cmd.Parameters.AddWithValue("@ComboId", comboId);
                                    cmd.Parameters.AddWithValue("@ProductoId", item.ProductoId);
                                    cmd.Parameters.AddWithValue("@Cantidad", item.Cantidad);
                                    cmd.ExecuteNonQuery();
                                }
                            }

                            transaction.Commit();
                        }
                        catch
                        {
                            transaction.Rollback();
                            throw;
                        }
                    }
                }

                TempData["Success"] = $"Combo '{model.Nombre}' creado exitosamente";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error al crear combo: {ex.Message}";
                model.ProductosDisponibles = ObtenerProductosDisponibles();
                return View(model);
            }
        }

        // GET: Combo/Editar/5
        public ActionResult Editar(int id)
        {
            if (Session["UserRol"] == null)
                return RedirectToAction("Login", "Account");

            ComboModels.CrearComboViewModel model = null;

            string query = @"
                SELECT ComboId, Nombre, Descripcion, PrecioCombo, Activo
                FROM Combo
                WHERE ComboId = @ComboId";

            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@ComboId", id);
                conn.Open();

                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        model = new ComboModels.CrearComboViewModel
                        {
                            ComboId = (int)reader["ComboId"],
                            Nombre = reader["Nombre"].ToString(),
                            Descripcion = reader["Descripcion"]?.ToString(),
                            PrecioCombo = (decimal)reader["PrecioCombo"],
                            Activo = (bool)reader["Activo"],
                            Productos = ObtenerProductosDelComboParaEditar(id),
                            ProductosDisponibles = ObtenerProductosDisponibles()
                        };
                    }
                }
            }

            if (model == null)
                return HttpNotFound();

            return View(model);
        }

        // POST: Combo/Editar/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Editar(ComboModels.CrearComboViewModel model)
        {
            if (Session["UserRol"] == null)
                return RedirectToAction("Login", "Account");

            if (model.Productos == null || model.Productos.Count == 0)
            {
                ModelState.AddModelError("", "Debe agregar al menos un producto al combo");
                model.ProductosDisponibles = ObtenerProductosDisponibles();
                return View(model);
            }

            try
            {
                // Calcular el precio regular total sumando los productos
                decimal precioRegularTotal = model.Productos.Sum(p => p.Cantidad * p.PrecioVenta);

                using (var conn = new SqlConnection(_connectionString))
                {
                    conn.Open();
                    using (var transaction = conn.BeginTransaction())
                    {
                        try
                        {
                            string updateCombo = @"
                                UPDATE Combo 
                                SET Nombre = @Nombre,
                                    Descripcion = @Descripcion,
                                    PrecioCombo = @PrecioCombo,
                                    PrecioRegularTotal = @PrecioRegularTotal,
                                    Activo = @Activo
                                WHERE ComboId = @ComboId";

                            using (var cmd = new SqlCommand(updateCombo, conn, transaction))
                            {
                                cmd.Parameters.AddWithValue("@ComboId", model.ComboId);
                                cmd.Parameters.AddWithValue("@Nombre", model.Nombre);
                                cmd.Parameters.AddWithValue("@Descripcion", string.IsNullOrEmpty(model.Descripcion) ? (object)DBNull.Value : model.Descripcion);
                                cmd.Parameters.AddWithValue("@PrecioCombo", model.PrecioCombo);
                                cmd.Parameters.AddWithValue("@PrecioRegularTotal", precioRegularTotal);
                                cmd.Parameters.AddWithValue("@Activo", model.Activo);
                                cmd.ExecuteNonQuery();
                            }

                            // Eliminar detalles antiguos
                            string deleteDetalles = "DELETE FROM ComboDetalle WHERE ComboId = @ComboId";
                            using (var cmd = new SqlCommand(deleteDetalles, conn, transaction))
                            {
                                cmd.Parameters.AddWithValue("@ComboId", model.ComboId);
                                cmd.ExecuteNonQuery();
                            }

                            // Insertar nuevos detalles
                            foreach (var item in model.Productos)
                            {
                                string insertDetalle = @"
                                    INSERT INTO ComboDetalle (ComboId, ProductoId, CantidadIncluida)
                                    VALUES (@ComboId, @ProductoId, @Cantidad)";

                                using (var cmd = new SqlCommand(insertDetalle, conn, transaction))
                                {
                                    cmd.Parameters.AddWithValue("@ComboId", model.ComboId);
                                    cmd.Parameters.AddWithValue("@ProductoId", item.ProductoId);
                                    cmd.Parameters.AddWithValue("@Cantidad", item.Cantidad);
                                    cmd.ExecuteNonQuery();
                                }
                            }

                            transaction.Commit();
                        }
                        catch
                        {
                            transaction.Rollback();
                            throw;
                        }
                    }
                }

                TempData["Success"] = $"Combo '{model.Nombre}' actualizado exitosamente";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error al actualizar combo: {ex.Message}";
                model.ProductosDisponibles = ObtenerProductosDisponibles();
                return View(model);
            }
        }

        // POST: Combo/CambiarEstado
        [HttpPost]
        public JsonResult CambiarEstado(int id, bool activo)
        {
            if (Session["UserRol"] == null)
                return Json(new { success = false, message = "Sesión expirada" });

            try
            {
                string query = "UPDATE Combo SET Activo = @Activo WHERE ComboId = @ComboId";
                using (var conn = new SqlConnection(_connectionString))
                using (var cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@ComboId", id);
                    cmd.Parameters.AddWithValue("@Activo", activo);
                    conn.Open();
                    cmd.ExecuteNonQuery();
                }

                string mensaje = activo ? "Combo activado exitosamente" : "Combo inactivado exitosamente";
                return Json(new { success = true, message = mensaje });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // Método para actualizar todos los combos (corregir precios)
        [HttpPost]
        public JsonResult ActualizarPreciosCombos()
        {
            if (Session["UserRol"] == null)
                return Json(new { success = false, message = "Sesión expirada" });

            try
            {
                string query = @"
                    SELECT DISTINCT c.ComboId
                    FROM Combo c";

                var comboIds = new List<int>();
                using (var conn = new SqlConnection(_connectionString))
                using (var cmd = new SqlCommand(query, conn))
                {
                    conn.Open();
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            comboIds.Add((int)reader["ComboId"]);
                        }
                    }
                }

                int actualizados = 0;
                foreach (var comboId in comboIds)
                {
                    var productos = ObtenerProductosDelCombo(comboId);
                    decimal precioRegularTotal = productos.Sum(p => p.CantidadIncluida * p.PrecioIndividual);

                    string updateQuery = "UPDATE Combo SET PrecioRegularTotal = @PrecioRegularTotal WHERE ComboId = @ComboId";
                    using (var conn = new SqlConnection(_connectionString))
                    using (var cmd = new SqlCommand(updateQuery, conn))
                    {
                        cmd.Parameters.AddWithValue("@ComboId", comboId);
                        cmd.Parameters.AddWithValue("@PrecioRegularTotal", precioRegularTotal);
                        conn.Open();
                        cmd.ExecuteNonQuery();
                    }
                    actualizados++;
                }

                return Json(new { success = true, message = $"{actualizados} combos actualizados correctamente" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // Métodos auxiliares privados
        private List<ComboModels.ComboDetalleViewModel> ObtenerProductosDelCombo(int comboId)
        {
            var productos = new List<ComboModels.ComboDetalleViewModel>();

            string query = @"
                SELECT 
                    cd.ProductoId,
                    p.Nombre AS ProductoNombre,
                    p.Codigo AS ProductoCodigo,
                    ISNULL(p.PrecioVenta, 0) AS PrecioIndividual,
                    cd.CantidadIncluida
                FROM ComboDetalle cd
                INNER JOIN Producto p ON cd.ProductoId = p.ProductoId
                WHERE cd.ComboId = @ComboId
                ORDER BY p.Nombre";

            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@ComboId", comboId);
                conn.Open();

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        productos.Add(new ComboModels.ComboDetalleViewModel
                        {
                            ProductoId = (int)reader["ProductoId"],
                            ProductoNombre = reader["ProductoNombre"].ToString(),
                            ProductoCodigo = reader["ProductoCodigo"].ToString(),
                            PrecioIndividual = (decimal)reader["PrecioIndividual"],
                            CantidadIncluida = (int)reader["CantidadIncluida"]
                        });
                    }
                }
            }

            return productos;
        }

        private List<ComboModels.ComboProductoItem> ObtenerProductosDelComboParaEditar(int comboId)
        {
            var productos = new List<ComboModels.ComboProductoItem>();

            string query = @"
                SELECT 
                    cd.ProductoId,
                    p.Nombre AS ProductoNombre,
                    p.Codigo AS ProductoCodigo,
                    ISNULL(p.PrecioVenta, 0) AS PrecioVenta,
                    cd.CantidadIncluida AS Cantidad
                FROM ComboDetalle cd
                INNER JOIN Producto p ON cd.ProductoId = p.ProductoId
                WHERE cd.ComboId = @ComboId
                ORDER BY p.Nombre";

            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@ComboId", comboId);
                conn.Open();

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        productos.Add(new ComboModels.ComboProductoItem
                        {
                            ProductoId = (int)reader["ProductoId"],
                            ProductoNombre = reader["ProductoNombre"].ToString(),
                            ProductoCodigo = reader["ProductoCodigo"].ToString(),
                            PrecioVenta = (decimal)reader["PrecioVenta"],
                            Cantidad = (int)reader["Cantidad"]
                        });
                    }
                }
            }

            return productos;
        }

        private List<SelectListItem> ObtenerProductosDisponibles()
        {
            var productos = new List<SelectListItem>();
            string query = "SELECT ProductoId, Nombre, Codigo FROM Producto WHERE Activo = 1 ORDER BY Nombre";

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
                            Text = $"{reader["Codigo"]} - {reader["Nombre"]}"
                        });
                    }
                }
            }

            return productos;
        }

        // GET: Combo/GetProductosDisponibles
        [HttpGet]
        public JsonResult GetProductosDisponibles()
        {
            var productos = new List<object>();
            string query = "SELECT ProductoId, Codigo, Nombre, PrecioVenta FROM Producto WHERE Activo = 1 ORDER BY Nombre";

            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand(query, conn))
            {
                conn.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        productos.Add(new
                        {
                            ProductoId = (int)reader["ProductoId"],
                            Codigo = reader["Codigo"].ToString(),
                            Nombre = reader["Nombre"].ToString(),
                            PrecioVenta = (decimal)reader["PrecioVenta"]
                        });
                    }
                }
            }

            return Json(productos, JsonRequestBehavior.AllowGet);
        }
    }
}