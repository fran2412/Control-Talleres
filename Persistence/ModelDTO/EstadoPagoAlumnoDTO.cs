using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ControlTalleresMVP.Persistence.ModelDTO
{
    public class EstadoPagoAlumnoDTO
    {
        public int AlumnoId { get; set; }
        public string NombreAlumno { get; set; } = string.Empty;
        public int TallerId { get; set; }
        public string NombreTaller { get; set; } = string.Empty;
        public DateTime FechaInicio { get; set; }
        public DateTime? FechaFin { get; set; }
        public int ClasesTotales { get; set; }
        public int ClasesPagadas { get; set; }
        public int ClasesPendientes { get; set; }
        public decimal MontoTotal { get; set; }
        public decimal MontoPagado { get; set; }
        public decimal MontoPendiente { get; set; }
        public bool TodasLasClasesPagadas { get; set; }
        public string EstadoPago { get; set; } = string.Empty;
        public DateTime FechaUltimaClase { get; set; }
        public DateTime FechaHoy { get; set; }
    }
}
