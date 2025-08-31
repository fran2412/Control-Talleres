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

namespace ControlTalleresMVP.UI.Component.Sede
{
    /// <summary>
    /// Lógica de interacción para FormularioSedeUserControl.xaml
    /// </summary>
    public partial class FormularioSedeUserControl : UserControl
    {
        private static readonly Regex regexCaracteres = new(@"^[a-zA-ZáéíóúÁÉÍÓÚüÜñÑ\s']+$");

        public FormularioSedeUserControl()
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

            NombreTextBox.Text = string.Join(" ", palabras.Select(p => char.ToUpper(p[0]) + p.Substring(1).ToLower()));

        }
    }
}
