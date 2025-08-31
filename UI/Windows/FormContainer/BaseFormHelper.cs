using ControlTalleresMVP.Helpers.Dialogs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ControlTalleresMVP.UI.Windows.FormContainer
{
    public static class BaseFormHelper
    {
        private static readonly Regex RegexSoloLetras =
            new(@"^[a-zA-ZáéíóúÁÉÍÓÚüÜñÑ\s']+$", RegexOptions.Compiled);
        private static readonly Regex RegexTelefonoInput =
            new(@"^[0-9+\(\)\-\s]+$", RegexOptions.Compiled);

        #region Validaciones de Input

        public static void ValidarSoloLetras_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !RegexSoloLetras.IsMatch(e.Text);
        }

        public static void ValidarTelefono_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !RegexTelefonoInput.IsMatch(e.Text);
        }

        #endregion

        #region Formateo

        public static void FormatearNombre_LostFocus(object sender, RoutedEventArgs e, IDialogService dialogService)
        {
            if (sender is not TextBox textBox) return;

            var texto = textBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(texto)) return;

            var palabras = texto.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            if (!ValidarCantidadPalabras(palabras.Length, textBox, dialogService)) return;

            // Aplicar formato Title Case
            textBox.Text = string.Join(" ",
                palabras.Select(p => char.ToUpper(p[0]) + p[1..].ToLower()));
        }

        public static void FormatearNombre_NoRangoLetras(object sender, RoutedEventArgs e, IDialogService dialogService)
        {
            if (sender is not TextBox textBox) return;

            var texto = textBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(texto)) return;

            var palabras = texto.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            // Aplicar formato Title Case
            textBox.Text = string.Join(" ",
                palabras.Select(p => char.ToUpper(p[0]) + p[1..].ToLower()));
        }

        public static void FormatearTelefono_LostFocus(object sender, RoutedEventArgs e, IDialogService dialogService)
        {
            if (sender is not TextBox textBox) return;

            var soloDigitos = new string(textBox.Text.Where(char.IsDigit).ToArray());

            if (string.IsNullOrEmpty(soloDigitos))
            {
                textBox.Text = "";
                return;
            }

            if (soloDigitos.Length != 10)
            {
                dialogService.Alerta("El teléfono debe tener exactamente 10 dígitos.");
                textBox.Text = "";
                return;
            }

            textBox.Text = FormatearTelefono(soloDigitos);
        }

        #endregion

        #region Validaciones

        public static bool ValidarCantidadPalabras(int cantidad, TextBox textBox, IDialogService dialogService)
        {
            return cantidad switch
            {
                < 3 => MostrarErrorNombre("El nombre debe contener al menos un nombre y dos apellidos (3 palabras).", textBox, dialogService),
                > 4 => MostrarErrorNombre("El nombre no puede tener más de dos nombres y dos apellidos (4 palabras).", textBox, dialogService),
                _ => true
            };
        }

        public static bool MostrarErrorNombre(string mensaje, TextBox textBox, IDialogService dialogService)
        {
            dialogService.Alerta(mensaje);
            textBox.Text = "";
            textBox.Focus();
            return false;
        }

        public static string FormatearTelefono(string digitos)
        {
            return digitos.StartsWith("55") || digitos.StartsWith("56")
                ? $"({digitos[..2]}) {digitos[2..6]}-{digitos[6..10]}"
                : $"({digitos[..3]}) {digitos[3..6]}-{digitos[6..10]}";
        }

        public static bool ValidarCampoObligatorio(TextBox textBox, string nombreCampo, IDialogService dialogService)
        {
            if (string.IsNullOrWhiteSpace(textBox.Text))
            {
                dialogService.Error($"{nombreCampo} es obligatorio.");
                textBox.Focus();
                return false;
            }
            return true;
        }

        #endregion

        #region Configuración de Eventos

        public static void ConfigurarValidacionesNombre(TextBox textBox, IDialogService dialogService)
        {
            textBox.PreviewTextInput += ValidarSoloLetras_PreviewTextInput;
            textBox.LostFocus += (s, e) => FormatearNombre_LostFocus(s, e, dialogService);
        }

        public static void FormatearTextoTitle(TextBox textBox, IDialogService dialogService)
        {
            textBox.PreviewTextInput += ValidarSoloLetras_PreviewTextInput;
            textBox.LostFocus += (s, e) => FormatearNombre_NoRangoLetras(s, e, dialogService);
        }

        public static void ConfigurarValidacionesTelefono(TextBox textBox, IDialogService dialogService)
        {
            textBox.PreviewTextInput += ValidarTelefono_PreviewTextInput;
            textBox.LostFocus += (s, e) => FormatearTelefono_LostFocus(s, e, dialogService);
        }

        public static void ConfigurarCerrarConEscape(Window window)
        {
            window.KeyDown += (s, e) =>
            {
                if (e.Key == Key.Escape)
                    window.Close();
            };
        }

        #endregion
    }
}
