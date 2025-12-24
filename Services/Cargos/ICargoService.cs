using ControlTalleresMVP.Persistence.ModelDTO;
using ControlTalleresMVP.Persistence.Models;

namespace ControlTalleresMVP.Services.Cargos
{
    public interface ICargosService
    {
        public Task<DestinoCargoDTO[]> ObtenerCargosPendientesActualesAsync(int alumnoId, CancellationToken ct = default);
        public Task<Cargo[]> ObtenerCargosAsync(int alumnoId, CancellationToken ct = default);
    }
}
