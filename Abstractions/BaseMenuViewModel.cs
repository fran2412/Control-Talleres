using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
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
    public abstract partial class BaseMenuViewModel<TDto, Item, TService> :ObservableObject where TDto : ICrudDTO where TService : ICrudService<Item>
    {
        public abstract ObservableCollection<TDto> Registros { get; }
        public ICollectionView? RegistrosView { get; set; }
        protected readonly TService _itemService;
        protected readonly IDialogService _dialogService;

        public ICommand EliminarCommand { get; }
        public ICommand ActualizarCommand { get; }

        public abstract string TextGuardarItemButton { get; }
        public abstract string TituloFormulario { get; }

        public BaseMenuViewModel(TService itemService, IDialogService dialogService)
        {
            _itemService = itemService;
            _dialogService = dialogService;
            EliminarCommand = new AsyncRelayCommand<TDto>(EliminarAsync);
            ActualizarCommand = new AsyncRelayCommand<TDto>(ActualizarAsync);
        }

        [ObservableProperty]
        private string campoTextoNombre = "";

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

        private ICommand? _cancelarRegistrarItemCommand;
        public ICommand CancelarRegistrarItemCommand =>
            _cancelarRegistrarItemCommand ??= new RelayCommand(CancelarRegistro);
        protected abstract Task RegistrarItemAsync();
        protected abstract void LimpiarCampos();
        public abstract bool Filtro(object o);
        protected abstract Task ActualizarAsync(TDto? ItemSeleccionado);
        protected async Task EliminarAsync(TDto? itemSeleccionado)
        {
            if (itemSeleccionado == null) return;
            if (!_dialogService.Confirmar($"¿Está seguro de eliminar el item {itemSeleccionado.Nombre}?")) return;
            try
            {
                await _itemService.EliminarAsync(itemSeleccionado.Id);
                _dialogService.Info("Item eliminado correctamente");
            }
            catch (Exception ex)
            {
                _dialogService.Error("Error al eliminar el alumno.\n" + ex.Message);
            }
        }

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

        private void CancelarRegistro()
        {
            bool confirmar = _dialogService.Confirmar("Está seguro que desea cancelar el registro? Los datos ingresados se perderán.");

            if (confirmar)
            {
                try
                {
                    LimpiarCampos();
                    _dialogService.Info("Campos limpiados correctamente.");
                }
                catch (Exception ex)
                {
                    _dialogService.Error("Error al limpiar los campos.\n" + ex.Message);

                }            
            }
        }
    }
}
