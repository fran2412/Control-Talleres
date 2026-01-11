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

        [ObservableProperty] private DateTime? fechaDesdeRegistros;
        [ObservableProperty] private DateTime? fechaHastaRegistros;
        [ObservableProperty] private DateTime? fechaFiltroDia = DateTime.Today;
        [ObservableProperty] private bool filtrarPorDiaEspecifico;

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

        // Título dinámico para la columna y el total
        public string TituloIngreso => FiltrarPorDiaEspecifico ? "Ingreso del día" : "Ingreso del periodo";

        private CancellationTokenSource? _ctsCarga;

        partial void OnFechaDesdeRegistrosChanged(DateTime? value)
        {
            // Evitar disparar la carga si estamos en medio de una sincronización por día específico
            if (FiltrarPorDiaEspecifico && FechaFiltroDia.HasValue && value != FechaFiltroDia.Value) return;

            NormalizarRangoFechas();
            SolicitarCarga();
        }

        partial void OnFechaHastaRegistrosChanged(DateTime? value)
        {
            if (FiltrarPorDiaEspecifico && FechaFiltroDia.HasValue && value != FechaFiltroDia.Value) return;

            NormalizarRangoFechas();
            SolicitarCarga();
        }

        partial void OnFechaFiltroDiaChanged(DateTime? value)
        {
            if (FiltrarPorDiaEspecifico)
                SincronizarRangoConDia();
        }

        partial void OnFiltrarPorDiaEspecificoChanged(bool value)
        {
            OnPropertyChanged(nameof(PuedeEditarRangoFechas));
            OnPropertyChanged(nameof(PuedeEditarDia));
            OnPropertyChanged(nameof(TituloIngreso)); // Notificar cambio de título

            if (value)
                SincronizarRangoConDia();
        }

        private void SolicitarCarga()
        {
            // Cancelar carga anterior si existe
            _ctsCarga?.Cancel();
            _ctsCarga = new CancellationTokenSource();
            var token = _ctsCarga.Token;

            // Ejecutar carga segura
            _ = Task.Run(async () =>
            {
                try
                {
                    // Pequeño delay para "debounce" si hay cambios rápidos
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

        private void NormalizarRangoFechas()
        {
            // Solo normalizar si NO estamos en modo día específico (para evitar loops raros)
            if (FiltrarPorDiaEspecifico) return;

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

        public bool PuedeEditarRangoFechas => !FiltrarPorDiaEspecifico;
        public bool PuedeEditarDia => FiltrarPorDiaEspecifico;

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

        private void SincronizarRangoConDia()
        {
            if (!FechaFiltroDia.HasValue) return;

            var fecha = FechaFiltroDia.Value.Date;
            bool cambiado = false;

            if (FechaDesdeRegistros != fecha)
            {
                // Actualizamos el campo backing field directamente para evitar disparadores en cadena si es posible,
                // o simplemente controlamos la lógica en el setter. 
                // Aquí usaremos SetProperty normal pero el debounce/cancelación manejará la carga.
                FechaDesdeRegistros = fecha;
                cambiado = true;
            }

            if (FechaHastaRegistros != fecha)
            {
                FechaHastaRegistros = fecha;
                cambiado = true;
            }

            // Si no hubo cambios (ya estaba en la fecha), forzamos recarga por si acaso el usuario re-seleccionó
            if (!cambiado) SolicitarCarga();
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

            // El filtrado por fecha ya se realiza en el servicio (Servidor)
            // con la lógica de "Clase en fecha" O "Pago en fecha".
            // No filtramos aquí para no ocultar resultados válidos.

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
