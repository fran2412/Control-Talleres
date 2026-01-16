using ControlTalleresMVP.Helpers.Dialogs;
using ControlTalleresMVP.Persistence.ModelDTO;
using ControlTalleresMVP.Persistence.Models;
using ControlTalleresMVP.Services.Alumnos;
using ControlTalleresMVP.UI.Windows.Select;
using ControlTalleresMVP.ViewModel.Menu;
using Microsoft.Extensions.DependencyInjection;

namespace ControlTalleresMVP.Services.Picker
{
    public class AlumnoPickerService : IAlumnoPickerService
    {
        private readonly IServiceProvider _sp;

        public AlumnoPickerService(IServiceProvider sp)
        {
            _sp = sp;
        }

        public Alumno? Pick(bool excluirBecados = false)
        {
            // Resuelve tu VM de registros (el mismo que ya usas en el UC):
            var registrosVM = _sp.GetRequiredService<MenuAlumnosViewModel>();

            var estadoAnteriorFiltroBecados = registrosVM.OcultarBecadosEnPicker;
            registrosVM.OcultarBecadosEnPicker = excluirBecados;

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

            try
            {
                var ok = win.ShowDialog() == true;
                return ok ? elegido : null;
            }
            finally
            {
                registrosVM.OcultarBecadosEnPicker = estadoAnteriorFiltroBecados;
            }
        }

        public async Task<Alumno?> PickConDeudasAsync(bool excluirBecados = false)
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

            // Si excluirBecados es true, obtener el costo de clase para filtrar
            decimal costoClase = 0;
            if (excluirBecados)
            {
                var configService = _sp.GetRequiredService<ControlTalleresMVP.Services.Configuracion.IConfiguracionService>();
                costoClase = configService.GetValorSede<int>("costo_clase", 150);
            }

            foreach (var alumno in alumnosConDeudas)
            {
                // Excluir alumnos con descuento >= costo de clase (100% becados)
                if (excluirBecados && costoClase > 0 && alumno.DescuentoPorClase >= costoClase)
                {
                    continue;
                }

                var alumnoDTO = new AlumnoDTO
                {
                    Id = alumno.AlumnoId,
                    Nombre = alumno.Nombre ?? "",
                    Telefono = alumno.Telefono ?? "",
                    Sede = alumno.Sede,
                    Promotor = alumno.Promotor,
                    CreadoEn = alumno.CreadoEn,
                    DescuentoPorClase = alumno.DescuentoPorClase
                };
                registrosVM.Registros.Add(alumnoDTO);
            }

            var estadoAnteriorFiltroBecados = registrosVM.OcultarBecadosEnPicker;
            registrosVM.OcultarBecadosEnPicker = excluirBecados;

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

            try
            {
                var ok = win.ShowDialog() == true;
                return ok ? elegido : null;
            }
            finally
            {
                registrosVM.OcultarBecadosEnPicker = estadoAnteriorFiltroBecados;
            }
        }
    }

}
