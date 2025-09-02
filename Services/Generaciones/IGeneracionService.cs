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
        public void NuevaGeneracion();
        public Generacion? ObtenerGeneracionActual();

    }
}
