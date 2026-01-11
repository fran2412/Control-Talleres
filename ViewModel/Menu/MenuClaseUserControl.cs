using CommunityToolkit.Mvvm.ComponentModel;

namespace ControlTalleresMVP.ViewModel.Menu
{
    public partial class MenuClaseUserControl : ObservableObject
    {
        public string TituloEncabezado { get; set; } = "Gestión de clases";

        public MenuClaseCobroViewModel MenuClaseCobroVM { get; }
        public MenuClaseRegistrosViewModel MenuClaseRegistrosVM { get; }

        public MenuClaseUserControl(MenuClaseCobroViewModel cobro, MenuClaseRegistrosViewModel registro)
        {
            MenuClaseCobroVM = cobro;
            MenuClaseRegistrosVM = registro;
        }
    }
}
