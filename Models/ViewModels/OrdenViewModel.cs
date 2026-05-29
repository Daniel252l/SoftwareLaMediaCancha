using System;
using System.Collections.Generic;

namespace LaMediaCancha.Models.ViewModels
{
    public class OrdenProductoViewModel
    {
        public int ProductoId { get; set; }
        public string NombreProducto { get; set; }
        public decimal Cantidad { get; set; }
        public decimal PrecioUnitario { get; set; }
        public string Nota { get; set; }
        public bool EsOferta { get; set; }
    }

    public class OrdenDetalleViewModel
    {
        public string ProductoNombre { get; set; }
        public decimal Cantidad { get; set; }
        public decimal PrecioUnitario { get; set; }
        public decimal Subtotal { get; set; }
        public string Nota { get; set; }
    }

    public class OrdenCuentaViewModel
    {
        public int OrdenId { get; set; }
        public string NumeroOrden { get; set; }
        public int MesaId { get; set; }
        public string ClienteNombre { get; set; }
        public DateTime FechaApertura { get; set; }
        public decimal Subtotal { get; set; }
        public decimal Impuesto { get; set; }
        public decimal Total { get; set; }
        public string Observaciones { get; set; }
        public List<OrdenDetalleViewModel> Detalles { get; set; }
        public List<OrdenModels.OrdenPersona> CuentasSeparadas { get; set; }
    }

    public class OrdenTomaPedidoAvanzadaViewModel
    {
        public int MesaId { get; set; }
        public int NumeroMesa { get; set; }
        public string Ubicacion { get; set; }
        public string ClienteNombre { get; set; }
        public string ClienteTelefono { get; set; }
        public string Observaciones { get; set; }

        // ========== PROPIEDADES PARA SILLAS ==========
        public int? NumeroSilla { get; set; }
        public int? SillaId { get; set; }
        public int? OrdenPersonaId { get; set; }

        public List<OrdenProductoViewModel> Productos { get; set; }
        public List<ComboSeleccionadoViewModel> Combos { get; set; }
        public List<OfertaAplicadaViewModel> OfertasAplicadas { get; set; }
        public bool UsarCuentasSeparadas { get; set; }
        public List<CuentaPersonaViewModel> CuentasPorPersona { get; set; }
    }

    public class ComboSeleccionadoViewModel
    {
        public int ComboId { get; set; }
        public string NombreCombo { get; set; }
        public int Cantidad { get; set; }
        public decimal PrecioCombo { get; set; }
        public bool VenderPorSeparado { get; set; }
        public List<OrdenProductoViewModel> ProductosSeparados { get; set; }
    }

    public class OfertaAplicadaViewModel
    {
        public int OfertaId { get; set; }
        public string NombreOferta { get; set; }
        public int ProductoId { get; set; }
        public decimal DescuentoAplicado { get; set; }
    }

    public class CuentaPersonaViewModel
    {
        public string NombreCliente { get; set; }
        public List<OrdenProductoViewModel> Productos { get; set; }
        public List<ComboSeleccionadoViewModel> Combos { get; set; }
        public decimal Subtotal { get; set; }
        public decimal Impuesto { get; set; }
        public decimal Total { get; set; }
    }

    public class TicketViewModel
    {
        public int OrdenId { get; set; }
        public string NumeroOrden { get; set; }
        public int MesaId { get; set; }
        public int NumeroMesa { get; set; }
        public int NumeroSilla { get; set; }
        public string ClienteNombre { get; set; }
        public string FechaStr { get; set; }  // <- Nueva propiedad para la fecha formateada
        public DateTime Fecha { get; set; }   // <- Mantener por compatibilidad
        public List<TicketDetalleViewModel> Detalles { get; set; }
        public decimal Subtotal { get; set; }
        public decimal Impuesto { get; set; }
        public decimal Total { get; set; }
        public bool EsCuentaSeparada { get; set; }
        public string NombrePersona { get; set; }
    }

    public class TicketDetalleViewModel
    {
        public string ProductoNombre { get; set; }
        public int Cantidad { get; set; }
        public decimal PrecioUnitario { get; set; }
        public decimal Subtotal { get; set; }
        public string Nota { get; set; }
        public bool EsDeCombo { get; set; }
        public string ComboNombre { get; set; }
    }

    public class OrdenViewModel
    {
        public int MesaId { get; set; }
        public int NumeroMesa { get; set; }
        public string Ubicacion { get; set; }
        public string ClienteNombre { get; set; }
        public string ClienteTelefono { get; set; }
        public string Observaciones { get; set; }
        public List<OrdenProductoViewModel> Productos { get; set; }
    }

    // ========== VIEWMODELS PARA SILLAS Y MESAS ==========

    public class OrdenCuentaMesaViewModel
    {
        public int OrdenId { get; set; }
        public string NumeroOrden { get; set; }
        public int MesaId { get; set; }
        public int NumeroMesa { get; set; }
        public DateTime FechaApertura { get; set; }
        public decimal Subtotal { get; set; }
        public decimal Impuesto { get; set; }
        public decimal Total { get; set; }
        public List<CuentaSillaViewModel> CuentasPorSilla { get; set; }
    }

    public class CuentaSillaViewModel
    {
        public int SillaId { get; set; }
        public int NumeroSilla { get; set; }
        public string NombreCliente { get; set; }
        public decimal Subtotal { get; set; }
        public decimal Impuesto { get; set; }
        public decimal Total { get; set; }
        public int OrdenPersonaId { get; set; }  // ← AGREGAR ESTA LÍNEA
        public List<OrdenModels.DetalleOrdenPersona> Detalles { get; set; }
    }

    // ==================== HISTORIAL DE ÓRDENES ====================

    public class HistorialOrdenViewModel
    {
        public int OrdenId { get; set; }
        public string NumeroOrden { get; set; }
        public int NumeroMesa { get; set; }
        public string UbicacionMesa { get; set; }
        public string ClienteNombre { get; set; }
        public DateTime FechaApertura { get; set; }
        public DateTime? FechaCierre { get; set; }
        public decimal Subtotal { get; set; }
        public decimal Impuesto { get; set; }
        public decimal Total { get; set; }
        public string Estado { get; set; }
        public string UsuarioNombre { get; set; }
        public int CantidadProductos { get; set; }
        public List<HistorialDetalleViewModel> Detalles { get; set; }
        public List<HistorialSillaViewModel> Sillas { get; set; }
    }

    public class HistorialDetalleViewModel
    {
        public string ProductoNombre { get; set; }
        public decimal Cantidad { get; set; }
        public decimal PrecioUnitario { get; set; }
        public decimal Subtotal { get; set; }
        public string Nota { get; set; }
        public bool EsDeCombo { get; set; }
        public string ComboNombre { get; set; }
    }

    public class HistorialSillaViewModel
    {
        public int NumeroSilla { get; set; }
        public string NombreCliente { get; set; }
        public decimal Total { get; set; }
        public bool Pagado { get; set; }
    }

    public class HistorialFiltroViewModel
    {
        public DateTime? FechaInicio { get; set; }
        public DateTime? FechaFin { get; set; }
        public int? NumeroMesa { get; set; }
        public string Estado { get; set; }
        public string Buscar { get; set; }
    }
}