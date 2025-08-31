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
    public abstract class BaseMenuViewModel<TDto, TService> : INotifyPropertyChanged
    {
        public abstract ObservableCollection<TDto> Registros { get; }
        public ICollectionView? RegistrosView { get; set; }

        protected readonly TService _itemService;
        protected readonly IDialogService _dialogService;
        public BaseMenuViewModel(TService itemService, IDialogService dialogService)
        {
            _itemService = itemService;
            _dialogService = dialogService;

            InitializeView(Registros, Filtro);
        }

        private string _campoTextoNombre = "";
        public string CampoTextoNombre
        {
            get => _campoTextoNombre;
            set
            {
                if (_campoTextoNombre != value)
                {
                    _campoTextoNombre = value;
                    OnPropertyChanged(nameof(CampoTextoNombre));
                }
            }
        }

        private string _filtroRegistros = "";
        public string FiltroRegistros
        {
            get => _filtroRegistros;
            set
            {
                if (value != _filtroRegistros)
                {
                    _filtroRegistros = value;
                    OnPropertyChanged(nameof(FiltroRegistros));
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

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
