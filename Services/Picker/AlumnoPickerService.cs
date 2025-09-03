using ControlTalleresMVP.Persistence.Models;
using ControlTalleresMVP.Services.Alumnos;
using ControlTalleresMVP.UI.Windows.Select;
using ControlTalleresMVP.ViewModel.Menu;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace ControlTalleresMVP.Services.Picker
{
    public class AlumnoPickerService : IAlumnoPickerService
    {
        private readonly IServiceProvider _sp;

        public AlumnoPickerService(IServiceProvider sp)
        {
            _sp = sp;
        }

        public Alumno? Pick()
        {
            // Resuelve tu VM de registros (el mismo que ya usas en el UC):
            var registrosVM = _sp.GetRequiredService<MenuAlumnosViewModel>();

            var win = new SeleccionarAlumnoWindow(registrosVM); // tu ventana host
            win.RegistrosUC.IsPickerMode = true;
            win.RegistrosUC.DataContext = registrosVM;

            Alumno? elegido = null;
            win.RegistrosUC.PickRequested += a =>
            {
                elegido = a;
                win.DialogResult = true;  // cierra modal
                win.Close();
            };

            var ok = win.ShowDialog() == true;
            return ok ? elegido : null;
        }
    }

}
