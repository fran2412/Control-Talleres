using Microsoft.Extensions.DependencyInjection;
using ControlTalleresMVP.Helpers.Dialogs;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ControlTalleresMVP.UI.Windows
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly IDialogService _dialogService;
        public MainWindow(IDialogService dialogService)
        {
            InitializeComponent();
            _dialogService = dialogService;
        }

        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            if (txtPassword.Password == string.Empty || txtUsuario.Text == string.Empty)
            {
                _dialogService.Alerta("Llene ambos campos antes de continuar.");
                return;
            }
            if (txtPassword.Password == "admin" && txtUsuario.Text == "admin")
            {
                // Abrir ventana de selección de sede
                var seleccionSedeWindow = App.ServiceProvider!.GetRequiredService<SeleccionSedeWindow>();
                var resultado = seleccionSedeWindow.ShowDialog();

                if (resultado == true)
                {
                    // La sede fue seleccionada, abrir menú principal
                    new MenuWindow().Show();
                    Close();
                }
                // Si se canceló, permanecer en la ventana de login
            }
            else
            {
                _dialogService.Error("Usuario o contraseña inválidos.");
            }
        }

        private void txtPassword_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (sender is PasswordBox pb)
            {
                var placeholder = (TextBlock)pb.Template.FindName("Placeholder", pb);
                if (placeholder != null)
                    placeholder.Visibility = string.IsNullOrEmpty(pb.Password) ? Visibility.Visible : Visibility.Collapsed;
            }
        }
    }
}