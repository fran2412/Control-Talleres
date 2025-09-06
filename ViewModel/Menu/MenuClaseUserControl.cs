using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ControlTalleresMVP.Helpers.Dialogs;
using ControlTalleresMVP.Persistence.ModelDTO;
using ControlTalleresMVP.Persistence.Models;
using ControlTalleresMVP.Services.Clases;
using ControlTalleresMVP.Services.Configuracion;
using ControlTalleresMVP.Services.Inscripciones;
using ControlTalleresMVP.Services.Picker;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Data; // ICollectionView
using System.ComponentModel; // para ICollectionView events

namespace ControlTalleresMVP.ViewModel.Menu
{
    public partial class MenuClaseUserControl : ObservableObject
    {
        public MenuClaseCobroViewModel MenuClaseCobroVM { get; }
        public MenuClaseRegistrosViewModel MenuClaseRegistrosVM { get; }

        public MenuClaseUserControl(MenuClaseCobroViewModel cobro, MenuClaseRegistrosViewModel registro)
        {
            MenuClaseCobroVM = cobro;
            MenuClaseRegistrosVM = registro;
        }
    }
}
