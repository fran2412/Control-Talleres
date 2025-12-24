using ControlTalleresMVP.Persistence.ModelDTO;
using ControlTalleresMVP.Persistence.Models;

namespace ControlTalleresMVP.Services.Clases
{
    public interface IClaseService
    {
        public Task<RegistrarClaseResult> RegistrarClaseAsync(
        int alumnoId,
        int tallerId,
        DateTime fecha,
        decimal montoAbono,
        Guid? grupoOperacionId = null,
        CancellationToken ct = default);

        Task<List<ClaseFinancieraDTO>> ObtenerClasesFinancierasAsync(
        int? alumnoId = null,
        int? tallerId = null,
        DateTime? desde = null,
        DateTime? hasta = null,
        CancellationToken ct = default);
        Task<Clase[]> ObtenerClasesDeAlumnoAsync(int alumnoId, CancellationToken ct = default);

        Task CancelarAsync(int claseID, string? motivo = null, CancellationToken ct = default);

        Task<ClasePagoEstadoDTO[]> ObtenerEstadoPagoHoyAsync(
            int alumnoId,
            int[] tallerIds,
            DateTime fecha,                // usa DateTime.Today al invocar
            CancellationToken ct = default);

        Task<Clase[]> ObtenerClasesPagadasAsync(int alumnoId, int tallerId, CancellationToken ct = default);

        Task<EstadoPagoAlumnoDTO[]> ObtenerEstadoPagoAlumnosAsync(
            int? tallerId = null,
            int? alumnoId = null,
            int? generacionId = null,
            CancellationToken ct = default);
    }
}
