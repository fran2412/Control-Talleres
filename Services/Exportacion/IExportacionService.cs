using ControlTalleresMVP.Persistence.ModelDTO;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ControlTalleresMVP.Services.Exportacion
{
    public interface IExportacionService
    {
        Task<string> ExportarEstadoPagosAsync(IEnumerable<EstadoPagoAlumnoDTO> datos, string formato = "csv");
        Task<string> ExportarInscripcionesAsync(IEnumerable<InscripcionReporteDTO> datos, string formato = "csv");
    }
}
