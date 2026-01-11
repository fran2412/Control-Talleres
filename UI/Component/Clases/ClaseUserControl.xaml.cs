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

            if (DataContext is MenuClaseUserControl menuClaseUserControl)
            {

                // Configurar DataContext para los UserControls hijos
                var formularioControl = FindName("FormularioClaseUserControl") as FormularioClaseUserControl;
                var registrosControl = FindName("RegistrosClasesUserControl") as RegistrosClasesUserControl;

                if (formularioControl != null)
                {
                    formularioControl.DataContext = menuClaseUserControl.MenuClaseCobroVM;
                }

                if (registrosControl != null)
                {
                    registrosControl.DataContext = menuClaseUserControl.MenuClaseRegistrosVM;


                }
            }
        }
    }
}
