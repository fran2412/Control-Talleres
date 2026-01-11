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
using ControlTalleresMVP.Services.Backup;

namespace ControlTalleresMVP.UI.Component.Backup
{
    /// <summary>
    /// Lógica de interacción para BackupUserControl.xaml
    /// </summary>
    public partial class BackupUserControl : UserControl
    {
        public BackupUserControl()
        {
            InitializeComponent();
        }

        private void DataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }
    }
}
