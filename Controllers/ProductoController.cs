using LaMediaCancha.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;
using System.Web.Mvc;
using static LaMediaCancha.Models.ProductoModels;
using Producto = LaMediaCancha.Models.ProductoModels.Producto;
using InventarioProducto = LaMediaCancha.Models.ProductoModels.InventarioProducto;

namespace LaMediaCancha.Controllers
{
    public class ProductoController : Controller
    {
        private readonly string _connectionString;

        public ProductoController()
        {
            _connectionString = ConfigurationManager.ConnectionStrings["LaMediaCanchaDB"].ConnectionString;
        }

        // GET: Producto/Index
        public ActionResult Index()
        {
            if (Session["UserRol"] == null)
                return RedirectToAction("Login", "Account");

            var productos = new List<Producto>();

            string query = @"
                SELECT 
                    ProductoId, Codigo, Nombre, Descripcion, 
                    PrecioCompra, PrecioVenta, Activo, FechaCreacion
                FROM Producto 
                ORDER BY Activo DESC, Nombre";

            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand(query, conn))
            {
                conn.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        productos.Add(new Producto
                        {
                            ProductoId = (int)reader["ProductoId"],
                            Codigo = reader["Codigo"]?.ToString() ?? "",
                            Nombre = reader["Nombre"]?.ToString() ?? "",
                            Descripcion = reader["Descripcion"]?.ToString(),
                            PrecioCompra = reader["PrecioCompra"] as decimal?,
                            PrecioVenta = reader["PrecioVenta"] != DBNull.Value ? (decimal)reader["PrecioVenta"] : 0,
                            Activo = (bool)reader["Activo"],
                            FechaCreacion = (DateTime)reader["FechaCreacion"]
                        });
                    }
                }
            }
            return View(productos);
        }

        // GET: Producto/Detalle/5
        public ActionResult Detalle(int id)
        {
            if (Session["UserRol"] == null)
                return RedirectToAction("Login", "Account");

            Producto producto = null;

            string query = @"
                SELECT 
                    p.ProductoId, p.Codigo, p.CodigoBarras, p.Nombre, p.Descripcion,
                    p.PrecioCompra, p.PrecioVenta, p.EstaEnOferta, p.PrecioOferta,
                    p.FechaInicioOferta, p.FechaFinOferta, p.Activo, p.FechaCreacion, p.FechaModificacion,
                    p.SubDepartamentoId
                FROM Producto p
                WHERE p.ProductoId = @ProductoId";

            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@ProductoId", id);
                conn.Open();

                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        producto = new Producto
                        {
                            ProductoId = (int)reader["ProductoId"],
                            Codigo = reader["Codigo"]?.ToString() ?? "",
                            CodigoBarras = reader["CodigoBarras"]?.ToString(),
                            Nombre = reader["Nombre"]?.ToString() ?? "",
                            Descripcion = reader["Descripcion"]?.ToString(),
                            PrecioCompra = reader["PrecioCompra"] as decimal?,
                            PrecioVenta = reader["PrecioVenta"] != DBNull.Value ? (decimal)reader["PrecioVenta"] : 0,
                            EstaEnOferta = reader["EstaEnOferta"] as bool?,
                            PrecioOferta = reader["PrecioOferta"] as decimal?,
                            FechaInicioOferta = reader["FechaInicioOferta"] as DateTime?,
                            FechaFinOferta = reader["FechaFinOferta"] as DateTime?,
                            Activo = (bool)reader["Activo"],
                            FechaCreacion = (DateTime)reader["FechaCreacion"],
                            FechaModificacion = reader["FechaModificacion"] as DateTime?,
                            SubDepartamentoId = reader["SubDepartamentoId"] as int?
                        };
                    }
                }
            }

            if (producto == null)
                return HttpNotFound();

            return View(producto);
        }

        // GET: Producto/Crear
        public ActionResult Crear()
        {
            if (Session["UserRol"] == null)
                return RedirectToAction("Login", "Account");

            CargarCombos();
            return View(new Producto());
        }

        // POST: Producto/Crear
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Crear(FormCollection form)
        {
            if (Session["UserRol"] == null)
                return RedirectToAction("Login", "Account");

            var producto = new Producto
            {
                Codigo = form["Codigo"],
                CodigoBarras = form["CodigoBarras"],
                Nombre = form["Nombre"],
                Descripcion = form["Descripcion"],
                PresentacionId = string.IsNullOrEmpty(form["PresentacionId"]) ? (int?)null : Convert.ToInt32(form["PresentacionId"]),
                MarcaId = string.IsNullOrEmpty(form["MarcaId"]) ? (int?)null : Convert.ToInt32(form["MarcaId"]),
                PrecioCompra = string.IsNullOrEmpty(form["PrecioCompra"]) ? (decimal?)null : Convert.ToDecimal(form["PrecioCompra"]),
                PrecioVenta = string.IsNullOrEmpty(form["PrecioVenta"]) ? 0 : Convert.ToDecimal(form["PrecioVenta"]),
                EstaEnOferta = form["EstaEnOferta"] == "true",
                PrecioOferta = string.IsNullOrEmpty(form["PrecioOferta"]) ? (decimal?)null : Convert.ToDecimal(form["PrecioOferta"]),
                FechaInicioOferta = string.IsNullOrEmpty(form["FechaInicioOferta"]) ? (DateTime?)null : Convert.ToDateTime(form["FechaInicioOferta"]),
                FechaFinOferta = string.IsNullOrEmpty(form["FechaFinOferta"]) ? (DateTime?)null : Convert.ToDateTime(form["FechaFinOferta"]),
                Activo = form["Activo"] == "true"
            };

            int? subDepartamentoId = null;
            if (!string.IsNullOrEmpty(form["SubDepartamentoId"]))
                subDepartamentoId = Convert.ToInt32(form["SubDepartamentoId"]);

            producto.SubDepartamentoId = subDepartamentoId;

            if (producto.PrecioCompra < 0)
                ModelState.AddModelError("PrecioCompra", "El precio de compra debe ser mayor o igual a 0");
            if (producto.PrecioVenta < 0)
                ModelState.AddModelError("PrecioVenta", "El precio de venta debe ser mayor o igual a 0");

            if (!string.IsNullOrEmpty(producto.Codigo))
            {
                string checkQuery = "SELECT COUNT(*) FROM Producto WHERE Codigo = @Codigo";
                using (var conn = new SqlConnection(_connectionString))
                using (var cmd = new SqlCommand(checkQuery, conn))
                {
                    cmd.Parameters.AddWithValue("@Codigo", producto.Codigo);
                    conn.Open();
                    int existe = (int)cmd.ExecuteScalar();
                    if (existe > 0)
                    {
                        ModelState.AddModelError("Codigo", "Ya existe un producto con este código");
                        CargarCombos();
                        return View(producto);
                    }
                }
            }

            if (!subDepartamentoId.HasValue)
            {
                ModelState.AddModelError("SubDepartamentoId", "Debe seleccionar un subdepartamento");
                CargarCombos();
                return View(producto);
            }

            if (ModelState.IsValid)
            {
                string query = @"
                    INSERT INTO Producto (SubDepartamentoId, PresentacionId, MarcaId, 
                                         Codigo, CodigoBarras, Nombre, Descripcion, 
                                         PrecioCompra, PrecioVenta, EstaEnOferta, PrecioOferta,
                                         FechaInicioOferta, FechaFinOferta, Activo, FechaCreacion)
                    VALUES (@SubDepartamentoId, @PresentacionId, @MarcaId,
                            @Codigo, @CodigoBarras, @Nombre, @Descripcion,
                            @PrecioCompra, @PrecioVenta, @EstaEnOferta, @PrecioOferta,
                            @FechaInicioOferta, @FechaFinOferta, @Activo, GETDATE());
                    SELECT SCOPE_IDENTITY();";

                using (var conn = new SqlConnection(_connectionString))
                using (var cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@SubDepartamentoId", subDepartamentoId.Value);
                    cmd.Parameters.AddWithValue("@PresentacionId", producto.PresentacionId ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@MarcaId", producto.MarcaId ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@Codigo", producto.Codigo ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@CodigoBarras", producto.CodigoBarras ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@Nombre", producto.Nombre ?? "");
                    cmd.Parameters.AddWithValue("@Descripcion", producto.Descripcion ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@PrecioCompra", producto.PrecioCompra ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@PrecioVenta", producto.PrecioVenta);
                    cmd.Parameters.AddWithValue("@EstaEnOferta", producto.EstaEnOferta);
                    cmd.Parameters.AddWithValue("@PrecioOferta", producto.PrecioOferta ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@FechaInicioOferta", producto.FechaInicioOferta ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@FechaFinOferta", producto.FechaFinOferta ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@Activo", producto.Activo);

                    conn.Open();
                    int newId = Convert.ToInt32(cmd.ExecuteScalar());
                }

                TempData["SuccessCrear"] = "Producto creado exitosamente";
                return RedirectToAction("Index");
            }

            CargarCombos();
            return View(producto);
        }

        // GET: Producto/Editar/5
        public ActionResult Editar(int id)
        {
            if (Session["UserRol"] == null)
                return RedirectToAction("Login", "Account");

            Producto producto = null;

            string query = @"
                SELECT ProductoId, SubDepartamentoId, PresentacionId, MarcaId,
                       Codigo, CodigoBarras, Nombre, Descripcion,
                       PrecioCompra, PrecioVenta, EstaEnOferta, PrecioOferta,
                       FechaInicioOferta, FechaFinOferta, Activo
                FROM Producto
                WHERE ProductoId = @ProductoId";

            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@ProductoId", id);
                conn.Open();

                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        producto = new Producto
                        {
                            ProductoId = (int)reader["ProductoId"],
                            SubDepartamentoId = reader["SubDepartamentoId"] as int?,
                            PresentacionId = reader["PresentacionId"] as int?,
                            MarcaId = reader["MarcaId"] as int?,
                            Codigo = reader["Codigo"]?.ToString() ?? "",
                            CodigoBarras = reader["CodigoBarras"]?.ToString(),
                            Nombre = reader["Nombre"]?.ToString() ?? "",
                            Descripcion = reader["Descripcion"]?.ToString(),
                            PrecioCompra = reader["PrecioCompra"] as decimal?,
                            PrecioVenta = reader["PrecioVenta"] != DBNull.Value ? (decimal)reader["PrecioVenta"] : 0,
                            EstaEnOferta = reader["EstaEnOferta"] as bool?,
                            PrecioOferta = reader["PrecioOferta"] as decimal?,
                            FechaInicioOferta = reader["FechaInicioOferta"] as DateTime?,
                            FechaFinOferta = reader["FechaFinOferta"] as DateTime?,
                            Activo = (bool)reader["Activo"]
                        };
                    }
                }
            }

            if (producto == null)
                return HttpNotFound();

            int? departamentoId = null;
            if (producto.SubDepartamentoId.HasValue)
            {
                string queryDepto = "SELECT DepartamentoId FROM SubDepartamento WHERE SubDepartamentoId = @SubDepartamentoId";
                using (var conn = new SqlConnection(_connectionString))
                using (var cmd = new SqlCommand(queryDepto, conn))
                {
                    cmd.Parameters.AddWithValue("@SubDepartamentoId", producto.SubDepartamentoId.Value);
                    conn.Open();
                    var result = cmd.ExecuteScalar();
                    if (result != null)
                        departamentoId = (int)result;
                }
            }

            CargarCombos(producto.SubDepartamentoId, producto.PresentacionId, producto.MarcaId, departamentoId);
            return View(producto);
        }

        // POST: Producto/Editar/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Editar(FormCollection form)
        {
            if (Session["UserRol"] == null)
                return RedirectToAction("Login", "Account");

            var producto = new Producto
            {
                ProductoId = Convert.ToInt32(form["ProductoId"]),
                Codigo = form["Codigo"],
                CodigoBarras = form["CodigoBarras"],
                Nombre = form["Nombre"],
                Descripcion = form["Descripcion"],
                PresentacionId = string.IsNullOrEmpty(form["PresentacionId"]) ? (int?)null : Convert.ToInt32(form["PresentacionId"]),
                MarcaId = string.IsNullOrEmpty(form["MarcaId"]) ? (int?)null : Convert.ToInt32(form["MarcaId"]),
                PrecioCompra = string.IsNullOrEmpty(form["PrecioCompra"]) ? (decimal?)null : Convert.ToDecimal(form["PrecioCompra"]),
                PrecioVenta = string.IsNullOrEmpty(form["PrecioVenta"]) ? 0 : Convert.ToDecimal(form["PrecioVenta"]),
                EstaEnOferta = form["EstaEnOferta"] == "true",
                PrecioOferta = string.IsNullOrEmpty(form["PrecioOferta"]) ? (decimal?)null : Convert.ToDecimal(form["PrecioOferta"]),
                FechaInicioOferta = string.IsNullOrEmpty(form["FechaInicioOferta"]) ? (DateTime?)null : Convert.ToDateTime(form["FechaInicioOferta"]),
                FechaFinOferta = string.IsNullOrEmpty(form["FechaFinOferta"]) ? (DateTime?)null : Convert.ToDateTime(form["FechaFinOferta"]),
                Activo = form["Activo"] == "true"
            };

            if (!string.IsNullOrEmpty(form["SubDepartamentoId"]))
                producto.SubDepartamentoId = Convert.ToInt32(form["SubDepartamentoId"]);

            if (ModelState.IsValid)
            {
                string query = @"
                    UPDATE Producto 
                    SET SubDepartamentoId = @SubDepartamentoId,
                        PresentacionId = @PresentacionId,
                        MarcaId = @MarcaId,
                        Codigo = @Codigo,
                        CodigoBarras = @CodigoBarras,
                        Nombre = @Nombre,
                        Descripcion = @Descripcion,
                        PrecioCompra = @PrecioCompra,
                        PrecioVenta = @PrecioVenta,
                        EstaEnOferta = @EstaEnOferta,
                        PrecioOferta = @PrecioOferta,
                        FechaInicioOferta = @FechaInicioOferta,
                        FechaFinOferta = @FechaFinOferta,
                        Activo = @Activo,
                        FechaModificacion = GETDATE()
                    WHERE ProductoId = @ProductoId";

                using (var conn = new SqlConnection(_connectionString))
                using (var cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@ProductoId", producto.ProductoId);
                    cmd.Parameters.AddWithValue("@SubDepartamentoId", producto.SubDepartamentoId ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@PresentacionId", producto.PresentacionId ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@MarcaId", producto.MarcaId ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@Codigo", producto.Codigo ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@CodigoBarras", producto.CodigoBarras ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@Nombre", producto.Nombre ?? "");
                    cmd.Parameters.AddWithValue("@Descripcion", producto.Descripcion ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@PrecioCompra", producto.PrecioCompra ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@PrecioVenta", producto.PrecioVenta);
                    cmd.Parameters.AddWithValue("@EstaEnOferta", producto.EstaEnOferta);
                    cmd.Parameters.AddWithValue("@PrecioOferta", producto.PrecioOferta ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@FechaInicioOferta", producto.FechaInicioOferta ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@FechaFinOferta", producto.FechaFinOferta ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@Activo", producto.Activo);

                    conn.Open();
                    cmd.ExecuteNonQuery();
                }

                TempData["Success"] = "Producto actualizado exitosamente";
                return RedirectToAction("Index");
            }

            CargarCombos(producto.SubDepartamentoId, producto.PresentacionId, producto.MarcaId, null);
            return View(producto);
        }

        // POST: Producto/CambiarEstado
        [HttpPost]
        public JsonResult CambiarEstado(int id, bool activo)
        {
            try
            {
                string query = "UPDATE Producto SET Activo = @Activo, FechaModificacion = GETDATE() WHERE ProductoId = @ProductoId";
                using (var conn = new SqlConnection(_connectionString))
                using (var cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@ProductoId", id);
                    cmd.Parameters.AddWithValue("@Activo", activo);
                    conn.Open();
                    cmd.ExecuteNonQuery();
                }

                string mensaje = activo ? "Producto activado exitosamente" : "Producto inactivado exitosamente";
                return Json(new { success = true, message = mensaje });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // POST: Producto/Inactivar
        [HttpPost]
        public JsonResult Inactivar(int id)
        {
            return CambiarEstado(id, false);
        }

        // GET: Producto/Inventario
        public ActionResult Inventario()
        {
            if (Session["UserRol"] == null)
                return RedirectToAction("Login", "Account");

            var inventario = new List<InventarioProducto>();

            string query = @"
                SELECT 
                    p.ProductoId, p.Codigo, p.Nombre,
                    ISNULL(i.ExistenciaActual, 0) AS ExistenciaActual,
                    ISNULL(i.StockMinimo, 10) AS StockMinimo,
                    ISNULL(i.StockMaximo, 100) AS StockMaximo
                FROM Producto p
                LEFT JOIN Inventario i ON p.ProductoId = i.ProductoId
                WHERE p.Activo = 1
                ORDER BY p.Nombre";

            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand(query, conn))
            {
                conn.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        inventario.Add(new InventarioProducto
                        {
                            ProductoId = (int)reader["ProductoId"],
                            Codigo = reader["Codigo"]?.ToString() ?? "",
                            Nombre = reader["Nombre"]?.ToString() ?? "",
                            ExistenciaActual = reader["ExistenciaActual"] != DBNull.Value ? Convert.ToInt32(reader["ExistenciaActual"]) : 0,
                            StockMinimo = reader["StockMinimo"] != DBNull.Value ? Convert.ToInt32(reader["StockMinimo"]) : 10,
                            StockMaximo = reader["StockMaximo"] != DBNull.Value ? Convert.ToInt32(reader["StockMaximo"]) : 100
                        });
                    }
                }
            }
            return View(inventario);
        }

        // POST: Producto/AjustarInventario
        [HttpPost]
        public JsonResult AjustarInventario(int productoId, int nuevoStock, int stockMinimo, int stockMaximo, string motivo)
        {
            try
            {
                string checkQuery = "SELECT COUNT(*) FROM Inventario WHERE ProductoId = @ProductoId";
                using (var conn = new SqlConnection(_connectionString))
                {
                    conn.Open();
                    using (var cmd = new SqlCommand(checkQuery, conn))
                    {
                        cmd.Parameters.AddWithValue("@ProductoId", productoId);
                        int existe = (int)cmd.ExecuteScalar();

                        string query;
                        if (existe > 0)
                            query = @"UPDATE Inventario SET ExistenciaActual = @ExistenciaActual, StockMinimo = @StockMinimo, StockMaximo = @StockMaximo WHERE ProductoId = @ProductoId";
                        else
                            query = @"INSERT INTO Inventario (ProductoId, ExistenciaActual, StockMinimo, StockMaximo) VALUES (@ProductoId, @ExistenciaActual, @StockMinimo, @StockMaximo)";

                        using (var cmd2 = new SqlCommand(query, conn))
                        {
                            cmd2.Parameters.AddWithValue("@ProductoId", productoId);
                            cmd2.Parameters.AddWithValue("@ExistenciaActual", nuevoStock);
                            cmd2.Parameters.AddWithValue("@StockMinimo", stockMinimo);
                            cmd2.Parameters.AddWithValue("@StockMaximo", stockMaximo);
                            cmd2.ExecuteNonQuery();
                        }
                    }
                }
                return Json(new { success = true, message = "Inventario ajustado exitosamente" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // ==================== MÉTODOS AUXILIARES PARA COMBOS ====================

        private void CargarCombos(int? subDepartamentoId = null, int? presentacionId = null, int? marcaId = null, int? departamentoId = null)
        {
            // Departamentos — se asignan directo porque ObtenerDepartamentos() ya devuelve List<SelectListItem>
            var departamentos = ObtenerDepartamentos();
            if (departamentoId.HasValue)
            {
                var seleccionado = departamentos.FirstOrDefault(d => d.Value == departamentoId.Value.ToString());
                if (seleccionado != null) seleccionado.Selected = true;
            }
            ViewBag.Departamentos = departamentos;

            // SubDepartamentos
            var subDepartamentos = ObtenerSubDepartamentos(departamentoId ?? 0);
            if (subDepartamentoId.HasValue)
            {
                var seleccionado = subDepartamentos.FirstOrDefault(s => s.Value == subDepartamentoId.Value.ToString());
                if (seleccionado != null) seleccionado.Selected = true;
            }
            ViewBag.SubDepartamentos = subDepartamentos;

            // Presentaciones
            var presentaciones = ObtenerPresentaciones();
            if (presentacionId.HasValue)
            {
                var seleccionado = presentaciones.FirstOrDefault(p => p.Value == presentacionId.Value.ToString());
                if (seleccionado != null) seleccionado.Selected = true;
            }
            ViewBag.Presentaciones = presentaciones;

            // Marcas
            var marcas = ObtenerMarcas();
            if (marcaId.HasValue)
            {
                var seleccionado = marcas.FirstOrDefault(m => m.Value == marcaId.Value.ToString());
                if (seleccionado != null) seleccionado.Selected = true;
            }
            ViewBag.Marcas = marcas;
        }

        private List<SelectListItem> ObtenerDepartamentos()
        {
            var lista = new List<SelectListItem>();
            string query = "SELECT DepartamentoId, Nombre FROM Departamento WHERE Activo = 1 ORDER BY Nombre";

            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand(query, conn))
            {
                conn.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        lista.Add(new SelectListItem
                        {
                            Value = reader["DepartamentoId"].ToString(),
                            Text = reader["Nombre"].ToString()
                        });
                    }
                }
            }
            return lista;
        }

        private List<SelectListItem> ObtenerSubDepartamentos(int departamentoId)
        {
            var lista = new List<SelectListItem>();
            string query = "SELECT SubDepartamentoId, Nombre FROM SubDepartamento WHERE DepartamentoId = @DepartamentoId AND Activo = 1 ORDER BY Nombre";

            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@DepartamentoId", departamentoId);
                conn.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        lista.Add(new SelectListItem
                        {
                            Value = reader["SubDepartamentoId"].ToString(),
                            Text = reader["Nombre"].ToString()
                        });
                    }
                }
            }
            return lista;
        }

        private List<SelectListItem> ObtenerPresentaciones()
        {
            var lista = new List<SelectListItem>();
            string query = "SELECT PresentacionId, Nombre FROM Presentacion WHERE Activo = 1 ORDER BY Nombre";

            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand(query, conn))
            {
                conn.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        lista.Add(new SelectListItem
                        {
                            Value = reader["PresentacionId"].ToString(),
                            Text = reader["Nombre"].ToString()
                        });
                    }
                }
            }
            return lista;
        }

        private List<SelectListItem> ObtenerMarcas()
        {
            var lista = new List<SelectListItem>();
            string query = "SELECT MarcaId, Nombre FROM Marca WHERE Activo = 1 ORDER BY Nombre";

            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand(query, conn))
            {
                conn.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        lista.Add(new SelectListItem
                        {
                            Value = reader["MarcaId"].ToString(),
                            Text = reader["Nombre"].ToString()
                        });
                    }
                }
            }
            return lista;
        }

        // API para AJAX - Carga dinámica de subdepartamentos
        [HttpGet]
        public JsonResult GetSubDepartamentos(int departamentoId)
        {
            var subDepartamentos = ObtenerSubDepartamentos(departamentoId);
            return Json(subDepartamentos, JsonRequestBehavior.AllowGet);
        }
    }
}