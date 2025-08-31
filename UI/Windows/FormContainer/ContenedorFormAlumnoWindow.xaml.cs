using ControlTalleresMVP.Helpers.Dialogs;
using ControlTalleresMVP.Persistence.Models;
using ControlTalleresMVP.Services.Alumnos;
using ControlTalleresMVP.Services.Promotores;
using ControlTalleresMVP.Services.Sedes;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace ControlTalleresMVP.UI.Windows.FormContainer
{
    /// <summary>
    /// Lógica de interacción para ContenedorFormAlumnoWindow.xaml
    /// </summary>
    public partial class ContenedorFormAlumnoWindow : Window
    {
        private readonly IAlumnoService _alumnoService;
        private readonly IDialogService _dialogService;
        private readonly IPromotorService _promotorService;
        private readonly ISedeService _sedeService;
        private readonly Alumno _alumnoOriginal;

        public ContenedorFormAlumnoWindow(Alumno alumno)
        {
            InitializeComponent();

            _alumnoService = App.ServiceProvider!.GetRequiredService<IAlumnoService>();
            _dialogService = App.ServiceProvider!.GetRequiredService<IDialogService>();
            _promotorService = App.ServiceProvider!.GetRequiredService<IPromotorService>();
            _sedeService = App.ServiceProvider!.GetRequiredService<ISedeService>();
            _alumnoOriginal = alumno;

            ConfigurarValidaciones();
            CargarDatos();
        }

        private void ConfigurarValidaciones()
        {
            // Configurar validaciones usando el helper
            BaseFormHelper.ConfigurarValidacionesNombre(NombreTextBox, _dialogService);
            BaseFormHelper.ConfigurarValidacionesTelefono(TelefonoTextBox, _dialogService);
            BaseFormHelper.ConfigurarCerrarConEscape(this);
        }

        private void CargarDatos()
        {
            NombreTextBox.Text = _alumnoOriginal.Nombre ?? "";
            TelefonoTextBox.Text = _alumnoOriginal.Telefono ?? "";
            SedeComboBox.ItemsSource = new ObservableCollection<Sede>(_sedeService.ObtenerTodos());
            SedeComboBox.SelectedValue = _alumnoOriginal.Sede?.IdSede;
            PromotorComboBox.ItemsSource = new ObservableCollection<Promotor>(_promotorService.ObtenerTodos());
            PromotorComboBox.SelectedValue = _alumnoOriginal.Promotor?.IdPromotor;
        }

        private bool ValidarFormulario()
        {
            return BaseFormHelper.ValidarCampoObligatorio(NombreTextBox, "El nombre", _dialogService);
        }

        private async void EditarAlumno_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidarFormulario()) return;

            var alumnoActualizado = CrearAlumnoActualizado();

            if (!_dialogService.Confirmar("¿Desea guardar los cambios?")) return;

            try
            {
                await _alumnoService.ActualizarAsync(alumnoActualizado);
                Close();
                _dialogService.Info("Alumno actualizado correctamente.");
            }
            catch (Exception ex)
            {
                var mensaje = ex.InnerException?.Message ?? ex.Message;
                _dialogService.Error($"Error al actualizar el alumno: {mensaje}");
            }
        }

        public void Cancelar_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private Alumno CrearAlumnoActualizado()
        {
            return new Alumno
            {
                IdAlumno = _alumnoOriginal.IdAlumno,
                Nombre = NombreTextBox.Text.Trim(),
                Telefono = string.IsNullOrWhiteSpace(TelefonoTextBox.Text) ? null : TelefonoTextBox.Text.Trim(),
                IdSede = SedeComboBox.SelectedValue as int?,
                IdPromotor = PromotorComboBox.SelectedValue as int?,
                CreadoEn = _alumnoOriginal.CreadoEn
            };
        }
    }
}
