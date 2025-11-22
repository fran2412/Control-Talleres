using ControlTalleresMVP.Persistence.ModelDTO;
using ControlTalleresMVP.Persistence.Models;
using ControlTalleresMVP.Services.Alumnos;
using ControlTalleresMVP.UI.Windows.Select;
using ControlTalleresMVP.ViewModel.Menu;
using ControlTalleresMVP.Helpers.Dialogs;
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

        public async Task<Alumno?> PickConDeudasAsync()
        {
            var alumnoService = _sp.GetRequiredService<IAlumnoService>();
            var alumnosConDeudas = await alumnoService.ObtenerAlumnosConDeudasPendientesAsync();

            if (alumnosConDeudas.Count == 0)
            {
                // Mostrar mensaje específico cuando no hay alumnos con deudas
                var dialogService = _sp.GetRequiredService<IDialogService>();
                dialogService.Alerta("No hay alumnos con deudas pendientes.");
                return null;
            }

            // Crear un ViewModel temporal solo con alumnos con deudas
            var registrosVM = _sp.GetRequiredService<MenuAlumnosViewModel>();
            
            // Limpiar la colección actual y cargar solo alumnos con deudas
            registrosVM.Registros.Clear();
            foreach (var alumno in alumnosConDeudas)
            {
                var alumnoDTO = new AlumnoDTO
                {
                    Id = alumno.AlumnoId,
                    Nombre = alumno.Nombre ?? "",
                    Telefono = alumno.Telefono ?? "",
                    Sede = alumno.Sede,
                    Promotor = alumno.Promotor,
                    CreadoEn = alumno.CreadoEn,
                    EsBecado = alumno.EsBecado,
                    DescuentoPorClase = alumno.DescuentoPorClase
                };
                registrosVM.Registros.Add(alumnoDTO);
            }

            var win = new SeleccionarAlumnoWindow(registrosVM);
            win.RegistrosUC.IsPickerMode = true;
            win.RegistrosUC.DataContext = registrosVM;

            Alumno? elegido = null;
            win.RegistrosUC.PickRequested += a =>
            {
                elegido = a;
                win.DialogResult = true;
                win.Close();
            };

            var ok = win.ShowDialog() == true;
            return ok ? elegido : null;
        }
    }

}
