using ControlTalleresMVP.Persistence.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ControlTalleresMVP.Services.Inscripciones
{
    public interface IInscripcionService
    {
        Task<bool> ExisteActivaAsync(int alumnoId, int tallerId, int generacionId, CancellationToken ct = default);
        Task<Inscripcion> InscribirAsync(
            int alumnoId, int tallerId, int generacionId,
            decimal abonoInicial = 0m,
            DateTime? fecha = null, CancellationToken ct = default);
        Task RecalcularEstadoAsync(int inscripcionId, CancellationToken ct = default);
        Task CancelarAsync(int inscripcionId, string? motivo = null, CancellationToken ct = default);

    }
}
