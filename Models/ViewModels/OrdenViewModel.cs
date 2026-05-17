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
        public string ClienteNombre { get; set; }
        public DateTime Fecha { get; set; }
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

    // ============================================
    // CLASE FALTANTE - Agregar esta!
    // ============================================
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


}