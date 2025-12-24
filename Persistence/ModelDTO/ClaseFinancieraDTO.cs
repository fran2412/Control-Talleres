using ControlTalleresMVP.Persistence.Models;
using System.ComponentModel.DataAnnotations;

namespace ControlTalleresMVP.Persistence.ModelDTO
{
    public class ClaseFinancieraDTO
    {
        // Identificadores (ocultos en UI)
        [ScaffoldColumn(false)] public int CargoId { get; set; }
        [ScaffoldColumn(false)] public int ClaseId { get; set; }
        [ScaffoldColumn(false)] public int TallerId { get; set; }
        [ScaffoldColumn(false)] public int AlumnoId { get; set; }

        // Contexto
        [Display(Name = "Fecha clase")]
        [DisplayFormat(DataFormatString = "{0:dd/MM/yyyy}")]
        public DateTime FechaClase { get; set; }

        [Display(Name = "Taller")]
        public string TallerNombre { get; set; } = string.Empty;

        [Display(Name = "Alumno")]
        public string AlumnoNombre { get; set; } = string.Empty;

        // Importes
        [Display(Name = "Valor Total (Devengado)")]
        [DisplayFormat(DataFormatString = "{0:C}")]
        public decimal MontoTotal { get; set; }

        [Display(Name = "Ingreso del día")]
        [DisplayFormat(DataFormatString = "{0:C}")]
        public decimal IngresoPorFecha { get; set; }

        [Display(Name = "Pagado Acumulado")]
        [DisplayFormat(DataFormatString = "{0:C}")]
        public decimal MontoPagado { get; set; }

        [Display(Name = "Saldo")]
        [DisplayFormat(DataFormatString = "{0:C}")]
        public decimal SaldoActual { get; set; }

        [Display(Name = "% Pagado")]
        public int PorcentajePagado { get; set; }  // 0..100 (redondeado)

        // Estado
        [ScaffoldColumn(false)]
        public EstadoCargo EstadoCargo { get; set; }

        [Display(Name = "Estado")]
        public string EstadoTexto { get; set; } = string.Empty; // “Pagada/Parcial/Pendiente/Anulada”

        // Trazabilidad de pagos
        [Display(Name = "Último pago")]
        [DisplayFormat(DataFormatString = "{0:dd/MM/yyyy HH:mm}")]
        public DateTime? UltimoPagoFecha { get; set; }
    }
}
