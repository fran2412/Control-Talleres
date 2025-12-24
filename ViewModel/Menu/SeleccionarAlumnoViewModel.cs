using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ControlTalleresMVP.Persistence.Models;
using ControlTalleresMVP.Services.Alumnos;
using System.Collections.ObjectModel;
using System.Windows;

namespace ControlTalleresMVP.ViewModel.Menu
{
    public partial class SeleccionarAlumnoViewModel : ObservableObject
    {
        private readonly IAlumnoService _alumnoService;

        public ObservableCollection<Alumno> Alumnos { get; } = new();

        [ObservableProperty]
        private Alumno? seleccionado;

        public SeleccionarAlumnoViewModel(IAlumnoService alumnoService)
        {
            _alumnoService = alumnoService;
        }

        public void Cargar()
        {
            Alumnos.Clear();
            var lista = _alumnoService.ObtenerTodos(); // ajusta al método real
            foreach (var a in lista) Alumnos.Add(a);
        }

        [RelayCommand]
        private void Aceptar(Window window)
        {
            if (Seleccionado is null) return;
            window.DialogResult = true;
            window.Close();
        }

        [RelayCommand]
        private void Cancelar(Window window)
        {
            window.DialogResult = false;
            window.Close();
        }
    }
}
