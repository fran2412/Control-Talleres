using ControlTalleresMVP.Abstractions;
using ControlTalleresMVP.Persistence.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ControlTalleresMVP.Persistence.ModelDTO
{
    public class AlumnoDTO: ICrudDTO
    {
        [ScaffoldColumn(false)]
        public int Id { get; set; }

        [Display(Name = "Nombre completo")]
        public string Nombre { get; set; } = "";

        [ScaffoldColumn(false)]
        public Sede? Sede { get; set; }

        [Display(Name = "Sede")]
        public string? SedeNombre => Sede?.Nombre;

        [ScaffoldColumn(false)]
        public Promotor? Promotor { get; set; }
        [Display(Name = "Promotor")]
        public string? PromotorNombre => Promotor?.Nombre;

        [Display(Name = "Teléfono")]
        public string? Telefono { get; set; } = "";

        [Display(Name = "Fecha de alta")]
        [DisplayFormat(DataFormatString = "{0:dd/MM/yyyy HH:mm}")]
        public DateTime CreadoEn { get; set; }
    }
}