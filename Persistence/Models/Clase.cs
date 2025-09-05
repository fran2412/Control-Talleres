using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ControlTalleresMVP.Persistence.Models
{
    public class Clase
    {
        public int ClaseId { get; set; }
        public int TallerId { get; set; }
        public DateTime Fecha { get; set; }

        public DateTime CreadoEn { get; set; } = DateTime.Now;
        public DateTime ActualizadoEn { get; set; } = DateTime.Now;
        public bool Eliminado { get; set; }
        public DateTime? EliminadoEn { get; set; }

        public Taller Taller { get; set; } = null!;
    }
}
