using LaMediaCancha.Models;
using static LaMediaCancha.Models.EmpleadoModels;

namespace LaMediaCancha.Services
{
    public interface IEmpleadoService
    {
        Empleado ObtenerEmpleadoPorUsuarioId(int usuarioId);
        Empleado ObtenerEmpleadoPorId(int empleadoId);
    }
}