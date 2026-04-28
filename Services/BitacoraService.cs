using System;
using System.Data.SqlClient;
using System.Configuration;

namespace LaMediaCancha.Services
{
    public class BitacoraService
    {
        private readonly string _connectionString;
        private readonly string _connectionStringSeguridad;

        public BitacoraService()
        {
            _connectionString = ConfigurationManager.ConnectionStrings["LaMediaCanchaDB"].ConnectionString;
            _connectionStringSeguridad = ConfigurationManager.ConnectionStrings["LaMediaCanchaSeguridad"].ConnectionString;
        }

        public void Registrar(int usuarioId, string accion, string tabla, string detalle)
        {
            try
            {
                string usuarioNombre = GetUsuarioNombre(usuarioId);

                string query = @"
                    INSERT INTO Bitacora (UsuarioId, UsuarioNombre, Accion, Tabla, Detalle, Fecha)
                    VALUES (@UsuarioId, @UsuarioNombre, @Accion, @Tabla, @Detalle, GETDATE())";

                using (var conn = new SqlConnection(_connectionString))
                using (var cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@UsuarioId", usuarioId);
                    cmd.Parameters.AddWithValue("@UsuarioNombre", usuarioNombre);
                    cmd.Parameters.AddWithValue("@Accion", accion);
                    cmd.Parameters.AddWithValue("@Tabla", tabla);
                    cmd.Parameters.AddWithValue("@Detalle", detalle ?? "");
                    conn.Open();
                    cmd.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error en bitácora: {ex.Message}");
            }
        }

        private string GetUsuarioNombre(int usuarioId)
        {
            string query = "SELECT NombreCompleto FROM Usuarios WHERE UsuarioId = @UsuarioId";
            using (var conn = new SqlConnection(_connectionStringSeguridad))
            using (var cmd = new SqlCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@UsuarioId", usuarioId);
                conn.Open();
                var result = cmd.ExecuteScalar();
                if (result != null)
                {
                    return result.ToString();
                }
            }
            return "Usuario Desconocido";
        }

        // ================================================
        // PROVEEDORES
        // ================================================
        public void RegistrarProveedorCreacion(int usuarioId, string nombre, string nit)
        {
            Registrar(usuarioId, "CREAR", "Proveedor", $"Se creó el proveedor: {nombre} (NIT: {nit})");
        }

        public void RegistrarProveedorEdicion(int usuarioId, string nombre)
        {
            Registrar(usuarioId, "EDITAR", "Proveedor", $"Se editó el proveedor: {nombre}");
        }

        public void RegistrarProveedorInactivacion(int usuarioId, string nombre)
        {
            Registrar(usuarioId, "INACTIVAR", "Proveedor", $"Se inactivó el proveedor: {nombre}");
        }

        // ================================================
        // PRODUCTOS
        // ================================================
        public void RegistrarProductoCreacion(int usuarioId, string nombre, string codigo)
        {
            Registrar(usuarioId, "CREAR", "Producto", $"Se creó el producto: {nombre} (Código: {codigo})");
        }

        public void RegistrarProductoEdicion(int usuarioId, string nombre)
        {
            Registrar(usuarioId, "EDITAR", "Producto", $"Se editó el producto: {nombre}");
        }

        public void RegistrarProductoEliminacion(int usuarioId, string nombre)
        {
            Registrar(usuarioId, "ELIMINAR", "Producto", $"Se eliminó el producto: {nombre}");
        }

        // ================================================
        // COMPRAS
        // ================================================
        public void RegistrarCompraCreacion(int usuarioId, string numeroDocumento, decimal total, int cantidadProductos)
        {
            Registrar(usuarioId, "CREAR", "Compra", $"Se creó la compra #{numeroDocumento} con {cantidadProductos} productos. Total: Q {total:N2}");
        }

        // ================================================
        // DEVOLUCIONES
        // ================================================
        public void RegistrarDevolucionCreacion(int usuarioId, string numeroCompra, int cantidadProductos, decimal monto)
        {
            Registrar(usuarioId, "CREAR", "Devolución", $"Se registró devolución de la compra #{numeroCompra}. {cantidadProductos} productos devueltos. Monto: Q {monto:N2}");
        }

        // ================================================
        // USUARIOS
        // ================================================
        public void RegistrarUsuarioCreacion(int usuarioId, string nombre, string email, string rol)
        {
            Registrar(usuarioId, "CREAR", "Usuario", $"Se creó el usuario: {nombre} ({email}) con rol: {rol}");
        }

        public void RegistrarUsuarioEdicion(int usuarioId, string nombre, string cambios)
        {
            Registrar(usuarioId, "EDITAR", "Usuario", $"Se editó el usuario: {nombre}. Cambios: {cambios}");
        }

        public void RegistrarUsuarioInactivacion(int usuarioId, string nombre)
        {
            Registrar(usuarioId, "INACTIVAR", "Usuario", $"Se inactivó el usuario: {nombre}");
        }
    }
}