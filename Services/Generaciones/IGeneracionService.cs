using ControlTalleresMVP.Persistence.ModelDTO;
using ControlTalleresMVP.Persistence.Models;
using System.Collections.ObjectModel;

namespace ControlTalleresMVP.Services.Generaciones
{
    public interface IGeneracionService
    {
        public ObservableCollection<GeneracionDTO> RegistrosGeneraciones { get; set; }
        public Task NuevaGeneracion(CancellationToken ct = default);
        public Task FinalizarGeneracionActual(CancellationToken ct = default);
        public bool TieneGeneracionAbierta();
        public Generacion? ObtenerGeneracionActual();
        public Task InicializarRegistros(CancellationToken ct = default);
        public Task<List<GeneracionDTO>> ObtenerGeneracionesParaGridAsync(CancellationToken ct = default);
    }
}
