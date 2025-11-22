using ControlTalleresMVP.Helpers.Dialogs;
using ControlTalleresMVP.ViewModel.Navigation;
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
using System.Windows.Threading;

namespace ControlTalleresMVP.UI.Windows
{
    /// <summary>
    /// Lógica de interacción para MenuWindow.xaml
    /// </summary>
    public partial class MenuWindow : Window
    {

        private DispatcherTimer? _timerInactividad;

        public MenuWindow()
        {
            InitializeComponent();
            DataContext = App.ServiceProvider!.GetRequiredService<ShellViewModel>();

            InicializarTemporizador();
            RegistrarActividadGlobal();
        }

        private void InicializarTemporizador()
        {
            _timerInactividad = new DispatcherTimer
            {
                Interval = TimeSpan.FromMinutes(5)
            };
            _timerInactividad.Tick += (_, __) => VolverAlLogin();
            _timerInactividad.Start();
        }

        private void RegistrarActividadGlobal()
        {
            InputManager.Current.PreProcessInput += (_, __) =>
            {
                _timerInactividad?.Stop();
                _timerInactividad?.Start();
            };
        }

        private void VolverAlLogin()
        {
            try
            {
                _timerInactividad?.Stop();

                var ventanaLogin = App.ServiceProvider!.GetRequiredService<MainWindow>();

                if (!ventanaLogin.IsVisible)
                {
                    ventanaLogin = new MainWindow(
                        App.ServiceProvider!.GetRequiredService<IDialogService>()
                    );
                }

                ventanaLogin.Show();
                ventanaLogin.Activate();

                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"No se pudo volver al login: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
