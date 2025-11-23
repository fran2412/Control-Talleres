using ControlTalleresMVP.Persistence.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ControlTalleresMVP.Services.Picker
{
    public interface IAlumnoPickerService
    {
        Alumno? Pick(bool excluirBecados = false);
        Task<Alumno?> PickConDeudasAsync(bool excluirBecados = false);
    }

}
