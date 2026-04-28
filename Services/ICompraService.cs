using LaMediaCancha.Models;
using System.Collections.Generic;
using static LaMediaCancha.Models.CompraModels;

namespace LaMediaCancha.Services
{
    public interface ICompraService
    {
        EncabezadoCompra ObtenerCompraPorId(int compraId);
        List<EncabezadoCompra> ObtenerTodasCompras();
        int RegistrarCompra(RegistrarCompraRequest request);
        bool CancelarCompra(int compraId, string motivo);
    }
}