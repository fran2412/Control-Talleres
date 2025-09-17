using ControlTalleresMVP.Persistence.ModelDTO;
using ControlTalleresMVP.Persistence.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ControlTalleresMVP.Services.Cargos
{
    public interface ICargosService
    {
        public Task<DestinoCargoDTO[]> ObtenerCargosPendientesActualesAsync(int alumnoId, CancellationToken ct = default);
        public Task<Cargo[]> ObtenerCargosAsync(int alumnoId, CancellationToken ct = default);
        public Generacion? GetGeneracionActual();
    }
}
