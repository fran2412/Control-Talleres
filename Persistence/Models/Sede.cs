using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ControlTalleresMVP.Persistence.Models
{
    public class Sede
    {
        public int SedeId { get; set; }
        public string Nombre { get; set; } = null!;

        public DateTime CreadoEn { get; set; }
        public DateTime ActualizadoEn { get; set; }

        public bool Eliminado { get; set; } = false;
        public DateTime? EliminadoEn { get; set; }

        // Relación inversa: una sede puede tener muchos alumnos
        public ICollection<Alumno> Alumnos { get; set; } = new List<Alumno>();
        
        // Relación inversa: una sede puede tener muchos talleres
        public ICollection<Taller> Talleres { get; set; } = new List<Taller>();
    }
}
