using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ControlTalleresMVP.Persistence.Models
{
    public class Generacion
    {
        public int GeneracionId { get; set; }
        public string Nombre { get; set; } = null!;

        public DateTime FechaInicio { get; set; }
        public DateTime FechaFin { get; set; }
        public bool EsActual { get; set; }

        public DateTimeOffset CreadoEn { get; set; }
        public DateTimeOffset ActualizadoEn { get; set; }
        public bool Eliminado { get; set; } = false;
        public DateTimeOffset? EliminadoEn { get; set; }

        public ICollection<Inscripcion> Inscripciones { get; set; } = new List<Inscripcion>();
    }
}
