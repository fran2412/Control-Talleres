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
    public partial class FormularioAlumnoUserControl : UserControl, INotifyPropertyChanged
    {
        public bool InscribirEnTaller { get; set; } = false;

        public FormularioAlumnoUserControl()
        {
            InitializeComponent();
        }

        // Validar que Abono sea numérico
        private void AbonoTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new ("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }

        private void TelefonoTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new (@"^[0-9+]+$");
            e.Handled = !regex.IsMatch(e.Text);
        }

        private void TelefonoTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            string telefono = TelefonoTextBox.Text;

            // Elimina espacios y guiones
            telefono = telefono.Replace(" ", "").Replace("-", "");

            // 👉 Si está vacío, no se valida (opcional)
            if (string.IsNullOrEmpty(telefono))
                return;

            // Validar que tenga 10 dígitos
            if (telefono.Length != 10 || !Regex.IsMatch(telefono, @"^\d{10}$"))
            {
                MessageBox.Show("El teléfono debe tener exactamente 10 dígitos.",
                                "Teléfono inválido",
                                MessageBoxButton.OK,
                                MessageBoxImage.Warning);
                TelefonoTextBox.Text = string.Empty;
                return;
            }

            // Formatear según lada (2 o 3 dígitos)
            string formato;
            if (telefono.StartsWith("55") || telefono.StartsWith("56")) // CDMX tiene lada de 2 dígitos
            {
                // Ej: 5512345678 → (55) 1234-5678
                formato = $"({telefono.Substring(0, 2)}) {telefono.Substring(2, 4)}-{telefono.Substring(6, 4)}";
            }
            else
            {
                // Ej: 4525187855 → (452) 518-7855
                formato = $"({telefono.Substring(0, 3)}) {telefono.Substring(3, 3)}-{telefono.Substring(6, 4)}";
            }

            // Mostrar ya formateado en el textbox
            TelefonoTextBox.Text = formato;
        }

        // 👉 implementación de INotifyPropertyChanged
        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
