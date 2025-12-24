using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ControlTalleresMVP.Helpers.Dialogs;
using ControlTalleresMVP.Persistence.ModelDTO;
using ControlTalleresMVP.Services.Configuracion;
using ControlTalleresMVP.Services.Generaciones;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Text;
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

        // ViewModel para el sistema de backup
        public MenuBackupViewModel MenuBackupVM { get; }

        [ObservableProperty]
        private string costoInscripcion = "";

        [ObservableProperty]
        private string costoPorClase = "";

        public MenuAdministracionViewModel(IGeneracionService generacionService, IConfiguracionService configuracionService, IDialogService dialogService, MenuBackupViewModel menuBackupVM)
        {
            _generacionService = generacionService;
            _dialogService = dialogService;
            _configuracionService = configuracionService;
            MenuBackupVM = menuBackupVM;

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
                    return true; // Mostrar si el filtro está vacío
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

            if (valor < 100 || valor > 2000)
            {
                _dialogService.Alerta($"El costo de inscripción debe estar entre $100 y $2,000.");
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

            if (valor < 50 || valor > 500)
            {
                _dialogService.Alerta($"El costo por clase debe estar entre $50 y $500.");
                return;
            }

            // Validar que el costo por clase no sea mayor que el costo de inscripción
            var costoInscripcion = _configuracionService.GetValor<int>("costo_inscripcion", 600);
            if (valor > costoInscripcion)
            {
                _dialogService.Alerta($"El costo por clase (${valor}) no puede ser mayor que el costo de inscripción (${costoInscripcion}).");
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
