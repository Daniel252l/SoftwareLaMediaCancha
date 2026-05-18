using LaMediaCancha.Models;
using LaMediaCancha.Services;
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
        private readonly InventarioService _inventarioService;

        public ProductoController()
        {
            _connectionString = ConfigurationManager.ConnectionStrings["LaMediaCanchaDB"].ConnectionString;
            _inventarioService = new InventarioService();
        }

        // ==================== MATERIAS PRIMAS ====================

        // GET: Producto/Index
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
                    pc.FechaCreacion,
                    ISNULL((
                        SELECT COUNT(*) 
                        FROM LoteCompra l 
                        WHERE l.ProductoCompraId = pc.ProductoCompraId 
                          AND l.Activo = 1 
                          AND l.CantidadActual > 0
                    ), 0) AS LotesActivos
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
                        // Manejo seguro de conversión de decimal a int
                        object stockActualObj = reader["StockActual"];
                        object stockMinimoObj = reader["StockMinimo"];

                        decimal stockActualDecimal = 0;
                        decimal stockMinimoDecimal = 0;

                        if (stockActualObj != DBNull.Value)
                            stockActualDecimal = Convert.ToDecimal(stockActualObj);
                        if (stockMinimoObj != DBNull.Value)
                            stockMinimoDecimal = Convert.ToDecimal(stockMinimoObj);

                        int stockActualInt = (int)Math.Floor(stockActualDecimal);
                        int stockMinimoInt = (int)Math.Floor(stockMinimoDecimal);

                        productos.Add(new ProductoModels.ProductoCompra
                        {
                            ProductoCompraId = Convert.ToInt32(reader["ProductoCompraId"]),
                            Codigo = reader["Codigo"]?.ToString() ?? "",
                            Nombre = reader["Nombre"]?.ToString() ?? "",
                            Descripcion = reader["Descripcion"]?.ToString(),
                            UnidadMedida = reader["UnidadMedida"]?.ToString() ?? "",
                            PrecioCompra = Convert.ToDecimal(reader["PrecioCompra"]),
                            StockActual = stockActualInt,
                            StockMinimo = stockMinimoInt,
                            Categoria = reader["Categoria"]?.ToString() ?? "Sin categoría",
                            Activo = Convert.ToBoolean(reader["Activo"]),
                            FechaCreacion = Convert.ToDateTime(reader["FechaCreacion"]),
                            LotesActivos = Convert.ToInt32(reader["LotesActivos"])
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
                    pc.ProductoCompraId, 
                    pc.Codigo, 
                    pc.Nombre, 
                    pc.Descripcion,
                    pc.UnidadMedida,
                    pc.PrecioCompra,
                    pc.StockActual,
                    pc.StockMinimo,
                    pc.CategoriaId,
                    ISNULL(cc.Nombre, 'Sin categoría') AS Categoria,
                    pc.Activo,
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
                        // Manejo seguro de conversión
                        object stockActualObj = reader["StockActual"];
                        object stockMinimoObj = reader["StockMinimo"];

                        decimal stockActualDecimal = 0;
                        decimal stockMinimoDecimal = 0;

                        if (stockActualObj != DBNull.Value)
                            stockActualDecimal = Convert.ToDecimal(stockActualObj);
                        if (stockMinimoObj != DBNull.Value)
                            stockMinimoDecimal = Convert.ToDecimal(stockMinimoObj);

                        int stockActualInt = (int)Math.Floor(stockActualDecimal);
                        int stockMinimoInt = (int)Math.Floor(stockMinimoDecimal);

                        producto = new ProductoModels.ProductoCompra
                        {
                            ProductoCompraId = Convert.ToInt32(reader["ProductoCompraId"]),
                            Codigo = reader["Codigo"]?.ToString() ?? "",
                            Nombre = reader["Nombre"]?.ToString() ?? "",
                            Descripcion = reader["Descripcion"]?.ToString(),
                            UnidadMedida = reader["UnidadMedida"]?.ToString() ?? "",
                            PrecioCompra = Convert.ToDecimal(reader["PrecioCompra"]),
                            StockActual = stockActualInt,
                            StockMinimo = stockMinimoInt,
                            CategoriaId = reader["CategoriaId"] as int?,
                            Categoria = reader["Categoria"]?.ToString() ?? "Sin categoría",
                            Activo = Convert.ToBoolean(reader["Activo"]),
                            FechaCreacion = Convert.ToDateTime(reader["FechaCreacion"]),
                            Lotes = ObtenerLotesPorProducto(id)
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
                string checkQuery = "SELECT COUNT(*) FROM ProductoCompra WHERE Codigo = @Codigo";
                using (var conn = new SqlConnection(_connectionString))
                using (var cmd = new SqlCommand(checkQuery, conn))
                {
                    cmd.Parameters.AddWithValue("@Codigo", model.Codigo);
                    conn.Open();
                    int existe = Convert.ToInt32(cmd.ExecuteScalar());
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
                        object stockActualObj = reader["StockActual"];
                        object stockMinimoObj = reader["StockMinimo"];

                        decimal stockActualDecimal = 0;
                        decimal stockMinimoDecimal = 0;

                        if (stockActualObj != DBNull.Value)
                            stockActualDecimal = Convert.ToDecimal(stockActualObj);
                        if (stockMinimoObj != DBNull.Value)
                            stockMinimoDecimal = Convert.ToDecimal(stockMinimoObj);

                        int stockActualInt = (int)Math.Floor(stockActualDecimal);
                        int stockMinimoInt = (int)Math.Floor(stockMinimoDecimal);

                        producto = new ProductoModels.ProductoCompra
                        {
                            ProductoCompraId = Convert.ToInt32(reader["ProductoCompraId"]),
                            Codigo = reader["Codigo"]?.ToString() ?? "",
                            Nombre = reader["Nombre"]?.ToString() ?? "",
                            Descripcion = reader["Descripcion"]?.ToString(),
                            UnidadMedida = reader["UnidadMedida"]?.ToString() ?? "",
                            PrecioCompra = Convert.ToDecimal(reader["PrecioCompra"]),
                            StockActual = stockActualInt,
                            StockMinimo = stockMinimoInt,
                            CategoriaId = reader["CategoriaId"] as int?,
                            Categoria = reader["Categoria"]?.ToString() ?? "Sin categoría",
                            Activo = Convert.ToBoolean(reader["Activo"])
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

        // POST: Producto/CambiarEstado
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
        public JsonResult CrearLote(int productoCompraId, int proveedorId, string numeroLote, int cantidad, decimal precioUnitario, DateTime? fechaFabricacion, DateTime? fechaVencimiento, decimal costoCompra)
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
                            // 1. Insertar el lote
                            string insertLote = @"
                        INSERT INTO LoteCompra (ProductoCompraId, ProveedorId, NumeroLote, CantidadInicial, CantidadActual, PrecioUnitario, CostoCompra, FechaIngreso, FechaFabricacion, FechaVencimiento, Activo)
                        VALUES (@ProductoCompraId, @ProveedorId, @NumeroLote, @Cantidad, @Cantidad, @PrecioUnitario, @CostoCompra, GETDATE(), @FechaFabricacion, @FechaVencimiento, 1)";

                            using (var cmd = new SqlCommand(insertLote, conn, transaction))
                            {
                                cmd.Parameters.AddWithValue("@ProductoCompraId", productoCompraId);
                                cmd.Parameters.AddWithValue("@ProveedorId", proveedorId);
                                cmd.Parameters.AddWithValue("@NumeroLote", numeroLote);
                                cmd.Parameters.AddWithValue("@Cantidad", cantidad);
                                cmd.Parameters.AddWithValue("@PrecioUnitario", precioUnitario);
                                cmd.Parameters.AddWithValue("@CostoCompra", costoCompra);
                                cmd.Parameters.AddWithValue("@FechaFabricacion", fechaFabricacion.HasValue ? (object)fechaFabricacion.Value : DBNull.Value);
                                cmd.Parameters.AddWithValue("@FechaVencimiento", fechaVencimiento.HasValue ? (object)fechaVencimiento.Value : DBNull.Value);
                                cmd.ExecuteNonQuery();
                            }

                            // 2. Actualizar el stock del producto
                            string updateStock = @"
                        UPDATE ProductoCompra 
                        SET StockActual = ISNULL(StockActual, 0) + @Cantidad
                        WHERE ProductoCompraId = @ProductoCompraId";

                            using (var cmd = new SqlCommand(updateStock, conn, transaction))
                            {
                                cmd.Parameters.AddWithValue("@ProductoCompraId", productoCompraId);
                                cmd.Parameters.AddWithValue("@Cantidad", cantidad);
                                cmd.ExecuteNonQuery();
                            }

                            // 3. ACTUALIZAR EL PRECIO COMPRA DEL PRODUCTO CON EL PRECIO DEL NUEVO LOTE
                            string updatePrecioCompra = @"
                        UPDATE ProductoCompra 
                        SET PrecioCompra = @PrecioUnitario
                        WHERE ProductoCompraId = @ProductoCompraId";

                            using (var cmd = new SqlCommand(updatePrecioCompra, conn, transaction))
                            {
                                cmd.Parameters.AddWithValue("@ProductoCompraId", productoCompraId);
                                cmd.Parameters.AddWithValue("@PrecioUnitario", precioUnitario);
                                int rows = cmd.ExecuteNonQuery();

                                System.Diagnostics.Debug.WriteLine($"Actualizando PrecioCompra - ProductoId: {productoCompraId}, NuevoPrecio: {precioUnitario}, Filas afectadas: {rows}");
                            }

                            transaction.Commit();
                        }
                        catch (Exception ex)
                        {
                            transaction.Rollback();
                            System.Diagnostics.Debug.WriteLine($"Error en transacción: {ex.Message}");
                            throw;
                        }
                    }
                }

                return Json(new { success = true, message = "Lote agregado y stock actualizado exitosamente" });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error general: {ex.Message}");
                return Json(new { success = false, message = ex.Message });
            }
        }

        // POST: Producto/ConsumirStock
        [HttpPost]
        public JsonResult ConsumirStock(int loteId, int cantidad, string motivo)
        {
            if (Session["UserRol"] == null)
                return Json(new { success = false, message = "Sesión expirada" });  // ← CORREGIDO

            try
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    conn.Open();
                    using (var transaction = conn.BeginTransaction())
                    {
                        try
                        {
                            int productoCompraId = 0;
                            string getProducto = "SELECT ProductoCompraId FROM LoteCompra WHERE LoteCompraId = @LoteCompraId";
                            using (var cmd = new SqlCommand(getProducto, conn, transaction))
                            {
                                cmd.Parameters.AddWithValue("@LoteCompraId", loteId);
                                productoCompraId = Convert.ToInt32(cmd.ExecuteScalar());
                            }

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

        // ==================== INVENTARIO ====================

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
                    ISNULL((
                        SELECT COUNT(*) 
                        FROM LoteCompra l 
                        WHERE l.ProductoCompraId = pc.ProductoCompraId 
                          AND l.Activo = 1 
                          AND l.CantidadActual > 0
                    ), 0) AS LotesActivos,
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
                        object stockActualObj = reader["StockActual"];
                        object stockMinimoObj = reader["StockMinimo"];

                        decimal stockActualDecimal = 0;
                        decimal stockMinimoDecimal = 0;

                        if (stockActualObj != DBNull.Value)
                            stockActualDecimal = Convert.ToDecimal(stockActualObj);
                        if (stockMinimoObj != DBNull.Value)
                            stockMinimoDecimal = Convert.ToDecimal(stockMinimoObj);

                        int stockActualInt = (int)Math.Floor(stockActualDecimal);
                        int stockMinimoInt = (int)Math.Floor(stockMinimoDecimal);

                        inventario.Add(new ProductoModels.InventarioCompra
                        {
                            ProductoCompraId = Convert.ToInt32(reader["ProductoCompraId"]),
                            Codigo = reader["Codigo"]?.ToString() ?? "",
                            Nombre = reader["Nombre"]?.ToString() ?? "",
                            UnidadMedida = reader["UnidadMedida"]?.ToString() ?? "",
                            PrecioCompra = Convert.ToDecimal(reader["PrecioCompra"]),
                            StockActual = stockActualInt,
                            StockMinimo = stockMinimoInt,
                            Categoria = reader["Categoria"]?.ToString() ?? "Sin categoría",
                            Activo = Convert.ToBoolean(reader["Activo"]),
                            EstadoStock = reader["EstadoStock"]?.ToString() ?? "Normal",
                            LotesActivos = Convert.ToInt32(reader["LotesActivos"])
                        });
                    }
                }
            }

            return View(inventario);
        }

        // GET: Producto/StockBajo
        public ActionResult StockBajo()
        {
            if (Session["UserRol"] == null)
                return RedirectToAction("Login", "Account");

            var stockBajo = _inventarioService.ObtenerProductosStockBajo();
            return View(stockBajo);
        }

        // GET: Producto/Movimientos
        public ActionResult Movimientos(int id)
        {
            if (Session["UserRol"] == null)
                return RedirectToAction("Login", "Account");

            var movimientos = _inventarioService.ObtenerMovimientosPorProducto(id);
            string nombreProducto = "";

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

            ViewBag.ProductoNombre = nombreProducto;
            ViewBag.ProductoCompraId = id;
            return View(movimientos);
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
                    l.CostoCompra,
                    l.FechaIngreso,
                    l.FechaFabricacion,
                    l.FechaVencimiento,
                    l.Activo,
                    p.RazonSocial AS ProveedorNombre,
                    CASE 
                        WHEN l.FechaVencimiento IS NOT NULL AND l.FechaVencimiento < GETDATE() THEN 1
                        ELSE 0
                    END AS EstaVencido
                FROM LoteCompra l
                LEFT JOIN Proveedor p ON l.ProveedorId = p.ProveedorId
                WHERE l.ProductoCompraId = @ProductoCompraId
                ORDER BY ISNULL(l.FechaVencimiento, '9999-12-31') ASC, l.FechaIngreso ASC";

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
                            LoteCompraId = Convert.ToInt32(reader["LoteCompraId"]),
                            NumeroLote = reader["NumeroLote"]?.ToString() ?? "",
                            CantidadInicial = Convert.ToInt32(reader["CantidadInicial"]),
                            CantidadActual = Convert.ToInt32(reader["CantidadActual"]),
                            PrecioUnitario = Convert.ToDecimal(reader["PrecioUnitario"]),
                            CostoCompra = Convert.ToDecimal(reader["CostoCompra"]),
                            FechaIngreso = Convert.ToDateTime(reader["FechaIngreso"]),
                            FechaFabricacion = reader["FechaFabricacion"] as DateTime?,
                            FechaVencimiento = reader["FechaVencimiento"] as DateTime?,
                            Activo = Convert.ToBoolean(reader["Activo"]),
                            ProveedorNombre = reader["ProveedorNombre"]?.ToString() ?? "N/A",
                            EstaVencido = Convert.ToInt32(reader["EstaVencido"]) == 1
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
        [HttpPost]
        public JsonResult RecalcularPreciosProductos()
        {
            if (Session["UserRol"] == null)
                return Json(new { success = false, message = "Sesión expirada" });

            try
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    conn.Open();

                    string query = @"
                UPDATE pc
                SET pc.PrecioCompra = (
                    SELECT TOP 1 l.PrecioUnitario 
                    FROM LoteCompra l 
                    WHERE l.ProductoCompraId = pc.ProductoCompraId 
                    ORDER BY l.FechaIngreso DESC
                )
                FROM ProductoCompra pc
                WHERE EXISTS (SELECT 1 FROM LoteCompra WHERE ProductoCompraId = pc.ProductoCompraId)";

                    using (var cmd = new SqlCommand(query, conn))
                    {
                        int rowsAffected = cmd.ExecuteNonQuery();
                        return Json(new { success = true, message = $"{rowsAffected} productos actualizados" });
                    }
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
    }
}