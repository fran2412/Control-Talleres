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
        private readonly IAlumnoService _alumnoService = App.ServiceProvider!.GetRequiredService<IAlumnoService>();
        private readonly IDialogService _dialogService = App.ServiceProvider!.GetRequiredService<IDialogService>();
        private static readonly Regex regexCaracteres = new(@"^[a-zA-ZáéíóúÁÉÍÓÚüÜñÑ\s']+$");
        public int id;
        public string nombreCompleto = "";
        public string telefono = "";
        public int? idSede;
        public int? promotor;

        public ContenedorFormAlumno(Alumno alumno)
        {
            InitializeComponent();

            if (alumno != null)
            {
                NombreTextBox.Text = alumno.Nombre;
                if (!string.IsNullOrEmpty(alumno.Telefono))
                {
                    id = alumno.IdAlumno;
                    telefono = alumno.Telefono;
                    TelefonoTextBox.Text = alumno.Telefono;
                    _telefonoSoloDigitos = new string(telefono.Where(char.IsDigit).ToArray());
                }
                if (alumno.IdSede.HasValue)
                    idSede = alumno.IdSede.Value;

                if (alumno.IdPromotor.HasValue)
                    promotor = alumno.IdPromotor.Value;
            }
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

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void EditarAlumnoButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(NombreTextBox.Text))
            {
                _dialogService.Error("El nombre es obligatorio.");
                return;
            }

            var nuevoAlumno = new Alumno
            {
                IdAlumno = id,
                Nombre = NombreTextBox.Text.Trim(),
                Telefono = TelefonoTextBox.Text,
                IdSede = idSede,
                IdPromotor = promotor
            };
            try
            {
                _alumnoService.ActualizarAsync(nuevoAlumno).Wait();
                _dialogService.Info("Alumno actualizado correctamente.");
                this.Close(); return;
            }
            catch (Exception ex)
            {
                _dialogService.Error($"Error al procesar los datos: {ex.InnerException}");
                return;
            }

        }
    }
}
