using ControlTalleresMVP.Helpers.Dialogs;
using ControlTalleresMVP.ViewModel.Navigation;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace ControlTalleresMVP.UI.Windows
{
    /// <summary>
    /// Lógica de interacción para MenuWindow.xaml
    /// </summary>
    public partial class MenuWindow : Window
    {
        private readonly IDialogService _dialogService;
        private DispatcherTimer? _timerInactividad;
        private PreProcessInputEventHandler? _actividadHandler;

        public MenuWindow()
        {
            InitializeComponent();
            DataContext = App.ServiceProvider!.GetRequiredService<ShellViewModel>();
            _dialogService = App.ServiceProvider!.GetRequiredService<IDialogService>();

            InicializarTemporizador();
            RegistrarActividadGlobal();
        }

        private void InicializarTemporizador()
        {
            _timerInactividad = new DispatcherTimer
            {
                Interval = TimeSpan.FromMinutes(5),
            };
            _timerInactividad.Tick += TimerInactividad_Tick;
            _timerInactividad.Start();
        }

        private void TimerInactividad_Tick(object? sender, EventArgs e)
            => VolverAlLogin(true);

        private void RegistrarActividadGlobal()
        {
            _actividadHandler = InputManager_PreProcessInput;
            InputManager.Current.PreProcessInput += _actividadHandler;
        }

        private void InputManager_PreProcessInput(object sender, PreProcessInputEventArgs e)
            => ReiniciarTemporizador();

        private void ReiniciarTemporizador()
        {
            if (_timerInactividad is null) return;

            _timerInactividad.Stop();
            _timerInactividad.Start();
        }

        private void VolverAlLogin(bool porInactividad)
        {
            try
            {
                _timerInactividad?.Stop();

                var ventanaLogin = App.ServiceProvider!.GetRequiredService<MainWindow>();

                if (!ventanaLogin.IsVisible)
                {
                    ventanaLogin = new MainWindow(_dialogService);
                }

                ventanaLogin.Show();
                ventanaLogin.Activate();

                Close();

                if (porInactividad)
                {
                    _dialogService.Info("Su sesión se ha cerrado debido a inactividad. Por favor, inicie sesión nuevamente.", "Sesión cerrada");
                    return;
                }

            }
            catch (Exception ex)
            {
                MessageBox.Show($"No se pudo volver al login: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
        }

        private void BotonCerrarSesion_Click(object sender, RoutedEventArgs e)
        {
            VolverAlLogin(false);
        }

        protected override void OnClosed(EventArgs e)
        {
            if (_actividadHandler is not null)
            {
                InputManager.Current.PreProcessInput -= _actividadHandler;
                _actividadHandler = null;
            }

            if (_timerInactividad is not null)
            {
                _timerInactividad.Tick -= TimerInactividad_Tick;
                _timerInactividad.Stop();
                _timerInactividad = null;
            }

            base.OnClosed(e);
        }
    }
}
