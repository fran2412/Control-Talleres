using ControlTalleresMVP.Abstractions;
using ControlTalleresMVP.Persistence.ModelDTO;
using ControlTalleresMVP.Persistence.Models;
using System.Collections.ObjectModel;

namespace ControlTalleresMVP.Services.Sedes
{
    public interface ISedeService : ICrudService<Sede>
    {
        public ObservableCollection<SedeDTO> RegistrosSedes { get; set; }
        public Task<List<SedeDTO>> ObtenerSedesParaGridAsync(CancellationToken ct = default);
        public Task InicializarRegistros(CancellationToken ct = default);

    }
}
