using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ControlTalleresMVP.Persistence.Models
{
    public class Sede
    {
        public int IdSede { get; set; }
        public string Nombre { get; set; } = null!;

        // Relación inversa: una sede puede tener muchos alumnos
        public ICollection<Alumno> Alumnos { get; set; } = new List<Alumno>();
    }
}
