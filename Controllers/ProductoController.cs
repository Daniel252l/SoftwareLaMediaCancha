using LaMediaCancha.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;
using System.Web.Mvc;

namespace LaMediaCancha.Controllers
{
    public class ProductoController : Controller
    {
        private readonly string _connectionString;

        public ProductoController()
        {
            _connectionString = ConfigurationManager.ConnectionStrings["LaMediaCanchaDB"].ConnectionString;
        }

        // ==================== PRODUCTOS DE COMPRA (MATERIAS PRIMAS) ====================

        // GET: Producto/Index - Muestra TODOS los productos (activos e inactivos)
        public ActionResult Index()
        {
            if (Session["UserRol"] == null)
                return RedirectToAction("Login", "Account");

            var productos = new List<ProductoModels.ProductoCompra>();

            string query = @"
                SELECT 
                    pc.ProductoCompraId, 
                    pc.Codigo, 
                    pc.Nombre, 
                    pc.Descripcion,
                    pc.UnidadMedida,
                    pc.PrecioCompra,
                    pc.StockActual,
                    pc.StockMinimo,
                    ISNULL(cc.Nombre, 'Sin categoría') AS Categoria,
                    pc.Activo,
                    pc.FechaCreacion
                FROM ProductoCompra pc
                LEFT JOIN CategoriaCompra cc ON pc.CategoriaId = cc.CategoriaCompraId
                ORDER BY pc.Activo DESC, pc.Nombre";

            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand(query, conn))
            {
                conn.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        productos.Add(new ProductoModels.ProductoCompra
                        {
                            ProductoCompraId = (int)reader["ProductoCompraId"],
                            Codigo = reader["Codigo"].ToString(),
                            Nombre = reader["Nombre"].ToString(),
                            Descripcion = reader["Descripcion"]?.ToString(),
                            UnidadMedida = reader["UnidadMedida"].ToString(),
                            PrecioCompra = (decimal)reader["PrecioCompra"],
                            StockActual = (decimal)reader["StockActual"],
                            StockMinimo = (decimal)reader["StockMinimo"],
                            Categoria = reader["Categoria"].ToString(),
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

            ProductoModels.ProductoCompra producto = null;

            string query = @"
                SELECT 
                    pc.ProductoCompraId, pc.Codigo, pc.Nombre, pc.Descripcion,
                    pc.UnidadMedida, pc.PrecioCompra, pc.StockActual, pc.StockMinimo,
                    pc.CategoriaId, ISNULL(cc.Nombre, 'Sin categoría') AS Categoria, pc.Activo,
                    pc.FechaCreacion
                FROM ProductoCompra pc
                LEFT JOIN CategoriaCompra cc ON pc.CategoriaId = cc.CategoriaCompraId
                WHERE pc.ProductoCompraId = @ProductoCompraId";

            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@ProductoCompraId", id);
                conn.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        producto = new ProductoModels.ProductoCompra
                        {
                            ProductoCompraId = (int)reader["ProductoCompraId"],
                            Codigo = reader["Codigo"].ToString(),
                            Nombre = reader["Nombre"].ToString(),
                            Descripcion = reader["Descripcion"]?.ToString(),
                            UnidadMedida = reader["UnidadMedida"].ToString(),
                            PrecioCompra = (decimal)reader["PrecioCompra"],
                            StockActual = (decimal)reader["StockActual"],
                            StockMinimo = (decimal)reader["StockMinimo"],
                            CategoriaId = reader["CategoriaId"] as int?,
                            Categoria = reader["Categoria"].ToString(),
                            Activo = (bool)reader["Activo"],
                            FechaCreacion = (DateTime)reader["FechaCreacion"]
                        };
                    }
                }
            }

            if (producto == null)
                return HttpNotFound();

            // Obtener los lotes del producto
            producto.Lotes = ObtenerLotesPorProducto(id);

            return View(producto);
        }

        // GET: Producto/Crear
        public ActionResult Crear()
        {
            if (Session["UserRol"] == null)
                return RedirectToAction("Login", "Account");

            CargarCategoriasCompra();
            return View(new ProductoModels.ProductoCompra());
        }

