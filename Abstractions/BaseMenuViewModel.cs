using CommunityToolkit.Mvvm.ComponentModel;
using ControlTalleresMVP.Helpers.Dialogs;
using ControlTalleresMVP.Persistence.ModelDTO;
using ControlTalleresMVP.Services.Alumnos;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Input;

namespace ControlTalleresMVP.Abstractions
{
    public abstract partial class BaseMenuViewModel<TDto, TService> : ObservableObject
    {
        public abstract ObservableCollection<TDto> Registros { get; }
        public ICollectionView? RegistrosView { get; set; }
        protected readonly TService _itemService;
        protected readonly IDialogService _dialogService;

        public BaseMenuViewModel(TService itemService, IDialogService dialogService)
        {
            _itemService = itemService;
            _dialogService = dialogService;
        }

        [ObservableProperty]
        private string campoTextoNombre = "";

        private string _filtroRegistros = "";
        // ✅ CORREGIDO: Propiedad manual para controlar el refresh
        public string FiltroRegistros
        {
            get => _filtroRegistros;
            set
            {
                if (SetProperty(ref _filtroRegistros, value))
                {
                    // ✅ CORREGIDO: Refrescar la vista cuando cambie el filtro
                    RegistrosView?.Refresh();
                }
            }
        }

        protected abstract Task RegistrarItemAsync();
        protected abstract void LimpiarCampos();
        public abstract bool Filtro(object o);

        protected static string Normalizar(string s)
        {
            var formD = s.Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder(formD.Length);
            foreach (var ch in formD)
            {
                var uc = CharUnicodeInfo.GetUnicodeCategory(ch);
                if (uc != UnicodeCategory.NonSpacingMark)
                    sb.Append(char.ToLowerInvariant(ch));
            }
            return sb.ToString().Normalize(NormalizationForm.FormC);
        }

        protected void InitializeView(ObservableCollection<TDto> registros, Predicate<object> filtro)
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
