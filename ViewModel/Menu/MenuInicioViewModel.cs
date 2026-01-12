using ControlTalleresMVP.Abstractions;
using ControlTalleresMVP.Helpers.Commands;
using ControlTalleresMVP.Services.Sesion;
using System.Windows.Input;

namespace ControlTalleresMVP.ViewModel.Menu
{
    public class MenuInicioViewModel
    {
        private readonly ISesionService _sesionService;
        public string TextoBienvenida => $"¡Bienvenido al Sistema!\nSede {_sesionService.ObtenerNombreSede()}";
        public INavigatorService Navigator { get; }
        public ICommand GoAlumnos { get; }
        public ICommand GoTalleres { get; }
        public ICommand GoPagos { get; }

        public MenuInicioViewModel(INavigatorService navigator, ISesionService sesionService)
        {
            Navigator = navigator;
            _sesionService = sesionService;

            GoAlumnos = new RelayCommand(() => Navigator.NavigateTo<MenuAlumnosViewModel>());
            GoTalleres = new RelayCommand(() => Navigator.NavigateTo<MenuTalleresViewModel>());
            GoPagos = new RelayCommand(() => Navigator.NavigateTo<MenuPagosViewModel>());
        }
    }
}
