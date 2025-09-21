using System;
using System.Collections.Generic;
using System.ComponentModel;
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

namespace ControlTalleresMVP.UI.Windows.Select
{
    /// <summary>
    /// Lógica de interacción para SeleccionarAlumnoWindow.xaml
    /// </summary>
    public partial class SeleccionarAlumnoWindow : Window, INotifyPropertyChanged
    {
        private Persistence.Models.Alumno? _selectedAlumno;
        public Persistence.Models.Alumno? SelectedAlumno
        {
            get => _selectedAlumno;
            set { _selectedAlumno = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectedAlumno))); }
        }

        public SeleccionarAlumnoWindow(object registrosViewModel)
        {
            InitializeComponent();
            // Reutiliza tu VM de registros para el UC:
            RegistrosUC.DataContext = registrosViewModel;
            DataContext = this; // solo para SelectedAlumno binding
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private void Aceptar_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedAlumno == null)
            {
                MessageBox.Show("Selecciona un alumno.");
                return;
            }
            DialogResult = true;
            Close();
        }

        private void Cancelar_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                DialogResult = false;
                Close();
            }
        }
    }
}
