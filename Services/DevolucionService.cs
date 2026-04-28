using LaMediaCancha.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using static LaMediaCancha.Models.DevolucionModels;

namespace LaMediaCancha.Services
{
    public class DevolucionService : IDevolucionService
    {
        private readonly string _connectionString;

        public DevolucionService()
        {
            _connectionString = ConfigurationManager.ConnectionStrings["LaMediaCanchaDB"].ConnectionString;
        }

        public bool ValidarPlazoDevolucion(int compraId)
        {
            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand("SELECT dbo.fn_ValidarPlazoDevolucion(@CompraId)", conn))
            {
                cmd.Parameters.AddWithValue("@CompraId", compraId);
                conn.Open();
                return (bool)cmd.ExecuteScalar();
            }
        }

        public List<ProductoDevolucion> ObtenerProductosDisponiblesParaDevolver(int compraId)
        {
            var productos = new List<ProductoDevolucion>();

            string query = @"
                SELECT 
                    p.ProductoId,
                    p.Codigo AS CodigoProducto,
                    p.Nombre AS NombreProducto,
                    ISNULL(pr.Nombre, 'Unidad') AS Presentacion,
                    dc.Cantidad AS CantidadComprada,
                    ISNULL(dc.CantidadDevuelta, 0) AS CantidadYaDevuelta,
                    dc.PrecioUnitario,
                    ISNULL(dc.EstabaEnOferta, 0) AS EstaEnOferta,
                    dc.PrecioOferta
                FROM DetalleCompra dc
                INNER JOIN Producto p ON dc.ProductoId = p.ProductoId
                LEFT JOIN Presentacion pr ON p.PresentacionId = pr.PresentacionId
                WHERE dc.CompraId = @CompraId
                  AND dc.Cantidad > ISNULL(dc.CantidadDevuelta, 0)
                  AND ISNULL(dc.EstabaEnOferta, 0) = 0
                ORDER BY p.Nombre";

            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@CompraId", compraId);
                conn.Open();

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        productos.Add(new ProductoDevolucion
                        {
                            ProductoId = (int)reader["ProductoId"],
                            CodigoProducto = reader["CodigoProducto"].ToString(),
                            NombreProducto = reader["NombreProducto"].ToString(),
                            Presentacion = reader["Presentacion"].ToString(),
                            CantidadComprada = (decimal)reader["CantidadComprada"],
                            CantidadYaDevuelta = (decimal)reader["CantidadYaDevuelta"],
                            PrecioUnitario = (decimal)reader["PrecioUnitario"],
                            EstaEnOferta = (bool)reader["EstaEnOferta"],
                            PrecioOferta = reader["PrecioOferta"] as decimal?,
                            CantidadADevolver = 0
                        });
                    }
                }
            }

            return productos;
        }

        public int RegistrarDevolucion(RegistrarDevolucionRequest request)
        {
            int devolucionId = 0;

            using (var conn = new SqlConnection(_connectionString))
            {
                conn.Open();

                var productosTable = new DataTable();
                productosTable.Columns.Add("ProductoId", typeof(int));
                productosTable.Columns.Add("Cantidad", typeof(decimal));

                foreach (var producto in request.Productos.Where(p => p.Cantidad > 0))
                {
                    productosTable.Rows.Add(producto.ProductoId, producto.Cantidad);
                }

                if (productosTable.Rows.Count == 0)
                {
                    throw new Exception("No hay productos para devolver");
                }

                using (var cmd = new SqlCommand("sp_RegistrarDevolucion", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("@CompraId", request.CompraId);
                    cmd.Parameters.AddWithValue("@EmpleadoId", request.EmpleadoId);
                    cmd.Parameters.AddWithValue("@Motivo", request.Motivo);
                    cmd.Parameters.AddWithValue("@TipoDevolucion", request.TipoDevolucion);
                    cmd.Parameters.AddWithValue("@Observaciones", (object)request.Observaciones ?? DBNull.Value);

                    var tvpParam = cmd.Parameters.AddWithValue("@ProductosDevolver", productosTable);
                    tvpParam.SqlDbType = SqlDbType.Structured;
                    tvpParam.TypeName = "dbo.TipoProductoDevolucion";

                    var outputParam = cmd.Parameters.Add("@DevolucionId", SqlDbType.Int);
                    outputParam.Direction = ParameterDirection.Output;

                    cmd.ExecuteNonQuery();
                    devolucionId = (int)outputParam.Value;
                }
            }

            return devolucionId;
        }

        public EncabezadoDevolucion ObtenerDevolucionPorId(int devolucionId)
        {
            EncabezadoDevolucion devolucion = null;

            string query = @"
                SELECT 
                    ed.*,
                    e.CodigoEmpleado,
                    p.Nombres + ' ' + p.Apellidos AS EmpleadoNombre
                FROM EncabezadoDevolucion ed
                INNER JOIN Empleado e ON ed.EmpleadoId = e.EmpleadoId
                INNER JOIN Persona p ON e.PersonaId = p.PersonaId
                WHERE ed.DevolucionId = @DevolucionId";

            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@DevolucionId", devolucionId);
                conn.Open();

                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        devolucion = new EncabezadoDevolucion
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
                            Detalles = ObtenerDetallesDevolucion(devolucionId)
                        };
                    }
                }
            }

            return devolucion;
        }

        public List<DetalleDevolucion> ObtenerDetallesDevolucion(int devolucionId)
        {
            var detalles = new List<DetalleDevolucion>();

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
                        detalles.Add(new DetalleDevolucion
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

        public List<EncabezadoDevolucion> ObtenerDevolucionesPorCompra(int compraId)
        {
            var devoluciones = new List<EncabezadoDevolucion>();

            string query = @"
                SELECT 
                    ed.*,
                    e.CodigoEmpleado,
                    p.Nombres + ' ' + p.Apellidos AS EmpleadoNombre
                FROM EncabezadoDevolucion ed
                INNER JOIN Empleado e ON ed.EmpleadoId = e.EmpleadoId
                INNER JOIN Persona p ON e.PersonaId = p.PersonaId
                WHERE ed.CompraId = @CompraId
                ORDER BY ed.FechaDevolucion DESC";

            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@CompraId", compraId);
                conn.Open();

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        devoluciones.Add(new EncabezadoDevolucion
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
                            FechaModificacion = reader["FechaModificacion"] as DateTime?
                        });
                    }
                }
            }

            return devoluciones;
        }

        public List<EncabezadoDevolucion> ObtenerDevolucionesPendientes()
        {
            var devoluciones = new List<EncabezadoDevolucion>();

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
                WHERE d.Estado = 'Pendiente'
                ORDER BY d.FechaDevolucion DESC";

            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand(query, conn))
            {
                conn.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        devoluciones.Add(new EncabezadoDevolucion
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

            return devoluciones;
        }

        public List<EncabezadoDevolucion> ObtenerTodasDevoluciones(int? pagina = null, int? registros = null)
        {
            var devoluciones = new List<EncabezadoDevolucion>();

            string baseQuery = @"
                SELECT 
                    ed.DevolucionId,
                    ed.CompraId,
                    ed.EmpleadoId,
                    ed.NumeroDocCompra,
                    ed.FechaCompraRef,
                    ed.TeniaProductosEnOferta,
                    ed.FechaDevolucion,
                    ed.Motivo,
                    ed.TipoDevolucion,
                    ed.MontoTotal,
                    ed.Estado,
                    ed.Observaciones,
                    ed.Activo,
                    ed.FechaCreacion,
                    ed.FechaModificacion,
                    e.CodigoEmpleado,
                    p.Nombres + ' ' + p.Apellidos AS EmpleadoNombre,
                    pr.RazonSocial AS ProveedorNombre
                FROM EncabezadoDevolucion ed
                INNER JOIN Empleado e ON ed.EmpleadoId = e.EmpleadoId
                INNER JOIN Persona p ON e.PersonaId = p.PersonaId
                INNER JOIN EncabezadoCompra ec ON ed.CompraId = ec.CompraId
                INNER JOIN Proveedor pr ON ec.ProveedorId = pr.ProveedorId";

            string orderBy = " ORDER BY ed.FechaDevolucion DESC";
            string query = baseQuery + orderBy;

            if (pagina.HasValue && registros.HasValue)
            {
                int offset = (pagina.Value - 1) * registros.Value;
                query = $@"
                    SELECT * FROM (
                        SELECT ROW_NUMBER() OVER (ORDER BY ed.FechaDevolucion DESC) AS RowNum, *
                        FROM ({baseQuery}) AS SubQuery
                    ) AS Paged
                    WHERE RowNum > {offset} AND RowNum <= {offset + registros.Value}";
            }

            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand(query, conn))
            {
                conn.Open();

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        devoluciones.Add(new EncabezadoDevolucion
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
                            FechaModificacion = reader["FechaModificacion"] as DateTime?
                        });
                    }
                }
            }

            return devoluciones;
        }

        public bool CambiarEstadoDevolucion(int devolucionId, string nuevoEstado, string observaciones = null)
        {
            string query = @"
                UPDATE EncabezadoDevolucion 
                SET Estado = @NuevoEstado, 
                    FechaModificacion = GETDATE(),
                    Observaciones = ISNULL(Observaciones, '') + ISNULL(@Observaciones, '')
                WHERE DevolucionId = @DevolucionId";

            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@DevolucionId", devolucionId);
                cmd.Parameters.AddWithValue("@NuevoEstado", nuevoEstado);
                cmd.Parameters.AddWithValue("@Observaciones", string.IsNullOrEmpty(observaciones) ? "" : " - " + observaciones);
                conn.Open();
                return cmd.ExecuteNonQuery() > 0;
            }
        }

        public bool CancelarDevolucion(int devolucionId, string motivoCancelacion)
        {
            var devolucion = ObtenerDevolucionPorId(devolucionId);
            if (devolucion == null) return false;
            if (devolucion.Estado == "Cerrada" || devolucion.Estado == "Cancelada")
                return false;

            if (devolucion.Detalles != null)
            {
                foreach (var detalle in devolucion.Detalles)
                {
                    string updateInventario = @"
                        UPDATE Inventario 
                        SET ExistenciaActual = ExistenciaActual + @Cantidad
                        WHERE ProductoId = @ProductoId";

                    using (var conn = new SqlConnection(_connectionString))
                    using (var cmd = new SqlCommand(updateInventario, conn))
                    {
                        cmd.Parameters.AddWithValue("@Cantidad", detalle.Cantidad);
                        cmd.Parameters.AddWithValue("@ProductoId", detalle.ProductoId);
                        conn.Open();
                        cmd.ExecuteNonQuery();
                    }
                }
            }

            return CambiarEstadoDevolucion(devolucionId, "Cancelada", $"Cancelada: {motivoCancelacion}");
        }
    }
}