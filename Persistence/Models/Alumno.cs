using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace ControlTalleresMVP.Persistence.Models
{
    public class Alumno
    {
        public int AlumnoId { get; set; }

        public string Nombre { get; set; } = null!;
        public string? Telefono { get; set; }


        public DateTime CreadoEn { get; set; }
        public DateTime ActualizadoEn { get; set; }
        public bool Eliminado { get; set; } = false;
        public DateTime? EliminadoEn { get; set; }

        // Clave foránea a Sede
        public int? SedeId { get; set; }
        public Sede? Sede { get; set; }

        // Clave foránea a Promotor
        public int? PromotorId { get; set; }
        public Promotor? Promotor { get; set; }

        public ICollection<Inscripcion> Inscripciones { get; set; } = new List<Inscripcion>();
        public ICollection<Cargo> Cargos { get; set; } = new List<Cargo>();
        public ICollection<Pago> Pagos { get; set; } = new List<Pago>();
    }
}
