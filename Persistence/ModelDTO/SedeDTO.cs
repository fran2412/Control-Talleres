using ControlTalleresMVP.Abstractions;
using System.ComponentModel.DataAnnotations;

namespace ControlTalleresMVP.Persistence.ModelDTO
{
    public class SedeDTO : ICrudDTO
    {
        [ScaffoldColumn(false)]
        public int Id { get; set; }

        [Display(Name = "Nombre de la sede")]
        public string Nombre { get; set; } = "";

        [Display(Name = "Fecha de alta")]
        [DisplayFormat(DataFormatString = "{0:dd/MM/yyyy HH:mm}")]
        public DateTime CreadoEn { get; set; }

    }
}
