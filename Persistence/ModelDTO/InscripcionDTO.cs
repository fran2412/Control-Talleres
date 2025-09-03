using ControlTalleresMVP.Persistence.Models;
using System;
using System.ComponentModel.DataAnnotations;

namespace ControlTalleresMVP.Persistence.ModelDTO
{
    public class InscripcionDTO
    {
        [ScaffoldColumn(false)]
        public int Id { get; set; }

        [Display(Name = "Nombre del alumno")]
        public string Nombre { get; set; } = string.Empty;

        [Display(Name = "Taller")]
        public string Taller { get; set; } = string.Empty;

        [Display(Name = "Costo")]
        public decimal Costo { get; set; }

        [Display(Name = "Saldo actual")]
        public decimal SaldoActual { get; set; }

        [Display(Name = "Estado")]
        public EstadoInscripcion Estado { get; set; }

        [Display(Name = "Fecha de inscripción")]
        [DisplayFormat(DataFormatString = "{0:dd/MM/yyyy HH:mm}")]
        public DateTime CreadoEn { get; set; }
    }
}
