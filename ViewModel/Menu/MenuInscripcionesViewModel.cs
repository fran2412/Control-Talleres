using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ControlTalleresMVP.Helpers.Dialogs;
using ControlTalleresMVP.Persistence.ModelDTO;
using ControlTalleresMVP.Persistence.Models;
using ControlTalleresMVP.Services.Configuracion;
using ControlTalleresMVP.Services.Inscripciones;
using ControlTalleresMVP.Services.Picker;
using ControlTalleresMVP.Services.Talleres;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace ControlTalleresMVP.ViewModel.Menu
{
    public partial class MenuInscripcionesViewModel : ObservableObject
    {
        public string TituloEncabezado { get; set; } = "Gestión de Inscripciones";

        public ObservableCollection<InscripcionDTO> Registros => _inscripcionService.RegistrosInscripciones;
        public ICollectionView? RegistrosView { get; set; }

        public MenuInscripcionRegistrosViewModel MenuInscripcionRegistrosVM { get; }

        private readonly IInscripcionService _inscripcionService;
        private readonly ITallerService _tallerService;
        private readonly IConfiguracionService _configuracionService; // opcional
        private readonly IAlumnoPickerService _alumnoPicker;
        private readonly IDialogService _dialogService;

        // ========== Constructor ==========
        public MenuInscripcionesViewModel(
            IInscripcionService inscripcionService,
            ITallerService tallerService,
            IConfiguracionService configuracionService,
            IAlumnoPickerService alumnoPicker,
            IDialogService dialogService,
            MenuInscripcionRegistrosViewModel menuInscripcionRegistrosVM)
        {
            System.Diagnostics.Debug.WriteLine("Inicializando MenuInscripcionesViewModel");
            _inscripcionService = inscripcionService;
            _tallerService = tallerService;
            _configuracionService = configuracionService;
            _alumnoPicker = alumnoPicker;
            _dialogService = dialogService;
            MenuInscripcionRegistrosVM = menuInscripcionRegistrosVM;

            TalleresDisponibles.CollectionChanged += TalleresDisponibles_CollectionChanged;

            _inscripcionService.InicializarRegistros();
            InicializarVista();

            // Carga inicial
            _ = CargarTalleresAsync();
        }

        // ========== Campos/props de Alumno ==========
        [ObservableProperty]
        private int? alumnoSeleccionadoId;

        [ObservableProperty]
        private string? campoTextoNombre; // mostrado en el TextBox (solo lectura)

        // ========== Lista de talleres a inscribir ==========
        public ObservableCollection<TallerInscripcionDTO> TalleresDisponibles { get; } = new();

        // ========== Totales ==========
        [ObservableProperty] private decimal totalCostoSeleccionado;
        [ObservableProperty] private decimal totalAbonoSeleccionado;
        [ObservableProperty] private decimal totalSaldoSeleccionado;

        // (Opcional) notas generales
        [ObservableProperty] private string? notasInscripcion;

        // ========== Comandos ==========

        [RelayCommand]
        private void BuscarAlumno()
        {
            var alumno = _alumnoPicker.Pick(); // abre tu ventana con el UC
            if (alumno is null) return;

            AlumnoSeleccionadoId = alumno.AlumnoId;
            CampoTextoNombre = alumno.Nombre;
        }

        [RelayCommand]
        private void CancelarRegistrarItem()
        {
            if (!_dialogService.Confirmar("¿Cancelar y limpiar el formulario?")) return;
            LimpiarCampos();
            _dialogService.Info("Formulario limpiado.");
        }

        [RelayCommand]
        private async Task RegistrarItemAsync()
        {
            // Validaciones
            if (AlumnoSeleccionadoId is null || AlumnoSeleccionadoId <= 0)
            {
                _dialogService.Alerta("Debes seleccionar un alumno.");
                return;
            }

            var seleccionados = TalleresDisponibles.Where(t => t.EstaSeleccionado).ToList();
            if (seleccionados.Count == 0)
            {
                _dialogService.Alerta("Selecciona al menos un taller para inscribir.");
                return;
            }

            if (_dialogService.Confirmar($"¿Confirmas inscribir a {CampoTextoNombre} en {seleccionados.Count} taller(es)?") != true)
                return;

            int ok = 0;
            var errores = new System.Collections.Generic.List<string>();

            foreach (var t in seleccionados)
            {
                try
                {
                    await _inscripcionService.InscribirAsync(
                        alumnoId: AlumnoSeleccionadoId.Value,
                        tallerId: t.TallerId,
                        abonoInicial: t.Abono,
                        fecha: DateTime.Now
                    );
                    ok++;
                }
                catch (Exception ex)
                {
                    errores.Add($"{t.Nombre}: {ex.Message}");
                }
            }

            if (ok > 0)
                _dialogService.Info($"Se registraron {ok} inscripción(es) correctamente.");

            if (errores.Count > 0)
                _dialogService.Alerta("Algunas inscripciones no se pudieron procesar:\n" + string.Join("\n", errores));

            if (ok > 0 && errores.Count == 0)
                LimpiarCampos();
        }

        // ========== Métodos privados ==========
        private async Task CargarTalleresAsync()
        {
            try
            {
                TalleresDisponibles.Clear();

                // Obtén talleres activos desde tu servicio real
                var talleres = await _tallerService.ObtenerTalleresParaGridAsync();

                // Costo base desde configuración (si todos comparten costo)
                var costoBase = _configuracionService.GetValor<int>("costo_inscripcion", 600);

                foreach (var t in talleres)
                {
                    var dto = new TallerInscripcionDTO
                    {
                        TallerId = t.Id,
                        Nombre = t.Nombre,
                        Costo = costoBase,
                        Abono = 0,
                        EstaSeleccionado = false
                    };
                    dto.PropertyChanged += TallerDTO_PropertyChanged;
                    TalleresDisponibles.Add(dto);
                }

                RecalcularTotales();
            }
            catch (Exception ex)
            {
                _dialogService.Error("No fue posible cargar los talleres.\n" + ex.Message);
            }
        }

        [RelayCommand]
        private async Task CancelarInscripcionAsync(int inscripcionId)
        {
            if (!_dialogService.Confirmar("¿Seguro que deseas cancelar esta inscripción?")) return;

            try
            {
                await _inscripcionService.CancelarAsync(inscripcionId);

                // Refrescar la lista después de cancelar
                await _inscripcionService.InicializarRegistros();

                _dialogService.Info("Inscripción cancelada correctamente.");
            }
            catch (Exception ex)
            {
                _dialogService.Error("No se pudo cancelar la inscripción.\n" + ex.Message);
            }
        }

        private void TalleresDisponibles_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.OldItems != null)
                foreach (TallerInscripcionDTO dto in e.OldItems)
                    dto.PropertyChanged -= TallerDTO_PropertyChanged;

            if (e.NewItems != null)
                foreach (TallerInscripcionDTO dto in e.NewItems)
                    dto.PropertyChanged += TallerDTO_PropertyChanged;

            RecalcularTotales();
        }

        private void TallerDTO_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            // Nos interesan cambios que afectan totales
            if (e.PropertyName == nameof(TallerInscripcionDTO.EstaSeleccionado) ||
                e.PropertyName == nameof(TallerInscripcionDTO.Abono) ||
                e.PropertyName == nameof(TallerInscripcionDTO.Costo))
            {
                RecalcularTotales();
            }
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
            if (o is InscripcionDTO dto)
            {
                if (string.IsNullOrWhiteSpace(FiltroRegistros))
                    return true;

                return (dto.Nombre?.Contains(FiltroRegistros, StringComparison.OrdinalIgnoreCase) == true)
                    || (dto.Taller?.Contains(FiltroRegistros, StringComparison.OrdinalIgnoreCase) == true)
                    || dto.Estado.ToString().Contains(FiltroRegistros, StringComparison.OrdinalIgnoreCase);
            }
            return false;
        }


        private void RecalcularTotales()
        {
            var sel = TalleresDisponibles.Where(x => x.EstaSeleccionado).ToList();

            TotalCostoSeleccionado = sel.Sum(x => x.Costo);
            TotalAbonoSeleccionado = sel.Sum(x => x.Abono);
            TotalSaldoSeleccionado = sel.Sum(x => x.SaldoPendiente);
        }

        private void LimpiarCampos()
        {
            AlumnoSeleccionadoId = null;
            CampoTextoNombre = string.Empty;
            NotasInscripcion = string.Empty;

            foreach (var t in TalleresDisponibles)
            {
                t.EstaSeleccionado = false;
                t.Abono = 0;
                // t.Costo se queda como esté configurado
            }

            RecalcularTotales();
        }

        protected void InicializarVista()
        {
            InitializeView(Registros, Filtro);
        }

        protected void InitializeView(ObservableCollection<InscripcionDTO> registros, Predicate<object> filtro)
        {
            RegistrosView = CollectionViewSource.GetDefaultView(registros);
            RegistrosView.Filter = filtro;
        }

    }
}
