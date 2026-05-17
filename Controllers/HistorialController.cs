using LaMediaCancha.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;
using System.Web.Mvc;

namespace LaMediaCancha.Controllers
{
    public class HistorialController : Controller
    {
        private readonly string _connectionString;

        public HistorialController()
        {
            _connectionString = ConfigurationManager.ConnectionStrings["LaMediaCanchaDB"].ConnectionString;
        }

        // GET: Historial/Index
        public ActionResult Index(DateTime? fechaInicio, DateTime? fechaFin, string tipoVenta = "", string estado = "")
        {
            if (Session["UserRol"] == null)
                return RedirectToAction("Login", "Account");

            if (!fechaInicio.HasValue)
                fechaInicio = DateTime.Now.Date;
            if (!fechaFin.HasValue)
                fechaFin = DateTime.Now.Date.AddDays(1).AddSeconds(-1);

            ViewBag.FechaInicio = fechaInicio.Value.ToString("yyyy-MM-dd");
            ViewBag.FechaFin = fechaFin.Value.ToString("yyyy-MM-dd");
            ViewBag.TipoVentaSeleccionado = tipoVenta;
            ViewBag.EstadoSeleccionado = estado;

            var ventas = new List<VentaModels.HistorialVenta>();

            string query = @"
                SELECT 
                    o.OrdenId,
                    o.NumeroOrden,
                    o.MesaId,
                    m.NumeroMesa,
                    o.ClienteNombre,
                    o.FechaApertura,
                    o.FechaCierre,
                    o.Subtotal,
                    o.Impuesto,
                    o.Total,
                    o.Estado,
                    o.UsuarioNombre,
                    CASE WHEN o.MesaId IS NOT NULL AND o.MesaId > 0 THEN 'Mesa' ELSE 'Mostrador' END AS TipoVenta
                FROM Orden o
                LEFT JOIN Mesa m ON o.MesaId = m.MesaId
                WHERE o.FechaApertura BETWEEN @FechaInicio AND @FechaFin
                  AND (@TipoVenta = '' OR 
                       (@TipoVenta = 'Mesa' AND o.MesaId IS NOT NULL AND o.MesaId > 0) OR
                       (@TipoVenta = 'Mostrador' AND (o.MesaId IS NULL OR o.MesaId = 0)))
                  AND (@Estado = '' OR o.Estado = @Estado)
                ORDER BY o.FechaApertura DESC";

            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@FechaInicio", fechaInicio.Value);
                cmd.Parameters.AddWithValue("@FechaFin", fechaFin.Value);
                cmd.Parameters.AddWithValue("@TipoVenta", tipoVenta);
                cmd.Parameters.AddWithValue("@Estado", estado);
                conn.Open();

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        string estadoOrden = reader["Estado"].ToString();
                        DateTime? fechaCierre = reader["FechaCierre"] as DateTime?;

                        if (fechaCierre.HasValue && estadoOrden == "Abierta")
                        {
                            estadoOrden = "Cerrada";
                        }

                        ventas.Add(new VentaModels.HistorialVenta
                        {
                            OrdenId = (int)reader["OrdenId"],
                            NumeroOrden = reader["NumeroOrden"].ToString(),
                            MesaId = reader["MesaId"] as int?,
                            NumeroMesa = reader["NumeroMesa"] as int?,
                            ClienteNombre = reader["ClienteNombre"]?.ToString() ?? "Cliente",
                            FechaApertura = (DateTime)reader["FechaApertura"],
                            FechaCierre = fechaCierre,
                            Subtotal = (decimal)reader["Subtotal"],
                            Impuesto = (decimal)reader["Impuesto"],
                            Total = (decimal)reader["Total"],
                            Estado = estadoOrden,
                            TipoVenta = reader["TipoVenta"].ToString(),
                            UsuarioNombre = reader["UsuarioNombre"]?.ToString()
                        });
                    }
                }
            }

            return View(ventas);
        }

        // GET: Historial/Detalle/5
        public ActionResult Detalle(int id)
        {
            if (Session["UserRol"] == null)
                return RedirectToAction("Login", "Account");

            VentaModels.HistorialVenta venta = null;

            string query = @"
                SELECT 
                    o.OrdenId,
                    o.NumeroOrden,
                    o.MesaId,
                    m.NumeroMesa,
                    o.ClienteNombre,
                    o.FechaApertura,
                    o.FechaCierre,
                    o.Subtotal,
                    o.Impuesto,
                    o.Total,
                    o.Estado,
                    o.UsuarioNombre,
                    CASE WHEN o.MesaId IS NOT NULL AND o.MesaId > 0 THEN 'Mesa' ELSE 'Mostrador' END AS TipoVenta
                FROM Orden o
                LEFT JOIN Mesa m ON o.MesaId = m.MesaId
                WHERE o.OrdenId = @OrdenId";

            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@OrdenId", id);
                conn.Open();

                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        string estadoOrden = reader["Estado"].ToString();
                        DateTime? fechaCierre = reader["FechaCierre"] as DateTime?;

                        if (fechaCierre.HasValue && estadoOrden == "Abierta")
                        {
                            estadoOrden = "Cerrada";
                        }

                        venta = new VentaModels.HistorialVenta
                        {
                            OrdenId = (int)reader["OrdenId"],
                            NumeroOrden = reader["NumeroOrden"].ToString(),
                            MesaId = reader["MesaId"] as int?,
                            NumeroMesa = reader["NumeroMesa"] as int?,
                            ClienteNombre = reader["ClienteNombre"]?.ToString() ?? "Cliente",
                            FechaApertura = (DateTime)reader["FechaApertura"],
                            FechaCierre = fechaCierre,
                            Subtotal = (decimal)reader["Subtotal"],
                            Impuesto = (decimal)reader["Impuesto"],
                            Total = (decimal)reader["Total"],
                            Estado = estadoOrden,
                            TipoVenta = reader["TipoVenta"].ToString(),
                            UsuarioNombre = reader["UsuarioNombre"]?.ToString(),
                            Detalles = ObtenerDetallesVenta(id)
                        };
                    }
                }
            }

            if (venta == null)
                return HttpNotFound();

            return View(venta);
        }

        private List<VentaModels.HistorialDetalleVenta> ObtenerDetallesVenta(int ordenId)
        {
            var detalles = new List<VentaModels.HistorialDetalleVenta>();

            string query = @"
        -- Detalles de orden normal
        SELECT 
            d.DetalleOrdenId AS Id,
            d.ProductoId,
            d.ProductoNombre,
            d.Cantidad,
            d.PrecioUnitario,
            d.Subtotal,
            d.Nota,
            d.EsDeCombo,
            d.ComboId,
            c.Nombre AS ComboNombre,
            'Normal' AS Tipo,
            p.Codigo AS ProductoCodigo
        FROM DetalleOrden d
        LEFT JOIN Combo c ON d.ComboId = c.ComboId
        LEFT JOIN Producto p ON d.ProductoId = p.ProductoId
        WHERE d.OrdenId = @OrdenId
        
        UNION ALL
        
        -- Detalles de cuentas separadas
        SELECT 
            dop.DetalleOrdenPersonaId AS Id,
            dop.ProductoId,
            dop.ProductoNombre,
            dop.Cantidad,
            dop.PrecioUnitario,
            dop.Subtotal,
            dop.Nota,
            dop.EsDeCombo,
            dop.ComboId,
            c.Nombre AS ComboNombre,
            'Separada' AS Tipo,
            p.Codigo AS ProductoCodigo
        FROM DetalleOrdenPersona dop
        LEFT JOIN Combo c ON dop.ComboId = c.ComboId
        LEFT JOIN Producto p ON dop.ProductoId = p.ProductoId
        INNER JOIN OrdenPersona op ON dop.OrdenPersonaId = op.OrdenPersonaId
        WHERE op.OrdenId = @OrdenId
        ORDER BY Id ASC";

            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@OrdenId", ordenId);
                conn.Open();

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        detalles.Add(new VentaModels.HistorialDetalleVenta
                        {
                            DetalleOrdenId = (int)reader["Id"],
                            ProductoId = (int)reader["ProductoId"],
                            ProductoCodigo = reader["ProductoCodigo"]?.ToString() ?? "",
                            ProductoNombre = reader["ProductoNombre"].ToString(),
                            Cantidad = (decimal)reader["Cantidad"],
                            PrecioUnitario = (decimal)reader["PrecioUnitario"],
                            Subtotal = (decimal)reader["Subtotal"],
                            Nota = reader["Nota"]?.ToString(),
                            EsDeCombo = (bool)reader["EsDeCombo"],
                            ComboNombre = reader["ComboNombre"]?.ToString(),
                            TipoDetalle = reader["Tipo"].ToString(),
                            EstabaEnOferta = false,
                            PrecioOferta = null
                        });
                    }
                }
            }

            return detalles;
        }

        // GET: Historial/VerificarDetalles
        [HttpGet]
        public JsonResult VerificarDetalles(int ordenId)
        {
            try
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    conn.Open();

                    // Verificar detalles en DetalleOrden
                    int detallesNormales = 0;
                    string queryNormal = "SELECT COUNT(*) FROM DetalleOrden WHERE OrdenId = @OrdenId";
                    using (var cmd = new SqlCommand(queryNormal, conn))
                    {
                        cmd.Parameters.AddWithValue("@OrdenId", ordenId);
                        detallesNormales = (int)cmd.ExecuteScalar();
                    }

                    // Verificar detalles en DetalleOrdenPersona
                    int detallesSeparados = 0;
                    string querySeparado = @"
                        SELECT COUNT(*) 
                        FROM DetalleOrdenPersona dop
                        INNER JOIN OrdenPersona op ON dop.OrdenPersonaId = op.OrdenPersonaId
                        WHERE op.OrdenId = @OrdenId";
                    using (var cmd = new SqlCommand(querySeparado, conn))
                    {
                        cmd.Parameters.AddWithValue("@OrdenId", ordenId);
                        detallesSeparados = (int)cmd.ExecuteScalar();
                    }

                    return Json(new
                    {
                        success = true,
                        ordenId = ordenId,
                        detallesNormales = detallesNormales,
                        detallesSeparados = detallesSeparados,
                        total = detallesNormales + detallesSeparados
                    }, JsonRequestBehavior.AllowGet);
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        // POST: Historial/DevolverProducto
        [HttpPost]
        public JsonResult DevolverProducto(int detalleOrdenId, decimal cantidad, string motivo, string tipoDetalle = "Normal")
        {
            if (Session["UserRol"] == null)
                return Json(new { success = false, message = "Sesión expirada" });

            try
            {
                int usuarioId = (int)Session["UserId"];
                string usuarioNombre = Session["UserNombre"]?.ToString() ?? "Usuario";

                int productoId = 0;
                int ordenId = 0;
                decimal cantidadOriginal = 0;
                decimal precioUnitario = 0;
                decimal subtotal = 0;

                if (tipoDetalle == "Normal")
                {
                    string getDetalle = @"
                        SELECT d.ProductoId, d.Cantidad, d.PrecioUnitario, d.Subtotal, d.OrdenId
                        FROM DetalleOrden d
                        WHERE d.DetalleOrdenId = @DetalleOrdenId";

                    using (var conn = new SqlConnection(_connectionString))
                    using (var cmd = new SqlCommand(getDetalle, conn))
                    {
                        cmd.Parameters.AddWithValue("@DetalleOrdenId", detalleOrdenId);
                        conn.Open();
                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                productoId = (int)reader["ProductoId"];
                                ordenId = (int)reader["OrdenId"];
                                cantidadOriginal = (decimal)reader["Cantidad"];
                                precioUnitario = (decimal)reader["PrecioUnitario"];
                                subtotal = (decimal)reader["Subtotal"];
                            }
                            else
                            {
                                return Json(new { success = false, message = "Detalle de venta no encontrado" });
                            }
                        }
                    }
                }
                else // Separada
                {
                    string getDetalle = @"
                        SELECT dop.ProductoId, dop.Cantidad, dop.PrecioUnitario, dop.Subtotal, op.OrdenId
                        FROM DetalleOrdenPersona dop
                        INNER JOIN OrdenPersona op ON dop.OrdenPersonaId = op.OrdenPersonaId
                        WHERE dop.DetalleOrdenPersonaId = @DetalleOrdenId";

                    using (var conn = new SqlConnection(_connectionString))
                    using (var cmd = new SqlCommand(getDetalle, conn))
                    {
                        cmd.Parameters.AddWithValue("@DetalleOrdenId", detalleOrdenId);
                        conn.Open();
                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                productoId = (int)reader["ProductoId"];
                                ordenId = (int)reader["OrdenId"];
                                cantidadOriginal = (decimal)reader["Cantidad"];
                                precioUnitario = (decimal)reader["PrecioUnitario"];
                                subtotal = (decimal)reader["Subtotal"];
                            }
                            else
                            {
                                return Json(new { success = false, message = "Detalle de venta no encontrado" });
                            }
                        }
                    }
                }

                if (cantidad > cantidadOriginal)
                {
                    return Json(new { success = false, message = "La cantidad a devolver no puede ser mayor a la cantidad vendida" });
                }

                decimal totalDevolver = cantidad * precioUnitario;
                int notaCreditoId = 0;
                string numeroNotaCredito = $"NC-{DateTime.Now:yyyyMMddHHmmss}";

                using (var conn = new SqlConnection(_connectionString))
                {
                    conn.Open();
                    using (var transaction = conn.BeginTransaction())
                    {
                        try
                        {
                            string insertNotaCredito = @"
                                INSERT INTO NotaCredito (FacturaOriginalId, NumeroNotaCredito, FechaEmision, MontoTotal, Motivo, Estado, FechaCreacion, UsuarioId, UsuarioNombre)
                                VALUES (@FacturaOriginalId, @NumeroNotaCredito, GETDATE(), @MontoTotal, @Motivo, 'Vigente', GETDATE(), @UsuarioId, @UsuarioNombre);
                                SELECT SCOPE_IDENTITY();";

                            using (var cmd = new SqlCommand(insertNotaCredito, conn, transaction))
                            {
                                cmd.Parameters.AddWithValue("@FacturaOriginalId", ordenId);
                                cmd.Parameters.AddWithValue("@NumeroNotaCredito", numeroNotaCredito);
                                cmd.Parameters.AddWithValue("@MontoTotal", totalDevolver);
                                cmd.Parameters.AddWithValue("@Motivo", motivo);
                                cmd.Parameters.AddWithValue("@UsuarioId", usuarioId);
                                cmd.Parameters.AddWithValue("@UsuarioNombre", usuarioNombre);
                                notaCreditoId = Convert.ToInt32(cmd.ExecuteScalar());
                            }

                            transaction.Commit();
                        }
                        catch (Exception ex)
                        {
                            transaction.Rollback();
                            throw new Exception($"Error en transacción: {ex.Message}");
                        }
                    }
                }

                return Json(new
                {
                    success = true,
                    message = $"Devolución registrada exitosamente. Nota de Crédito: {numeroNotaCredito}",
                    notaCreditoId = notaCreditoId,
                    numeroNotaCredito = numeroNotaCredito
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // POST: Historial/CerrarOrden
        [HttpPost]
        public JsonResult CerrarOrden(int ordenId)
        {
            if (Session["UserRol"] == null)
                return Json(new { success = false, message = "Sesión expirada" });

            try
            {
                string query = @"
                    UPDATE Orden 
                    SET Estado = 'Cerrada', FechaCierre = GETDATE() 
                    WHERE OrdenId = @OrdenId AND Estado = 'Abierta'";

                using (var conn = new SqlConnection(_connectionString))
                using (var cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@OrdenId", ordenId);
                    conn.Open();
                    int rowsAffected = cmd.ExecuteNonQuery();

                    if (rowsAffected > 0)
                    {
                        return Json(new { success = true, message = "Orden cerrada exitosamente" });
                    }
                    else
                    {
                        return Json(new { success = false, message = "La orden ya está cerrada o no existe" });
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