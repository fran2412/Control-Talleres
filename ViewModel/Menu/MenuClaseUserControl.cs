using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ControlTalleresMVP.Helpers.Dialogs;
using ControlTalleresMVP.Persistence.Models;
using ControlTalleresMVP.Services.Clases;
using ControlTalleresMVP.Services.Inscripciones;
using ControlTalleresMVP.Services.Picker;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Security.AccessControl;
using System.Text;
using System.Threading.Tasks;

namespace ControlTalleresMVP.ViewModel.Menu
{
    public partial class MenuClaseUserControl : ObservableObject
    {
        private readonly IClaseService _claseService;
        private readonly IInscripcionService _inscripcionService;
        private readonly IDialogService _dialogService;
        private readonly IAlumnoPickerService _alumnoPicker;

        public MenuClaseUserControl(IClaseService claseService, IInscripcionService inscripcionService,IDialogService dialogService, IAlumnoPickerService alumnoPicker)
        {
            _claseService = claseService;
            _dialogService = dialogService;
            _alumnoPicker = alumnoPicker;
            _inscripcionService = inscripcionService;
            FechaDeHoy = DateTime.Now.ToString("dd/MM/yyyy");
        }

        // ====================
        // PROPIEDADES BINDING
        // ====================

        [ObservableProperty]
        private string fechaDeHoy;

        [ObservableProperty]
        private string? alumnoNombre;

        [ObservableProperty]
        private Alumno? alumnoSeleccionado;

        [ObservableProperty]
        private ObservableCollection<Taller> talleresDelAlumno = new();

        [ObservableProperty]
        private Taller? tallerSeleccionado;

        [ObservableProperty]
        private int cantidadSeleccionada = 1;

        [ObservableProperty]
        private decimal? montoIngresado;

        [ObservableProperty]
        private decimal montoSugerido;

        [ObservableProperty]
        private string? mensajeValidacion;

        // ====================
        // COMANDOS
        // ====================

        [RelayCommand]
        private async Task BuscarAlumno()
        {
            // Aquí abres un diálogo de búsqueda de alumnos
            var alumno = _alumnoPicker.Pick();
            if (alumno is null) return;

            // 🔹 Traer inscripciones del alumno
            var inscripciones = await _inscripcionService.ObtenerInscripcionesAsync(alumno.AlumnoId);

            // 🔹 Extraer talleres únicos
            var talleresDisponibles = new ObservableCollection<Taller>(
                inscripciones
                    .Where(i => i.Taller != null)
                    .Select(i => i.Taller!)
                    .GroupBy(t => t.TallerId) // 🔹 agrupa por ID
                    .Select(g => g.First())
                    .ToList()
            );

            if (talleresDisponibles.Count == 0)
            {
                _dialogService.Alerta("Este alumno no está inscrito en ningún taller.\n" +
                    "Inscríbalo en alguno para poder registrar sus pagos a clases.");
                return;
            }

            TalleresDelAlumno = talleresDisponibles;

            TallerSeleccionado = TalleresDelAlumno.First();
            AlumnoSeleccionado = alumno;
            AlumnoNombre = alumno.Nombre;

        }

        [RelayCommand]
        private void LimpiarSeleccion()
        {
            AlumnoSeleccionado = null;
            AlumnoNombre = string.Empty;
            TallerSeleccionado = null;
            CantidadSeleccionada = 1;
            MontoIngresado = null;
            MensajeValidacion = null;
        }

        [RelayCommand]
        private void UsarMontoSugerido()
        {
            MontoIngresado = MontoSugerido;
        }

        [RelayCommand]
        private async Task GuardarPagoClases(CancellationToken ct)
        {
            try
            {
                MensajeValidacion = null;

                if (AlumnoSeleccionado is null)
                {
                    MensajeValidacion = "Debes seleccionar un alumno.";
                    return;
                }

                if (TallerSeleccionado is null)
                {
                    MensajeValidacion = "Debes seleccionar un taller.";
                    return;
                }

                if (MontoIngresado is null || MontoIngresado <= 0)
                {
                    MensajeValidacion = "El monto debe ser mayor a 0.";
                    return;
                }

                for (int i = 0; i < CantidadSeleccionada; i++)
                {
                    await _claseService.RegistrarClaseAsync(
                        AlumnoSeleccionado.AlumnoId,
                        TallerSeleccionado.TallerId,
                        DateTime.Now,
                        i == 0 ? MontoIngresado.Value : 0m, // abono inicial solo en la primera clase
                        ct);
                }

                _dialogService.Info("Pago registrado correctamente.", "Clases");
                LimpiarSeleccion();
            }
            catch (Exception ex)
            {
                _dialogService.Error($"Error al registrar el pago: {ex.Message}", "Clases");
            }
        }

        [RelayCommand]
        private void CancelarPagoClases()
        {
            LimpiarSeleccion();
            _dialogService.Info("Registro cancelado.", "Clases");
        }

        // ====================
        // MÉTODO AUXILIAR
        // ====================

        public void CalcularMontoSugerido(decimal costoClase)
        {
            MontoSugerido = costoClase * CantidadSeleccionada;
        }
    }
}