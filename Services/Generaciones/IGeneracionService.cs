using ControlTalleresMVP.Abstractions;
using ControlTalleresMVP.Persistence.ModelDTO;
using ControlTalleresMVP.Persistence.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ControlTalleresMVP.Services.Generaciones
{
    public interface IGeneracionService: ICrudService<Generacion>
    {
        public ObservableCollection<GeneracionDTO> RegistrosGeneraciones { get; set; }
        public Task NuevaGeneracion(CancellationToken ct = default);
        public Generacion? ObtenerGeneracionActual();
        public Task InicializarRegistros(CancellationToken ct = default);
    }
}
