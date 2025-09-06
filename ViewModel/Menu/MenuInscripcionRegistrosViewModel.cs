using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using ControlTalleresMVP.Helpers.Dialogs;
using ControlTalleresMVP.Messages;
using ControlTalleresMVP.Persistence.ModelDTO;
using ControlTalleresMVP.Services.Inscripciones;
using ControlTalleresMVP.Services.Picker;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Data;

namespace ControlTalleresMVP.ViewModel.Menu
{
    public partial class MenuInscripcionRegistrosViewModel : ObservableObject
    {
        public ObservableCollection<InscripcionRegistroDTO> RegistrosInscripciones { get; } = new();
        public ICollectionView? RegistrosInscripcionesView { get; private set; }

        private readonly IInscripcionService _inscripcionService;
        private readonly IAlumnoPickerService _alumnoPicker;
        private readonly IDialogService _dialogService;

        [ObservableProperty] private int? alumnoSeleccionadoId;
        [ObservableProperty] private string? alumnoNombre;

        [ObservableProperty] private DateTime? fechaDesdeRegistros;
        [ObservableProperty] private DateTime? fechaHastaRegistros;

        private string _filtroRegistrosInscripciones = string.Empty;
        public string FiltroRegistrosInscripciones
        {
            get => _filtroRegistrosInscripciones;
            set { if (SetProperty(ref _filtroRegistrosInscripciones, value)) RegistrosInscripcionesView?.Refresh(); }
        }

        [ObservableProperty] private decimal totalMontoInscripciones;
        [ObservableProperty] private decimal totalPagadoInscripciones;
        [ObservableProperty] private decimal totalSaldoInscripciones;

        partial void OnFechaDesdeRegistrosChanged(DateTime? value)
        {
            NormalizarRangoFechas();
            _ = CargarRegistrosInscripcionesAsync();
        }

        partial void OnFechaHastaRegistrosChanged(DateTime? value)
        {
            NormalizarRangoFechas();
            _ = CargarRegistrosInscripcionesAsync();
        }

        private void NormalizarRangoFechas()
        {
            if (FechaDesdeRegistros.HasValue && FechaHastaRegistros.HasValue &&
                FechaDesdeRegistros.Value.Date > FechaHastaRegistros.Value.Date)
            {
                var tmp = FechaDesdeRegistros;
                FechaDesdeRegistros = FechaHastaRegistros;
                FechaHastaRegistros = tmp;
            }
        }

        public MenuInscripcionRegistrosViewModel(
            IInscripcionService inscripcionService,
            IAlumnoPickerService alumnoPicker,
            IDialogService dialogService)
        {
            System.Diagnostics.Debug.WriteLine("Inicializando MenuInscripcionRegistrosViewModel");
            _inscripcionService = inscripcionService;
            _alumnoPicker = alumnoPicker;
            _dialogService = dialogService;

            FechaHastaRegistros = DateTime.Today.AddDays(7);
            FechaDesdeRegistros = DateTime.Today.AddMonths(-1);

            RegistrosInscripcionesView = CollectionViewSource.GetDefaultView(RegistrosInscripciones);
            RegistrosInscripcionesView.Filter = FiltroRegistrosPredicate;
            RegistrosInscripcionesView.CurrentChanged += (_, __) => RecalcularTotales();
            
            System.Diagnostics.Debug.WriteLine("CollectionView inicializado");

            // Suscríbete a los cambios de inscripciones
            WeakReferenceMessenger.Default.Register<InscripcionesActualizadasMessage>(this, async (_, m) =>
            {
                // Siempre recargar los registros cuando se actualiza una inscripción
                await CargarRegistrosInscripcionesAsync();
            });

            _ = CargarRegistrosInscripcionesAsync();
        }

        [RelayCommand]
        private async Task CargarRegistrosInscripcionesAsync(CancellationToken ct = default)
        {
            try
            {
                var datos = await _inscripcionService.ObtenerInscripcionesCompletasAsync(
                    alumnoId: AlumnoSeleccionadoId,
                    desde: FechaDesdeRegistros,
                    hasta: FechaHastaRegistros,
                    filtro: FiltroRegistrosInscripciones,
                    ct: ct);

                RegistrosInscripciones.Clear();
                foreach (var r in datos) RegistrosInscripciones.Add(r);

                RecalcularTotales();
                
                // Debug: verificar cuántos registros se cargaron
                System.Diagnostics.Debug.WriteLine($"Cargados {datos.Count} registros de inscripciones");
            }
            catch (Exception ex)
            {
                _dialogService.Error("No fue posible cargar el registro de inscripciones.\n" + ex.Message);
            }
        }

        [RelayCommand]
        private async Task CancelarInscripcionAsync(int inscripcionId)
        {
            if (!_dialogService.Confirmar("¿Seguro que deseas cancelar esta inscripción?")) return;

            try
            {
                await _inscripcionService.CancelarAsync(inscripcionId);
                await CargarRegistrosInscripcionesAsync();
                _dialogService.Info("Inscripción cancelada correctamente.");
            }
            catch (Exception ex)
            {
                _dialogService.Error("No se pudo cancelar la inscripción.\n" + ex.Message);
            }
        }

        private bool FiltroRegistrosPredicate(object o)
        {
            if (o is not InscripcionRegistroDTO dto) return false;

            // Temporalmente simplificar el filtro para debug
            System.Diagnostics.Debug.WriteLine($"Filtrando registro: {dto.AlumnoNombre} - {dto.TallerNombre}");

            if (!string.IsNullOrWhiteSpace(FiltroRegistrosInscripciones))
            {
                var f = FiltroRegistrosInscripciones.Trim();
                if (!((dto.AlumnoNombre?.Contains(f, StringComparison.OrdinalIgnoreCase) == true)
                    || (dto.TallerNombre?.Contains(f, StringComparison.OrdinalIgnoreCase) == true)
                    || dto.EstadoTexto.Contains(f, StringComparison.OrdinalIgnoreCase)))
                    return false;
            }

            if (FechaDesdeRegistros.HasValue && dto.FechaInscripcion.Date < FechaDesdeRegistros.Value.Date) return false;
            if (FechaHastaRegistros.HasValue && dto.FechaInscripcion.Date > FechaHastaRegistros.Value.Date) return false;

            if (AlumnoSeleccionadoId.HasValue && dto.AlumnoId != AlumnoSeleccionadoId.Value) return false;

            return true;
        }

        private void RecalcularTotales()
        {
            if (RegistrosInscripcionesView is null) return;

            decimal monto = 0, pagado = 0, saldo = 0;
            foreach (var item in RegistrosInscripcionesView)
            {
                if (item is InscripcionRegistroDTO r)
                {
                    monto += r.Monto;
                    pagado += r.MontoPagado;
                    saldo += r.SaldoActual;
                }
            }
            TotalMontoInscripciones = monto;
            TotalPagadoInscripciones = pagado;
            TotalSaldoInscripciones = saldo;
        }
    }
}
