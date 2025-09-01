using ControlTalleresMVP.Helpers.Dialogs;
using ControlTalleresMVP.Persistence.Models;
using ControlTalleresMVP.Services.Promotores;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
    /// Lógica de interacción para ContenedorFormPromotorWindow.xaml
    /// </summary>
    public partial class ContenedorFormPromotorWindow : Window
    {
        private readonly IPromotorService _promotorService;
        private readonly IDialogService _dialogService;
        private readonly Promotor _promotorOriginal;

        public ContenedorFormPromotorWindow(Promotor promotor)
        {
            _promotorService = App.ServiceProvider!.GetRequiredService<IPromotorService>();
            _dialogService = App.ServiceProvider!.GetRequiredService<IDialogService>();
            _promotorOriginal = promotor;

            InitializeComponent();

            ConfigurarValidaciones();
            CargarDatos();
        }

        private void ConfigurarValidaciones()
        {
            // Configurar validaciones usando el helper
            BaseFormHelper.ConfigurarValidacionesNombre(NombreTextBox, _dialogService);
        }

        private void CargarDatos()
        {
            NombreTextBox.Text = _promotorOriginal.Nombre ?? "";
        }

        private bool ValidarFormulario()
        {
            return BaseFormHelper.ValidarCampoObligatorio(NombreTextBox, "El nombre", _dialogService);
        }

        private async void EditarPromotor_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidarFormulario()) return;

            var promotorActualizado = CrearPromotorActualizado();

            if (!_dialogService.Confirmar("¿Desea guardar los cambios?")) return;

            try
            {
                await _promotorService.ActualizarAsync(promotorActualizado);
                Close();
                _dialogService.Info("Promotor actualizado correctamente.");
            }
            catch (Exception ex)
            {
                var mensaje = ex.InnerException?.Message ?? ex.Message;
                _dialogService.Error($"Error al actualizar el promotor: {mensaje}");
            }
        }

        public void Cancelar_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private Promotor CrearPromotorActualizado()
        {
            return new Promotor
            {
                PromotorId = _promotorOriginal.PromotorId,
                Nombre = NombreTextBox.Text.Trim(),
                CreadoEn = _promotorOriginal.CreadoEn
            };
        }
    }
}
