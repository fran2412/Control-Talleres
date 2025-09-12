using ControlTalleresMVP.Abstractions;
using ControlTalleresMVP.Persistence.ModelDTO;
using ControlTalleresMVP.Persistence.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ControlTalleresMVP.Services.Talleres
{
    public interface ITallerService : ICrudService<Taller>
    {
        public ObservableCollection<TallerDTO> RegistrosTalleres { get; set; }
        public Task<List<TallerDTO>> ObtenerTalleresParaGridAsync(CancellationToken ct = default);
        public Task<List<TallerDTO>> ObtenerTalleresParaGridAsync(bool incluirEliminados, CancellationToken ct = default);
        public Task InicializarRegistros(CancellationToken ct = default);

    }
}
