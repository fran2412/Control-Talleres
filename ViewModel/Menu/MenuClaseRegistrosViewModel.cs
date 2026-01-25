using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using ControlTalleresMVP.Helpers.Dialogs;
using ControlTalleresMVP.Messages;
using ControlTalleresMVP.Persistence.ModelDTO;
using ControlTalleresMVP.Services.Clases;
using ControlTalleresMVP.Services.Picker;
using System.Collections.ObjectModel;
using System.ComponentModel;
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

        [ObservableProperty] private DateTime fechaFiltro = DateTime.Today;

        private string _filtroRegistrosClases = string.Empty;
        public string FiltroRegistrosClases
        {
            get => _filtroRegistrosClases;
            set { if (SetProperty(ref _filtroRegistrosClases, value)) RegistrosClasesView?.Refresh(); }
        }

        [ObservableProperty] private decimal totalMontoClases;
        [ObservableProperty] private decimal totalPagadoClases;
        [ObservableProperty] private decimal totalSaldoClases;
        [ObservableProperty] private decimal totalIngresoReal;

        public string TituloIngreso => "Ingreso del día";

        private CancellationTokenSource? _ctsCarga;

        partial void OnFechaFiltroChanged(DateTime value)
        {
            // Notify ResumenAsistenciaDia about the date change
            WeakReferenceMessenger.Default.Send(new FechaClasesSeleccionadaCambiadaMessage(value));
            SolicitarCarga();
        }

        private void SolicitarCarga()
        {
            _ctsCarga?.Cancel();
            _ctsCarga = new CancellationTokenSource();
            var token = _ctsCarga.Token;

            _ = Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(100, token);
                    if (token.IsCancellationRequested) return;

                    await System.Windows.Application.Current.Dispatcher.InvokeAsync(async () =>
                    {
                        await CargarRegistrosClasesAsync(token);
                    });
                }
                catch (OperationCanceledException) { }
            }, token);
        }

        public MenuClaseRegistrosViewModel(
            IClaseService claseService,
            IAlumnoPickerService alumnoPicker,
            IDialogService dialogService)
        {
            _claseService = claseService;
            _alumnoPicker = alumnoPicker;
            _dialogService = dialogService;

            CancelarClaseAsyncCommand = new AsyncRelayCommand<int>(CancelarClaseAsync);

            RegistrosClasesView = CollectionViewSource.GetDefaultView(RegistrosClases);
            RegistrosClasesView.Filter = FiltroRegistrosPredicate;
            RegistrosClasesView.CurrentChanged += (_, __) => RecalcularTotales();

            // Listen for class payment updates
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
                var fecha = FechaFiltro.Date;
                var datos = await _claseService.ObtenerClasesFinancierasAsync(
                    alumnoId: AlumnoSeleccionadoId,
                    tallerId: null,
                    desde: fecha,
                    hasta: fecha,
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
            if (!_dialogService.Confirmar("¿Seguro que deseas cancelar esta clase?"))
            {
                return;
            }

            try
            {
                await _claseService.CancelarAsync(claseId);
                await CargarRegistrosClasesAsync();
                _dialogService.Info("Clase cancelada correctamente.");
            }
            catch (Exception ex)
            {
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

            if (AlumnoSeleccionadoId.HasValue && dto.AlumnoId != AlumnoSeleccionadoId.Value) return false;

            return true;
        }

        private void RecalcularTotales()
        {
            if (RegistrosClasesView is null) return;

            decimal monto = 0, pagado = 0, saldo = 0, ingresoReal = 0;
            foreach (var item in RegistrosClasesView)
            {
                if (item is ClaseFinancieraDTO r)
                {
                    monto += r.MontoTotal;
                    pagado += r.MontoPagado;
                    saldo += r.SaldoActual;
                    ingresoReal += r.IngresoPorFecha;
                }
            }
            TotalMontoClases = monto;
            TotalPagadoClases = pagado;
            TotalSaldoClases = saldo;
            TotalIngresoReal = ingresoReal;
        }
    }
}
