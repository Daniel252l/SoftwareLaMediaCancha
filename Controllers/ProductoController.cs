using LaMediaCancha.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web.Mvc;
// Alias para simplificar
using Producto = LaMediaCancha.Models.ProductoModels.Producto;

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
            var productos = new List<Producto>();

            string query = @"
                SELECT 
                    p.ProductoId,
                    p.Codigo,
                    p.Nombre,
                    p.Descripcion,
                    p.PrecioCompra,
                    p.PrecioVenta,
                    p.Activo,
                    p.FechaCreacion,
                    d.Nombre AS DepartamentoNombre,
                    sd.Nombre AS SubDepartamentoNombre,
                    pr.Nombre AS PresentacionNombre,
                    m.Nombre AS MarcaNombre
                FROM Producto p
                LEFT JOIN SubDepartamento sd ON p.SubDepartamentoId = sd.SubDepartamentoId
                LEFT JOIN Departamento d ON sd.DepartamentoId = d.DepartamentoId
                LEFT JOIN Presentacion pr ON p.PresentacionId = pr.PresentacionId
                LEFT JOIN Marca m ON p.MarcaId = m.MarcaId
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
                        productos.Add(new Producto
                        {
                            ProductoId = (int)reader["ProductoId"],
                            Codigo = reader["Codigo"].ToString(),
                            Nombre = reader["Nombre"].ToString(),
                            Descripcion = reader["Descripcion"]?.ToString(),
                            PrecioCompra = (decimal)reader["PrecioCompra"],
                            PrecioVenta = (decimal)reader["PrecioVenta"],
                            Activo = (bool)reader["Activo"],
                            FechaCreacion = (DateTime)reader["FechaCreacion"],
                            DepartamentoNombre = reader["DepartamentoNombre"]?.ToString(),
                            SubDepartamentoNombre = reader["SubDepartamentoNombre"]?.ToString(),
                            PresentacionNombre = reader["PresentacionNombre"]?.ToString(),
                            MarcaNombre = reader["MarcaNombre"]?.ToString()
                        });
                    }
                }
            }

            return View(productos);
        }

        // GET: Producto/Detalle/5
        public ActionResult Detalle(int id)
        {
            Producto producto = null;

            string query = @"
                SELECT 
                    p.ProductoId,
                    p.Codigo,
                    p.CodigoBarras,
                    p.Nombre,
                    p.Descripcion,
                    p.PrecioCompra,
                    p.PrecioVenta,
                    p.EstaEnOferta,
                    p.PrecioOferta,
                    p.FechaInicioOferta,
                    p.FechaFinOferta,
                    p.Activo,
                    p.FechaCreacion,
                    p.FechaModificacion,
                    p.SubDepartamentoId,
                    d.DepartamentoId,
                    d.Nombre AS DepartamentoNombre,
                    sd.Nombre AS SubDepartamentoNombre,
                    pr.Nombre AS PresentacionNombre,
                    pr.Abreviatura AS PresentacionAbreviatura,
                    m.Nombre AS MarcaNombre
                FROM Producto p
                LEFT JOIN SubDepartamento sd ON p.SubDepartamentoId = sd.SubDepartamentoId
                LEFT JOIN Departamento d ON sd.DepartamentoId = d.DepartamentoId
                LEFT JOIN Presentacion pr ON p.PresentacionId = pr.PresentacionId
                LEFT JOIN Marca m ON p.MarcaId = m.MarcaId
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
                            Codigo = reader["Codigo"].ToString(),
                            CodigoBarras = reader["CodigoBarras"]?.ToString(),
                            Nombre = reader["Nombre"].ToString(),
                            Descripcion = reader["Descripcion"]?.ToString(),
                            PrecioCompra = (decimal)reader["PrecioCompra"],
                            PrecioVenta = (decimal)reader["PrecioVenta"],
                            EstaEnOferta = (bool)reader["EstaEnOferta"],
                            PrecioOferta = reader["PrecioOferta"] as decimal?,
                            FechaInicioOferta = reader["FechaInicioOferta"] as DateTime?,
                            FechaFinOferta = reader["FechaFinOferta"] as DateTime?,
                            Activo = (bool)reader["Activo"],
                            FechaCreacion = (DateTime)reader["FechaCreacion"],
                            FechaModificacion = reader["FechaModificacion"] as DateTime?,
                            SubDepartamentoId = (int)reader["SubDepartamentoId"],
                            DepartamentoId = (int)reader["DepartamentoId"],
                            DepartamentoNombre = reader["DepartamentoNombre"].ToString(),
                            SubDepartamentoNombre = reader["SubDepartamentoNombre"].ToString(),
                            PresentacionNombre = reader["PresentacionNombre"].ToString(),
                            PresentacionAbreviatura = reader["PresentacionAbreviatura"].ToString(),
                            MarcaNombre = reader["MarcaNombre"]?.ToString()
                        };
                    }
                }
            }

            if (producto == null)
            {
                return HttpNotFound();
            }

            return View(producto);
        }

        // GET: Producto/Crear
        public ActionResult Crear()
        {
            CargarCombos();
            return View(new Producto());
        }

        // POST: Producto/Crear
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Crear(Producto producto)
        {
            if (ModelState.IsValid)
            {
                string query = @"
                    INSERT INTO Producto (SubDepartamentoId, PresentacionId, MarcaId, Codigo, CodigoBarras, 
                                         Nombre, Descripcion, PrecioCompra, PrecioVenta, EstaEnOferta, 
                                         PrecioOferta, FechaInicioOferta, FechaFinOferta, Activo, FechaCreacion)
                    VALUES (@SubDepartamentoId, @PresentacionId, @MarcaId, @Codigo, @CodigoBarras,
                            @Nombre, @Descripcion, @PrecioCompra, @PrecioVenta, @EstaEnOferta,
                            @PrecioOferta, @FechaInicioOferta, @FechaFinOferta, @Activo, GETDATE());
                    SELECT SCOPE_IDENTITY();";

                using (var conn = new SqlConnection(_connectionString))
                using (var cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@SubDepartamentoId", producto.SubDepartamentoId);
                    cmd.Parameters.AddWithValue("@PresentacionId", producto.PresentacionId);
                    cmd.Parameters.AddWithValue("@MarcaId", (object)producto.MarcaId ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Codigo", producto.Codigo);
                    cmd.Parameters.AddWithValue("@CodigoBarras", (object)producto.CodigoBarras ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Nombre", producto.Nombre);
                    cmd.Parameters.AddWithValue("@Descripcion", (object)producto.Descripcion ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@PrecioCompra", producto.PrecioCompra);
                    cmd.Parameters.AddWithValue("@PrecioVenta", producto.PrecioVenta);
                    cmd.Parameters.AddWithValue("@EstaEnOferta", producto.EstaEnOferta);
                    cmd.Parameters.AddWithValue("@PrecioOferta", (object)producto.PrecioOferta ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@FechaInicioOferta", (object)producto.FechaInicioOferta ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@FechaFinOferta", (object)producto.FechaFinOferta ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Activo", producto.Activo);

                    conn.Open();
                    int newId = Convert.ToInt32(cmd.ExecuteScalar());

                    // Crear inventario para el nuevo producto
                    string queryInventario = @"
                        INSERT INTO Inventario (ProductoId, ExistenciaActual, StockMinimo, StockMaximo)
                        VALUES (@ProductoId, 0, 0, 0)";

                    using (var cmdInv = new SqlCommand(queryInventario, conn))
                    {
                        cmdInv.Parameters.AddWithValue("@ProductoId", newId);
                        cmdInv.ExecuteNonQuery();
                    }
                }

                TempData["Success"] = "Producto creado exitosamente";
                return RedirectToAction("Index");
            }

            CargarCombos();
            return View(producto);
        }

        // GET: Producto/Editar/5
        public ActionResult Editar(int id)
        {
            Producto producto = null;

            string query = @"
                SELECT 
                    p.ProductoId,
                    p.Codigo,
                    p.CodigoBarras,
                    p.Nombre,
                    p.Descripcion,
                    p.PrecioCompra,
                    p.PrecioVenta,
                    p.EstaEnOferta,
                    p.PrecioOferta,
                    p.FechaInicioOferta,
                    p.FechaFinOferta,
                    p.Activo,
                    p.SubDepartamentoId,
                    sd.DepartamentoId
                FROM Producto p
                INNER JOIN SubDepartamento sd ON p.SubDepartamentoId = sd.SubDepartamentoId
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
                            Codigo = reader["Codigo"].ToString(),
                            CodigoBarras = reader["CodigoBarras"]?.ToString(),
                            Nombre = reader["Nombre"].ToString(),
                            Descripcion = reader["Descripcion"]?.ToString(),
                            PrecioCompra = (decimal)reader["PrecioCompra"],
                            PrecioVenta = (decimal)reader["PrecioVenta"],
                            EstaEnOferta = (bool)reader["EstaEnOferta"],
                            PrecioOferta = reader["PrecioOferta"] as decimal?,
                            FechaInicioOferta = reader["FechaInicioOferta"] as DateTime?,
                            FechaFinOferta = reader["FechaFinOferta"] as DateTime?,
                            Activo = (bool)reader["Activo"],
                            SubDepartamentoId = (int)reader["SubDepartamentoId"],
                            DepartamentoId = (int)reader["DepartamentoId"]
                        };
                    }
                }
            }

            if (producto == null)
            {
                return HttpNotFound();
            }

            CargarCombos(producto.DepartamentoId, producto.SubDepartamentoId);
            return View(producto);
        }

        // POST: Producto/Editar/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Editar(Producto producto)
        {
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
                    cmd.Parameters.AddWithValue("@SubDepartamentoId", producto.SubDepartamentoId);
                    cmd.Parameters.AddWithValue("@PresentacionId", producto.PresentacionId);
                    cmd.Parameters.AddWithValue("@MarcaId", (object)producto.MarcaId ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Codigo", producto.Codigo);
                    cmd.Parameters.AddWithValue("@CodigoBarras", (object)producto.CodigoBarras ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Nombre", producto.Nombre);
                    cmd.Parameters.AddWithValue("@Descripcion", (object)producto.Descripcion ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@PrecioCompra", producto.PrecioCompra);
                    cmd.Parameters.AddWithValue("@PrecioVenta", producto.PrecioVenta);
                    cmd.Parameters.AddWithValue("@EstaEnOferta", producto.EstaEnOferta);
                    cmd.Parameters.AddWithValue("@PrecioOferta", (object)producto.PrecioOferta ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@FechaInicioOferta", (object)producto.FechaInicioOferta ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@FechaFinOferta", (object)producto.FechaFinOferta ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Activo", producto.Activo);

                    conn.Open();
                    cmd.ExecuteNonQuery();
                }

                TempData["Success"] = "Producto actualizado exitosamente";
                return RedirectToAction("Index");
            }

            CargarCombos(producto.DepartamentoId, producto.SubDepartamentoId);
            return View(producto);
        }

        // POST: Producto/Eliminar
        [HttpPost]
        public JsonResult Eliminar(int id)
        {
            try
            {
                // Primero eliminar el inventario
                string deleteInventario = "DELETE FROM Inventario WHERE ProductoId = @ProductoId";
                using (var conn = new SqlConnection(_connectionString))
                using (var cmd = new SqlCommand(deleteInventario, conn))
                {
                    cmd.Parameters.AddWithValue("@ProductoId", id);
                    conn.Open();
                    cmd.ExecuteNonQuery();
                }

                // Luego eliminar el producto
                string deleteProducto = "DELETE FROM Producto WHERE ProductoId = @ProductoId";
                using (var conn = new SqlConnection(_connectionString))
                using (var cmd = new SqlCommand(deleteProducto, conn))
                {
                    cmd.Parameters.AddWithValue("@ProductoId", id);
                    conn.Open();
                    cmd.ExecuteNonQuery();
                }

                return Json(new { success = true, message = "Producto eliminado exitosamente" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // GET: Producto/Inventario
        public ActionResult Inventario()
        {
            var inventario = new List<ProductoModels.InventarioProducto>();

            string query = @"
                SELECT 
                    p.ProductoId,
                    p.Codigo,
                    p.Nombre,
                    i.ExistenciaActual,
                    i.StockMinimo,
                    i.StockMaximo,
                    d.Nombre AS Departamento
                FROM Producto p
                INNER JOIN Inventario i ON p.ProductoId = i.ProductoId
                INNER JOIN SubDepartamento sd ON p.SubDepartamentoId = sd.SubDepartamentoId
                INNER JOIN Departamento d ON sd.DepartamentoId = d.DepartamentoId
                ORDER BY p.Nombre";

            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand(query, conn))
            {
                conn.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        inventario.Add(new ProductoModels.InventarioProducto
                        {
                            ProductoId = (int)reader["ProductoId"],
                            Codigo = reader["Codigo"].ToString(),
                            Nombre = reader["Nombre"].ToString(),
                            ExistenciaActual = (int)reader["ExistenciaActual"],
                            StockMinimo = (int)reader["StockMinimo"],
                            StockMaximo = (int)reader["StockMaximo"],
                            Departamento = reader["Departamento"].ToString()
                        });
                    }
                }
            }

            return View(inventario);
        }

        // Métodos auxiliares para combos
        private void CargarCombos(int? departamentoId = null, int? subDepartamentoId = null)
        {
            // Departamentos
            ViewBag.Departamentos = new SelectList(ObtenerDepartamentos(), "DepartamentoId", "Nombre", departamentoId);

            // SubDepartamentos
            if (departamentoId.HasValue)
            {
                ViewBag.SubDepartamentos = new SelectList(ObtenerSubDepartamentos(departamentoId.Value), "SubDepartamentoId", "Nombre", subDepartamentoId);
            }

            // Presentaciones
            ViewBag.Presentaciones = new SelectList(ObtenerPresentaciones(), "PresentacionId", "Nombre");

            // Marcas
            ViewBag.Marcas = new SelectList(ObtenerMarcas(), "MarcaId", "Nombre");
        }

        private List<Departamento> ObtenerDepartamentos()
        {
            var lista = new List<Departamento>();
            string query = "SELECT DepartamentoId, Nombre FROM Departamento WHERE Activo = 1 ORDER BY Nombre";

            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand(query, conn))
            {
                conn.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        lista.Add(new Departamento { DepartamentoId = (int)reader["DepartamentoId"], Nombre = reader["Nombre"].ToString() });
                    }
                }
            }
            return lista;
        }

        private List<SubDepartamento> ObtenerSubDepartamentos(int departamentoId)
        {
            var lista = new List<SubDepartamento>();
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
                        lista.Add(new SubDepartamento { SubDepartamentoId = (int)reader["SubDepartamentoId"], Nombre = reader["Nombre"].ToString() });
                    }
                }
            }
            return lista;
        }

        private List<Presentacion> ObtenerPresentaciones()
        {
            var lista = new List<Presentacion>();
            string query = "SELECT PresentacionId, Nombre, Abreviatura FROM Presentacion WHERE Activo = 1 ORDER BY Nombre";

            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand(query, conn))
            {
                conn.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        lista.Add(new Presentacion { PresentacionId = (int)reader["PresentacionId"], Nombre = reader["Nombre"].ToString(), Abreviatura = reader["Abreviatura"].ToString() });
                    }
                }
            }
            return lista;
        }

        private List<Marca> ObtenerMarcas()
        {
            var lista = new List<Marca>();
            string query = "SELECT MarcaId, Nombre FROM Marca WHERE Activo = 1 ORDER BY Nombre";

            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand(query, conn))
            {
                conn.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        lista.Add(new Marca { MarcaId = (int)reader["MarcaId"], Nombre = reader["Nombre"].ToString() });
                    }
                }
            }
            return lista;
        }

        // API para AJAX
        public JsonResult GetSubDepartamentos(int departamentoId)
        {
            var subDepartamentos = ObtenerSubDepartamentos(departamentoId);
            return Json(subDepartamentos.Select(s => new { Value = s.SubDepartamentoId, Text = s.Nombre }), JsonRequestBehavior.AllowGet);
        }
    }
    // Clases auxiliares
    public class Departamento
    {
        public int DepartamentoId { get; set; }
        public string Nombre { get; set; }
    }

    public class SubDepartamento
    {
        public int SubDepartamentoId { get; set; }
        public string Nombre { get; set; }
    }

    public class Presentacion
    {
        public int PresentacionId { get; set; }
        public string Nombre { get; set; }
        public string Abreviatura { get; set; }
    }

    public class Marca
    {
        public int MarcaId { get; set; }
        public string Nombre { get; set; }
    }
}