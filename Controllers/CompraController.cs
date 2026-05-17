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
                    dc.DetalleCompraId, 
                    dc.Cantidad, 
                    dc.PrecioUnitario, 
                    dc.Subtotal,
                    dc.EstabaEnOferta, 
                    dc.PrecioOferta,
                    pc.Nombre AS ProductoNombre, 
                    pc.Codigo AS ProductoCodigo
                FROM DetalleCompra dc
                INNER JOIN ProductoCompra pc ON dc.ProductoId = pc.ProductoCompraId
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
                            ProductoCodigo = reader["ProductoCodigo"].ToString(),
                            EstabaEnOferta = (bool)reader["EstabaEnOferta"],
                            PrecioOferta = reader["PrecioOferta"] as decimal?
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

                // Obtener descuentos y precios por proveedor
                var descuentosProveedor = ObtenerDiccionarioDescuentosPorProveedor(model.ProveedorId);
                var preciosProveedor = ObtenerDiccionarioPreciosPorProveedor(model.ProveedorId);

                decimal subtotal = 0;

                // Validar que todos los productos existan en ProductoCompra
                foreach (var item in model.Productos)
                {
                    if (!ExisteProductoCompra(item.ProductoId))
                    {
                        return Json(new { success = false, message = $"El producto con ID {item.ProductoId} no existe en la base de datos de materias primas" });
                    }

                    // Aplicar descuento del proveedor si existe
                    decimal descuentoAplicado = item.Descuento / 100;

                    if (descuentosProveedor.ContainsKey(item.ProductoId) && descuentosProveedor[item.ProductoId] > 0)
                    {
                        descuentoAplicado = descuentosProveedor[item.ProductoId] / 100;
                    }

                    // Usar precio del proveedor si existe
                    decimal precioUnitario = item.PrecioUnitario;
                    if (preciosProveedor.ContainsKey(item.ProductoId) && preciosProveedor[item.ProductoId] > 0)
                    {
                        precioUnitario = preciosProveedor[item.ProductoId];
                    }

                    // Usar precio de oferta si está seleccionado
                    decimal precioFinal = item.EstabaEnOferta && item.PrecioOferta.HasValue ? item.PrecioOferta.Value : precioUnitario;
                    decimal subtotalItem = item.Cantidad * precioFinal * (1 - descuentoAplicado);
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
                        decimal descuentoAplicado = item.Descuento;
                        if (descuentosProveedor.ContainsKey(item.ProductoId) && descuentosProveedor[item.ProductoId] > 0)
                        {
                            descuentoAplicado = descuentosProveedor[item.ProductoId];
                        }

                        decimal precioUnitario = item.PrecioUnitario;
                        if (preciosProveedor.ContainsKey(item.ProductoId) && preciosProveedor[item.ProductoId] > 0)
                        {
                            precioUnitario = preciosProveedor[item.ProductoId];
                        }

                        decimal precioFinal = item.EstabaEnOferta && item.PrecioOferta.HasValue ? item.PrecioOferta.Value : precioUnitario;
                        decimal subtotalItem = item.Cantidad * precioFinal * (1 - descuentoAplicado / 100);

                        string detalleQuery = @"
                            INSERT INTO DetalleCompra (
                                CompraId, ProductoId, Cantidad, PrecioUnitario, 
                                Descuento, Subtotal, EstabaEnOferta, PrecioOferta)
                            VALUES (
                                @CompraId, @ProductoId, @Cantidad, @PrecioUnitario, 
                                @Descuento, @Subtotal, @EstabaEnOferta, @PrecioOferta)";

                        using (var cmdDetalle = new SqlCommand(detalleQuery, conn))
                        {
                            cmdDetalle.Parameters.AddWithValue("@CompraId", compraId);
                            cmdDetalle.Parameters.AddWithValue("@ProductoId", item.ProductoId);
                            cmdDetalle.Parameters.AddWithValue("@Cantidad", item.Cantidad);
                            cmdDetalle.Parameters.AddWithValue("@PrecioUnitario", precioUnitario);
                            cmdDetalle.Parameters.AddWithValue("@Descuento", descuentoAplicado);
                            cmdDetalle.Parameters.AddWithValue("@Subtotal", subtotalItem);
                            cmdDetalle.Parameters.AddWithValue("@EstabaEnOferta", item.EstabaEnOferta);
                            cmdDetalle.Parameters.AddWithValue("@PrecioOferta", item.EstabaEnOferta && item.PrecioOferta.HasValue ? (object)item.PrecioOferta.Value : DBNull.Value);
                            cmdDetalle.ExecuteNonQuery();
                        }
                    }

                    // Crear factura de compra automáticamente
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
                        INSERT INTO Factura (
                            CompraId, NumeroFactura, NumeroDocumento, FechaEmision, 
                            ClienteNombre, ClienteDocumento, ClienteTelefono, TipoPago, 
                            Subtotal, Impuesto, Descuento, Total, Estado, FechaCreacion)
                        VALUES (
                            @CompraId, @NumeroFactura, @NumeroDocumento, GETDATE(), 
                            @ClienteNombre, @ClienteDocumento, @ClienteTelefono, @TipoPago, 
                            @Subtotal, @Impuesto, @Descuento, @Total, 'Vigente', GETDATE())";

                    using (var cmdFactura = new SqlCommand(insertFacturaQuery, conn))
                    {
                        cmdFactura.Parameters.AddWithValue("@CompraId", compraId);
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

                // Actualizar stock de productos de compra
                ActualizarStockProductosCompra(model.Productos);

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

        private bool ExisteProductoCompra(int productoId)
        {
            string query = "SELECT COUNT(*) FROM ProductoCompra WHERE ProductoCompraId = @ProductoId";
            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@ProductoId", productoId);
                conn.Open();
                int count = (int)cmd.ExecuteScalar();
                return count > 0;
            }
        }

        private void ActualizarStockProductosCompra(List<ProductoCompraItem> productos)
        {
            string query = @"
                UPDATE ProductoCompra 
                SET StockActual = StockActual + @Cantidad
                WHERE ProductoCompraId = @ProductoCompraId";

            using (var conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                foreach (var item in productos)
                {
                    using (var cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@ProductoCompraId", item.ProductoId);
                        cmd.Parameters.AddWithValue("@Cantidad", item.Cantidad);
                        cmd.ExecuteNonQuery();
                    }
                }
            }
        }

        private Dictionary<int, decimal> ObtenerDiccionarioDescuentosPorProveedor(int proveedorId)
        {
            var descuentos = new Dictionary<int, decimal>();

            string query = @"
                SELECT ProductoCompraId, DescuentoBase
                FROM ProveedorProductoCompra
                WHERE ProveedorId = @ProveedorId AND Activo = 1 AND DescuentoBase > 0";

            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@ProveedorId", proveedorId);
                conn.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        descuentos.Add((int)reader["ProductoCompraId"], (decimal)reader["DescuentoBase"]);
                    }
                }
            }
            return descuentos;
        }

        private Dictionary<int, decimal> ObtenerDiccionarioPreciosPorProveedor(int proveedorId)
        {
            var precios = new Dictionary<int, decimal>();

            string query = @"
                SELECT ProductoCompraId, PrecioProveedor
                FROM ProveedorProductoCompra
                WHERE ProveedorId = @ProveedorId AND Activo = 1 AND PrecioProveedor IS NOT NULL AND PrecioProveedor > 0";

            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@ProveedorId", proveedorId);
                conn.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        precios.Add((int)reader["ProductoCompraId"], (decimal)reader["PrecioProveedor"]);
                    }
                }
            }
            return precios;
        }

        public JsonResult ListarProductos(string filtro, int? proveedorId = null)
        {
            var productos = new List<object>();

            try
            {
                string query = @"
                    SELECT 
                        pc.ProductoCompraId, 
                        pc.Codigo, 
                        pc.Nombre AS NombreProducto,
                        ISNULL(pc.UnidadMedida, 'Unidad') AS Presentacion,
                        pc.PrecioCompra AS PrecioUnitario
                    FROM ProductoCompra pc
                    WHERE pc.Activo = 1
                    ORDER BY pc.Nombre";

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
                                ProductoId = Convert.ToInt32(reader["ProductoCompraId"]),
                                Codigo = reader["Codigo"]?.ToString() ?? "",
                                NombreProducto = reader["NombreProducto"]?.ToString() ?? "",
                                Presentacion = reader["Presentacion"]?.ToString() ?? "",
                                PrecioUnitario = Convert.ToDecimal(reader["PrecioUnitario"])
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error en ListarProductos: {ex.Message}");
                return Json(new List<object>(), JsonRequestBehavior.AllowGet);
            }

            return Json(productos, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public JsonResult VerificarProductosCompra()
        {
            var productos = new List<object>();
            string query = "SELECT ProductoCompraId, Codigo, Nombre FROM ProductoCompra WHERE Activo = 1 ORDER BY Nombre";

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
                            ProductoCompraId = reader["ProductoCompraId"],
                            Codigo = reader["Codigo"],
                            Nombre = reader["Nombre"]
                        });
                    }
                }
            }

            return Json(productos, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public JsonResult TestProductos()
        {
            try
            {
                var productos = new List<object>();
                string query = "SELECT ProductoCompraId, Codigo, Nombre, PrecioCompra FROM ProductoCompra WHERE Activo = 1";

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
                                Id = reader["ProductoCompraId"],
                                Codigo = reader["Codigo"],
                                Nombre = reader["Nombre"],
                                Precio = reader["PrecioCompra"]
                            });
                        }
                    }
                }

                return Json(new { success = true, count = productos.Count, productos = productos }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        public JsonResult GuardarDescuentoProveedor(int proveedorId, int productoId, decimal descuentoBase, decimal? precioProveedor = null)
        {
            if (Session["UserRol"] == null)
                return Json(new { success = false, message = "Sesión expirada" });

            try
            {
                string query = @"
                    IF EXISTS (SELECT 1 FROM ProveedorProductoCompra WHERE ProveedorId = @ProveedorId AND ProductoCompraId = @ProductoCompraId)
                    BEGIN
                        UPDATE ProveedorProductoCompra 
                        SET DescuentoBase = @DescuentoBase,
                            PrecioProveedor = ISNULL(@PrecioProveedor, PrecioProveedor),
                            FechaCreacion = GETDATE()
                        WHERE ProveedorId = @ProveedorId AND ProductoCompraId = @ProductoCompraId
                    END
                    ELSE
                    BEGIN
                        INSERT INTO ProveedorProductoCompra (ProveedorId, ProductoCompraId, DescuentoBase, PrecioProveedor, Activo)
                        VALUES (@ProveedorId, @ProductoCompraId, @DescuentoBase, @PrecioProveedor, 1)
                    END";

                using (var conn = new SqlConnection(_connectionString))
                using (var cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@ProveedorId", proveedorId);
                    cmd.Parameters.AddWithValue("@ProductoCompraId", productoId);
                    cmd.Parameters.AddWithValue("@DescuentoBase", descuentoBase);
                    cmd.Parameters.AddWithValue("@PrecioProveedor", precioProveedor.HasValue ? (object)precioProveedor.Value : DBNull.Value);
                    conn.Open();
                    cmd.ExecuteNonQuery();
                }

                return Json(new { success = true, message = $"Descuento de {descuentoBase}% guardado correctamente" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public JsonResult ObtenerDescuentosPorProveedor(int proveedorId)
        {
            var descuentos = new List<object>();

            string query = @"
                SELECT 
                    ppc.ProductoCompraId,
                    pc.Codigo,
                    pc.Nombre AS ProductoNombre,
                    ISNULL(ppc.DescuentoBase, 0) AS DescuentoBase,
                    ppc.PrecioProveedor,
                    pc.PrecioCompra AS PrecioRegular,
                    CASE 
                        WHEN ppc.PrecioProveedor IS NOT NULL AND ppc.PrecioProveedor > 0 
                        THEN ppc.PrecioProveedor 
                        ELSE pc.PrecioCompra 
                    END AS PrecioFinal
                FROM ProveedorProductoCompra ppc
                INNER JOIN ProductoCompra pc ON ppc.ProductoCompraId = pc.ProductoCompraId
                WHERE ppc.ProveedorId = @ProveedorId AND ppc.Activo = 1
                ORDER BY pc.Nombre";

            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@ProveedorId", proveedorId);
                conn.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        decimal descuentoBase = Convert.ToDecimal(reader["DescuentoBase"]);
                        decimal precioFinal = Convert.ToDecimal(reader["PrecioFinal"]);
                        decimal precioConDescuento = precioFinal * (1 - descuentoBase / 100);

                        descuentos.Add(new
                        {
                            ProductoId = reader["ProductoCompraId"],
                            Codigo = reader["Codigo"],
                            ProductoNombre = reader["ProductoNombre"],
                            DescuentoBase = descuentoBase,
                            PrecioProveedor = reader["PrecioProveedor"] != DBNull.Value ? reader["PrecioProveedor"] : null,
                            PrecioRegular = reader["PrecioRegular"],
                            PrecioFinal = precioFinal,
                            PrecioConDescuento = precioConDescuento
                        });
                    }
                }
            }

            return Json(descuentos, JsonRequestBehavior.AllowGet);
        }

        private int ObtenerEmpleadoIdValido()
        {
            try
            {
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

            return 19;
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
                        lista.Add(new SelectListItem
                        {
                            Value = reader["ProveedorId"].ToString(),
                            Text = reader["RazonSocial"].ToString()
                        });
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