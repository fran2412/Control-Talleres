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

namespace ControlTalleresMVP.UI.Component.Clases
{
    /// <summary>
    /// Lógica de interacción para ClaseUserControl.xaml
    /// </summary>
    public partial class ClaseUserControl : UserControl
    {
        public ClaseUserControl()
        {
            InitializeComponent();
            this.DataContextChanged += ClaseUserControl_DataContextChanged;
        }

        private void ClaseUserControl_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("ClaseUserControl_DataContextChanged - DataContext cambiado");
            
            if (DataContext is MenuClaseUserControl menuClaseUserControl)
            {
                System.Diagnostics.Debug.WriteLine("ClaseUserControl_DataContextChanged - MenuClaseUserControl detectado");
                
                // Configurar DataContext para los UserControls hijos
                var formularioControl = FindName("FormularioClaseUserControl") as FormularioClaseUserControl;
                var registrosControl = FindName("RegistrosClasesUserControl") as RegistrosClasesUserControl;
                
                if (formularioControl != null)
                {
                    System.Diagnostics.Debug.WriteLine("ClaseUserControl_DataContextChanged - Configurando FormularioClaseUserControl");
                    formularioControl.DataContext = menuClaseUserControl.MenuClaseCobroVM;
                }
                
                if (registrosControl != null)
                {
                    System.Diagnostics.Debug.WriteLine("ClaseUserControl_DataContextChanged - Configurando RegistrosClasesUserControl");
                    System.Diagnostics.Debug.WriteLine($"ClaseUserControl_DataContextChanged - MenuClaseRegistrosVM: {menuClaseUserControl.MenuClaseRegistrosVM != null}");
                    registrosControl.DataContext = menuClaseUserControl.MenuClaseRegistrosVM;
                    
                    // Verificar que el comando existe
                    if (menuClaseUserControl.MenuClaseRegistrosVM != null)
                    {
                        System.Diagnostics.Debug.WriteLine($"ClaseUserControl_DataContextChanged - CancelarClaseAsyncCommand existe: {menuClaseUserControl.MenuClaseRegistrosVM.CancelarClaseAsyncCommand != null}");
                    }
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"ClaseUserControl_DataContextChanged - DataContext no es MenuClaseUserControl: {DataContext?.GetType().Name}");
            }
        }
    }
}
