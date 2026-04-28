using System;
using System.ComponentModel.DataAnnotations;

    namespace LaMediaCancha.Models
    {
        public class Proveedor
        {
            public int ProveedorId { get; set; }
            public int PersonaId { get; set; }

            [Required(ErrorMessage = "El NIT es requerido")]
            [StringLength(13, MinimumLength = 13, ErrorMessage = "El NIT debe tener exactamente 13 caracteres")]
            [RegularExpression(@"^[0-9-]+$", ErrorMessage = "El NIT solo puede contener números y guiones")]
            public string Nit { get; set; }

            [Required(ErrorMessage = "La razón social es requerida")]
            [StringLength(150, ErrorMessage = "La razón social no puede exceder 150 caracteres")]
            public string RazonSocial { get; set; }

            [StringLength(100, ErrorMessage = "El contacto no puede exceder 100 caracteres")]
            public string Contacto { get; set; }

            [RegularExpression(@"^\d{8}$", ErrorMessage = "El teléfono debe tener exactamente 8 dígitos")]
            public string Telefono { get; set; }

            [EmailAddress(ErrorMessage = "El correo electrónico no es válido")]
            public string Correo { get; set; }

            [StringLength(250, ErrorMessage = "La dirección no puede exceder 250 caracteres")]
            public string Direccion { get; set; }

            [Required(ErrorMessage = "Los nombres son requeridos")]
            [StringLength(100, ErrorMessage = "Los nombres no pueden exceder 100 caracteres")]
            public string Nombres { get; set; }

            [Required(ErrorMessage = "Los apellidos son requeridos")]
            [StringLength(100, ErrorMessage = "Los apellidos no pueden exceder 100 caracteres")]
            public string Apellidos { get; set; }

            public string NombreCompleto => $"{Nombres} {Apellidos}";
            public bool Activo { get; set; }
            public int? MantenimientoId { get; set; }
            public string PoliticaNombre { get; set; }
            public int DiasMaximosDevolucion { get; set; }
            public string PoliticaDevolucion { get; set; }
        }
    }

    public class ProveedorProducto
    {
        public int ProveedorProductoId { get; set; }
        public int ProveedorId { get; set; }
        public string ProveedorNombre { get; set; }
        public int ProductoId { get; set; }
        public string ProductoNombre { get; set; }
        public string ProductoCodigo { get; set; }
        public decimal PrecioProveedor { get; set; }
        public bool Activo { get; set; }
    }
