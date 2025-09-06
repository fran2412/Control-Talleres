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
            if (sender is DataGrid dataGrid)
            {
                var selectedBackup = dataGrid.SelectedItem as BackupInfo;
                System.Diagnostics.Debug.WriteLine($"DataGrid_SelectionChanged - Backup seleccionado: {(selectedBackup?.FileName ?? "NINGUNO")}");
                
                if (selectedBackup != null)
                {
                    System.Diagnostics.Debug.WriteLine($"  - Archivo: {selectedBackup.FileName}");
                    System.Diagnostics.Debug.WriteLine($"  - Fecha: {selectedBackup.CreatedDate}");
                    System.Diagnostics.Debug.WriteLine($"  - Tamaño: {selectedBackup.SizeFormatted}");
                    System.Diagnostics.Debug.WriteLine($"  - Válido: {selectedBackup.IsValid}");
                }
            }
        }
    }
}
