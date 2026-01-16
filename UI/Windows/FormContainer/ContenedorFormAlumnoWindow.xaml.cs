using ControlTalleresMVP.Helpers.Dialogs;
using ControlTalleresMVP.Persistence.Models;
using ControlTalleresMVP.Services.Alumnos;
using ControlTalleresMVP.Services.Configuracion;
using ControlTalleresMVP.Services.Promotores;
using ControlTalleresMVP.Services.Sesion;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Globalization;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

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
        private readonly ISesionService _sesionService;
        private readonly IConfiguracionService _configuracionService;
        private readonly Alumno _alumnoOriginal;
        private readonly decimal _maxDescuentoPorClase;

        public static readonly DependencyProperty DescuentoVisualProperty =
            DependencyProperty.Register(nameof(DescuentoVisual), typeof(decimal), typeof(ContenedorFormAlumnoWindow), new PropertyMetadata(0m));

        public decimal DescuentoVisual
        {
            get => (decimal)GetValue(DescuentoVisualProperty);
            set => SetValue(DescuentoVisualProperty, value);
        }

        public ContenedorFormAlumnoWindow(Alumno alumno)
        {
            InitializeComponent();

            _alumnoService = App.ServiceProvider!.GetRequiredService<IAlumnoService>();
            _dialogService = App.ServiceProvider!.GetRequiredService<IDialogService>();
            _promotorService = App.ServiceProvider!.GetRequiredService<IPromotorService>();
            _sesionService = App.ServiceProvider!.GetRequiredService<ISesionService>();
            _configuracionService = App.ServiceProvider!.GetRequiredService<IConfiguracionService>();
            _alumnoOriginal = alumno;

            var costoClase = Math.Max(1, _configuracionService.GetValorSede<int>("costo_clase", 150));
            _maxDescuentoPorClase = costoClase;

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
            PromotorComboBox.ItemsSource = new ObservableCollection<Promotor>(_promotorService.ObtenerTodos());
            PromotorComboBox.SelectedValue = _alumnoOriginal.Promotor?.PromotorId;
            var descuento = Math.Min(_alumnoOriginal.DescuentoPorClase, _maxDescuentoPorClase);
            DescuentoTextBox.Text = descuento.ToString("0.##", CultureInfo.InvariantCulture);
            DescuentoVisual = descuento;
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
                SedeId = _sesionService.ObtenerIdSede(),
                PromotorId = PromotorComboBox.SelectedValue as int?,
                DescuentoPorClase = ObtenerDescuentoPorClase(),
                CreadoEn = _alumnoOriginal.CreadoEn
            };
        }

        private bool ValidarDescuento()
        {
            var descuento = ObtenerDescuentoPorClase();

            if (descuento > _maxDescuentoPorClase)
            {
                _dialogService.Alerta($"El descuento máximo permitido es {_maxDescuentoPorClase:C} (costo de la clase).");
                return false;
            }

            return true;
        }

        private decimal ObtenerDescuentoPorClase()
        {
            var texto = DescuentoTextBox.Text?.Replace(',', '.') ?? "0";
            if (!decimal.TryParse(texto, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var valor))
            {
                return 0m;
            }

            return Math.Min(valor, _maxDescuentoPorClase);
        }

        private void DescuentoTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            if (sender is not TextBox textBox)
            {
                e.Handled = true;
                return;
            }

            if (!char.IsDigit(e.Text, 0) && e.Text != "." && e.Text != ",")
            {
                e.Handled = true;
                return;
            }

            var textoPropuesto = ObtenerTextoPropuesto(textBox, e.Text);
            if (string.IsNullOrWhiteSpace(textoPropuesto))
                return;

            var normalizado = textoPropuesto.Replace(',', '.');
            if (!decimal.TryParse(normalizado, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var valor))
            {
                e.Handled = true;
                return;
            }

        }

        private void DescuentoTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is not TextBox textBox)
                return;

            var textoActual = string.IsNullOrWhiteSpace(textBox.Text) ? "0" : textBox.Text;
            var normalizado = textoActual.Replace(',', '.');

            if (!decimal.TryParse(normalizado, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var valor))
            {
                valor = 0m;
                textBox.Text = "0";
                textBox.CaretIndex = textBox.Text.Length;
            }

            var valorNormalizado = Math.Min(valor, _maxDescuentoPorClase);

            if (valorNormalizado != valor)
            {
                textBox.Text = valorNormalizado.ToString("0.##", CultureInfo.InvariantCulture);
                textBox.CaretIndex = textBox.Text.Length;
            }

            DescuentoVisual = valorNormalizado;
        }

        private void ActualizarDescuentoVisualDesdeTexto()
        {
            var texto = DescuentoTextBox.Text?.Replace(',', '.') ?? "0";
            if (!decimal.TryParse(texto, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var valor))
            {
                valor = 0m;
            }

            DescuentoVisual = Math.Min(valor, _maxDescuentoPorClase);
        }

        private static string ObtenerTextoPropuesto(TextBox textBox, string input)
        {
            var textoActual = textBox.Text ?? string.Empty;
            if (!string.IsNullOrEmpty(textBox.SelectedText))
            {
                var inicio = textBox.SelectionStart;
                var fin = textoActual.Remove(inicio, textBox.SelectionLength);
                return fin.Insert(inicio, input);
            }

            return textoActual.Insert(textBox.CaretIndex, input);
        }
    }
}
