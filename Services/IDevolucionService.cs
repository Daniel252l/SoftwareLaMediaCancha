using LaMediaCancha.Models;
using System.Collections.Generic;
using static LaMediaCancha.Models.DevolucionModels;

namespace LaMediaCancha.Services
{
    public interface IDevolucionService
    {
        bool ValidarPlazoDevolucion(int compraId);
        List<ProductoDevolucion> ObtenerProductosDisponiblesParaDevolver(int compraId);
        int RegistrarDevolucion(RegistrarDevolucionRequest request);
        EncabezadoDevolucion ObtenerDevolucionPorId(int devolucionId);
        List<DetalleDevolucion> ObtenerDetallesDevolucion(int devolucionId);
        List<EncabezadoDevolucion> ObtenerDevolucionesPorCompra(int compraId);
        List<EncabezadoDevolucion> ObtenerDevolucionesPendientes();
        List<EncabezadoDevolucion> ObtenerTodasDevoluciones(int? pagina = null, int? registros = null);
        bool CambiarEstadoDevolucion(int devolucionId, string nuevoEstado, string observaciones = null);
        bool CancelarDevolucion(int devolucionId, string motivoCancelacion);
    }
}