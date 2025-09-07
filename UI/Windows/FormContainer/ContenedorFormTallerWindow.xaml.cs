using ControlTalleresMVP.Helpers.Dialogs;
using ControlTalleresMVP.Persistence.Models;
using ControlTalleresMVP.Services.Sedes;
using ControlTalleresMVP.Services.Talleres;
using ControlTalleresMVP.UI.Component.Taller;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
    /// Lógica de interacción para ContenedorFormTallerWindow.xaml
    /// </summary>
    public partial class ContenedorFormTallerWindow : Window
    {
        private readonly ITallerService _tallerService;
        private readonly IDialogService _dialogService;
        private readonly Taller _tallerOriginal;

        public ContenedorFormTallerWindow(Taller taller)
        {
            _tallerService = App.ServiceProvider!.GetRequiredService<ITallerService>();
            _dialogService = App.ServiceProvider!.GetRequiredService<IDialogService>();
            _tallerOriginal = taller;

            InitializeComponent();

            ConfigurarValidaciones();
            CargarDatos();
        }

        private void ConfigurarValidaciones()
        {
            // Configurar validaciones usando el helper
            BaseFormHelper.ConfigurarTitleCase(NombreTextBox);
            BaseFormHelper.ConfigurarCerrarConEscape(this);
        }

        private void CargarDatos()
        {
            NombreTextBox.Text = _tallerOriginal.Nombre ?? "";
            HorarioInicioTextBox.Text = FormatearHora(_tallerOriginal.HorarioInicio);
            HorarioFinTextBox.Text = FormatearHora(_tallerOriginal.HorarioFin);
            DiaSemanaComboBox.SelectedItem = _tallerOriginal.DiaSemana;
            FechaInicioDatePicker.SelectedDate = _tallerOriginal.FechaInicio;
            FechaFinDatePicker.SelectedDate = _tallerOriginal.FechaFin;
        }

        private string FormatearHora(TimeSpan hora)
        {
            return $"{hora.Hours}:{hora.Minutes:D2}";
        }

        private void HorarioInicioTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            // Permitir escritura libre
        }

        private void HorarioFinTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            // Permitir escritura libre
        }

        private void HorarioInicioTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            ValidarYFormatearHora(HorarioInicioTextBox);
        }

        private void HorarioFinTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            ValidarYFormatearHora(HorarioFinTextBox);
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

        private bool ValidarFormulario()
        {
            if (!BaseFormHelper.ValidarCampoObligatorio(NombreTextBox, "El nombre", _dialogService))
                return false;
            
            var horarioInicio = ParsearHora(HorarioInicioTextBox.Text);
            if (!horarioInicio.HasValue)
            {
                _dialogService.Alerta("El horario de inicio no es válido (formato 24h). Ejemplos: 9, 9:30, 9AM, 14:30, 2:30PM");
                return false;
            }
            
            var horarioFin = ParsearHora(HorarioFinTextBox.Text);
            if (!horarioFin.HasValue)
            {
                _dialogService.Alerta("El horario de fin no es válido (formato 24h). Ejemplos: 11, 11:30, 11AM, 15:30, 3:30PM");
                return false;
            }
            
            if (horarioFin.Value <= horarioInicio.Value)
            {
                _dialogService.Alerta("El horario de fin debe ser posterior al horario de inicio");
                return false;
            }
            
            if (FechaInicioDatePicker.SelectedDate == null)
            {
                _dialogService.Alerta("Debe seleccionar una fecha de inicio");
                return false;
            }
            
            if (FechaFinDatePicker.SelectedDate.HasValue && 
                FechaFinDatePicker.SelectedDate.Value <= FechaInicioDatePicker.SelectedDate.Value)
            {
                _dialogService.Alerta("La fecha de fin debe ser posterior a la fecha de inicio");
                return false;
            }
            
            return true;
        }

        private TimeSpan? ParsearHora(string texto)
        {
            if (string.IsNullOrWhiteSpace(texto)) return null;

            // Limpiar el texto
            texto = texto.Trim().Replace(" ", "").ToUpper();
            
            // Intentar parsear directamente como TimeSpan
            if (TimeSpan.TryParse(texto, out var timeSpan))
                return timeSpan;

            // Intentar parsear con formato AM/PM
            var patron = @"^(\d{1,2})(?::(\d{2}))?\s*(AM|PM)?$";
            var match = System.Text.RegularExpressions.Regex.Match(texto, patron, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            
            if (match.Success)
            {
                if (!int.TryParse(match.Groups[1].Value, out var hora)) return null;
                var minuto = match.Groups[2].Success && int.TryParse(match.Groups[2].Value, out var m) ? m : 0;
                var ampm = match.Groups[3].Value.ToUpper();

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
                    if (hora < 0 || hora > 23) return null;
                }

                return new TimeSpan(hora, minuto, 0);
            }

            // Intentar parsear formato simple
            if (texto.Length == 1 && int.TryParse(texto, out var h1) && h1 >= 0 && h1 <= 23)
                return new TimeSpan(h1, 0, 0);
            else if (texto.Length == 2 && int.TryParse(texto, out var h2) && h2 >= 0 && h2 <= 23)
                return new TimeSpan(h2, 0, 0);
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
                    int.TryParse(partes[1], out var minuto) &&
                    hora >= 0 && hora <= 23 && minuto >= 0 && minuto <= 59)
                    return new TimeSpan(hora, minuto, 0);
            }

            return null;
        }

        private async void EditarTaller_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidarFormulario()) return;

            var tallerActualizado = CrearTallerActualizado();

            if (!_dialogService.Confirmar("¿Desea guardar los cambios?")) return;

            try
            {
                await _tallerService.ActualizarAsync(tallerActualizado);
                Close();
                _dialogService.Info("Taller actualizado correctamente.");
            }
            catch (Exception ex)
            {
                var mensaje = ex.InnerException?.Message ?? ex.Message;
                _dialogService.Error($"Error al actualizar el taller: {mensaje}");
            }
        }

        public void Cancelar_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private Taller CrearTallerActualizado()
        {
            return new Taller
            {
                TallerId = _tallerOriginal.TallerId,
                Nombre = NombreTextBox.Text.Trim(),
                HorarioInicio = ParsearHora(HorarioInicioTextBox.Text) ?? TimeSpan.Zero,
                HorarioFin = ParsearHora(HorarioFinTextBox.Text) ?? TimeSpan.Zero,
                DiaSemana = (DayOfWeek)DiaSemanaComboBox.SelectedItem,
                FechaInicio = FechaInicioDatePicker.SelectedDate ?? DateTime.Today,
                FechaFin = FechaFinDatePicker.SelectedDate,
                CreadoEn = _tallerOriginal.CreadoEn
            };
        }
    }
}
