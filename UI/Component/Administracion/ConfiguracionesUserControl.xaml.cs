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

namespace ControlTalleresMVP.UI.Component.Administracion
{
    /// <summary>
    /// Lógica de interacción para ConfiguracionesUserControl.xaml
    /// </summary>
    public partial class ConfiguracionesUserControl : UserControl
    {
        public ConfiguracionesUserControl()
        {
            InitializeComponent();
        }

        private void SoloNumeros_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !int.TryParse(e.Text, out _); // solo deja pasar dígitos
        }

    }
}