        // POST: Producto/Crear
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Crear(ProductoModels.ProductoCompra model)
        {
            if (Session["UserRol"] == null)
                return RedirectToAction("Login", "Account");

            if (!ModelState.IsValid)
            {
                CargarCategoriasCompra(model.CategoriaId);
                return View(model);
            }

            try
            {
                // Verificar si el código ya existe
                string checkQuery = "SELECT COUNT(*) FROM ProductoCompra WHERE Codigo = @Codigo";
                using (var conn = new SqlConnection(_connectionString))
                using (var cmd = new SqlCommand(checkQuery, conn))
                {
                    cmd.Parameters.AddWithValue("@Codigo", model.Codigo);
                    conn.Open();
                    int existe = (int)cmd.ExecuteScalar();
                    if (existe > 0)
                    {
                        TempData["Error"] = "Ya existe un producto con este código";
                        CargarCategoriasCompra(model.CategoriaId);
                        return View(model);
                    }
                }

                string query = @"
                    INSERT INTO ProductoCompra (Codigo, Nombre, Descripcion, UnidadMedida, PrecioCompra, StockActual, StockMinimo, CategoriaId, Activo, FechaCreacion)
                    VALUES (@Codigo, @Nombre, @Descripcion, @UnidadMedida, @PrecioCompra, 0, @StockMinimo, @CategoriaId, 1, GETDATE());
                    SELECT SCOPE_IDENTITY();";

                using (var conn = new SqlConnection(_connectionString))
                using (var cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@Codigo", model.Codigo);
                    cmd.Parameters.AddWithValue("@Nombre", model.Nombre);
                    cmd.Parameters.AddWithValue("@Descripcion", string.IsNullOrEmpty(model.Descripcion) ? (object)DBNull.Value : model.Descripcion);
                    cmd.Parameters.AddWithValue("@UnidadMedida", model.UnidadMedida);
                    cmd.Parameters.AddWithValue("@PrecioCompra", model.PrecioCompra);
                    cmd.Parameters.AddWithValue("@StockMinimo", model.StockMinimo);
                    cmd.Parameters.AddWithValue("@CategoriaId", model.CategoriaId.HasValue ? (object)model.CategoriaId.Value : DBNull.Value);
                    conn.Open();
                    cmd.ExecuteScalar();
                }

                TempData["Success"] = $"La materia prima '{model.Nombre}' ha sido creada exitosamente.";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error al crear: {ex.Message}";
                CargarCategoriasCompra(model.CategoriaId);
                return View(model);
            }
        }

