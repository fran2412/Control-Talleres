using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ControlTalleresMVP.Helpers.Dialogs;
using ControlTalleresMVP.Persistence.ModelDTO;
using ControlTalleresMVP.Services.Configuracion;
using ControlTalleresMVP.Services.Generaciones;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Data;

namespace ControlTalleresMVP.ViewModel.Menu
{
    public partial class MenuAdministracionViewModel : ObservableObject
    {

        public ObservableCollection<GeneracionDTO> Registros => _generacionService.RegistrosGeneraciones;
        public ICollectionView? RegistrosView { get; set; }

        protected readonly IGeneracionService _generacionService;
        protected readonly IDialogService _dialogService;
        private readonly IConfiguracionService _configuracionService;

        public MenuBackupViewModel MenuBackupVM { get; }

        public string TituloEncabezado { get; set; } = "Administración del sistema";

        private int CostoInscripcionAlmacenado;
        private int CostoPorClaseAlmacenado;

        [ObservableProperty]
        private string costoInscripcion = "";

        [ObservableProperty]
        private string costoPorClase = "";

        public static readonly int CostoClaseMinimo = 50;
        public static readonly int CostoInscripcionMinimo = 100;
        public static readonly int CostoInscripcionMaximo = 2000;

        public static readonly int CostoDefaultInscripcion = 600;
        public static readonly int CostoDefaultClase = 150;

        public MenuAdministracionViewModel(IGeneracionService generacionService, IConfiguracionService configuracionService, IDialogService dialogService, MenuBackupViewModel menuBackupVM)
        {
            _generacionService = generacionService;
            _dialogService = dialogService;
            _configuracionService = configuracionService;
            MenuBackupVM = menuBackupVM;

            _generacionService.InicializarRegistros();
            InicializarVista(Registros, Filtro);
            InicializarCostos();
        }

        [ObservableProperty]
        private string filtroRegistros = "";
        partial void OnFiltroRegistrosChanged(string value)
        {
            RegistrosView?.Refresh();
        }

        public bool Filtro(object o)
        {
            if (o is not GeneracionDTO dto) return false;

            if (string.IsNullOrWhiteSpace(FiltroRegistros)) return true;

            return dto.Nombre.Contains(FiltroRegistros, StringComparison.OrdinalIgnoreCase);
        }

        protected void InicializarVista(ObservableCollection<GeneracionDTO> registros, Predicate<object> filtro)
        {
            RegistrosView = CollectionViewSource.GetDefaultView(registros);
            RegistrosView.Filter = filtro;
        }

        [RelayCommand]
        private void GuardarCostoInscripcion()
        {
            if (!int.TryParse(CostoInscripcion, out int valor))
            {
                _dialogService.Alerta("El costo de inscripción debe ser un número entero válido.");
                return;
            }

            if (valor < CostoInscripcionMinimo || valor > CostoInscripcionMaximo)
            {
                _dialogService.Alerta($"El costo de inscripción debe estar entre ${CostoInscripcionMinimo} y ${CostoInscripcionMaximo}.");
                return;
            }

            if (!ConfirmarActualizacionCosto(nombreCampo: "costo de inscripción", nuevoValor: valor)) return;

            bool resultadoActualizarValor = _configuracionService.SetValorSede("costo_inscripcion", valor.ToString());

            if (!resultadoActualizarValor)
            {
                _dialogService.Error("No se pudo actualizar el costo de inscripción. Por favor, inténtalo de nuevo.");
                return;
            }

            _dialogService.Info("Costo de inscripción actualizado correctamente.");
            InicializarCostoPorInscripcion();
        }

        [RelayCommand]
        private void GuardarCostoClase()
        {
            if (!int.TryParse(CostoPorClase, out int valor))
            {
                _dialogService.Alerta("El costo por clase debe ser un número entero válido.");
                return;
            }

            if (valor < CostoClaseMinimo || valor > CostoInscripcionAlmacenado)
            {
                _dialogService.Alerta($"El costo por clase debe de ser mayor a ${CostoClaseMinimo} y menor a ${CostoInscripcionAlmacenado}.");
                return;
            }

            if (!ConfirmarActualizacionCosto(nombreCampo: "costo por clase", nuevoValor: valor)) return;

            bool resultadoActualizarValor = _configuracionService.SetValorSede("costo_clase", valor.ToString());

            if (!resultadoActualizarValor)
            {
                _dialogService.Error("No se pudo actualizar el costo por clase. Por favor, inténtalo de nuevo.");
                return;
            }

            _dialogService.Info("Costo por clase actualizado correctamente.");
            InicializarCostoPorClase();
        }

        private bool ConfirmarActualizacionCosto(string nombreCampo, int nuevoValor)
        {
            return _dialogService.ConfirmarOkCancel($"¿Estás seguro de que deseas actualizar el {nombreCampo} a {nuevoValor}?", "Confirmar actualización");
        }

        private void InicializarCostos()
        {
            InicializarCostoPorInscripcion();
            InicializarCostoPorClase();
        }

        private void InicializarCostoPorInscripcion()
        {
            CostoInscripcionAlmacenado = _configuracionService.GetValorSede<int>(clave: "costo_inscripcion", valorPorDefecto: CostoDefaultInscripcion);
            CostoInscripcion = CostoInscripcionAlmacenado.ToString();
        }

        private void InicializarCostoPorClase()
        {
            CostoPorClaseAlmacenado = _configuracionService.GetValorSede<int>(clave: "costo_clase", valorPorDefecto: CostoDefaultClase);
            CostoPorClase = CostoPorClaseAlmacenado.ToString();
        }
    }
}
