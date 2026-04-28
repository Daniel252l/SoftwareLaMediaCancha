using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace LaMediaCancha.Models
{
    public class EmpleadoModels
    {
        public class Empleado
        {
            public int EmpleadoId { get; set; }
            public int PersonaId { get; set; }
            public int? UsuarioId { get; set; }
            public string CodigoEmpleado { get; set; }
            public string Cargo { get; set; }
            public string Departamento { get; set; }
            public string Nombres { get; set; }
            public string Apellidos { get; set; }
            public string Telefono { get; set; }
            public string Correo { get; set; }
            public string Direccion { get; set; }
            public bool Activo { get; set; }
            public DateTime FechaCreacion { get; set; }

            public string NombreCompleto => $"{Nombres} {Apellidos}";
        }

        public class Persona
        {
            public int PersonaId { get; set; }
            public string Nombres { get; set; }
            public string Apellidos { get; set; }
            public string Telefono { get; set; }
            public string Correo { get; set; }
            public string Direccion { get; set; }
            public bool Activo { get; set; }
        }
    }
}