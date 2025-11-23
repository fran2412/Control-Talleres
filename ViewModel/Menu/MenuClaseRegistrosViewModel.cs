using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using ControlTalleresMVP.Helpers.Dialogs;
using ControlTalleresMVP.Messages;
using ControlTalleresMVP.Persistence.ModelDTO;
using ControlTalleresMVP.Services.Clases;
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
    public partial class MenuClaseRegistrosViewModel : ObservableObject
    {
        public ObservableCollection<ClaseFinancieraDTO> RegistrosClases { get; } = new();
        public ICollectionView? RegistrosClasesView { get; private set; }

        private readonly IClaseService _claseService;
        private readonly IAlumnoPickerService _alumnoPicker;
        private readonly IDialogService _dialogService;

        [ObservableProperty] private int? alumnoSeleccionadoId;
        [ObservableProperty] private string? alumnoNombre;

        [ObservableProperty] private DateTime? fechaDesdeRegistros;
        [ObservableProperty] private DateTime? fechaHastaRegistros;

        private string _filtroRegistrosClases = string.Empty;
        public string FiltroRegistrosClases
        {
            get => _filtroRegistrosClases;
            set { if (SetProperty(ref _filtroRegistrosClases, value)) RegistrosClasesView?.Refresh(); }
        }

        [ObservableProperty] private decimal totalMontoClases;
        [ObservableProperty] private decimal totalPagadoClases;
        [ObservableProperty] private decimal totalSaldoClases;

        partial void OnFechaDesdeRegistrosChanged(DateTime? value)
        {
            NormalizarRangoFechas();
            _ = CargarRegistrosClasesAsync();
        }

        partial void OnFechaHastaRegistrosChanged(DateTime? value)
        {
            NormalizarRangoFechas();
            _ = CargarRegistrosClasesAsync();
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

        public MenuClaseRegistrosViewModel(
            IClaseService claseService,
            IAlumnoPickerService alumnoPicker,
            IDialogService dialogService)
        {
            _claseService = claseService;
            _alumnoPicker = alumnoPicker;
            _dialogService = dialogService;

            // Inicializar comando
            CancelarClaseAsyncCommand = new AsyncRelayCommand<int>(CancelarClaseAsync);

            FechaHastaRegistros = DateTime.Today.AddDays(7);
            FechaDesdeRegistros = DateTime.Today.AddMonths(-1);

            RegistrosClasesView = CollectionViewSource.GetDefaultView(RegistrosClases);
            RegistrosClasesView.Filter = FiltroRegistrosPredicate;
            RegistrosClasesView.CurrentChanged += (_, __) => RecalcularTotales();

            // Suscríbete a los cambios de cobro
            WeakReferenceMessenger.Default.Register<ClasesActualizadasMessage>(this, async (_, m) =>
            {
                if (alumnoSeleccionadoId is null || alumnoSeleccionadoId == m.AlumnoId)
                    await CargarRegistrosClasesAsync();
            });

            _ = CargarRegistrosClasesAsync();
        }

        [RelayCommand]
        private async Task CargarRegistrosClasesAsync(CancellationToken ct = default)
        {
            try
            {
                var datos = await _claseService.ObtenerClasesFinancierasAsync(
                    alumnoId: AlumnoSeleccionadoId,
                    tallerId: null,
                    desde: FechaDesdeRegistros,
                    hasta: FechaHastaRegistros,
                    ct: ct);

                RegistrosClases.Clear();
                foreach (var r in datos) RegistrosClases.Add(r);

                RecalcularTotales();
            }
            catch (Exception ex)
            {
                _dialogService.Error("No fue posible cargar el registro de clases.\n" + ex.Message);
            }
        }

        public IAsyncRelayCommand<int> CancelarClaseAsyncCommand { get; }

        private async Task CancelarClaseAsync(int claseId)
        {
            System.Diagnostics.Debug.WriteLine($"CancelarClaseAsync - Comando ejecutado para claseId: {claseId}");
            
            if (!_dialogService.Confirmar("¿Seguro que deseas cancelar esta clase?")) 
            {
                System.Diagnostics.Debug.WriteLine("CancelarClaseAsync - Usuario canceló la operación");
                return;
            }

            try
            {
                System.Diagnostics.Debug.WriteLine($"CancelarClaseAsync - Cancelando clase {claseId}");
                await _claseService.CancelarAsync(claseId);
                System.Diagnostics.Debug.WriteLine("CancelarClaseAsync - Clase cancelada, recargando registros");
                await CargarRegistrosClasesAsync();
                _dialogService.Info("Clase cancelada correctamente.");
                System.Diagnostics.Debug.WriteLine("CancelarClaseAsync - Proceso completado exitosamente");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"CancelarClaseAsync - Error: {ex.Message}");
                _dialogService.Error("No se pudo cancelar la clase.\n" + ex.Message);
            }
        }

        private bool FiltroRegistrosPredicate(object o)
        {
            if (o is not ClaseFinancieraDTO dto) return false;

            if (!string.IsNullOrWhiteSpace(FiltroRegistrosClases))
            {
                var f = FiltroRegistrosClases.Trim();
                if (!((dto.AlumnoNombre?.Contains(f, StringComparison.OrdinalIgnoreCase) == true)
                    || (dto.TallerNombre?.Contains(f, StringComparison.OrdinalIgnoreCase) == true)
                    || dto.EstadoTexto.Contains(f, StringComparison.OrdinalIgnoreCase)))
                    return false;
            }

            if (FechaDesdeRegistros.HasValue)
            {
                if (!dto.UltimoPagoFecha.HasValue || dto.UltimoPagoFecha.Value.Date < FechaDesdeRegistros.Value.Date)
                    return false;
            }

            if (FechaHastaRegistros.HasValue)
            {
                if (!dto.UltimoPagoFecha.HasValue || dto.UltimoPagoFecha.Value.Date > FechaHastaRegistros.Value.Date)
                    return false;
            }

            if (AlumnoSeleccionadoId.HasValue && dto.AlumnoId != AlumnoSeleccionadoId.Value) return false;

            return true;
        }

        private void RecalcularTotales()
        {
            if (RegistrosClasesView is null) return;

            decimal monto = 0, pagado = 0, saldo = 0;
            foreach (var item in RegistrosClasesView)
            {
                if (item is ClaseFinancieraDTO r)
                {
                    monto += r.Monto;
                    pagado += r.MontoPagado;
                    saldo += r.SaldoActual;
                }
            }
            TotalMontoClases = monto;
            TotalPagadoClases = pagado;
            TotalSaldoClases = saldo;
        }
    }
}
