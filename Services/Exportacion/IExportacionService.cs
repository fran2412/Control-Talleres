using ControlTalleresMVP.Persistence.ModelDTO;

namespace ControlTalleresMVP.Services.Exportacion
{
    public interface IExportacionService
    {
        Task<string> ExportarEstadoPagosAsync(IEnumerable<EstadoPagoAlumnoDTO> datos, string formato = "csv");
        Task<string> ExportarInscripcionesAsync(IEnumerable<InscripcionReporteDTO> datos, string formato = "csv");
    }
}
