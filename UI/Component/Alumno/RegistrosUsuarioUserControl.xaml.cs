using ControlTalleresMVP.Persistence.ModelDTO;
using ControlTalleresMVP.Persistence.Models;
using ControlTalleresMVP.UI.Windows.FormContainer;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
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

namespace ControlTalleresMVP.UI.Component.Alumno
{
    /// <summary>
    /// Lógica de interacción para RegistrosUsuarioUserControl.xaml
    /// </summary>
    public partial class RegistrosUsuarioUserControl : UserControl
    {
        public RegistrosUsuarioUserControl()
        {
            InitializeComponent();
            Loaded += (_, __) =>
            {
                // engancha doble click (no cambia estilos)
                if (AlumnosGrid != null)
                    AlumnosGrid.MouseDoubleClick += AlumnosGrid_MouseDoubleClick;

                // Si está en modo picker, dar foco al TextBox de búsqueda
                if (IsPickerMode && BusquedaTextBox != null)
                {
                    BusquedaTextBox.Focus();
                    BusquedaTextBox.SelectAll();
                }
            };
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

        // --- EVENTO que captura el IAlumnoPicker (retorna el alumno)
        public event Action<Persistence.Models.Alumno>? PickRequested;

        // Doble click: si está en modo selector, emite el alumno
        private void AlumnosGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (!IsPickerMode) return;

            var dep = e.OriginalSource as DependencyObject;
            var row = ItemsControl.ContainerFromElement(AlumnosGrid, dep) as DataGridRow;
            if (row?.Item is null) return;

            Persistence.Models.Alumno? alumno = row.Item switch
            {
                Persistence.Models.Alumno aModel => aModel,
                AlumnoDTO dto => MapFromDto(dto),
                _ => null
            };

            if (alumno is null) return;

            SelectedAlumno = alumno;     // actualiza el DP
            PickRequested?.Invoke(alumno); // <- el IAlumnoPicker escucha esto
        }

        // Map común para reusar
        private static Persistence.Models.Alumno MapFromDto(AlumnoDTO alumnoDto) => new Persistence.Models.Alumno
        {
            AlumnoId = alumnoDto.Id,
            Nombre = alumnoDto.Nombre,
            Telefono = alumnoDto.Telefono,
            Sede = alumnoDto.Sede,
            Promotor = alumnoDto.Promotor,
            DescuentoPorClase = alumnoDto.DescuentoPorClase
        };

        // --- Tu handler actual de editar; ignora clicks en modo selector:
        private void EditarButton_Click(object sender, RoutedEventArgs e)
        {
            if (IsPickerMode) return; // <- evita editar cuando estás seleccionando

            if (sender is Button btn && btn.DataContext is AlumnoDTO alumnoDto)
            {
                var alumno = MapFromDto(alumnoDto);
                new ContenedorFormAlumnoWindow(alumno).ShowDialog();
            }
        }

        public static readonly DependencyProperty SelectedAlumnoProperty =
        DependencyProperty.Register(
            nameof(SelectedAlumno),
            typeof(Persistence.Models.Alumno),
            typeof(RegistrosUsuarioUserControl),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        public Persistence.Models.Alumno? SelectedAlumno
        {
            get => (Persistence.Models.Alumno?)GetValue(SelectedAlumnoProperty);
            set => SetValue(SelectedAlumnoProperty, value);
        }

        // IsPickerMode
        public static readonly DependencyProperty IsPickerModeProperty =
            DependencyProperty.Register(
                nameof(IsPickerMode),
                typeof(bool),
                typeof(RegistrosUsuarioUserControl),
                new PropertyMetadata(false));

        public bool IsPickerMode
        {
            get => (bool)GetValue(IsPickerModeProperty);
            set
            {
                SetValue(IsPickerModeProperty, value);
                // Si se activa el modo picker, dar foco al TextBox de búsqueda
                if (value && BusquedaTextBox != null)
                {
                    BusquedaTextBox.Focus();
                    BusquedaTextBox.SelectAll();
                }
            }
        }
    }
}
