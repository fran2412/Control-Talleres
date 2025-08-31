using ControlTalleresMVP.Abstractions;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ControlTalleresMVP.Persistence.ModelDTO
{
    public class SedeDTO: ICrudDTO
    {
        [ScaffoldColumn(false)]
        public int Id { get; set; }

        [Display(Name = "Nombre del promotor")]
        public string Nombre { get; set; } = "";

        [Display(Name = "Fecha de alta")]
        [DisplayFormat(DataFormatString = "{0:dd/MM/yyyy HH:mm}")]
        public DateTimeOffset CreadoEn { get; set; }

    }
}
