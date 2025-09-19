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
        public TimeSpan HorarioInicio { get; set; }
        public TimeSpan HorarioFin { get; set; }
        public DayOfWeek DiaSemana { get; set; }
        public DateTime FechaInicio { get; set; }
        public DateTime? FechaFin { get; set; }
        public int SedeId { get; set; }
        public DateTime CreadoEn { get; set; }
        public DateTime ActualizadoEn { get; set; }
        public bool Eliminado { get; set; } = false;
        public DateTime? EliminadoEn { get; set; }

        // Navegación
        public Sede Sede { get; set; } = null!;
        public ICollection<Inscripcion> Inscripciones { get; set; } = new List<Inscripcion>();
    }
}
