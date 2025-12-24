using ControlTalleresMVP.Persistence.ModelDTO;

namespace ControlTalleresMVP.Services.Inscripciones
{
    public interface IInscripcionReporteService
    {
        Task<InscripcionReporteDTO[]> ObtenerInscripcionesReporteAsync(
            int? tallerId = null,
            int? promotorId = null,
            int? generacionId = null,
            DateTime? desde = null,
            DateTime? hasta = null,
            bool incluirTalleresEliminados = false,
            CancellationToken ct = default);

        Task<InscripcionEstadisticasDTO> ObtenerEstadisticasInscripcionesAsync(
            int? tallerId = null,
            int? generacionId = null,
            DateTime? desde = null,
            DateTime? hasta = null,
            bool incluirTalleresEliminados = false,
            CancellationToken ct = default);
    }
}
