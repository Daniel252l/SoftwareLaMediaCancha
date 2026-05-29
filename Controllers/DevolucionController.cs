using LaMediaCancha.Models;
using LaMediaCancha.Models.ViewModels;
using LaMediaCancha.Services;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Web.Mvc;
using static LaMediaCancha.Models.DevolucionModels;

namespace LaMediaCancha.Controllers
{
    public class DevolucionController : Controller
    {
        private readonly string _connectionString;
        private readonly DevolucionService _devolucionService;

        public DevolucionController()
        {
            _connectionString = ConfigurationManager.ConnectionStrings["LaMediaCanchaDB"].ConnectionString;
            _devolucionService = new DevolucionService();
        }

        // GET: Devolucion/Index
        public ActionResult Index(string tipo = "")
        {
            if (Session["UserRol"] == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var devoluciones = _devolucionService.ObtenerDevolucionesPorTipo(tipo);
            ViewBag.TipoSeleccionado = tipo;
            return View(devoluciones);
        }

        // GET: Devolucion/Detalle/5
        public ActionResult Detalle(int id)
        {
            if (Session["UserRol"] == null)
            {
                return RedirectToAction("Login", "Account");
            }

            EncabezadoDevolucion devolucion = null;

            string query = @"
                SELECT 
                    d.*,
                    e.CodigoEmpleado,
                    p.Nombres + ' ' + p.Apellidos AS EmpleadoNombre,
                    prov.RazonSocial AS ProveedorNombre,
                    auth.Nombres + ' ' + auth.Apellidos AS AutorizadoPorNombre
                FROM EncabezadoDevolucion d
                INNER JOIN Empleado e ON d.EmpleadoId = e.EmpleadoId
                INNER JOIN Persona p ON e.PersonaId = p.PersonaId
                LEFT JOIN EncabezadoCompra ec ON d.CompraId = ec.CompraId
                LEFT JOIN Proveedor prov ON ec.ProveedorId = prov.ProveedorId
                LEFT JOIN Empleado authEmp ON d.AutorizadoPor = authEmp.EmpleadoId
                LEFT JOIN Persona auth ON authEmp.PersonaId = auth.PersonaId
                WHERE d.DevolucionId = @DevolucionId";

            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@DevolucionId", id);
                conn.Open();

                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        devolucion = new EncabezadoDevolucion
                        {
                            DevolucionId = (int)reader["DevolucionId"],
                            CompraId = reader["CompraId"] as int?,
                            OrdenId = reader["OrdenId"] as int?,
                            NumeroDevolucion = reader["NumeroDevolucion"]?.ToString(),
                            NumeroDocCompra = reader["NumeroDocCompra"]?.ToString(),
                            EmpleadoId = (int)reader["EmpleadoId"],
                            EmpleadoNombre = reader["EmpleadoNombre"].ToString(),
                            ProveedorNombre = reader["ProveedorNombre"]?.ToString(),
                            ClienteNombre = reader["ClienteNombre"]?.ToString(),
                            FechaCompraRef = reader["FechaCompraRef"] != DBNull.Value ? (DateTime)reader["FechaCompraRef"] : DateTime.Now,
                            TeniaProductosEnOferta = (bool)reader["TeniaProductosEnOferta"],
                            FechaDevolucion = (DateTime)reader["FechaDevolucion"],
                            Motivo = reader["Motivo"].ToString(),
                            TipoDevolucion = reader["TipoDevolucion"].ToString(),
                            MontoTotal = (decimal)reader["MontoTotal"],
                            Estado = reader["Estado"].ToString(),
                            Observaciones = reader["Observaciones"]?.ToString(),
                            Tipo = reader["Tipo"]?.ToString(),
                            FormaCompensacion = reader["FormaCompensacion"]?.ToString(),
                            NumeroNotaCredito = reader["NumeroNotaCredito"]?.ToString(),
                            AutorizadoPor = reader["AutorizadoPor"] as int?,
                            AutorizadoPorNombre = reader["AutorizadoPorNombre"]?.ToString(),
                            Activo = (bool)reader["Activo"],
                            FechaCreacion = (DateTime)reader["FechaCreacion"],
                            FechaModificacion = reader["FechaModificacion"] as DateTime?,
                            Detalles = ObtenerDetallesDevolucion(id)
                        };
                    }
                }
            }

            if (devolucion == null)
                return HttpNotFound();

