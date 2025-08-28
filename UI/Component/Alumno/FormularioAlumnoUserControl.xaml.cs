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

namespace ControlTalleresMVP.UI.Component.Alumno
{
    /// <summary>
    /// Lógica de interacción para FormularioAlumnoUserControl.xaml
    /// </summary>
    public partial class FormularioAlumnoUserControl : UserControl
    {
        private const int MinimoACuenta = 50;

        private const int LongitudTelefono = 10;
        private const int CostoInscripcion = 1200; //traer de configuración


        public FormularioAlumnoUserControl()
        {
            InitializeComponent();
            SaldoPendienteTextBox.Text = "$"+CostoInscripcion.ToString(); // inicia mostrando el total
        }

        private void AbonoTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]+"); // cualquier cosa que no sea dígito
            e.Handled = regex.IsMatch(e.Text);
        }

        private void AbonoTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (int.TryParse(AbonoTextBox.Text, out int valor))
            {
                if (valor < MinimoACuenta || valor > CostoInscripcion)
                {
                    MessageBox.Show($"El valor debe estar entre {MinimoACuenta} y {CostoInscripcion} (El costo de la inscripción).",
                                    "Valor fuera de rango",
                                    MessageBoxButton.OK,
                                    MessageBoxImage.Warning);
                    AbonoTextBox.Text = string.Empty;
                }
            }
        }

        private void AbonoTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (int.TryParse(AbonoTextBox.Text, out int abono))
            {
                int resta = CostoInscripcion - abono;
                if (resta < 0)
                {
                    MessageBox.Show("El abono no puede ser mayor al costo de inscripción.",
                                    "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    AbonoTextBox.Text = string.Empty;
                    SaldoPendienteTextBox.Text = CostoInscripcion.ToString();
                }
                else
                {
                    SaldoPendienteTextBox.Text = "$" + resta.ToString();
                }
            }
            else
            {
                SaldoPendienteTextBox.Text = "$" + CostoInscripcion.ToString();
            }
        }

        private void TelefonoTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex(@"^[0-9+]+$");
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
    }
}
