using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ControlTalleresMVP.Persistence.Models
{
    public class Inscripcion
    {
        public int InscripcionId { get; set; }
        public DateTime Fecha { get; set; } = DateTime.Now;

        public decimal Costo { get; set; }

        public decimal SaldoActual { get; set; }

        public EstadoInscripcion Estado { get; set; } = EstadoInscripcion.Pendiente;

        public DateTime CreadoEn { get; set; } = DateTime.Now;
        public DateTime ActualizadoEn { get; set; } = DateTime.Now;
        public bool Eliminado { get; set; } = false;
        public DateTime? EliminadoEn { get; set; }

        public int AlumnoId { get; set; }   
        public Alumno Alumno { get; set; } = null!;

        public int TallerId { get; set; }
        public Taller Taller { get; set; } = null!;

        public int GeneracionId { get; set; }
        public Generacion Generacion { get; set; } = null!;

    }
}