            return View(devolucion);
        }

        private List<DetalleDevolucion> ObtenerDetallesDevolucion(int devolucionId)
        {
            var detalles = new List<DetalleDevolucion>();

            string query = @"
                SELECT 
                    dd.DetalleDevolucionId,
                    dd.DevolucionId,
                    dd.DetalleOrdenId,
                    dd.LoteCompraId,
                    dd.ProductoId,
                    dd.Cantidad,
                    dd.PrecioReferencia,
                    dd.Subtotal,
                    dd.MotivoDetalle,
                    dd.EstabaEnOferta,
                    dd.PrecioOfertaRef,
                    dd.Tipo,
                    dd.DestinoStock,
                    dd.Autorizado,
                    COALESCE(pc.Codigo, p.Codigo) AS ProductoCodigo,
                    COALESCE(pc.Nombre, p.Nombre) AS ProductoNombre,
                    l.NumeroLote
                FROM DetalleDevolucion dd
                LEFT JOIN ProductoCompra pc ON dd.ProductoId = pc.ProductoCompraId
                LEFT JOIN Producto p ON dd.ProductoId = p.ProductoId
                LEFT JOIN LoteCompra l ON dd.LoteCompraId = l.LoteCompraId
                WHERE dd.DevolucionId = @DevolucionId";

            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@DevolucionId", devolucionId);
                conn.Open();

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        detalles.Add(new DetalleDevolucion
                        {
                            DetalleDevolucionId = (int)reader["DetalleDevolucionId"],
                            DevolucionId = (int)reader["DevolucionId"],
                            DetalleOrdenId = reader["DetalleOrdenId"] as int?,
                            LoteCompraId = reader["LoteCompraId"] as int?,
                            ProductoId = (int)reader["ProductoId"],
                            ProductoCodigo = reader["ProductoCodigo"].ToString(),
                            ProductoNombre = reader["ProductoNombre"].ToString(),
                            Cantidad = (decimal)reader["Cantidad"],
                            PrecioReferencia = (decimal)reader["PrecioReferencia"],
                            Subtotal = (decimal)reader["Subtotal"],
                            MotivoDetalle = reader["MotivoDetalle"]?.ToString(),
                            EstabaEnOferta = (bool)reader["EstabaEnOferta"],
                            PrecioOfertaRef = reader["PrecioOfertaRef"] as decimal?,
                            Tipo = reader["Tipo"]?.ToString(),
                            DestinoStock = reader["DestinoStock"]?.ToString(),
                            Autorizado = (bool)reader["Autorizado"]
                        });
                    }
                }
            }

            return detalles;
        }

        // GET: Devolucion/RegistrarProveedor
        public ActionResult RegistrarProveedor(int? compraId = null, string factura = "")
        {
            if (Session["UserRol"] == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var viewModel = new RegistrarDevolucionViewModel
            {
                CompraId = compraId ?? 0,
                NumeroFacturaBuscado = factura,
                Productos = new List<ProductoDevolucion>(),
                Tipo = "Proveedor"
            };

            return View(viewModel);
        }

        // GET: Devolucion/RegistrarCliente
        public ActionResult RegistrarCliente(int? ordenId = null, string factura = "")
        {
            if (Session["UserRol"] == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var viewModel = new RegistrarDevolucionViewModel
            {
                OrdenId = ordenId ?? 0,
                NumeroFacturaBuscado = factura,
                Productos = new List<ProductoDevolucion>(),
                Tipo = "Cliente",
                FormaCompensacion = "CreditoCasa"
            };

            return View(viewModel);
        }

        [HttpPost]
        public JsonResult Registrar(RegistrarDevolucionViewModel model)
        {
            System.Diagnostics.Debug.WriteLine("=== REGISTRAR DEVOLUCIÓN ===");
            System.Diagnostics.Debug.WriteLine($"CompraId: {model.CompraId}, OrdenId: {model.OrdenId}, Tipo: {model.Tipo}");

            if (Session["UserRol"] == null)
            {
                return Json(new { success = false, message = "Sesión expirada" });
            }

            try
            {
                int empleadoId = ObtenerEmpleadoIdPorUsuario();

                if (model.Tipo == "Proveedor")
                {
                    // Devolución a proveedor
                    var productosADevolver = model.Productos.Where(p => p.CantidadADevolver > 0).ToList();

                    if (productosADevolver.Count == 0)
                    {
                        return Json(new { success = false, message = "Debe seleccionar al menos un producto para devolver" });
                    }

                    var request = new RegistrarDevolucionRequest
                    {
                        CompraId = model.CompraId,
                        EmpleadoId = empleadoId,
                        Motivo = model.Motivo,
                        TipoDevolucion = model.TipoDevolucion,
                        Observaciones = model.Observaciones,
                        Productos = productosADevolver.Select(p => new ProductoDevolucionItem
                        {
                            ProductoId = p.ProductoId,
                            Cantidad = p.CantidadADevolver,
                            LoteCompraId = p.LoteCompraId
                        }).ToList()
                    };

                    int devolucionId = _devolucionService.RegistrarDevolucion(request);

                    return Json(new
                    {
                        success = true,
                        message = "Devolución a proveedor registrada exitosamente",
                        devolucionId = devolucionId
                    });
                }
                else
                {
                    // Devolución de cliente
                    var productosADevolver = model.ProductosCliente.Where(p => p.CantidadADevolver > 0).ToList();

                    if (productosADevolver.Count == 0)
                    {
                        return Json(new { success = false, message = "Debe seleccionar al menos un producto para devolver" });
                    }

                    var request = new RegistrarDevolucionClienteRequest
                    {
                        OrdenId = model.OrdenId,
                        EmpleadoId = empleadoId,
                        Motivo = model.Motivo,
                        TipoDevolucion = model.TipoDevolucion,
                        FormaCompensacion = model.FormaCompensacion,
                        Observaciones = model.Observaciones,
                        Productos = productosADevolver.Select(p => new DevolucionClienteItem
                        {
                            DetalleOrdenId = p.DetalleOrdenId,
                            ProductoId = p.ProductoId,
                            Cantidad = p.CantidadADevolver,
                            DestinoStock = p.DestinoStock,
                            Autorizado = false
                        }).ToList()
                    };

                    int devolucionId = _devolucionService.RegistrarDevolucionCliente(request);

                    return Json(new
                    {
                        success = true,
                        message = "Devolución de cliente registrada exitosamente",
                        devolucionId = devolucionId,
                        requiereAutorizacion = productosADevolver.Any(p => p.EstabaEnOferta)
                    });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public JsonResult AutorizarDevolucion(int devolucionId, string motivo)
        {
            if (Session["UserRol"] == null)
                return Json(new { success = false, message = "Sesión expirada" });

            try
            {
                int empleadoId = ObtenerEmpleadoIdPorUsuario();
                bool resultado = _devolucionService.AutorizarDevolucionCliente(devolucionId, empleadoId, motivo);

                if (resultado)
                {
                    return Json(new { success = true, message = "Devolución autorizada exitosamente" });
                }
                else
                {
                    return Json(new { success = false, message = "Error al autorizar la devolución" });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public JsonResult ActualizarNotaCredito(int devolucionId, string numeroNotaCredito)
        {
            if (Session["UserRol"] == null)
                return Json(new { success = false, message = "Sesión expirada" });

            try
            {
                bool resultado = _devolucionService.ActualizarNotaCredito(devolucionId, numeroNotaCredito);

                if (resultado)
                {
                    return Json(new { success = true, message = "Nota de crédito actualizada exitosamente" });
                }
                else
                {
                    return Json(new { success = false, message = "Error al actualizar la nota de crédito" });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        public JsonResult BuscarPorFactura(string numeroFactura, string tipo = "Proveedor")
        {
            try
            {
                if (string.IsNullOrEmpty(numeroFactura))
                {
                    return Json(new { success = false, message = "Ingrese un número de factura o documento de compra" }, JsonRequestBehavior.AllowGet);
                }

                numeroFactura = numeroFactura.Trim();

                if (tipo == "Proveedor")
                {
                    // Buscar compra
                    string query = @"
                        SELECT 
                            ec.CompraId,
                            ec.NumeroDocumento,
                            ec.FechaCompra,
                            ec.Estado,
                            ec.NumeroFactura,
                            ISNULL(p.RazonSocial, 'Proveedor no encontrado') AS ProveedorNombre,
                            ISNULL(p.DiasMaximosDevolucion, 10) AS DiasMaximos
                        FROM EncabezadoCompra ec
                        INNER JOIN Proveedor p ON ec.ProveedorId = p.ProveedorId
                        WHERE (ec.NumeroDocumento = @NumeroFactura OR ec.NumeroFactura = @NumeroFactura)
                          AND ec.Activo = 1";

                    using (var conn = new SqlConnection(_connectionString))
                    {
                        conn.Open();
                        using (var cmd = new SqlCommand(query, conn))
                        {
                            cmd.Parameters.AddWithValue("@NumeroFactura", numeroFactura);

                            using (var reader = cmd.ExecuteReader())
                            {
                                if (reader.Read())
                                {
                                    int compraId = reader.GetInt32(reader.GetOrdinal("CompraId"));
                                    string numeroDocumento = reader.GetString(reader.GetOrdinal("NumeroDocumento"));
                                    DateTime fechaCompra = reader.GetDateTime(reader.GetOrdinal("FechaCompra"));
                                    string proveedorNombre = reader.GetString(reader.GetOrdinal("ProveedorNombre"));
                                    int diasMaximos = reader.GetInt32(reader.GetOrdinal("DiasMaximos"));
                                    string estadoCompra = reader.GetString(reader.GetOrdinal("Estado"));

                                    if (estadoCompra == "Cerrada")
                                    {
                                        return Json(new
                                        {
                                            success = false,
                                            message = "Esta compra ya tiene una devolución registrada. No se pueden registrar más devoluciones."
                                        }, JsonRequestBehavior.AllowGet);
                                    }

                                    int diasTranscurridos = (int)(DateTime.Now.Date - fechaCompra.Date).TotalDays;
                                    bool dentroDePlazo = diasTranscurridos <= diasMaximos;

                                    return Json(new
                                    {
                                        success = true,
                                        tipo = "Proveedor",
                                        compraId = compraId,
                                        ordenId = 0,
                                        numeroDocumento = numeroDocumento,
                                        fecha = fechaCompra.ToString("yyyy-MM-dd"),
                                        clienteProveedorNombre = proveedorNombre,
                                        diasMaximos = diasMaximos,
                                        diasTranscurridos = diasTranscurridos,
                                        dentroDePlazo = dentroDePlazo,
                                        mensajePlazo = dentroDePlazo
                                            ? $"✅ Dentro del plazo ({diasTranscurridos}/{diasMaximos} días)"
                                            : $"❌ Fuera de plazo ({diasTranscurridos}/{diasMaximos} días)"
                                    }, JsonRequestBehavior.AllowGet);
                                }
                            }
                        }
                    }
                }
                else
                {
                    // Buscar orden/venta
                    string query = @"
                        SELECT 
                            o.OrdenId,
                            o.NumeroOrden,
                            o.FechaApertura,
                            o.Estado,
                            o.ClienteNombre,
                            m.NumeroMesa
                        FROM Orden o
                        LEFT JOIN Mesa m ON o.MesaId = m.MesaId
                        WHERE o.NumeroOrden = @NumeroFactura AND o.Estado = 'Cerrada'";

                    using (var conn = new SqlConnection(_connectionString))
                    {
                        conn.Open();
                        using (var cmd = new SqlCommand(query, conn))
                        {
                            cmd.Parameters.AddWithValue("@NumeroFactura", numeroFactura);

                            using (var reader = cmd.ExecuteReader())
                            {
                                if (reader.Read())
                                {
                                    int ordenId = reader.GetInt32(reader.GetOrdinal("OrdenId"));
                                    string numeroOrden = reader.GetString(reader.GetOrdinal("NumeroOrden"));
                                    DateTime fechaVenta = reader.GetDateTime(reader.GetOrdinal("FechaApertura"));
                                    string clienteNombre = reader["ClienteNombre"]?.ToString() ?? "Cliente";

                                    return Json(new
                                    {
                                        success = true,
                                        tipo = "Cliente",
                                        compraId = 0,
                                        ordenId = ordenId,
                                        numeroDocumento = numeroOrden,
                                        fecha = fechaVenta.ToString("yyyy-MM-dd"),
                                        clienteProveedorNombre = clienteNombre,
                                        diasMaximos = 0,
                                        diasTranscurridos = 0,
                                        dentroDePlazo = true,
                                        mensajePlazo = "✅ Venta cerrada - Aplican políticas de devolución"
                                    }, JsonRequestBehavior.AllowGet);
                                }
                            }
                        }
                    }
                }

                return Json(new
                {
                    success = false,
                    message = $"No se encontró ningún documento con el número: {numeroFactura}"
                }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = "Error interno: " + ex.Message
                }, JsonRequestBehavior.AllowGet);
            }
        }

        public JsonResult GetProductosPorCompra(int compraId)
        {
            try
            {
                var productos = _devolucionService.ObtenerProductosDisponiblesParaDevolver(compraId);
                return Json(productos, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        public JsonResult GetProductosPorOrden(int ordenId)
        {
            try
            {
                var productos = _devolucionService.ObtenerProductosDisponiblesParaDevolverCliente(ordenId);
                return Json(productos, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        private int ObtenerEmpleadoIdPorUsuario()
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
    }
}