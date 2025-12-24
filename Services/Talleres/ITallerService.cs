using ControlTalleresMVP.Abstractions;
using ControlTalleresMVP.Persistence.ModelDTO;
using ControlTalleresMVP.Persistence.Models;
using System.Collections.ObjectModel;

namespace ControlTalleresMVP.Services.Talleres
{
    public interface ITallerService : ICrudService<Taller>
    {
        public ObservableCollection<TallerDTO> RegistrosTalleres { get; set; }
        public Task<List<TallerDTO>> ObtenerTalleresParaGridAsync(CancellationToken ct = default);
        public Task<List<TallerDTO>> ObtenerTalleresParaGridAsync(bool incluirEliminados, CancellationToken ct = default);
        public Task InicializarRegistros(CancellationToken ct = default);
        public List<TallerInscripcionDTO> ObtenerTalleresParaInscripcion(decimal costoInscripcion);


    }
}
