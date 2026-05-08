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

        public DevolucionController()
        {
            _connectionString = ConfigurationManager.ConnectionStrings["LaMediaCanchaDB"].ConnectionString;
        }

        public ActionResult Index()
        {
            if (Session["UserRol"] == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var devoluciones = new List<DevolucionModels.EncabezadoDevolucion>();

            string query = @"
                SELECT 
                    d.DevolucionId,
                    d.NumeroDocCompra,
                    d.FechaDevolucion,
                    d.Motivo,
                    d.TipoDevolucion,
                    d.MontoTotal,
                    d.Estado
                FROM EncabezadoDevolucion d
                ORDER BY d.FechaDevolucion DESC";

            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand(query, conn))
            {
                conn.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        devoluciones.Add(new DevolucionModels.EncabezadoDevolucion
                        {
                            DevolucionId = (int)reader["DevolucionId"],
                            NumeroDocCompra = reader["NumeroDocCompra"].ToString(),
                            FechaDevolucion = (DateTime)reader["FechaDevolucion"],
                            Motivo = reader["Motivo"].ToString(),
                            TipoDevolucion = reader["TipoDevolucion"].ToString(),
                            MontoTotal = (decimal)reader["MontoTotal"],
                            Estado = reader["Estado"].ToString()
                        });
                    }
                }
            }

            return View(devoluciones);
        }

        public ActionResult Detalle(int id)
        {
            if (Session["UserRol"] == null)
            {
                return RedirectToAction("Login", "Account");
            }

            DevolucionModels.EncabezadoDevolucion devolucion = null;

            string query = @"
                SELECT 
                    d.*,
                    e.CodigoEmpleado,
                    p.Nombres + ' ' + p.Apellidos AS EmpleadoNombre
                FROM EncabezadoDevolucion d
                INNER JOIN Empleado e ON d.EmpleadoId = e.EmpleadoId
                INNER JOIN Persona p ON e.PersonaId = p.PersonaId
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
                        devolucion = new DevolucionModels.EncabezadoDevolucion
                        {
                            DevolucionId = (int)reader["DevolucionId"],
                            CompraId = (int)reader["CompraId"],
                            EmpleadoId = (int)reader["EmpleadoId"],
                            EmpleadoNombre = reader["EmpleadoNombre"].ToString(),
                            NumeroDocCompra = reader["NumeroDocCompra"].ToString(),
                            FechaCompraRef = (DateTime)reader["FechaCompraRef"],
                            TeniaProductosEnOferta = (bool)reader["TeniaProductosEnOferta"],
                            FechaDevolucion = (DateTime)reader["FechaDevolucion"],
                            Motivo = reader["Motivo"].ToString(),
                            TipoDevolucion = reader["TipoDevolucion"].ToString(),
                            MontoTotal = (decimal)reader["MontoTotal"],
                            Estado = reader["Estado"].ToString(),
                            Observaciones = reader["Observaciones"]?.ToString(),
                            Activo = (bool)reader["Activo"],
                            FechaCreacion = (DateTime)reader["FechaCreacion"],
                            FechaModificacion = reader["FechaModificacion"] as DateTime?,
                            Detalles = ObtenerDetallesDevolucion(id)
                        };
                    }
                }
            }

            if (devolucion == null)
            {
                return HttpNotFound();
            }

            return View(devolucion);
        }

        private List<DevolucionModels.DetalleDevolucion> ObtenerDetallesDevolucion(int devolucionId)
        {
            var detalles = new List<DevolucionModels.DetalleDevolucion>();

            string query = @"
                SELECT 
                    dd.DetalleDevolucionId,
                    dd.DevolucionId,
                    dd.ProductoId,
                    dd.Cantidad,
                    dd.PrecioReferencia,
                    dd.Subtotal,
                    dd.MotivoDetalle,
                    dd.EstabaEnOferta,
                    dd.PrecioOfertaRef,
                    p.Codigo AS ProductoCodigo,
                    p.Nombre AS ProductoNombre
                FROM DetalleDevolucion dd
                INNER JOIN Producto p ON dd.ProductoId = p.ProductoId
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
                        detalles.Add(new DevolucionModels.DetalleDevolucion
                        {
                            DetalleDevolucionId = (int)reader["DetalleDevolucionId"],
                            DevolucionId = (int)reader["DevolucionId"],
                            ProductoId = (int)reader["ProductoId"],
                            ProductoCodigo = reader["ProductoCodigo"].ToString(),
                            ProductoNombre = reader["ProductoNombre"].ToString(),
                            Cantidad = (decimal)reader["Cantidad"],
                            PrecioReferencia = (decimal)reader["PrecioReferencia"],
                            Subtotal = (decimal)reader["Subtotal"],
                            MotivoDetalle = reader["MotivoDetalle"]?.ToString(),
                            EstabaEnOferta = (bool)reader["EstabaEnOferta"],
                            PrecioOfertaRef = reader["PrecioOfertaRef"] as decimal?
                        });
                    }
                }
            }

            return detalles;
        }

        public ActionResult Registrar(int? compraId = null, string factura = "")
        {
            if (Session["UserRol"] == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var viewModel = new RegistrarDevolucionViewModel
            {
                CompraId = compraId ?? 0,
                NumeroFacturaBuscado = factura,
                Productos = new List<DevolucionModels.ProductoDevolucion>()
            };

            return View(viewModel);
        }

        [HttpPost]
        public JsonResult Registrar(RegistrarDevolucionViewModel model)
        {
            System.Diagnostics.Debug.WriteLine("=== REGISTRAR DEVOLUCIÓN ===");
            System.Diagnostics.Debug.WriteLine($"CompraId: {model.CompraId}");

            if (Session["UserRol"] == null)
            {
                return Json(new { success = false, message = "Sesión expirada" });
            }

            try
            {
                int empleadoId = ObtenerEmpleadoIdPorUsuario();

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
                        Cantidad = p.CantidadADevolver
                    }).ToList()
                };

                var service = new DevolucionService();
                int devolucionId = service.RegistrarDevolucion(request);

                // Marcar compra como Cerrada
                string updateQuery = "UPDATE EncabezadoCompra SET Estado = 'Cerrada' WHERE CompraId = @CompraId AND Estado = 'Aprobada'";
                using (var conn = new SqlConnection(_connectionString))
                using (var cmd = new SqlCommand(updateQuery, conn))
                {
                    cmd.Parameters.AddWithValue("@CompraId", model.CompraId);
                    conn.Open();
                    cmd.ExecuteNonQuery();
                }

                return Json(new
                {
                    success = true,
                    message = "Devolución registrada exitosamente",
                    devolucionId = devolucionId
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        public JsonResult BuscarPorFactura(string numeroFactura)
        {
            try
            {
                if (string.IsNullOrEmpty(numeroFactura))
                {
                    return Json(new { success = false, message = "Ingrese un número de factura o documento de compra" }, JsonRequestBehavior.AllowGet);
                }

                numeroFactura = numeroFactura.Trim();

                // Buscar por NumeroDocumento (COMP-001) o por NumeroFactura
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

                                // Validar que no esté cerrada
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
                                    compraId = compraId,
                                    numeroDocumento = numeroDocumento,
                                    fechaCompra = fechaCompra.ToString("yyyy-MM-dd"),
                                    proveedorNombre = proveedorNombre,
                                    diasMaximos = diasMaximos,
                                    diasTranscurridos = diasTranscurridos,
                                    dentroDePlazo = dentroDePlazo,
                                    mensajePlazo = dentroDePlazo
                                        ? $"✅ Dentro del plazo ({diasTranscurridos}/{diasMaximos} días)"
                                        : $"❌ Fuera de plazo ({diasTranscurridos}/{diasMaximos} días)"
                                }, JsonRequestBehavior.AllowGet);
                            }
                            else
                            {
                                return Json(new
                                {
                                    success = false,
                                    message = $"No se encontró ninguna compra con el documento: {numeroFactura}"
                                }, JsonRequestBehavior.AllowGet);
                            }
                        }
                    }
                }
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
                var service = new DevolucionService();
                var productos = service.ObtenerProductosDisponiblesParaDevolver(compraId);

                var resultado = productos.Select(p => new
                {
                    p.ProductoId,
                    p.CodigoProducto,
                    p.NombreProducto,
                    p.Presentacion,
                    p.CantidadComprada,
                    p.CantidadYaDevuelta,
                    p.PrecioUnitario,
                    p.EstaEnOferta
                });

                return Json(resultado, JsonRequestBehavior.AllowGet);
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