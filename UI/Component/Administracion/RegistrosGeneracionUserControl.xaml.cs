using ControlTalleresMVP.Helpers.Dialogs;
using ControlTalleresMVP.Persistence.ModelDTO;
using ControlTalleresMVP.Persistence.Models;
using ControlTalleresMVP.Services.Generaciones;
using ControlTalleresMVP.UI.Windows.FormContainer;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ControlTalleresMVP.UI.Component.Administracion
{
    /// <summary>
    /// Lógica de interacción para RegistrosGeneracionUserControl.xaml
    /// </summary>
    public partial class RegistrosGeneracionUserControl : UserControl
    {
        private readonly IDialogService? _dialogService;
        private readonly IGeneracionService? _generacionService;

        public RegistrosGeneracionUserControl()
        {
            InitializeComponent();

            // Evitar DI cuando el XAML Designer está renderizando
            if (!DesignerProperties.GetIsInDesignMode(this))
            {
                    _dialogService     = App.ServiceProvider!.GetRequiredService<IDialogService>();
                    _generacionService = App.ServiceProvider!.GetRequiredService<IGeneracionService>();
            }
        }

        private void DataGrid_AutoGeneratingColumn(object sender, DataGridAutoGeneratingColumnEventArgs e)
        {
            var propertyDescriptor = e.PropertyDescriptor as System.ComponentModel.PropertyDescriptor;
            var tipo = propertyDescriptor?.ComponentType; // Aquí obtienes el tipo de la fila

            if (tipo == null)
                return;

            var property = tipo.GetProperty(e.PropertyName);
            if (property == null)
                return;

            // Ocultar si tiene [ScaffoldColumn(false)]
            var scaffoldAttribute = property.GetCustomAttributes(typeof(ScaffoldColumnAttribute), true)
                                   .OfType<ScaffoldColumnAttribute>()
                                   .FirstOrDefault();
            if (scaffoldAttribute != null && scaffoldAttribute.Scaffold == false)
            {
                e.Cancel = true;
                return;
            }

            // Cambiar encabezado con [Display(Name=...)]
            var displayAttribute = property.GetCustomAttributes(typeof(DisplayAttribute), true)
                                  .OfType<DisplayAttribute>()
                                  .FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(displayAttribute?.Name))
                e.Column.Header = displayAttribute.Name;

            // Formatear con [DisplayFormat(DataFormatString=...)]
            var formatAttribute = property.GetCustomAttributes(typeof(DisplayFormatAttribute), true)
                                 .OfType<DisplayFormatAttribute>()
                                 .FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(formatAttribute?.DataFormatString)
                && e.Column is DataGridBoundColumn boundCol)
            {
                if (boundCol.Binding is Binding binding)
                {
                    binding.StringFormat = formatAttribute.DataFormatString;
                }
                else
                {
                    boundCol.Binding = new Binding(e.PropertyName) { StringFormat = formatAttribute.DataFormatString };
                }
            }

            if (e.Column is DataGridTextColumn textColumn)
            {
                var style = new Style(typeof(TextBlock));
                style.Setters.Add(new Setter(TextBlock.TextWrappingProperty, TextWrapping.Wrap));
                style.Setters.Add(new Setter(TextBlock.VerticalAlignmentProperty, VerticalAlignment.Center));
                e.Column.Width = new DataGridLength(1, DataGridLengthUnitType.Star);

                textColumn.ElementStyle = style;
            }
        }

        private async void IniciarGeneracionButton_Click(object sender, RoutedEventArgs e)
        {
            string? input = _dialogService!.PedirTexto(
                    "ATENCIÓN\n\n" +
                    "Para iniciar una NUEVA GENERACIÓN debe escribir: CONFIRMAR\n\n" +
                    "Este proceso:\n" +
                    " - Dará FIN al ciclo anterior\n" +
                    " - Creará un NUEVO ciclo\n\n" +
                    "Solo confirme si está completamente seguro de continuar.",
                    "Confirmación crítica");

            if (input != "CONFIRMAR")
            {
                _dialogService.Info("Operación cancelada. No escribió CONFIRMAR.");
                return;
            }

            try
            {
                await _generacionService!.NuevaGeneracion();
                _dialogService.Info("Se inició la nueva generación correctamente.");
            }
            catch (Exception ex)
            {
                _dialogService.Error($"No fue posible iniciar la nueva generación.\n{ex.Message}");
            }
        }
    }
}
