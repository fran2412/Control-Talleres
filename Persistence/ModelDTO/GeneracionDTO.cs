using CommunityToolkit.Mvvm.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace ControlTalleresMVP.Persistence.ModelDTO
{
    public partial class GeneracionDTO : ObservableObject
    {
        [ScaffoldColumn(false)]
        [ObservableProperty]
        private int id;

        [Display(Name = "Nombre de la generación")]
        [ObservableProperty]
        private string nombre = string.Empty;

        [Display(Name = "Fecha de inicio")]
        [ObservableProperty]
        private DateTime fechaInicio;

        [Display(Name = "Fecha de fin")]
        [ObservableProperty]
        private DateTime? fechaFin;
    }
}
