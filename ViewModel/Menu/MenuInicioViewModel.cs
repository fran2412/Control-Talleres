using ControlTalleresMVP.Abstractions;
using ControlTalleresMVP.Helpers.Commands;
using System.Windows.Input;

namespace ControlTalleresMVP.ViewModel.Menu
{
    public class MenuInicioViewModel
    {
        public INavigatorService Navigator { get; }
        public ICommand GoAlumnos { get; }
        public ICommand GoTalleres { get; }
        public ICommand GoPagos { get; }

        public MenuInicioViewModel(INavigatorService navigator)
        {
            Navigator = navigator;

            GoAlumnos = new RelayCommand(() => Navigator.NavigateTo<MenuAlumnosViewModel>());
            GoTalleres = new RelayCommand(() => Navigator.NavigateTo<MenuTalleresViewModel>());
            GoPagos = new RelayCommand(() => Navigator.NavigateTo<MenuPagosViewModel>());
        }
    }
}
