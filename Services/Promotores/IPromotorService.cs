using ControlTalleresMVP.Persistence.ModelDTO;
using ControlTalleresMVP.Persistence.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ControlTalleresMVP.Services.Promotores
{
    public interface IPromotorService
    {
        public ObservableCollection<PromotorDTO> RegistrosPromotores { get; set; }
        public Task GuardarAsync(Promotor promotor, CancellationToken ct = default);
        public Task EliminarAsync(int id, CancellationToken ct = default);
        public Task ActualizarAsync(Promotor promotor, CancellationToken ct = default);
        public Task<List<PromotorDTO>> ObtenerPromotoresParaGridAsync(CancellationToken ct = default);
        public Task InicializarRegistros(CancellationToken ct = default);

    }
}
