using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ControlTalleresMVP.Persistence.Models
{
    public class Taller
    {
        public int TallerId { get; set; }
        public string Nombre { get; set; } = null!;
        public string Horario { get; set; } = null!;
        public DateTimeOffset CreadoEn { get; set; }
        public DateTimeOffset ActualizadoEn { get; set; }
        public bool Eliminado { get; set; } = false;
        public DateTimeOffset? EliminadoEn { get; set; }

        public ICollection<Inscripcion> Inscripciones { get; set; } = new List<Inscripcion>();
    }
}
