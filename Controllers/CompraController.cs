using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Configuration;
using System.Linq;
using System.Web.Mvc;
using LaMediaCancha.Models;
using LaMediaCancha.Models.ViewModels;
using LaMediaCancha.Services;

namespace LaMediaCancha.Controllers
{
    public class CompraController : Controller
    {
        private readonly string _connectionString;

        public CompraController()
        {
            _connectionString = ConfigurationManager.ConnectionStrings["LaMediaCanchaDB"].ConnectionString;
        }

        public ActionResult Index()
        {
            if (Session["UserRol"] == null)
                return RedirectToAction("Login", "Account");

            var compras = new List<CompraModels.EncabezadoCompra>();

            string query = @"
                SELECT 
                    ec.CompraId, ec.NumeroDocumento, ec.FechaCompra,
                    ec.Total, ec.Estado, p.RazonSocial AS ProveedorNombre
                FROM EncabezadoCompra ec
                INNER JOIN Proveedor p ON ec.ProveedorId = p.ProveedorId
                ORDER BY ec.FechaCompra DESC";

            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand(query, conn))
            {
                conn.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        compras.Add(new CompraModels.EncabezadoCompra
                        {
                            CompraId = (int)reader["CompraId"],
                            NumeroDocumento = reader["NumeroDocumento"].ToString(),
                            FechaCompra = (DateTime)reader["FechaCompra"],
                            Total = (decimal)reader["Total"],
                            Estado = reader["Estado"].ToString(),
                            ProveedorNombre = reader["ProveedorNombre"].ToString()
                        });
                    }
                }
            }

            return View(compras);
        }

        public ActionResult Detalle(int id)
        {
            if (Session["UserRol"] == null)
                return RedirectToAction("Login", "Account");

            CompraModels.EncabezadoCompra compra = null;

            string query = @"
                SELECT 
                    ec.CompraId, ec.NumeroDocumento, ec.FechaCompra,
                    ec.Subtotal, ec.Impuesto, ec.Total, ec.Estado,
                    ec.Observaciones, p.RazonSocial AS ProveedorNombre
                FROM EncabezadoCompra ec
                INNER JOIN Proveedor p ON ec.ProveedorId = p.ProveedorId
                WHERE ec.CompraId = @CompraId";

            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@CompraId", id);
                conn.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        compra = new CompraModels.EncabezadoCompra
                        {
                            CompraId = (int)reader["CompraId"],
                            NumeroDocumento = reader["NumeroDocumento"].ToString(),
                            FechaCompra = (DateTime)reader["FechaCompra"],
                            Subtotal = reader["Subtotal"] != DBNull.Value ? (decimal)reader["Subtotal"] : 0,
                            Impuesto = reader["Impuesto"] != DBNull.Value ? (decimal)reader["Impuesto"] : 0,
                            Total = (decimal)reader["Total"],
                            Estado = reader["Estado"].ToString(),
                            ProveedorNombre = reader["ProveedorNombre"].ToString(),
                            Observaciones = reader["Observaciones"]?.ToString(),
                            Detalles = ObtenerDetallesCompra(id)
                        };
                    }
                }
            }

            if (compra == null)
                return HttpNotFound();

            return View(compra);
        }

        private List<CompraModels.DetalleCompra> ObtenerDetallesCompra(int compraId)
        {
            var detalles = new List<CompraModels.DetalleCompra>();

            string query = @"
                SELECT 
                    dc.DetalleCompraId, dc.Cantidad, dc.PrecioUnitario, dc.Subtotal,
                    p.Nombre AS ProductoNombre, p.Codigo AS ProductoCodigo
                FROM DetalleCompra dc
                INNER JOIN Producto p ON dc.ProductoId = p.ProductoId
                WHERE dc.CompraId = @CompraId";

            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@CompraId", compraId);
                conn.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        detalles.Add(new CompraModels.DetalleCompra
                        {
                            DetalleCompraId = (int)reader["DetalleCompraId"],
                            Cantidad = (decimal)reader["Cantidad"],
                            PrecioUnitario = (decimal)reader["PrecioUnitario"],
                            Subtotal = (decimal)reader["Subtotal"],
                            ProductoNombre = reader["ProductoNombre"].ToString(),
                            ProductoCodigo = reader["ProductoCodigo"].ToString()
                        });
                    }
                }
            }

            return detalles;
        }

        public ActionResult Registrar()
        {
            if (Session["UserRol"] == null)
                return RedirectToAction("Login", "Account");

            var viewModel = new RegistrarCompraViewModel
            {
                Proveedores = ObtenerProveedores(),
                TiposCompra = ObtenerTiposCompra(),
                TiposPago = ObtenerTiposPago(),
                Productos = new List<ProductoCompraItem>(),
                NumeroDocumento = ObtenerSiguienteNumeroDocumento(),
                NumeroFactura = ObtenerSiguienteNumeroFactura()
            };

            return View(viewModel);
        }

        [HttpPost]
        public JsonResult GuardarCompra(RegistrarCompraViewModel model)
        {
            if (Session["UserRol"] == null)
                return Json(new { success = false, message = "Sesión expirada" });

            if (model.Productos == null || model.Productos.Count == 0)
                return Json(new { success = false, message = "Debe agregar al menos un producto" });

            try
            {
                int empleadoId = ObtenerEmpleadoIdValido();

                decimal subtotal = 0;
                foreach (var item in model.Productos)
                {
                    decimal descuentoAplicado = item.Descuento / 100;
                    decimal subtotalItem = item.Cantidad * item.PrecioUnitario * (1 - descuentoAplicado);
                    subtotal += subtotalItem;
                }

                decimal impuesto = subtotal * 0.12m;
                decimal total = subtotal + impuesto;

                string query = @"
            INSERT INTO EncabezadoCompra (
                EmpleadoId, ProveedorId, TipoCompraId, TipoPagoId, 
                NumeroDocumento, NumeroFactura, FechaCompra, FechaVencimiento, 
                Subtotal, Impuesto, Descuento, Total, Estado, Activo)
            VALUES (
                @EmpleadoId, @ProveedorId, @TipoCompraId, @TipoPagoId, 
                @NumeroDocumento, @NumeroFactura, GETDATE(), @FechaVencimiento, 
                @Subtotal, @Impuesto, 0, @Total, 'Aprobada', 1);
            SELECT SCOPE_IDENTITY();";

                int compraId;
                using (var conn = new SqlConnection(_connectionString))
                {
                    conn.Open();
                    using (var cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@EmpleadoId", empleadoId);
                        cmd.Parameters.AddWithValue("@ProveedorId", model.ProveedorId);
                        cmd.Parameters.AddWithValue("@TipoCompraId", model.TipoCompraId);
                        cmd.Parameters.AddWithValue("@TipoPagoId", model.TipoPagoId);
                        cmd.Parameters.AddWithValue("@NumeroDocumento", model.NumeroDocumento);
                        cmd.Parameters.AddWithValue("@NumeroFactura", string.IsNullOrEmpty(model.NumeroFactura) ? DBNull.Value : (object)model.NumeroFactura);
                        cmd.Parameters.AddWithValue("@FechaVencimiento", model.FechaVencimiento.HasValue ? (object)model.FechaVencimiento.Value : DBNull.Value);
                        cmd.Parameters.AddWithValue("@Subtotal", subtotal);
                        cmd.Parameters.AddWithValue("@Impuesto", impuesto);
                        cmd.Parameters.AddWithValue("@Total", total);

                        compraId = Convert.ToInt32(cmd.ExecuteScalar());
                    }

                    foreach (var item in model.Productos)
                    {
                        decimal descuentoAplicado = item.Descuento / 100;
                        decimal subtotalItem = item.Cantidad * item.PrecioUnitario * (1 - descuentoAplicado);

                        string detalleQuery = @"
                    INSERT INTO DetalleCompra (
                        CompraId, ProductoId, Cantidad, PrecioUnitario, 
                        Descuento, Subtotal, EstabaEnOferta)
                    VALUES (
                        @CompraId, @ProductoId, @Cantidad, @PrecioUnitario, 
                        @Descuento, @Subtotal, 0)";

                        using (var cmdDetalle = new SqlCommand(detalleQuery, conn))
                        {
                            cmdDetalle.Parameters.AddWithValue("@CompraId", compraId);
                            cmdDetalle.Parameters.AddWithValue("@ProductoId", item.ProductoId);
                            cmdDetalle.Parameters.AddWithValue("@Cantidad", item.Cantidad);
                            cmdDetalle.Parameters.AddWithValue("@PrecioUnitario", item.PrecioUnitario);
                            cmdDetalle.Parameters.AddWithValue("@Descuento", item.Descuento);
                            cmdDetalle.Parameters.AddWithValue("@Subtotal", subtotalItem);
                            cmdDetalle.ExecuteNonQuery();
                        }
                    }

                    // ==========================================
                    // CREAR FACTURA DE COMPRA AUTOMÁTICAMENTE
                    // ==========================================
                    string getProveedorQuery = @"
                SELECT RazonSocial, NIT, Telefono 
                FROM Proveedor 
                WHERE ProveedorId = @ProveedorId";

                    string proveedorNombre = "";
                    string proveedorNIT = "";
                    string proveedorTelefono = "";

                    using (var cmd = new SqlCommand(getProveedorQuery, conn))
                    {
                        cmd.Parameters.AddWithValue("@ProveedorId", model.ProveedorId);
                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                proveedorNombre = reader["RazonSocial"].ToString();
                                proveedorNIT = reader["NIT"]?.ToString() ?? "";
                                proveedorTelefono = reader["Telefono"]?.ToString() ?? "";
                            }
                        }
                    }

                    string insertFacturaQuery = @"
                INSERT INTO Factura (NumeroFactura, NumeroDocumento, FechaEmision, ClienteNombre, ClienteDocumento, ClienteTelefono, TipoPago, Subtotal, Impuesto, Descuento, Total, Estado, FechaCreacion)
                VALUES (@NumeroFactura, @NumeroDocumento, GETDATE(), @ClienteNombre, @ClienteDocumento, @ClienteTelefono, @TipoPago, @Subtotal, @Impuesto, @Descuento, @Total, 'Vigente', GETDATE())";

                    using (var cmdFactura = new SqlCommand(insertFacturaQuery, conn))
                    {
                        cmdFactura.Parameters.AddWithValue("@NumeroFactura", string.IsNullOrEmpty(model.NumeroFactura) ? DBNull.Value : (object)model.NumeroFactura);
                        cmdFactura.Parameters.AddWithValue("@NumeroDocumento", model.NumeroDocumento);
                        cmdFactura.Parameters.AddWithValue("@ClienteNombre", proveedorNombre);
                        cmdFactura.Parameters.AddWithValue("@ClienteDocumento", proveedorNIT);
                        cmdFactura.Parameters.AddWithValue("@ClienteTelefono", proveedorTelefono);
                        cmdFactura.Parameters.AddWithValue("@TipoPago", model.TipoPagoId == 1 ? "Contado" : "Crédito");
                        cmdFactura.Parameters.AddWithValue("@Subtotal", subtotal);
                        cmdFactura.Parameters.AddWithValue("@Impuesto", impuesto);
                        cmdFactura.Parameters.AddWithValue("@Descuento", 0);
                        cmdFactura.Parameters.AddWithValue("@Total", total);
                        cmdFactura.ExecuteNonQuery();
                    }
                }

                // Crear lotes
                var loteService = new LoteService();
                loteService.CrearLotesDesdeCompra(compraId);

                return Json(new { success = true, message = "Compra registrada exitosamente", compraId = compraId });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        private int ObtenerEmpleadoIdValido()
        {
            try
            {
                // Primero, intentar obtener el empleado asociado al usuario en sesión
                int usuarioId = Session["UserId"] != null ? (int)Session["UserId"] : 0;

                if (usuarioId > 0)
                {
                    using (var conn = new SqlConnection(_connectionString))
                    {
                        conn.Open();
                        string query = "SELECT EmpleadoId FROM Empleado WHERE UsuarioId = @UsuarioId AND Activo = 1";
                        using (var cmd = new SqlCommand(query, conn))
                        {
                            cmd.Parameters.AddWithValue("@UsuarioId", usuarioId);
                            var result = cmd.ExecuteScalar();
                            if (result != null && result != DBNull.Value)
                            {
                                return Convert.ToInt32(result);
                            }
                        }
                    }
                }

                // Si no se encuentra, obtener el primer empleado activo
                using (var conn = new SqlConnection(_connectionString))
                {
                    conn.Open();
                    string query = "SELECT TOP 1 EmpleadoId FROM Empleado WHERE Activo = 1";
                    using (var cmd = new SqlCommand(query, conn))
                    {
                        var result = cmd.ExecuteScalar();
                        if (result != null && result != DBNull.Value)
                        {
                            return Convert.ToInt32(result);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al obtener EmpleadoId: {ex.Message}");
            }

            // Valor por defecto
            return 19;
        }

        public JsonResult ListarProductos(string filtro, int? proveedorId = null)
        {
            var productos = new List<object>();

            string query = @"
                SELECT TOP 20 
                    p.ProductoId, p.Codigo, p.Nombre,
                    ISNULL(pr.Nombre, 'Unidad') AS Presentacion,
                    ISNULL(pp.PrecioProveedor, p.PrecioCompra) AS PrecioUnitario,
                    ISNULL(pp.DescuentoBase, 0) AS DescuentoBase
                FROM Producto p
                LEFT JOIN Presentacion pr ON p.PresentacionId = pr.PresentacionId
                LEFT JOIN ProveedorProducto pp ON p.ProductoId = pp.ProductoId 
                    AND pp.ProveedorId = @ProveedorId AND pp.Activo = 1
                WHERE p.Activo = 1 
                  AND (ISNULL(p.Codigo, '') LIKE @Filtro OR ISNULL(p.Nombre, '') LIKE @Filtro)
                ORDER BY p.Nombre";

            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@Filtro", "%" + (filtro ?? "") + "%");
                cmd.Parameters.AddWithValue("@ProveedorId", proveedorId.HasValue ? (object)proveedorId.Value : DBNull.Value);
                conn.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        productos.Add(new
                        {
                            ProductoId = Convert.ToInt32(reader["ProductoId"]),
                            Codigo = reader["Codigo"].ToString(),
                            NombreProducto = reader["Nombre"].ToString(),
                            Presentacion = reader["Presentacion"].ToString(),
                            PrecioUnitario = Convert.ToDecimal(reader["PrecioUnitario"]),
                            DescuentoBase = Convert.ToDecimal(reader["DescuentoBase"])
                        });
                    }
                }
            }

            return Json(productos, JsonRequestBehavior.AllowGet);
        }

        private List<SelectListItem> ObtenerProveedores()
        {
            var lista = new List<SelectListItem>();
            string query = "SELECT ProveedorId, RazonSocial FROM Proveedor WHERE Activo = 1 ORDER BY RazonSocial";
            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand(query, conn))
            {
                conn.Open();
                using (var reader = cmd.ExecuteReader())
                    while (reader.Read())
                        lista.Add(new SelectListItem { Value = reader["ProveedorId"].ToString(), Text = reader["RazonSocial"].ToString() });
            }
            return lista;
        }

        private List<SelectListItem> ObtenerTiposCompra()
        {
            var lista = new List<SelectListItem>();
            string query = "SELECT TipoCompraId, Nombre FROM TipoCompra WHERE Activo = 1";
            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand(query, conn))
            {
                conn.Open();
                using (var reader = cmd.ExecuteReader())
                    while (reader.Read())
                        lista.Add(new SelectListItem { Value = reader["TipoCompraId"].ToString(), Text = reader["Nombre"].ToString() });
            }
            return lista;
        }

        private List<SelectListItem> ObtenerTiposPago()
        {
            var lista = new List<SelectListItem>();
            string query = "SELECT TipoPagoId, Nombre FROM TipoPago WHERE Activo = 1";
            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand(query, conn))
            {
                conn.Open();
                using (var reader = cmd.ExecuteReader())
                    while (reader.Read())
                        lista.Add(new SelectListItem { Value = reader["TipoPagoId"].ToString(), Text = reader["Nombre"].ToString() });
            }
            return lista;
        }

        private string ObtenerSiguienteNumeroDocumento()
        {
            string query = "SELECT TOP 1 NumeroDocumento FROM EncabezadoCompra ORDER BY CompraId DESC";
            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand(query, conn))
            {
                conn.Open();
                var result = cmd.ExecuteScalar();
                if (result != null)
                {
                    string ultimo = result.ToString();
                    if (ultimo.Contains("-"))
                    {
                        var partes = ultimo.Split('-');
                        if (partes.Length == 2 && int.TryParse(partes[1], out int num))
                            return $"{partes[0]}-{(num + 1).ToString("D3")}";
                    }
                }
            }
            return "COMP-001";
        }

        private string ObtenerSiguienteNumeroFactura()
        {
            string query = "SELECT TOP 1 NumeroFactura FROM EncabezadoCompra WHERE NumeroFactura IS NOT NULL ORDER BY CompraId DESC";
            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand(query, conn))
            {
                conn.Open();
                var result = cmd.ExecuteScalar();
                if (result != null)
                {
                    string ultimo = result.ToString();
                    if (ultimo.Contains("-"))
                    {
                        var partes = ultimo.Split('-');
                        if (partes.Length == 2 && int.TryParse(partes[1], out int num))
                            return $"{partes[0]}-{(num + 1).ToString("D3")}";
                    }
                }
            }
            return "FAC-001";
        }
    }
}