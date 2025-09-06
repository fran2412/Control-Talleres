using ControlTalleresMVP.Persistence.ModelDTO;
using ControlTalleresMVP.Persistence.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ControlTalleresMVP.Services.Clases
{
    public interface IClaseService
    {
        public Task<RegistrarClaseResult> RegistrarClaseAsync(
        int alumnoId,
        int tallerId,
        DateTime fecha,
        decimal montoAbono,
        CancellationToken ct = default);

        Task<Clase[]> ObtenerClasesDeAlumnoAsync(int alumnoId, CancellationToken ct = default);

        Task CancelarAsync(int claseID, string? motivo = null, CancellationToken ct = default);

        Task<ClasePagoEstadoDTO[]> ObtenerEstadoPagoHoyAsync(
            int alumnoId,
            int[] tallerIds,
            DateTime fecha,                // usa DateTime.Today al invocar
            CancellationToken ct = default);
    }
}
