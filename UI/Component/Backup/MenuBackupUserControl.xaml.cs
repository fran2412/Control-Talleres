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
using System.Windows.Navigation;
using System.Windows.Shapes;
using ControlTalleresMVP.ViewModel.Menu;

namespace ControlTalleresMVP.UI.Component.Backup
{
    /// <summary>
    /// Lógica de interacción para MenuBackupUserControl.xaml
    /// </summary>
    public partial class MenuBackupUserControl : UserControl
    {
        public MenuBackupUserControl()
        {
            InitializeComponent();
            this.DataContextChanged += MenuBackupUserControl_DataContextChanged;
        }

        private void MenuBackupUserControl_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (DataContext is MenuBackupViewModel menuBackupViewModel)
            {
                // Configurar DataContext para el UserControl hijo
                if (BackupUserControl != null)
                {
                    BackupUserControl.DataContext = menuBackupViewModel;
                }
            }
        }
    }
}
