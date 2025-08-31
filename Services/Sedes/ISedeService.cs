using ControlTalleresMVP.Abstractions;
using ControlTalleresMVP.Persistence.ModelDTO;
using ControlTalleresMVP.Persistence.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ControlTalleresMVP.Services.Sedes
{
    public interface ISedeService : ICrudService<Sede>
    {
        public ObservableCollection<SedeDTO> RegistrosSedes { get; set; }
        public Task<List<SedeDTO>> ObtenerSedesParaGridAsync(CancellationToken ct = default);
        public Task InicializarRegistros(CancellationToken ct = default);

    }
}
