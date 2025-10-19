using ControlTalleresMVP.Persistence.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ControlTalleresMVP.Validators
{
    public interface IAlumnoValidator
    {
        bool ConfirmarNombresSimilares(string nombreAlumno);
    }
}
