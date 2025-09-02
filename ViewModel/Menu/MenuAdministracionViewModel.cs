using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ControlTalleresMVP.Abstractions;
using ControlTalleresMVP.Helpers.Dialogs;
using ControlTalleresMVP.Persistence.ModelDTO;
using ControlTalleresMVP.Persistence.Models;
using ControlTalleresMVP.Services.Generaciones;
using ControlTalleresMVP.Services.Talleres;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace ControlTalleresMVP.ViewModel.Menu
{
    public class MenuAdministracionViewModel : ObservableObject
    {
        public ObservableCollection<GeneracionDTO> Registros => _generacionService.RegistrosGeneraciones;
        public ICollectionView? RegistrosView { get; set; }
        protected readonly IGeneracionService _generacionService;
        protected readonly IDialogService _dialogService;

        public MenuAdministracionViewModel(IGeneracionService generacionService, IDialogService dialogService)
        {
            _generacionService = generacionService;
            _dialogService = dialogService;

            _generacionService.InicializarRegistros();
            InicializarVista();
        }


        private string _filtroRegistros = "";
        // Propiedad manual para controlar el refresh
        public string FiltroRegistros
        {
            get => _filtroRegistros;
            set
            {
                if (SetProperty(ref _filtroRegistros, value))
                {
                    // Refrescar la vista cuando cambie el filtro
                    RegistrosView?.Refresh();
                }
            }
        }

        public bool Filtro(object o)
        {
            if (o is GeneracionDTO dto)
            {
                if (string.IsNullOrWhiteSpace(FiltroRegistros))
                {
                    return true; // Mostrar todos si el filtro está vacío
                }
                return dto.Nombre.Contains(FiltroRegistros, StringComparison.OrdinalIgnoreCase);
            }
            return false;
        }

        protected static string Normalizar(string s)
        {
            var formD = s.Normalize(NormalizationForm.FormD);
            var stringBuilder = new StringBuilder(formD.Length);
            foreach (var ch in formD)
            {
                var unicode = CharUnicodeInfo.GetUnicodeCategory(ch);
                if (unicode != UnicodeCategory.NonSpacingMark)
                    stringBuilder.Append(char.ToLowerInvariant(ch));
            }
            return stringBuilder.ToString().Normalize(NormalizationForm.FormC);
        }

        protected void InitializeView(ObservableCollection<GeneracionDTO> registros, Predicate<object> filtro)
        {
            RegistrosView = CollectionViewSource.GetDefaultView(registros);
            RegistrosView.Filter = filtro;
        }

        protected void InicializarVista()
        {
            InitializeView(Registros, Filtro);
        }


    }
}
