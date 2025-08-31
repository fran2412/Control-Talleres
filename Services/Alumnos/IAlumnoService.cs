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
    public interface IAlumnoService
    {
        public ObservableCollection<AlumnoDTO> RegistrosAlumnos { get; set; }
        public Task GuardarAsync(Alumno alumno, CancellationToken ct = default);
        public Task EliminarAsync(int id, CancellationToken ct = default);
        public Task ActualizarAsync(Alumno alumno, CancellationToken ct = default);
        public Task<List<AlumnoDTO>> ObtenerAlumnosParaGridAsync(CancellationToken ct = default);
        public Task InicializarRegistros(CancellationToken ct = default);

    }
}
