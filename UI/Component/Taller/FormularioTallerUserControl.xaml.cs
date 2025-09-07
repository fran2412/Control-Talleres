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

namespace ControlTalleresMVP.UI.Component.Taller
{
    /// <summary>
    /// Lógica de interacción para FormularioTallerUserControl.xaml
    /// </summary>
    public partial class FormularioTallerUserControl : UserControl
    {
        public FormularioTallerUserControl()
        {
            InitializeComponent();
            Loaded += FormularioTallerUserControl_Loaded;
        }

        private void FormularioTallerUserControl_Loaded(object sender, RoutedEventArgs e)
        {
            // Cargar valores iniciales desde el ViewModel
            if (DataContext is ViewModel.Menu.MenuTalleresViewModel viewModel)
            {
                HorarioInicioTextBox.Text = FormatearHora(viewModel.HorarioInicio);
                HorarioFinTextBox.Text = FormatearHora(viewModel.HorarioFin);
            }
        }

        private void NombreTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            var texto = NombreTextBox.Text.Trim();

            var palabras = texto.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            NombreTextBox.Text = string.Join(" ",
                palabras.Select(p => char.ToUpper(p[0]) + p.Substring(1).ToLower()));
        }

        private void HorarioInicioTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            // Permitir escritura libre, no hacer nada especial
        }

        private void HorarioFinTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            // Permitir escritura libre, no hacer nada especial
        }

        private void HorarioInicioTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            ValidarYFormatearHora(HorarioInicioTextBox);
            ActualizarViewModel();
        }

        private void HorarioFinTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            ValidarYFormatearHora(HorarioFinTextBox);
            ActualizarViewModel();
        }

        private void ActualizarViewModel()
        {
            if (DataContext is ViewModel.Menu.MenuTalleresViewModel viewModel)
            {
                if (TimeSpan.TryParse(HorarioInicioTextBox.Text, out var horarioInicio))
                {
                    viewModel.HorarioInicio = horarioInicio;
                }
                if (TimeSpan.TryParse(HorarioFinTextBox.Text, out var horarioFin))
                {
                    viewModel.HorarioFin = horarioFin;
                }
            }
        }

        private void ValidarYFormatearHora(TextBox textBox)
        {
            var texto = textBox.Text.Trim();
            
            if (string.IsNullOrEmpty(texto))
            {
                textBox.Text = "9:00";
                return;
            }

            // Limpiar el texto de espacios y caracteres extra
            texto = texto.Replace(" ", "").ToUpper();
            
            // Intentar parsear directamente como TimeSpan
            if (TimeSpan.TryParse(texto, out var timeSpan))
            {
                textBox.Text = FormatearHora(timeSpan);
                return;
            }

            // Intentar parsear con formato AM/PM
            var horaFormateada = ParsearHoraConAMPM(texto);
            if (horaFormateada.HasValue)
            {
                textBox.Text = FormatearHora(horaFormateada.Value);
                return;
            }

            // Intentar parsear formato simple (solo números)
            var horaSimple = ParsearHoraSimple(texto);
            if (horaSimple.HasValue)
            {
                textBox.Text = FormatearHora(horaSimple.Value);
                return;
            }

            // Si nada funciona, usar valor por defecto
            textBox.Text = "9:00";
        }

        private string FormatearHora(TimeSpan hora)
        {
            return $"{hora.Hours}:{hora.Minutes:D2}";
        }

        private TimeSpan? ParsearHoraConAMPM(string texto)
        {
            // Patrones: "9:30AM", "9:30AM", "9AM", "9:30PM", "21:30PM"
            var patron = @"^(\d{1,2})(?::(\d{2}))?\s*(AM|PM)?$";
            var match = System.Text.RegularExpressions.Regex.Match(texto, patron, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            
            if (!match.Success) return null;

            if (!int.TryParse(match.Groups[1].Value, out var hora)) return null;
            var minuto = match.Groups[2].Success && int.TryParse(match.Groups[2].Value, out var m) ? m : 0;
            var ampm = match.Groups[3].Value.ToUpper();

            // Validar rango de minutos
            if (minuto < 0 || minuto > 59) return null;

            // Convertir AM/PM a 24 horas
            if (ampm == "AM")
            {
                if (hora == 12) hora = 0;
                if (hora < 0 || hora > 12) return null;
            }
            else if (ampm == "PM")
            {
                if (hora != 12) hora += 12;
                if (hora < 12 || hora > 23) return null;
            }
            else
            {
                // Sin AM/PM, validar rango 24 horas
                if (hora < 0 || hora > 23) return null;
            }

            return new TimeSpan(hora, minuto, 0);
        }

        private TimeSpan? ParsearHoraSimple(string texto)
        {
            // Patrones: "9", "9:30", "930", "0930", "21:30"
            if (texto.Length == 1 && int.TryParse(texto, out var h1))
            {
                if (h1 >= 0 && h1 <= 23)
                    return new TimeSpan(h1, 0, 0);
            }
            else if (texto.Length == 2 && int.TryParse(texto, out var h2))
            {
                if (h2 >= 0 && h2 <= 23)
                    return new TimeSpan(h2, 0, 0);
            }
            else if (texto.Length == 3 && int.TryParse(texto, out var h3))
            {
                var hora = h3 / 10;
                var minuto = h3 % 10 * 10;
                if (hora >= 0 && hora <= 23 && minuto >= 0 && minuto <= 59)
                    return new TimeSpan(hora, minuto, 0);
            }
            else if (texto.Length == 4 && int.TryParse(texto, out var h4))
            {
                var hora = h4 / 100;
                var minuto = h4 % 100;
                if (hora >= 0 && hora <= 23 && minuto >= 0 && minuto <= 59)
                    return new TimeSpan(hora, minuto, 0);
            }
            else if (texto.Contains(":"))
            {
                var partes = texto.Split(':');
                if (partes.Length == 2 && 
                    int.TryParse(partes[0], out var hora) && 
                    int.TryParse(partes[1], out var minuto))
                {
                    if (hora >= 0 && hora <= 23 && minuto >= 0 && minuto <= 59)
                        return new TimeSpan(hora, minuto, 0);
                }
            }

            return null;
        }

    }
}
