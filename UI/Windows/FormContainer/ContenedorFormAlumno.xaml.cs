using ControlTalleresMVP.Helpers.Dialogs;
using ControlTalleresMVP.Persistence.Models;
using ControlTalleresMVP.Services.Alumnos;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
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
    /// Lógica de interacción para ContenedorFormAlumno.xaml
    /// </summary>
    public partial class ContenedorFormAlumno : Window
    {
        private readonly IAlumnoService _alumnoService;
        private readonly IDialogService _dialogService;
        private readonly Alumno _alumnoOriginal;

        public ContenedorFormAlumno(Alumno alumno)
        {
            InitializeComponent();

            _alumnoService = App.ServiceProvider!.GetRequiredService<IAlumnoService>();
            _dialogService = App.ServiceProvider!.GetRequiredService<IDialogService>();
            _alumnoOriginal = alumno ?? throw new ArgumentNullException(nameof(alumno));

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
            SedeComboBox.SelectedValue = _alumnoOriginal.IdSede ?? null;
            PromotorComboBox.SelectedValue = _alumnoOriginal.IdPromotor ?? null;
        }

        private bool ValidarFormulario()
        {
            return BaseFormHelper.ValidarCampoObligatorio(NombreTextBox, "El nombre", _dialogService);
        }

        private async void EditarAlumno_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidarFormulario()) return;

            var alumnoActualizado = CrearAlumnoActualizado();

            try
            {
                await _alumnoService.ActualizarAsync(alumnoActualizado);
                _dialogService.Info("Alumno actualizado correctamente.");
                Close();
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
                Telefono = string.IsNullOrWhiteSpace(TelefonoTextBox.Text)
                    ? null : TelefonoTextBox.Text.Trim(),
                IdSede = _alumnoOriginal.IdSede,
                IdPromotor = _alumnoOriginal.IdPromotor,
                CreadoEn = _alumnoOriginal.CreadoEn
            };
        }
    }
}