        // GET: Producto/Editar/5
        public ActionResult Editar(int id)
        {
            if (Session["UserRol"] == null)
                return RedirectToAction("Login", "Account");

            ProductoModels.ProductoCompra producto = null;

            string query = @"
                SELECT 
                    pc.ProductoCompraId, pc.Codigo, pc.Nombre, pc.Descripcion,
                    pc.UnidadMedida, pc.PrecioCompra, pc.StockActual, pc.StockMinimo,
                    pc.CategoriaId, ISNULL(cc.Nombre, 'Sin categoría') AS Categoria, pc.Activo
                FROM ProductoCompra pc
                LEFT JOIN CategoriaCompra cc ON pc.CategoriaId = cc.CategoriaCompraId
                WHERE pc.ProductoCompraId = @ProductoCompraId";

            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@ProductoCompraId", id);
                conn.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        producto = new ProductoModels.ProductoCompra
                        {
                            ProductoCompraId = (int)reader["ProductoCompraId"],
                            Codigo = reader["Codigo"].ToString(),
                            Nombre = reader["Nombre"].ToString(),
                            Descripcion = reader["Descripcion"]?.ToString(),
                            UnidadMedida = reader["UnidadMedida"].ToString(),
                            PrecioCompra = (decimal)reader["PrecioCompra"],
                            StockActual = (decimal)reader["StockActual"],
                            StockMinimo = (decimal)reader["StockMinimo"],
                            CategoriaId = reader["CategoriaId"] as int?,
                            Categoria = reader["Categoria"].ToString(),
                            Activo = (bool)reader["Activo"]
                        };
                    }
                }
            }

            if (producto == null)
                return HttpNotFound();

            CargarCategoriasCompra(producto.CategoriaId);
            return View(producto);
        }

        // POST: Producto/Editar/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Editar(ProductoModels.ProductoCompra model)
        {
            if (Session["UserRol"] == null)
                return RedirectToAction("Login", "Account");

            if (!ModelState.IsValid)
            {
                CargarCategoriasCompra(model.CategoriaId);
                return View(model);
            }

            try
            {
                string query = @"
                    UPDATE ProductoCompra 
                    SET Codigo = @Codigo,
                        Nombre = @Nombre,
                        Descripcion = @Descripcion,
                        UnidadMedida = @UnidadMedida,
                        PrecioCompra = @PrecioCompra,
                        StockMinimo = @StockMinimo,
                        CategoriaId = @CategoriaId,
                        Activo = @Activo
                    WHERE ProductoCompraId = @ProductoCompraId";

                using (var conn = new SqlConnection(_connectionString))
                using (var cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@ProductoCompraId", model.ProductoCompraId);
                    cmd.Parameters.AddWithValue("@Codigo", model.Codigo);
                    cmd.Parameters.AddWithValue("@Nombre", model.Nombre);
                    cmd.Parameters.AddWithValue("@Descripcion", string.IsNullOrEmpty(model.Descripcion) ? (object)DBNull.Value : model.Descripcion);
                    cmd.Parameters.AddWithValue("@UnidadMedida", model.UnidadMedida);
                    cmd.Parameters.AddWithValue("@PrecioCompra", model.PrecioCompra);
                    cmd.Parameters.AddWithValue("@StockMinimo", model.StockMinimo);
                    cmd.Parameters.AddWithValue("@CategoriaId", model.CategoriaId.HasValue ? (object)model.CategoriaId.Value : DBNull.Value);
                    cmd.Parameters.AddWithValue("@Activo", model.Activo);
                    conn.Open();
                    cmd.ExecuteNonQuery();
                }

                TempData["Success"] = $"La materia prima '{model.Nombre}' ha sido actualizada exitosamente.";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error al actualizar: {ex.Message}";
                CargarCategoriasCompra(model.CategoriaId);
                return View(model);
            }
        }

        // POST: Producto/CambiarEstado - Inactivar/Activar producto
        [HttpPost]
        public JsonResult CambiarEstado(int id, bool activo)
        {
            try
            {
                string query = "UPDATE ProductoCompra SET Activo = @Activo WHERE ProductoCompraId = @ProductoCompraId";
                using (var conn = new SqlConnection(_connectionString))
                using (var cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@ProductoCompraId", id);
                    cmd.Parameters.AddWithValue("@Activo", activo);
                    conn.Open();
                    cmd.ExecuteNonQuery();
                }

                string mensaje = activo ? "El producto ha sido activado exitosamente." : "El producto ha sido inactivado exitosamente. Aún podrá recibir compras y lotes.";
                return Json(new { success = true, message = mensaje });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // ==================== LOTES DE COMPRA ====================

        // GET: Producto/Lotes/5
        public ActionResult Lotes(int id)
        {
            if (Session["UserRol"] == null)
                return RedirectToAction("Login", "Account");

            var lotes = ObtenerLotesPorProducto(id);
            string nombreProducto = "";

            // Obtener nombre del producto
            string queryNombre = "SELECT Nombre FROM ProductoCompra WHERE ProductoCompraId = @ProductoCompraId";
            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand(queryNombre, conn))
            {
                cmd.Parameters.AddWithValue("@ProductoCompraId", id);
                conn.Open();
                var result = cmd.ExecuteScalar();
                if (result != null)
                    nombreProducto = result.ToString();
            }

            ViewBag.NombreProducto = nombreProducto;
            ViewBag.ProductoCompraId = id;
            return View(lotes);
        }

        // GET: Producto/CrearLote
        public ActionResult CrearLote(int productoCompraId)
        {
            if (Session["UserRol"] == null)
                return RedirectToAction("Login", "Account");

            ViewBag.ProductoCompraId = productoCompraId;
            ViewBag.Proveedores = ObtenerProveedores();

            // Obtener nombre del producto
            string queryNombre = "SELECT Nombre FROM ProductoCompra WHERE ProductoCompraId = @ProductoCompraId";
            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand(queryNombre, conn))
            {
                cmd.Parameters.AddWithValue("@ProductoCompraId", productoCompraId);
                conn.Open();
                var result = cmd.ExecuteScalar();
                if (result != null)
                    ViewBag.NombreProducto = result.ToString();
            }

            return View();
        }

        // POST: Producto/CrearLote
        [HttpPost]
        public JsonResult CrearLote(int productoCompraId, int proveedorId, string numeroLote, decimal cantidad, decimal precioUnitario, DateTime? fechaVencimiento)
        {
            if (Session["UserRol"] == null)
                return Json(new { success = false, message = "Sesión expirada" });

            // Validaciones básicas
            if (productoCompraId <= 0)
                return Json(new { success = false, message = "ID de producto inválido" });

            if (proveedorId <= 0)
                return Json(new { success = false, message = "Debe seleccionar un proveedor" });

            if (string.IsNullOrEmpty(numeroLote))
                return Json(new { success = false, message = "Debe ingresar un número de lote" });

            if (cantidad <= 0)
                return Json(new { success = false, message = "La cantidad debe ser mayor a 0" });

            if (precioUnitario <= 0)
                return Json(new { success = false, message = "El precio unitario debe ser mayor a 0" });

            try
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    conn.Open();

                    // Insertar el lote
                    string insertLote = @"
                INSERT INTO LoteCompra (ProductoCompraId, ProveedorId, NumeroLote, CantidadInicial, CantidadActual, PrecioUnitario, FechaIngreso, FechaVencimiento, Activo)
                VALUES (@ProductoCompraId, @ProveedorId, @NumeroLote, @Cantidad, @Cantidad, @PrecioUnitario, GETDATE(), @FechaVencimiento, 1)";

                    using (var cmd = new SqlCommand(insertLote, conn))
                    {
                        cmd.Parameters.AddWithValue("@ProductoCompraId", productoCompraId);
                        cmd.Parameters.AddWithValue("@ProveedorId", proveedorId);
                        cmd.Parameters.AddWithValue("@NumeroLote", numeroLote);
                        cmd.Parameters.AddWithValue("@Cantidad", cantidad);
                        cmd.Parameters.AddWithValue("@PrecioUnitario", precioUnitario);
                        cmd.Parameters.AddWithValue("@FechaVencimiento", fechaVencimiento.HasValue ? (object)fechaVencimiento.Value : DBNull.Value);
                        cmd.ExecuteNonQuery();
                    }

                    // Actualizar el stock del producto
                    string updateStock = @"
                UPDATE ProductoCompra 
                SET StockActual = ISNULL(StockActual, 0) + @Cantidad
                WHERE ProductoCompraId = @ProductoCompraId";

                    using (var cmd = new SqlCommand(updateStock, conn))
                    {
                        cmd.Parameters.AddWithValue("@ProductoCompraId", productoCompraId);
                        cmd.Parameters.AddWithValue("@Cantidad", cantidad);
                        cmd.ExecuteNonQuery();
                    }
                }

                return Json(new { success = true, message = "Lote agregado y stock actualizado exitosamente" });
            }
            catch (SqlException ex)
            {
                // Error específico de SQL
                return Json(new { success = false, message = "Error de base de datos: " + ex.Message });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error al guardar: " + ex.Message });
            }
        }

        // POST: Producto/ConsumirStock
        [HttpPost]
        public JsonResult ConsumirStock(int loteId, decimal cantidad, string motivo)
        {
            if (Session["UserRol"] == null)
                return Json(new { success = false, message = "Sesión expirada" });

            try
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    conn.Open();
                    using (var transaction = conn.BeginTransaction())
                    {
                        try
                        {
                            // Obtener el ProductoCompraId del lote
                            int productoCompraId = 0;
                            string getProducto = "SELECT ProductoCompraId FROM LoteCompra WHERE LoteCompraId = @LoteCompraId";
                            using (var cmd = new SqlCommand(getProducto, conn, transaction))
                            {
                                cmd.Parameters.AddWithValue("@LoteCompraId", loteId);
                                productoCompraId = (int)cmd.ExecuteScalar();
                            }

                            // Actualizar el lote
                            string updateLote = @"
                                UPDATE LoteCompra 
                                SET CantidadActual = CantidadActual - @Cantidad
                                WHERE LoteCompraId = @LoteCompraId AND CantidadActual >= @Cantidad";

                            using (var cmd = new SqlCommand(updateLote, conn, transaction))
                            {
                                cmd.Parameters.AddWithValue("@LoteCompraId", loteId);
                                cmd.Parameters.AddWithValue("@Cantidad", cantidad);
                                int rows = cmd.ExecuteNonQuery();

                                if (rows == 0)
                                    return Json(new { success = false, message = "No hay suficiente stock en este lote" });
                            }

                            // Actualizar el stock del producto
                            string updateStock = @"
                                UPDATE ProductoCompra 
                                SET StockActual = StockActual - @Cantidad
                                WHERE ProductoCompraId = @ProductoCompraId";

                            using (var cmd = new SqlCommand(updateStock, conn, transaction))
                            {
                                cmd.Parameters.AddWithValue("@ProductoCompraId", productoCompraId);
                                cmd.Parameters.AddWithValue("@Cantidad", cantidad);
                                cmd.ExecuteNonQuery();
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

                return Json(new { success = true, message = "Stock consumido exitosamente" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // ==================== MÉTODOS AUXILIARES ====================

        private List<ProductoModels.LoteCompra> ObtenerLotesPorProducto(int productoCompraId)
        {
            var lotes = new List<ProductoModels.LoteCompra>();

            string query = @"
                SELECT 
                    l.LoteCompraId,
                    l.NumeroLote,
                    l.CantidadInicial,
                    l.CantidadActual,
                    l.PrecioUnitario,
                    l.FechaIngreso,
                    l.FechaVencimiento,
                    l.Activo,
                    p.RazonSocial AS ProveedorNombre
                FROM LoteCompra l
                LEFT JOIN Proveedor p ON l.ProveedorId = p.ProveedorId
                WHERE l.ProductoCompraId = @ProductoCompraId
                ORDER BY l.FechaIngreso DESC";

            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@ProductoCompraId", productoCompraId);
                conn.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        lotes.Add(new ProductoModels.LoteCompra
                        {
                            LoteCompraId = (int)reader["LoteCompraId"],
                            NumeroLote = reader["NumeroLote"].ToString(),
                            CantidadInicial = (decimal)reader["CantidadInicial"],
                            CantidadActual = (decimal)reader["CantidadActual"],
                            PrecioUnitario = (decimal)reader["PrecioUnitario"],
                            FechaIngreso = (DateTime)reader["FechaIngreso"],
                            FechaVencimiento = reader["FechaVencimiento"] as DateTime?,
                            Activo = (bool)reader["Activo"],
                            ProveedorNombre = reader["ProveedorNombre"]?.ToString() ?? "N/A"
                        });
                    }
                }
            }

            return lotes;
        }

        private void CargarCategoriasCompra(int? categoriaId = null)
        {
            var categorias = new List<SelectListItem>();
            string query = "SELECT CategoriaCompraId, Nombre FROM CategoriaCompra WHERE Activo = 1 ORDER BY Nombre";

            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand(query, conn))
            {
                conn.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    categorias.Add(new SelectListItem { Value = "", Text = "Seleccione una categoría..." });
                    while (reader.Read())
                    {
                        categorias.Add(new SelectListItem
                        {
                            Value = reader["CategoriaCompraId"].ToString(),
                            Text = reader["Nombre"].ToString()
                        });
                    }
                }
            }

            ViewBag.CategoriasCompra = new SelectList(categorias, "Value", "Text", categoriaId);
        }

        private SelectList ObtenerProveedores()
        {
            var proveedores = new List<SelectListItem>();
            string query = "SELECT ProveedorId, RazonSocial FROM Proveedor WHERE Activo = 1 ORDER BY RazonSocial";

            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand(query, conn))
            {
                conn.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    proveedores.Add(new SelectListItem { Value = "", Text = "Seleccione un proveedor..." });
                    while (reader.Read())
                    {
                        proveedores.Add(new SelectListItem
                        {
                            Value = reader["ProveedorId"].ToString(),
                            Text = reader["RazonSocial"].ToString()
                        });
                    }
                }
            }

            return new SelectList(proveedores, "Value", "Text");
        }
        // GET: Producto/Inventario
        public ActionResult Inventario()
        {
            if (Session["UserRol"] == null)
                return RedirectToAction("Login", "Account");

            var inventario = new List<ProductoModels.InventarioCompra>();

            string query = @"
        SELECT 
            pc.ProductoCompraId,
            pc.Codigo,
            pc.Nombre,
            pc.UnidadMedida,
            pc.PrecioCompra,
            pc.StockActual,
            pc.StockMinimo,
            ISNULL(cc.Nombre, 'Sin categoría') AS Categoria,
            pc.Activo,
            CASE 
                WHEN pc.StockActual <= pc.StockMinimo THEN 'Bajo'
                WHEN pc.StockActual <= pc.StockMinimo * 1.5 THEN 'Alerta'
                ELSE 'Normal'
            END AS EstadoStock
        FROM ProductoCompra pc
        LEFT JOIN CategoriaCompra cc ON pc.CategoriaId = cc.CategoriaCompraId
        ORDER BY pc.Nombre";

            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand(query, conn))
            {
                conn.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        inventario.Add(new ProductoModels.InventarioCompra
                        {
                            ProductoCompraId = (int)reader["ProductoCompraId"],
                            Codigo = reader["Codigo"].ToString(),
                            Nombre = reader["Nombre"].ToString(),
                            UnidadMedida = reader["UnidadMedida"].ToString(),
                            PrecioCompra = (decimal)reader["PrecioCompra"],
                            StockActual = (decimal)reader["StockActual"],
                            StockMinimo = (decimal)reader["StockMinimo"],
                            Categoria = reader["Categoria"].ToString(),
                            Activo = (bool)reader["Activo"],
                            EstadoStock = reader["EstadoStock"].ToString()
                        });
                    }
                }
            }

            return View(inventario);
        }
    }
}