using ControlTalleresMVP.Helpers.Dialogs;
using ControlTalleresMVP.Persistence.Models;
using ControlTalleresMVP.Services.Alumnos;
using ControlTalleresMVP.Services.Configuracion;
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
        private readonly IConfiguracionService _configuracionService;
        private readonly Alumno _alumnoOriginal;
        private readonly decimal _maxDescuentoPorClase;

        public ContenedorFormAlumnoWindow(Alumno alumno)
        {
            InitializeComponent();

            _alumnoService = App.ServiceProvider!.GetRequiredService<IAlumnoService>();
            _dialogService = App.ServiceProvider!.GetRequiredService<IDialogService>();
            _promotorService = App.ServiceProvider!.GetRequiredService<IPromotorService>();
            _sedeService = App.ServiceProvider!.GetRequiredService<ISedeService>();
            _configuracionService = App.ServiceProvider!.GetRequiredService<IConfiguracionService>();
            _alumnoOriginal = alumno;

            var costoClase = Math.Max(1, _configuracionService.GetValor<int>("costo_clase", 150));
            _maxDescuentoPorClase = Math.Max(0, costoClase - 1);

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
            SedeComboBox.SelectedValue = _alumnoOriginal.Sede?.SedeId;
            PromotorComboBox.ItemsSource = new ObservableCollection<Promotor>(_promotorService.ObtenerTodos());
            PromotorComboBox.SelectedValue = _alumnoOriginal.Promotor?.PromotorId;
            EsBecadoCheckBox.IsChecked = _alumnoOriginal.EsBecado;
            DescuentoUpDown.Maximum = _maxDescuentoPorClase;
            DescuentoUpDown.Value = Math.Min(_alumnoOriginal.DescuentoPorClase, _maxDescuentoPorClase);
            DescuentoUpDown.IsEnabled = !_alumnoOriginal.EsBecado;
        }

        private bool ValidarFormulario()
        {
            if (!BaseFormHelper.ValidarCampoObligatorio(NombreTextBox, "El nombre", _dialogService))
                return false;

            return ValidarDescuento();
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
                AlumnoId = _alumnoOriginal.AlumnoId,
                Nombre = NombreTextBox.Text.Trim(),
                Telefono = string.IsNullOrWhiteSpace(TelefonoTextBox.Text) ? null : TelefonoTextBox.Text.Trim(),
                SedeId = SedeComboBox.SelectedValue as int?,
                PromotorId = PromotorComboBox.SelectedValue as int?,
                EsBecado = EsBecadoCheckBox.IsChecked ?? false,
                DescuentoPorClase = ObtenerDescuentoPorClase(),
                CreadoEn = _alumnoOriginal.CreadoEn
            };
        }

        private bool ValidarDescuento()
        {
            if (EsBecadoCheckBox.IsChecked == true)
                return true;

            var descuento = ObtenerDescuentoPorClase();

            if (descuento > _maxDescuentoPorClase)
            {
                _dialogService.Alerta($"El descuento máximo permitido es {_maxDescuentoPorClase:C} (costo de la clase menos 1).");
                return false;
            }

            return true;
        }

        private decimal ObtenerDescuentoPorClase()
        {
            if (EsBecadoCheckBox.IsChecked == true)
                return 0m;

            return (decimal)(DescuentoUpDown.Value ?? 0m);
        }

        private void EsBecadoCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            DescuentoUpDown.Value = 0m;
            DescuentoUpDown.IsEnabled = false;
        }

        private void EsBecadoCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            DescuentoUpDown.IsEnabled = true;
        }
    }
}
