using ControlTalleresMVP.Abstractions;
using ControlTalleresMVP.Persistence.ModelDTO;
using ControlTalleresMVP.Persistence.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ControlTalleresMVP.Services.Alumnos
{
    public interface IAlumnoService: ICrudService<Alumno>
    {
        public ObservableCollection<AlumnoDTO> RegistrosAlumnos { get; set; }
        public Task<List<AlumnoDTO>> ObtenerAlumnosParaGridAsync(CancellationToken ct = default);
        public Task InicializarRegistros(CancellationToken ct = default);

    }
}
