using LaMediaCancha.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;
using System.Web.Mvc;

namespace LaMediaCancha.Controllers
{
    public class MenuController : Controller
    {
        private readonly string _connectionString;

        public MenuController()
        {
            _connectionString = ConfigurationManager.ConnectionStrings["LaMediaCanchaDB"].ConnectionString;
        }

        // GET: Menu/Index
        public ActionResult Index(string categoria = "")
        {
            if (Session["UserRol"] == null)
                return RedirectToAction("Login", "Account");

            ViewBag.CategoriaSeleccionada = categoria;

            var productos = new List<MenuModels.ProductoMenu>();

            // Consulta para productos individuales
            string query = @"
                SELECT 
                    p.ProductoId,
                    p.Codigo,
                    p.Nombre,
                    p.Descripcion,
                    ISNULL(p.PrecioVenta, 0) AS PrecioVenta,
                    ISNULL(p.Activo, 0) AS Activo,
                    ISNULL(d.Nombre, 'Sin categoría') AS Departamento,
                    ISNULL(sd.Nombre, 'Sin subcategoría') AS SubDepartamento,
                    ISNULL(pr.Nombre, '') AS Presentacion,
                    CASE 
                        WHEN o.OfertaId IS NOT NULL THEN 1 
                        ELSE 0 
                    END AS EnOferta,
                    ISNULL(o.DescuentoPorcentaje, 0) AS DescuentoPorcentaje,
                    CASE 
                        WHEN o.OfertaId IS NOT NULL THEN (ISNULL(p.PrecioVenta, 0) - (ISNULL(p.PrecioVenta, 0) * ISNULL(o.DescuentoPorcentaje, 0) / 100))
                        ELSE ISNULL(p.PrecioVenta, 0)
                    END AS PrecioOferta,
                    o.FechaFin,
                    'Producto' AS TipoItem
                FROM Producto p
                LEFT JOIN SubDepartamento sd ON p.SubDepartamentoId = sd.SubDepartamentoId
                LEFT JOIN Departamento d ON sd.DepartamentoId = d.DepartamentoId
                LEFT JOIN Presentacion pr ON p.PresentacionId = pr.PresentacionId
                LEFT JOIN Oferta o ON p.ProductoId = o.ProductoId AND o.Activo = 1 AND GETDATE() BETWEEN o.FechaInicio AND o.FechaFin
                WHERE p.Activo = 1
                ORDER BY d.Nombre, sd.Nombre, p.Nombre";

            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand(query, conn))
            {
                conn.Open();

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var producto = new MenuModels.ProductoMenu
                        {
                            ProductoId = reader["ProductoId"] != DBNull.Value ? Convert.ToInt32(reader["ProductoId"]) : 0,
                            Codigo = reader["Codigo"] != DBNull.Value ? reader["Codigo"].ToString() : "",
                            Nombre = reader["Nombre"] != DBNull.Value ? reader["Nombre"].ToString() : "",
                            Descripcion = reader["Descripcion"] != DBNull.Value ? reader["Descripcion"].ToString() : null,
                            PrecioVenta = reader["PrecioVenta"] != DBNull.Value ? Convert.ToDecimal(reader["PrecioVenta"]) : 0,
                            Activo = reader["Activo"] != DBNull.Value ? Convert.ToBoolean(reader["Activo"]) : false,
                            Departamento = reader["Departamento"] != DBNull.Value ? reader["Departamento"].ToString() : "Sin categoría",
                            SubDepartamento = reader["SubDepartamento"] != DBNull.Value ? reader["SubDepartamento"].ToString() : "Sin subcategoría",
                            Presentacion = reader["Presentacion"] != DBNull.Value ? reader["Presentacion"].ToString() : "",
                            EnOferta = reader["EnOferta"] != DBNull.Value ? Convert.ToBoolean(reader["EnOferta"]) : false,
                            DescuentoPorcentaje = reader["DescuentoPorcentaje"] != DBNull.Value ? Convert.ToDecimal(reader["DescuentoPorcentaje"]) : (decimal?)null,
                            PrecioOferta = reader["PrecioOferta"] != DBNull.Value ? Convert.ToDecimal(reader["PrecioOferta"]) : (decimal?)null,
                            FechaFinOferta = reader["FechaFin"] != DBNull.Value ? Convert.ToDateTime(reader["FechaFin"]) : (DateTime?)null,
                            TipoItem = "Producto"
                        };
                        productos.Add(producto);
                    }
                }
            }

            // Agregar combos por separado
            try
            {
                string queryCombos = @"
                    SELECT 
                        c.ComboId AS ProductoId,
                        c.Nombre,
                        c.Descripcion,
                        c.PrecioCombo AS PrecioVenta,
                        'Combos' AS Departamento,
                        'Combos Especiales' AS SubDepartamento,
                        'Combo' AS Presentacion,
                        'Combo' AS TipoItem
                    FROM Combo c
                    WHERE c.Activo = 1";

                using (var conn = new SqlConnection(_connectionString))
                using (var cmd = new SqlCommand(queryCombos, conn))
                {
                    conn.Open();

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var combo = new MenuModels.ProductoMenu
                            {
                                ProductoId = reader["ProductoId"] != DBNull.Value ? Convert.ToInt32(reader["ProductoId"]) : 0,
                                Codigo = reader["Nombre"] != DBNull.Value ? reader["Nombre"].ToString() : "",
                                Nombre = reader["Nombre"] != DBNull.Value ? reader["Nombre"].ToString() : "",
                                Descripcion = reader["Descripcion"] != DBNull.Value ? reader["Descripcion"].ToString() : null,
                                PrecioVenta = reader["PrecioVenta"] != DBNull.Value ? Convert.ToDecimal(reader["PrecioVenta"]) : 0,
                                Activo = true,
                                Departamento = reader["Departamento"] != DBNull.Value ? reader["Departamento"].ToString() : "Combos",
                                SubDepartamento = reader["SubDepartamento"] != DBNull.Value ? reader["SubDepartamento"].ToString() : "Combos Especiales",
                                Presentacion = reader["Presentacion"] != DBNull.Value ? reader["Presentacion"].ToString() : "",
                                EnOferta = false,
                                TipoItem = "Combo"
                            };
                            productos.Add(combo);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al cargar combos: {ex.Message}");
            }

            // Ordenar productos
            productos = productos.OrderBy(p => p.Departamento).ThenBy(p => p.SubDepartamento).ThenBy(p => p.Nombre).ToList();

            // Obtener categorías para el filtro
            var categorias = new List<SelectListItem>();
            categorias.Add(new SelectListItem { Value = "", Text = "Todas las categorías" });

            // Agregar categorías de productos
            var categoriasProductos = productos.Where(p => p.TipoItem == "Producto").Select(p => p.Departamento).Distinct().OrderBy(d => d);
            foreach (var cat in categoriasProductos)
            {
                if (cat != "Sin categoría")
                {
                    categorias.Add(new SelectListItem { Value = cat, Text = cat });
                }
            }

            // Agregar categoría de combos
            if (productos.Any(p => p.TipoItem == "Combo"))
            {
                categorias.Add(new SelectListItem { Value = "Combos", Text = "Combos Especiales" });
            }

            ViewBag.Categorias = categorias;

            // Filtrar por categoría si es necesario
            if (!string.IsNullOrEmpty(categoria))
            {
                if (categoria == "Combos")
                {
                    productos = productos.Where(p => p.TipoItem == "Combo").ToList();
                }
                else
                {
                    productos = productos.Where(p => p.Departamento == categoria && p.TipoItem == "Producto").ToList();
                }
            }

            return View(productos);
        }

        // GET: Menu/Detalle/5
        public ActionResult Detalle(int id, string tipo = "")
        {
            if (Session["UserRol"] == null)
                return RedirectToAction("Login", "Account");

            MenuModels.ProductoMenu producto = null;

            // Si el tipo es "Combo", buscar como combo directamente
            if (tipo == "Combo")
            {
                string queryCombo = @"
                    SELECT 
                        c.ComboId AS ProductoId,
                        c.Nombre,
                        c.Descripcion,
                        c.PrecioCombo AS PrecioVenta,
                        'Combos' AS Departamento,
                        'Combos Especiales' AS SubDepartamento,
                        'Combo' AS Presentacion,
                        'Combo' AS TipoItem
                    FROM Combo c
                    WHERE c.ComboId = @Id AND c.Activo = 1";

                using (var conn = new SqlConnection(_connectionString))
                using (var cmd = new SqlCommand(queryCombo, conn))
                {
                    cmd.Parameters.AddWithValue("@Id", id);
                    conn.Open();

                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            producto = new MenuModels.ProductoMenu
                            {
                                ProductoId = reader["ProductoId"] != DBNull.Value ? Convert.ToInt32(reader["ProductoId"]) : 0,
                                Codigo = reader["Nombre"] != DBNull.Value ? reader["Nombre"].ToString() : "",
                                Nombre = reader["Nombre"] != DBNull.Value ? reader["Nombre"].ToString() : "",
                                Descripcion = reader["Descripcion"] != DBNull.Value ? reader["Descripcion"].ToString() : null,
                                PrecioVenta = reader["PrecioVenta"] != DBNull.Value ? Convert.ToDecimal(reader["PrecioVenta"]) : 0,
                                Activo = true,
                                Departamento = reader["Departamento"] != DBNull.Value ? reader["Departamento"].ToString() : "Combos",
                                SubDepartamento = reader["SubDepartamento"] != DBNull.Value ? reader["SubDepartamento"].ToString() : "Combos Especiales",
                                Presentacion = reader["Presentacion"] != DBNull.Value ? reader["Presentacion"].ToString() : "",
                                TipoItem = "Combo",
                                ProductosCombo = ObtenerProductosDelCombo(id)
                            };
                        }
                    }
                }
            }
            else
            {
                // Buscar como producto
                string queryProducto = @"
                    SELECT 
                        p.ProductoId,
                        p.Codigo,
                        p.Nombre,
                        p.Descripcion,
                        ISNULL(p.PrecioVenta, 0) AS PrecioVenta,
                        ISNULL(p.Activo, 0) AS Activo,
                        ISNULL(d.Nombre, 'Sin categoría') AS Departamento,
                        ISNULL(sd.Nombre, 'Sin subcategoría') AS SubDepartamento,
                        ISNULL(pr.Nombre, '') AS Presentacion,
                        CASE 
                            WHEN o.OfertaId IS NOT NULL THEN 1 
                            ELSE 0 
                        END AS EnOferta,
                        ISNULL(o.DescuentoPorcentaje, 0) AS DescuentoPorcentaje,
                        CASE 
                            WHEN o.OfertaId IS NOT NULL THEN (ISNULL(p.PrecioVenta, 0) - (ISNULL(p.PrecioVenta, 0) * ISNULL(o.DescuentoPorcentaje, 0) / 100))
                            ELSE ISNULL(p.PrecioVenta, 0)
                        END AS PrecioOferta,
                        o.FechaInicio,
                        o.FechaFin,
                        'Producto' AS TipoItem
                    FROM Producto p
                    LEFT JOIN SubDepartamento sd ON p.SubDepartamentoId = sd.SubDepartamentoId
                    LEFT JOIN Departamento d ON sd.DepartamentoId = d.DepartamentoId
                    LEFT JOIN Presentacion pr ON p.PresentacionId = pr.PresentacionId
                    LEFT JOIN Oferta o ON p.ProductoId = o.ProductoId AND o.Activo = 1 AND GETDATE() BETWEEN o.FechaInicio AND o.FechaFin
                    WHERE p.ProductoId = @Id";

                using (var conn = new SqlConnection(_connectionString))
                using (var cmd = new SqlCommand(queryProducto, conn))
                {
                    cmd.Parameters.AddWithValue("@Id", id);
                    conn.Open();

                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            producto = new MenuModels.ProductoMenu
                            {
                                ProductoId = reader["ProductoId"] != DBNull.Value ? Convert.ToInt32(reader["ProductoId"]) : 0,
                                Codigo = reader["Codigo"] != DBNull.Value ? reader["Codigo"].ToString() : "",
                                Nombre = reader["Nombre"] != DBNull.Value ? reader["Nombre"].ToString() : "",
                                Descripcion = reader["Descripcion"] != DBNull.Value ? reader["Descripcion"].ToString() : null,
                                PrecioVenta = reader["PrecioVenta"] != DBNull.Value ? Convert.ToDecimal(reader["PrecioVenta"]) : 0,
                                Activo = reader["Activo"] != DBNull.Value ? Convert.ToBoolean(reader["Activo"]) : false,
                                Departamento = reader["Departamento"] != DBNull.Value ? reader["Departamento"].ToString() : "Sin categoría",
                                SubDepartamento = reader["SubDepartamento"] != DBNull.Value ? reader["SubDepartamento"].ToString() : "Sin subcategoría",
                                Presentacion = reader["Presentacion"] != DBNull.Value ? reader["Presentacion"].ToString() : "",
                                EnOferta = reader["EnOferta"] != DBNull.Value ? Convert.ToBoolean(reader["EnOferta"]) : false,
                                DescuentoPorcentaje = reader["DescuentoPorcentaje"] != DBNull.Value ? Convert.ToDecimal(reader["DescuentoPorcentaje"]) : (decimal?)null,
                                PrecioOferta = reader["PrecioOferta"] != DBNull.Value ? Convert.ToDecimal(reader["PrecioOferta"]) : (decimal?)null,
                                FechaInicioOferta = reader["FechaInicio"] != DBNull.Value ? Convert.ToDateTime(reader["FechaInicio"]) : (DateTime?)null,
                                FechaFinOferta = reader["FechaFin"] != DBNull.Value ? Convert.ToDateTime(reader["FechaFin"]) : (DateTime?)null,
                                TipoItem = "Producto"
                            };
                        }
                    }
                }

                // Si no se encontró como producto, buscar como combo
                if (producto == null)
                {
                    string queryCombo = @"
                        SELECT 
                            c.ComboId AS ProductoId,
                            c.Nombre,
                            c.Descripcion,
                            c.PrecioCombo AS PrecioVenta,
                            'Combos' AS Departamento,
                            'Combos Especiales' AS SubDepartamento,
                            'Combo' AS Presentacion,
                            'Combo' AS TipoItem
                        FROM Combo c
                        WHERE c.ComboId = @Id AND c.Activo = 1";

                    using (var conn = new SqlConnection(_connectionString))
                    using (var cmd = new SqlCommand(queryCombo, conn))
                    {
                        cmd.Parameters.AddWithValue("@Id", id);
                        conn.Open();

                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                producto = new MenuModels.ProductoMenu
                                {
                                    ProductoId = reader["ProductoId"] != DBNull.Value ? Convert.ToInt32(reader["ProductoId"]) : 0,
                                    Codigo = reader["Nombre"] != DBNull.Value ? reader["Nombre"].ToString() : "",
                                    Nombre = reader["Nombre"] != DBNull.Value ? reader["Nombre"].ToString() : "",
                                    Descripcion = reader["Descripcion"] != DBNull.Value ? reader["Descripcion"].ToString() : null,
                                    PrecioVenta = reader["PrecioVenta"] != DBNull.Value ? Convert.ToDecimal(reader["PrecioVenta"]) : 0,
                                    Activo = true,
                                    Departamento = reader["Departamento"] != DBNull.Value ? reader["Departamento"].ToString() : "Combos",
                                    SubDepartamento = reader["SubDepartamento"] != DBNull.Value ? reader["SubDepartamento"].ToString() : "Combos Especiales",
                                    Presentacion = reader["Presentacion"] != DBNull.Value ? reader["Presentacion"].ToString() : "",
                                    TipoItem = "Combo",
                                    ProductosCombo = ObtenerProductosDelCombo(id)
                                };
                            }
                        }
                    }
                }
            }

            if (producto == null)
                return HttpNotFound();

            return View(producto);
        }

        private List<MenuModels.ComboDetalleMenu> ObtenerProductosDelCombo(int comboId)
        {
            var productos = new List<MenuModels.ComboDetalleMenu>();

            string query = @"
                SELECT 
                    cd.ProductoId,
                    p.Nombre AS ProductoNombre,
                    cd.CantidadIncluida,
                    p.PrecioVenta AS PrecioIndividual
                FROM ComboDetalle cd
                INNER JOIN Producto p ON cd.ProductoId = p.ProductoId
                WHERE cd.ComboId = @ComboId";

            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@ComboId", comboId);
                conn.Open();

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        productos.Add(new MenuModels.ComboDetalleMenu
                        {
                            ProductoId = (int)reader["ProductoId"],
                            ProductoNombre = reader["ProductoNombre"].ToString(),
                            CantidadIncluida = (int)reader["CantidadIncluida"],
                            PrecioIndividual = (decimal)reader["PrecioIndividual"]
                        });
                    }
                }
            }

            return productos;
        }
    }
}