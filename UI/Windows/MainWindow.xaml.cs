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
        public MainWindow()
        {
            InitializeComponent();
        }

        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            if (txtPassword.Password == "admin" && txtUsuario.Text == "admin")
            {
                MessageBox.Show("¡Inicio de sesión exitoso!", "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);

                new MenuWindow().Show();
                this.Close();
            }
            else
            {
                MessageBox.Show("Usuario o contraseña inválidos.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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