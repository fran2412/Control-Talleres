using ControlTalleresMVP.Helpers.Dialogs;
using ControlTalleresMVP.ViewModel.Menu;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ControlTalleresMVP.UI.Component.Alumno
{
    /// <summary>
    /// Lógica de interacción para FormularioAlumnoUserControl.xaml
    /// </summary>
    public partial class FormularioAlumnoUserControl : UserControl
    {
        private static readonly Regex regexCaracteres = new(@"^[a-zA-ZáéíóúÁÉÍÓÚüÜñÑ\s']+$");

        public bool InscribirEnTaller { get; set; } = false;
        public FormularioAlumnoUserControl()
        {
            InitializeComponent();
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
    }
}
