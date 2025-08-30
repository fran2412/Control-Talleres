using ControlTalleresMVP.Persistence.ModelDTO;
using ControlTalleresMVP.Persistence.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ControlTalleresMVP.Services.Alumnos
{
    public interface IAlumnoService
    {
        void AgregarAlumno(Alumno alumno);
        void EditarAlumno(Alumno alumno);
        void EliminarAlumno(int id);
    }
}
