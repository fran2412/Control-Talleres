using ControlTalleresMVP.Abstractions;
using ControlTalleresMVP.Helpers.Commands;
using ControlTalleresMVP.ViewModel.Menu;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace ControlTalleresMVP.ViewModel.Navigation
{
    internal class ShellViewModel
    {
        public INavigatorService Navigator { get; }
        public ICommand GoInicio { get; }
        public ICommand GoAlumnos { get; set; }
        public ICommand GoTalleres { get; set; }
        public ICommand GoInscripciones { get; set; }
        public ICommand GoPagos { get; set; }
        public ICommand GoPromotores { get; set; }
        public ICommand NavigateCommand { get; }

        public ShellViewModel (INavigatorService navigator)
        {
            Navigator = navigator;

            GoInicio = new RelayCommand(() => Navigator.NavigateTo<MenuInicioViewModel>());
            GoAlumnos = new RelayCommand(() => Navigator.NavigateTo<MenuAlumnosViewModel>());
            GoTalleres = new RelayCommand(() => Navigator.NavigateTo<MenuTalleresViewModel>());
            GoInscripciones = new RelayCommand(() => Navigator.NavigateTo<MenuInscripcionesViewModel>());
            GoPagos = new RelayCommand(() => Navigator.NavigateTo<MenuPagosViewModel>());
            GoPromotores = new RelayCommand(() => Navigator.NavigateTo<MenuPromotorViewModel>());

            NavigateCommand = new RelayCommandGeneric<Type>(t => Navigator.NavigateTo(t));

            Navigator.NavigateTo<MenuInicioViewModel>();
        }
    }
}
