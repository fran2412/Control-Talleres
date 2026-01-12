using ControlTalleresMVP.ViewModel.Sesion;
using System.Windows;

namespace ControlTalleresMVP.UI.Windows
{
    /// <summary>
    /// Ventana para seleccionar la sede después del login.
    /// </summary>
    public partial class SeleccionSedeWindow : Window
    {
        private readonly SeleccionSedeViewModel _viewModel;

        public SeleccionSedeWindow(SeleccionSedeViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            DataContext = _viewModel;

            Loaded += SeleccionSedeWindow_Loaded;
        }

        private async void SeleccionSedeWindow_Loaded(object sender, RoutedEventArgs e)
        {
            await _viewModel.CargarSedesAsync();
        }

        private void BtnContinuar_Click(object sender, RoutedEventArgs e)
        {
            if (_viewModel.Confirmado)
            {
                DialogResult = true;
                Close();
            }
        }
    }
}
