using ControlTalleresMVP.Persistence.Models;
using System.ComponentModel.DataAnnotations;

namespace ControlTalleresMVP.Persistence.ModelDTO
{
    public class InscripcionRegistroDTO
    {
        // Identificadores (ocultos en UI)
        [ScaffoldColumn(false)] public int InscripcionId { get; set; }
        [ScaffoldColumn(false)] public int TallerId { get; set; }
        [ScaffoldColumn(false)] public int AlumnoId { get; set; }
        [ScaffoldColumn(false)] public int GeneracionId { get; set; }

        // Contexto
        [Display(Name = "Fecha inscripción")]
        [DisplayFormat(DataFormatString = "{0:dd/MM/yyyy}")]
        public DateTime FechaInscripcion { get; set; }

        [Display(Name = "Taller")]
        public string TallerNombre { get; set; } = string.Empty;

        [ScaffoldColumn(false)]
        public bool TallerEliminado { get; set; }

        [Display(Name = "Alumno")]
        public string AlumnoNombre { get; set; } = string.Empty;

        [Display(Name = "Generación")]
        public string GeneracionNombre { get; set; } = string.Empty;

        // Importes
        [Display(Name = "Costo")]
        [DisplayFormat(DataFormatString = "{0:C}")]
        public decimal Monto { get; set; }

        [Display(Name = "Pagado")]
        [DisplayFormat(DataFormatString = "{0:C}")]
        public decimal MontoPagado { get; set; }

        [Display(Name = "Saldo")]
        [DisplayFormat(DataFormatString = "{0:C}")]
        public decimal SaldoActual { get; set; }

        [Display(Name = "% Pagado")]
        public int PorcentajePagado { get; set; }  // 0..100 (redondeado)

        // Estado
        [ScaffoldColumn(false)]
        public EstadoInscripcion EstadoInscripcion { get; set; }

        [Display(Name = "Estado")]
        public string EstadoTexto { get; set; } = string.Empty; // "Pagada/Pendiente/Cancelada"

        // Trazabilidad de pagos
        [Display(Name = "Último pago")]
        [DisplayFormat(DataFormatString = "{0:dd/MM/yyyy HH:mm}")]
        public DateTime? UltimoPagoFecha { get; set; }

        // Información adicional
        [Display(Name = "Motivo cancelación")]
        public string? MotivoCancelacion { get; set; }

        [Display(Name = "Cancelada")]
        [DisplayFormat(DataFormatString = "{0:dd/MM/yyyy HH:mm}")]
        public DateTime? CanceladaEn { get; set; }
    }
}
