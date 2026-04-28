using LaMediaCancha.Models;
using System;
using System.Configuration;
using System.Data.SqlClient;
using static LaMediaCancha.Models.EmpleadoModels;

namespace LaMediaCancha.Services
{
    public class EmpleadoService : IEmpleadoService
    {
        private readonly string _connectionString;

        public EmpleadoService()
        {
            _connectionString = ConfigurationManager.ConnectionStrings["LaMediaCanchaDB"].ConnectionString;
        }

        public Empleado ObtenerEmpleadoPorUsuarioId(int usuarioId)
        {
            Empleado empleado = null;

            string query = @"
                SELECT 
                    e.EmpleadoId,
                    e.PersonaId,
                    e.UsuarioId,
                    e.CodigoEmpleado,
                    e.Cargo,
                    e.Departamento,
                    p.Nombres,
                    p.Apellidos,
                    p.Telefono,
                    p.Correo,
                    p.Direccion,
                    e.Activo,
                    e.FechaCreacion
                FROM Empleado e
                INNER JOIN Persona p ON e.PersonaId = p.PersonaId
                WHERE e.UsuarioId = @UsuarioId AND e.Activo = 1";

            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@UsuarioId", usuarioId);
                conn.Open();

                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        empleado = new Empleado
                        {
                            EmpleadoId = (int)reader["EmpleadoId"],
                            PersonaId = (int)reader["PersonaId"],
                            UsuarioId = reader["UsuarioId"] as int?,
                            CodigoEmpleado = reader["CodigoEmpleado"].ToString(),
                            Cargo = reader["Cargo"].ToString(),
                            Departamento = reader["Departamento"]?.ToString(),
                            Nombres = reader["Nombres"].ToString(),
                            Apellidos = reader["Apellidos"].ToString(),
                            Telefono = reader["Telefono"]?.ToString(),
                            Correo = reader["Correo"]?.ToString(),
                            Direccion = reader["Direccion"]?.ToString(),
                            Activo = (bool)reader["Activo"],
                            FechaCreacion = (DateTime)reader["FechaCreacion"]
                        };
                    }
                }
            }

            return empleado;
        }

        public Empleado ObtenerEmpleadoPorId(int empleadoId)
        {
            Empleado empleado = null;

            string query = @"
                SELECT 
                    e.EmpleadoId,
                    e.PersonaId,
                    e.UsuarioId,
                    e.CodigoEmpleado,
                    e.Cargo,
                    e.Departamento,
                    p.Nombres,
                    p.Apellidos,
                    p.Telefono,
                    p.Correo,
                    p.Direccion,
                    e.Activo,
                    e.FechaCreacion
                FROM Empleado e
                INNER JOIN Persona p ON e.PersonaId = p.PersonaId
                WHERE e.EmpleadoId = @EmpleadoId AND e.Activo = 1";

            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@EmpleadoId", empleadoId);
                conn.Open();

                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        empleado = new Empleado
                        {
                            EmpleadoId = (int)reader["EmpleadoId"],
                            PersonaId = (int)reader["PersonaId"],
                            UsuarioId = reader["UsuarioId"] as int?,
                            CodigoEmpleado = reader["CodigoEmpleado"].ToString(),
                            Cargo = reader["Cargo"].ToString(),
                            Departamento = reader["Departamento"]?.ToString(),
                            Nombres = reader["Nombres"].ToString(),
                            Apellidos = reader["Apellidos"].ToString(),
                            Telefono = reader["Telefono"]?.ToString(),
                            Correo = reader["Correo"]?.ToString(),
                            Direccion = reader["Direccion"]?.ToString(),
                            Activo = (bool)reader["Activo"],
                            FechaCreacion = (DateTime)reader["FechaCreacion"]
                        };
                    }
                }
            }

            return empleado;
        }
    }
}