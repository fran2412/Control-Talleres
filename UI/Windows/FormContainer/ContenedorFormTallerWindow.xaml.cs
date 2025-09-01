using ControlTalleresMVP.Helpers.Dialogs;
using ControlTalleresMVP.Persistence.Models;
using ControlTalleresMVP.Services.Sedes;
using ControlTalleresMVP.Services.Talleres;
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
            BaseFormHelper.ConfigurarTitleCase(HorarioTextBox);
            BaseFormHelper.ConfigurarCerrarConEscape(this);
        }

        private void CargarDatos()
        {
            NombreTextBox.Text = _tallerOriginal.Nombre ?? "";
            HorarioTextBox.Text = _tallerOriginal.Horario ?? "";
        }

        private bool ValidarFormulario()
        {
            return BaseFormHelper.ValidarCampoObligatorio(NombreTextBox, "El nombre", _dialogService) && BaseFormHelper.ValidarCampoObligatorio(HorarioTextBox, "El horario", _dialogService);
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
                Horario = HorarioTextBox.Text.Trim(),
                CreadoEn = _tallerOriginal.CreadoEn
            };
        }
    }
}
