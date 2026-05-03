using LaMediaCancha.Models;
using LaMediaCancha.Models.ViewModels;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;
using System.Web.Mvc;

namespace LaMediaCancha.Controllers
{
    public class OrdenController : Controller
    {
        private readonly string _connectionString;

        public OrdenController()
        {
            _connectionString = ConfigurationManager.ConnectionStrings["LaMediaCanchaDB"].ConnectionString;
        }

        // GET: Orden/Index
        public ActionResult Index()
        {
            if (Session["UserRol"] == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var mesas = new List<OrdenModels.Mesa>();

            string query = @"
                SELECT MesaId, NumeroMesa, Capacidad, Ubicacion, Estado 
                FROM Mesa 
                WHERE Activo = 1 
                ORDER BY NumeroMesa";

            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand(query, conn))
            {
                conn.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var mesa = new OrdenModels.Mesa
                        {
                            MesaId = (int)reader["MesaId"],
                            NumeroMesa = (int)reader["NumeroMesa"],
                            Capacidad = (int)reader["Capacidad"],
                            Ubicacion = reader["Ubicacion"]?.ToString() ?? "",
                            Estado = reader["Estado"]?.ToString() ?? "Disponible"
                        };
                        mesas.Add(mesa);
                    }
                }
            }

            return View(mesas);
        }

        // GET: Orden/Ordenar/5
        public ActionResult Ordenar(int mesaId)
        {
            if (Session["UserRol"] == null)
            {
                return RedirectToAction("Login", "Account");
            }

            string query = "SELECT MesaId, NumeroMesa, Ubicacion FROM Mesa WHERE MesaId = @MesaId";
            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@MesaId", mesaId);
                conn.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        var model = new OrdenTomaPedidoViewModel
                        {
                            MesaId = (int)reader["MesaId"],
                            NumeroMesa = (int)reader["NumeroMesa"],
                            Ubicacion = reader["Ubicacion"]?.ToString() ?? ""
                        };
                        return View(model);
                    }
                }
            }
            return RedirectToAction("Index");
        }

        // GET: Orden/Cuenta/5
        public ActionResult Cuenta(int mesaId)
        {
            if (Session["UserRol"] == null)
            {
                return RedirectToAction("Login", "Account");
            }

            string query = @"
                SELECT TOP 1 OrdenId, NumeroOrden, ClienteNombre, FechaApertura, Subtotal, Impuesto, Total, Observaciones
                FROM Orden 
                WHERE MesaId = @MesaId AND Estado = 'Abierta'
                ORDER BY OrdenId DESC";

            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@MesaId", mesaId);
                conn.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        var model = new OrdenCuentaViewModel
                        {
                            OrdenId = (int)reader["OrdenId"],
                            NumeroOrden = reader["NumeroOrden"]?.ToString() ?? "",
                            MesaId = mesaId,
                            ClienteNombre = reader["ClienteNombre"]?.ToString(),
                            FechaApertura = (DateTime)reader["FechaApertura"],
                            Subtotal = (decimal)reader["Subtotal"],
                            Impuesto = (decimal)reader["Impuesto"],
                            Total = (decimal)reader["Total"],
                            Observaciones = reader["Observaciones"]?.ToString(),
                            Detalles = ObtenerDetallesOrden((int)reader["OrdenId"])
                        };
                        return View(model);
                    }
                }
            }

            TempData["Error"] = "No hay una orden activa para esta mesa";
            return RedirectToAction("Index");
        }

        // GET: Orden/GetProductos
        public ActionResult GetProductos()
        {
            var productos = new List<object>();
            string query = @"
                SELECT ProductoId, Codigo, Nombre AS NombreProducto, PrecioVenta AS PrecioUnitario 
                FROM Producto 
                WHERE Activo = 1 
                ORDER BY Nombre";

            try
            {
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
                                ProductoId = (int)reader["ProductoId"],
                                Codigo = reader["Codigo"].ToString(),
                                NombreProducto = reader["NombreProducto"].ToString(),
                                PrecioUnitario = (decimal)reader["PrecioUnitario"]
                            });
                        }
                    }
                }
                return Json(productos, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        // POST: Orden/GuardarOrden
        [HttpPost]
        public JsonResult GuardarOrden(OrdenTomaPedidoViewModel model)
        {
            if (Session["UserRol"] == null)
            {
                return Json(new { success = false, message = "Sesión expirada" });
            }

            try
            {
                int usuarioId = (int)Session["UserId"];
                string usuarioNombre = Session["UserNombre"]?.ToString() ?? "Usuario";
                string numeroOrden = $"ORD-{DateTime.Now:yyyyMMddHHmmss}";
                decimal subtotal = 0, impuesto = 0, total = 0;

                if (model.Productos == null || model.Productos.Count == 0)
                {
                    return Json(new { success = false, message = "Debe agregar al menos un producto" });
                }

                foreach (var p in model.Productos)
                {
                    subtotal += p.Cantidad * p.PrecioUnitario;
                }
                impuesto = subtotal * 0.12m;
                total = subtotal + impuesto;

                using (var conn = new SqlConnection(_connectionString))
                {
                    conn.Open();
                    using (var transaction = conn.BeginTransaction())
                    {
                        int ordenId = 0;

                        string insertOrden = @"
                            INSERT INTO Orden (NumeroOrden, MesaId, ClienteNombre, ClienteTelefono, FechaApertura, Subtotal, Impuesto, Total, Estado, Observaciones, UsuarioId, UsuarioNombre)
                            VALUES (@NumeroOrden, @MesaId, @ClienteNombre, @ClienteTelefono, GETDATE(), @Subtotal, @Impuesto, @Total, 'Abierta', @Observaciones, @UsuarioId, @UsuarioNombre);
                            SELECT SCOPE_IDENTITY();";

                        using (var cmd = new SqlCommand(insertOrden, conn, transaction))
                        {
                            cmd.Parameters.AddWithValue("@NumeroOrden", numeroOrden);
                            cmd.Parameters.AddWithValue("@MesaId", model.MesaId);
                            cmd.Parameters.AddWithValue("@ClienteNombre", string.IsNullOrEmpty(model.ClienteNombre) ? DBNull.Value : (object)model.ClienteNombre);
                            cmd.Parameters.AddWithValue("@ClienteTelefono", string.IsNullOrEmpty(model.ClienteTelefono) ? DBNull.Value : (object)model.ClienteTelefono);
                            cmd.Parameters.AddWithValue("@Subtotal", subtotal);
                            cmd.Parameters.AddWithValue("@Impuesto", impuesto);
                            cmd.Parameters.AddWithValue("@Total", total);
                            cmd.Parameters.AddWithValue("@Observaciones", string.IsNullOrEmpty(model.Observaciones) ? DBNull.Value : (object)model.Observaciones);
                            cmd.Parameters.AddWithValue("@UsuarioId", usuarioId);
                            cmd.Parameters.AddWithValue("@UsuarioNombre", usuarioNombre);
                            ordenId = Convert.ToInt32(cmd.ExecuteScalar());
                        }

                        foreach (var p in model.Productos)
                        {
                            string insertDetalle = @"
                                INSERT INTO DetalleOrden (OrdenId, ProductoId, ProductoCodigo, ProductoNombre, Cantidad, PrecioUnitario, Subtotal, Nota)
                                SELECT @OrdenId, @ProductoId, Codigo, Nombre, @Cantidad, @PrecioUnitario, @Cantidad * @PrecioUnitario, @Nota
                                FROM Producto WHERE ProductoId = @ProductoId";

                            using (var cmd = new SqlCommand(insertDetalle, conn, transaction))
                            {
                                cmd.Parameters.AddWithValue("@OrdenId", ordenId);
                                cmd.Parameters.AddWithValue("@ProductoId", p.ProductoId);
                                cmd.Parameters.AddWithValue("@Cantidad", p.Cantidad);
                                cmd.Parameters.AddWithValue("@PrecioUnitario", p.PrecioUnitario);
                                cmd.Parameters.AddWithValue("@Nota", string.IsNullOrEmpty(p.Nota) ? DBNull.Value : (object)p.Nota);
                                cmd.ExecuteNonQuery();
                            }
                        }

                        string updateMesa = "UPDATE Mesa SET Estado = 'Ocupada' WHERE MesaId = @MesaId";
                        using (var cmd = new SqlCommand(updateMesa, conn, transaction))
                        {
                            cmd.Parameters.AddWithValue("@MesaId", model.MesaId);
                            cmd.ExecuteNonQuery();
                        }

                        transaction.Commit();
                        return Json(new { success = true, message = "Pedido guardado correctamente" });
                    }
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // POST: Orden/CerrarOrden
        [HttpPost]
        public JsonResult CerrarOrden(int ordenId)
        {
            if (Session["UserRol"] == null)
            {
                return Json(new { success = false, message = "Sesión expirada" });
            }

            try
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    conn.Open();
                    using (var transaction = conn.BeginTransaction())
                    {
                        int mesaId = 0;
                        string getMesaQuery = "SELECT MesaId FROM Orden WHERE OrdenId = @OrdenId";
                        using (var cmd = new SqlCommand(getMesaQuery, conn, transaction))
                        {
                            cmd.Parameters.AddWithValue("@OrdenId", ordenId);
                            var result = cmd.ExecuteScalar();
                            if (result != null)
                            {
                                mesaId = (int)result;
                            }
                        }

                        string updateOrden = @"
                            UPDATE Orden 
                            SET Estado = 'Cerrada', FechaCierre = GETDATE() 
                            WHERE OrdenId = @OrdenId";
                        using (var cmd = new SqlCommand(updateOrden, conn, transaction))
                        {
                            cmd.Parameters.AddWithValue("@OrdenId", ordenId);
                            cmd.ExecuteNonQuery();
                        }

                        string updateMesa = "UPDATE Mesa SET Estado = 'Disponible' WHERE MesaId = @MesaId";
                        using (var cmd = new SqlCommand(updateMesa, conn, transaction))
                        {
                            cmd.Parameters.AddWithValue("@MesaId", mesaId);
                            cmd.ExecuteNonQuery();
                        }

                        transaction.Commit();
                        return Json(new { success = true, message = "Cuenta cerrada exitosamente" });
                    }
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        private List<OrdenDetalleViewModel> ObtenerDetallesOrden(int ordenId)
        {
            var detalles = new List<OrdenDetalleViewModel>();

            string query = @"
                SELECT ProductoNombre, Cantidad, PrecioUnitario, Subtotal, Nota
                FROM DetalleOrden
                WHERE OrdenId = @OrdenId";

            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@OrdenId", ordenId);
                conn.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        detalles.Add(new OrdenDetalleViewModel
                        {
                            ProductoNombre = reader["ProductoNombre"]?.ToString() ?? "",
                            Cantidad = (decimal)reader["Cantidad"],
                            PrecioUnitario = (decimal)reader["PrecioUnitario"],
                            Subtotal = (decimal)reader["Subtotal"],
                            Nota = reader["Nota"]?.ToString()
                        });
                    }
                }
            }

            return detalles;
        }
    }
}