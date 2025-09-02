using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ControlTalleresMVP.Persistence.ModelDTO
{
    public class GeneracionDTO
    {
        [ScaffoldColumn(false)]
        public int Id { get; set; }

        [Display(Name = "Fecha de inicio")]
        public DateTime FechaInicio { get; set; }

        [Display(Name = "Fecha de fin")]
        public DateTime? FechaFin { get; set; }

        [Display(Name = "Nombre de la generación")]
        public string Nombre { get; set; } = "";

        [Display(Name = "Fecha de alta")]
        [DisplayFormat(DataFormatString = "{0:dd/MM/yyyy HH:mm}")]
        public DateTimeOffset CreadoEn { get; set; }

    }
}
