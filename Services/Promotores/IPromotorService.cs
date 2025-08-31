using ControlTalleresMVP.Abstractions;
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
    public interface IPromotorService: ICrudService<Promotor>
    {
        public ObservableCollection<PromotorDTO> RegistrosPromotores { get; set; }
        public Task<List<PromotorDTO>> ObtenerPromotoresParaGridAsync(CancellationToken ct = default);
        public Task InicializarRegistros(CancellationToken ct = default);

    }
}
