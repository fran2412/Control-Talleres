using ControlTalleresMVP.Services.Configuracion;
using ControlTalleresMVP.ViewModel.Menu;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ControlTalleresMVP.UI.Component.Alumno
{
    /// <summary>
    /// Lógica de interacción para FormularioAlumnoUserControl.xaml
    /// </summary>
    public partial class FormularioAlumnoUserControl : UserControl
    {
        private static readonly Regex regexCaracteres = new(@"^[a-zA-ZáéíóúÁÉÍÓÚüÜñÑ\s']+$");
        private readonly decimal _maxDescuentoPorClase;

        public bool InscribirEnTaller { get; set; } = false;
        public FormularioAlumnoUserControl()
        {
            InitializeComponent();

            var configService = App.ServiceProvider!.GetRequiredService<IConfiguracionService>();
            var costoClase = Math.Max(1, configService.GetValor<int>("costo_clase", 150));
            _maxDescuentoPorClase = Math.Max(0, costoClase - 1);

            Loaded += (_, __) => AjustarControlDescuento();
        }

        // Validar que Abono sea numérico
        private void AbonoTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }

        private void TelefonoTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new(@"^[0-9+]+$");
            e.Handled = !regex.IsMatch(e.Text);
        }

        private string _telefonoSoloDigitos = "";

        private void TelefonoTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            // Quitar todo excepto dígitos
            var soloDigitos = new string(TelefonoTextBox.Text.Where(char.IsDigit).ToArray());

            if (string.IsNullOrEmpty(soloDigitos))
                return;

            if (soloDigitos.Length != 10)
            {
                MessageBox.Show("El teléfono debe tener exactamente 10 dígitos.", "Teléfono inválido");
                TelefonoTextBox.Text = string.Empty;
                _telefonoSoloDigitos = "";
                return;
            }

            // Guardar la versión sin formato para la BD
            _telefonoSoloDigitos = soloDigitos;

            // Mostrar con formato
            if (soloDigitos.StartsWith("55") || soloDigitos.StartsWith("56"))
                TelefonoTextBox.Text = $"({soloDigitos.Substring(0, 2)}) {soloDigitos.Substring(2, 4)}-{soloDigitos.Substring(6, 4)}";
            else
                TelefonoTextBox.Text = $"({soloDigitos.Substring(0, 3)}) {soloDigitos.Substring(3, 3)}-{soloDigitos.Substring(6, 4)}";
        }

        private void NombreTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !regexCaracteres.IsMatch(e.Text);
        }

        private void NombreTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            var texto = NombreTextBox.Text.Trim();

            if (string.IsNullOrWhiteSpace(texto))
                return;

            // separar en palabras (quita dobles espacios)
            var palabras = texto.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            if (palabras.Length < 3)
            {
                MessageBox.Show(
                    "El nombre debe contener al menos un nombre y dos apellidos (3 palabras).",
                    "Validación",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning
                );
                NombreTextBox.Text = string.Empty;
                return;
            }

            if (palabras.Length > 4)
            {
                MessageBox.Show(
                    "El nombre no puede tener más de dos nombres y dos apellidos (4 palabras).",
                    "Validación",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning
                );
                NombreTextBox.Text = string.Empty;
                return;
            }

            NombreTextBox.Text = string.Join(" ",
                palabras.Select(p => char.ToUpper(p[0]) + p.Substring(1).ToLower()));
        }

        private void AjustarControlDescuento()
        {
            if (DataContext is MenuAlumnosViewModel vm && vm.DescuentoPorClase > _maxDescuentoPorClase)
            {
                vm.DescuentoPorClase = _maxDescuentoPorClase;
            }
        }

        private void DescuentoTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            if (EsAlumnoBecado())
            {
                e.Handled = true;
                return;
            }

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
            {
                return;
            }

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

        private bool EsAlumnoBecado()
        {
            return DataContext is MenuAlumnosViewModel vm && vm.EsBecado;
        }
    }
}
