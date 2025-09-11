using System.ComponentModel.DataAnnotations;

namespace ControlTalleresMVP.Persistence.ModelDTO
{
    public class InscripcionReporteDTO
    {
        public int InscripcionId { get; set; }
        
        [Display(Name = "Alumno")]
        public string NombreAlumno { get; set; } = "";
        
        public bool TallerEliminado { get; set; }
        
        [Display(Name = "Taller")]
        public string NombreTaller { get; set; } = "";
        
        [Display(Name = "Sede")]
        public string NombreSede { get; set; } = "";
        
        [Display(Name = "Promotor")]
        public string NombrePromotor { get; set; } = "";
        
        [Display(Name = "Fecha Inscripción")]
        [DisplayFormat(DataFormatString = "{0:dd/MM/yyyy}")]
        public DateTime FechaInscripcion { get; set; }
        
        [Display(Name = "Costo")]
        [DisplayFormat(DataFormatString = "{0:C2}")]
        public decimal Costo { get; set; }
        
        [Display(Name = "Saldo")]
        [DisplayFormat(DataFormatString = "{0:C2}")]
        public decimal SaldoActual { get; set; }
        
        [Display(Name = "Estado")]
        public string Estado { get; set; } = "";
        
        [Display(Name = "Estado de Pago")]
        public string EstadoPago { get; set; } = "";
        
        [Display(Name = "Monto Pagado")]
        [DisplayFormat(DataFormatString = "{0:C2}")]
        public decimal MontoPagado { get; set; }
        
        [Display(Name = "Día de la Semana")]
        public string DiaSemana { get; set; } = "";
        
        [Display(Name = "Fecha Inicio Taller")]
        [DisplayFormat(DataFormatString = "{0:dd/MM/yyyy}")]
        public DateTime FechaInicioTaller { get; set; }
        
        [Display(Name = "Fecha Fin Taller")]
        [DisplayFormat(DataFormatString = "{0:dd/MM/yyyy}")]
        public DateTime? FechaFinTaller { get; set; }
        
        [Display(Name = "Generación")]
        public string NombreGeneracion { get; set; } = "";
        
        [Display(Name = "Días Transcurridos")]
        public int DiasTranscurridos { get; set; }
        
        [Display(Name = "Días Restantes")]
        public int? DiasRestantes { get; set; }
        
        [Display(Name = "Progreso %")]
        [DisplayFormat(DataFormatString = "{0:F1}%")]
        public decimal ProgresoPorcentaje { get; set; }
    }
}
