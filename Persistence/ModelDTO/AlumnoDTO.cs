using ControlTalleresMVP.Persistence.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ControlTalleresMVP.Persistence.ModelDTO
{
    public class AlumnoDTO
    {
        [ScaffoldColumn(false)]
        public int Id { get; set; }

        [Display(Name = "Nombre completo")]
        public string Nombre { get; set; } = "";

        [Display(Name = "Sede")]
        public Sede? Sede { get; set; }

        [Display(Name = "Promotor")]
        public Promotor? Promotor { get; set; }

        [Display(Name = "Teléfono")]
        public string? Telefono { get; set; } = "";

        [Display(Name = "Fecha de alta")]
        [DisplayFormat(DataFormatString = "{0:dd/MM/yyyy HH:mm}")]
        public DateTimeOffset CreadoEn { get; set; }
    }
}
