using ControlTalleresMVP.Abstractions;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ControlTalleresMVP.Persistence.ModelDTO
{
    public class TallerDTO: ICrudDTO
    {
        [ScaffoldColumn(false)]
        public int Id { get; set; }

        [Display(Name = "Nombre del taller")]
        public string Nombre { get; set; } = "";

        [Display(Name = "Dia de clase")]
        public string DiaSemana { get; set; } = "";

        [Display(Name = "Horario de inicio")]
        [DisplayFormat(DataFormatString = "{0:HH:mm}")]
        public TimeSpan HorarioInicio { get; set; }

        [Display(Name = "Horario de fin")]
        [DisplayFormat(DataFormatString = "{0:HH:mm}")]
        public TimeSpan HorarioFin { get; set; }

        [Display(Name = "Fecha de inicio")]
        [DisplayFormat(DataFormatString = "{0:dd/MM/yyyy}")]
        public DateTime FechaInicio { get; set; }

        [Display(Name = "Fecha de fin")]
        [DisplayFormat(DataFormatString = "{0:dd/MM/yyyy}")]
        public DateTime? FechaFin { get; set; }

        [Display(Name = "Sede")]
        public int SedeId { get; set; }

        [Display(Name = "Nombre de la sede")]
        public string NombreSede { get; set; } = "";

        [Display(Name = "Fecha de alta")]
        [DisplayFormat(DataFormatString = "{0:dd/MM/yyyy HH:mm}")]
        public DateTime CreadoEn { get; set; }

        [ScaffoldColumn(false)]
        public bool Eliminado { get; set; }

        [ScaffoldColumn(false)]
        public DateTime? EliminadoEn { get; set; }
    }
}
