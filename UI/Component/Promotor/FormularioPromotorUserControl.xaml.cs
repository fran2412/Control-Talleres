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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ControlTalleresMVP.UI.Component.Promotor
{
    /// <summary>
    /// Lógica de interacción para FormularioPromotorUserControl.xaml
    /// </summary>
    public partial class FormularioPromotorUserControl : UserControl
    {
        private static readonly Regex regexCaracteres = new(@"^[a-zA-ZáéíóúÁÉÍÓÚüÜñÑ\s']+$");

        public FormularioPromotorUserControl()
        {
            InitializeComponent();
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
