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
            
            // Configurar el foco inicial en el TextBox de usuario
            this.Loaded += MainWindow_Loaded;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // Dar foco al TextBox de usuario y seleccionar todo el texto
            txtUsuario.Focus();
            txtUsuario.SelectAll();
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

                new MenuWindow().Show();
                this.Close();
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

        private void txtUsuario_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                // Siempre permitir Enter para iniciar sesión y recibir retroalimentación
                LoginButton_Click(sender, e);
            }
        }

        private void txtPassword_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                // Siempre permitir Enter para iniciar sesión y recibir retroalimentación
                LoginButton_Click(sender, e);
            }
        }
    }
}