using ControlTalleresMVP.Persistence.ModelDTO;
using ControlTalleresMVP.UI.Windows.FormContainer;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
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

namespace ControlTalleresMVP.UI.Component.Promotor
{
    /// <summary>
    /// Lógica de interacción para RegistrosPromotorUserControl.xaml
    /// </summary>
    public partial class RegistrosPromotorUserControl : UserControl
    {
        public RegistrosPromotorUserControl()
        {
            InitializeComponent();
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

        private void EditarButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is PromotorDTO promotorDto)
            {
                var promotor = new Persistence.Models.Promotor
                {
                    PromotorId = promotorDto.Id,
                    Nombre = promotorDto.Nombre,
                    Telefono = promotorDto.Telefono,
                };

                new ContenedorFormPromotorWindow(promotor).ShowDialog();

            }
        }

    }
}
