using ControlTalleresMVP.Persistence.ModelDTO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ControlTalleresMVP.Services.Cargos
{
    public interface ICargosService
    {
        Task<DestinoCargoDTO[]> ObtenerCargosPendientesAsync(int alumnoId, CancellationToken ct = default);
    }
}
