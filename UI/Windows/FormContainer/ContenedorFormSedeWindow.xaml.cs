using ControlTalleresMVP.Helpers.Dialogs;
using ControlTalleresMVP.Persistence.Models;
using ControlTalleresMVP.Services.Promotores;
using ControlTalleresMVP.Services.Sedes;
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
    /// Lógica de interacción para ContenedorFormSedeWindow.xaml
    /// </summary>
    public partial class ContenedorFormSedeWindow : Window
    {
        private readonly ISedeService _sedeService;
        private readonly IDialogService _dialogService;
        private readonly Sede _sedeOriginal;

        public ContenedorFormSedeWindow(Sede sede)
        {
            _sedeService = App.ServiceProvider!.GetRequiredService<ISedeService>();
            _dialogService = App.ServiceProvider!.GetRequiredService<IDialogService>();
            _sedeOriginal = sede;

            InitializeComponent();

            ConfigurarValidaciones();
            CargarDatos();
        }

        private void ConfigurarValidaciones()
        {
            // Configurar validaciones usando el helper
            BaseFormHelper.FormatearTextoTitle(NombreTextBox, _dialogService);
        }

        private void CargarDatos()
        {
            NombreTextBox.Text = _sedeOriginal.Nombre ?? "";
        }

        private bool ValidarFormulario()
        {
            return BaseFormHelper.ValidarCampoObligatorio(NombreTextBox, "El nombre", _dialogService);
        }

        private async void EditarSede_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidarFormulario()) return;

            var sedeActualizada = CrearSedeActualizada();

            if (!_dialogService.Confirmar("¿Desea guardar los cambios?")) return;

            try
            {
                await _sedeService.ActualizarAsync(sedeActualizada);
                Close();
                _dialogService.Info("Sede actualizada correctamente.");
            }
            catch (Exception ex)
            {
                var mensaje = ex.InnerException?.Message ?? ex.Message;
                _dialogService.Error($"Error al actualizar la sede: {mensaje}");
            }
        }

        public void Cancelar_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private Sede CrearSedeActualizada()
        {
            return new Sede
            {
                IdSede = _sedeOriginal.IdSede,
                Nombre = NombreTextBox.Text.Trim(),
                CreadoEn = _sedeOriginal.CreadoEn
            };
        }
    }
}
