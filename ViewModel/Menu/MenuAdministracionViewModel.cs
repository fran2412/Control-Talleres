using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ControlTalleresMVP.Abstractions;
using ControlTalleresMVP.Helpers.Dialogs;
using ControlTalleresMVP.Persistence.ModelDTO;
using ControlTalleresMVP.Persistence.Models;
using ControlTalleresMVP.Services.Configuracion;
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
    public partial class MenuAdministracionViewModel : ObservableObject
    {
        public string TituloEncabezado { get; set; } = "Administración del sistema";

        public ObservableCollection<GeneracionDTO> Registros => _generacionService.RegistrosGeneraciones;
        public ICollectionView? RegistrosView { get; set; }
        protected readonly IGeneracionService _generacionService;
        protected readonly IDialogService _dialogService;
        private readonly IConfiguracionService _configuracionService;

        [ObservableProperty]
        private string costoInscripcion = "";

        [ObservableProperty]
        private string costoPorClase = "";

        public MenuAdministracionViewModel(IGeneracionService generacionService, IConfiguracionService configuracionService, IDialogService dialogService)
        {
            _generacionService = generacionService;
            _dialogService = dialogService;
            _configuracionService = configuracionService;

            _generacionService.InicializarRegistros();
            InicializarVista();
            CostoInscripcion = _configuracionService.GetValor<int>("costo_inscripcion", 600).ToString();
            CostoPorClase = _configuracionService.GetValor<int>("costo_clase", 150).ToString();
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

        [RelayCommand]
        private void GuardarCostoInscripcion()
        {
            if (!int.TryParse(CostoInscripcion, out int valor))
            {
                // Aquí podrías usar tu IDialogService en lugar de Exception
                throw new InvalidOperationException("El costo de inscripción debe ser un número entero válido.");
            }

            if (valor < 400 || valor > 1400)
            {
                _dialogService.Alerta($"El costo de inscripción debe estar entre 400 y 1400.");
                return;
            }

            bool confirmacion = _dialogService.ConfirmarOkCancel($"¿Estás seguro de que deseas actualizar el costo de inscripción a {valor}?", "Confirmar actualización");

            if (confirmacion == false)
            {
                return;
            }


            _configuracionService.SetValor("costo_inscripcion", valor.ToString());

            _dialogService.Info("Costo de inscripción actualizado correctamente.");
        }

        [RelayCommand]
        private void GuardarCostoClase()
        {
            if (!int.TryParse(CostoPorClase, out int valor))
            {
                // Aquí podrías usar tu IDialogService en lugar de Exception
                throw new InvalidOperationException("El costo por clase debe ser un número entero válido.");
            }

            if (valor < 100 || valor > 300)
            {
                _dialogService.Alerta($"El costo por clase debe estar entre 100 y 300.");
                return;
            }

            bool confirmacion = _dialogService.ConfirmarOkCancel($"¿Estás seguro de que deseas actualizar el costo por clase a {valor}?", "Confirmar actualización");

            if (confirmacion == false)
            {
                return;
            }


            _configuracionService.SetValor("costo_clase", valor.ToString());

            _dialogService.Info("Costo por clase actualizado correctamente.");
        }
        protected void InicializarVista()
        {
            InitializeView(Registros, Filtro);
        }


    }
}
