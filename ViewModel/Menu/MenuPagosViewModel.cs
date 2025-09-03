using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ControlTalleresMVP.Helpers.Dialogs;
using ControlTalleresMVP.Persistence.ModelDTO;
using ControlTalleresMVP.Persistence.Models;
using ControlTalleresMVP.Services.Cargos;
using ControlTalleresMVP.Services.Pagos;
using ControlTalleresMVP.Services.Picker;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace ControlTalleresMVP.ViewModel.Menu
{
    public partial class MenuPagosViewModel: ObservableObject
    {
        public string TituloEncabezado { get; set; } = "Gestión de pagos";
        private readonly IAlumnoPickerService _alumnoPicker;
        private readonly ICargosService _cargosService;
        private readonly IPagoService _pagoService;
        private readonly IDialogService _dialog;

        [ObservableProperty] private string? alumnoNombre;
        [ObservableProperty] private int alumnoId;

        [ObservableProperty] private decimal montoTotal;

        [ObservableProperty] private bool puedeIngresarMonto = false;
        [ObservableProperty] private bool puedeSeleccionarMetodo = false;

        public MetodoPago[] MetodosPago { get; } = Enum.GetValues<MetodoPago>();

        [ObservableProperty] private MetodoPago metodoPagoSeleccionado;
        [ObservableProperty] private int indiceMetodoSeleccionado;

        public ObservableCollection<CargoPagoItemVM> CargosPendientes { get; } = new();

        [ObservableProperty] private string? mensajeValidacion;

        public decimal TotalAplicado => CargosPendientes.Sum(c => c.Monto);
        public decimal SaldoDisponible => Math.Max(0, MontoTotal - TotalAplicado);

        public MenuPagosViewModel(
            IAlumnoPickerService alumnoPicker,
            ICargosService cargosService,
            IPagoService pagoService,
            IDialogService dialog)
        {
            _alumnoPicker = alumnoPicker;
            _cargosService = cargosService;
            _pagoService = pagoService;
            _dialog = dialog;

            CargosPendientes.CollectionChanged += (_, __) => RecalcularResumen();
        }

        // Recalcula y reevalúa el CanExecute de Guardar
        private void RecalcularResumen()
        {
            OnPropertyChanged(nameof(TotalAplicado));
            OnPropertyChanged(nameof(SaldoDisponible));
            GuardarPagoCommand.NotifyCanExecuteChanged();
            MensajeValidacion = Validar();
        }

        partial void OnMontoTotalChanged(decimal value)
        {
            // Notifica a filas (por si necesitan recalcular clamp)
            foreach (var c in CargosPendientes)
                c.NotifyClampChange(); // no cambia el monto; solo fuerza reevaluación si se edita
            RecalcularResumen();
        }

        [RelayCommand]
        private async Task BuscarAlumnoAsync()
        {
            var seleccionado = _alumnoPicker.Pick();
            if (seleccionado is null)
            {
                PuedeIngresarMonto = false;
                PuedeSeleccionarMetodo = false;
                return;
            }

            // Carga cargos del alumno
            var cargos = await _cargosService.ObtenerCargosPendientesAsync(seleccionado.AlumnoId);

            if(cargos.Length == 0)
            {
                _dialog.Alerta("Este alumno no tiene cargos pendientes.");
                return;
            }
            CargosPendientes.Clear();
            foreach (var dto in cargos)
            {
                var vm = new CargoPagoItemVM(() => SaldoDisponible)
                {
                    CargoId = dto.CargoId,
                    Tipo = dto.Tipo,
                    Descripcion = dto.Descripcion,
                    SaldoPendiente = dto.SaldoPendiente,
                    InscripcionId = dto.InscripcionId,
                    ClaseId = dto.ClaseId
                };

                // Escucha cambios de la fila para refrescar resumen
                vm.PropertyChanged += ItemOnPropertyChanged;
                CargosPendientes.Add(vm);
            }

            AlumnoId = seleccionado.AlumnoId;
            AlumnoNombre = seleccionado.Nombre;
            PuedeIngresarMonto = true;
            PuedeSeleccionarMetodo = true;


            RecalcularResumen();
        }

        private void ItemOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName is nameof(CargoPagoItemVM.Monto) or nameof(CargoPagoItemVM.Seleccionado))
                RecalcularResumen();
        }

        [RelayCommand]
        private void LimpiarDistribucion()
        {
            foreach (var c in CargosPendientes)
            {
                c.Seleccionado = false;
                c.Monto = 0;
            }
            RecalcularResumen();
        }

        [RelayCommand(CanExecute = nameof(PuedeGuardar))]
        private async Task GuardarPagoAsync()
        {
            var valid = Validar();
            if (!string.IsNullOrEmpty(valid))
            {
                MensajeValidacion = valid;
                _dialog.Error(valid);
                return;
            }

            var aplicaciones = CargosPendientes
                .Where(c => c.Seleccionado && c.Monto > 0)
                .Select(c => new PagoAplicacionCapturaDTO(
                    c.CargoId,
                    c.InscripcionId,
                    c.ClaseId,
                    c.Monto
                ))
                .ToArray();

            var captura = new PagoCapturaDTO(
                AlumnoId,
                MontoTotal,
                MetodoPagoSeleccionado,
                aplicaciones
            );

            try
            {
                var pagoId = await _pagoService.GuardarPagoAsync(captura);
                _dialog.Info($"Pago #{pagoId} guardado correctamente.");
                LimpiarDistribucion();
                MontoTotal = 0;
            }
            catch (Exception ex)
            {
                _dialog.Error("No se pudo guardar el pago.\n" + ex.Message);
            }
        }

        private bool PuedeGuardar() => string.IsNullOrEmpty(Validar());
        private string Validar()
        {
            if (AlumnoId <= 0) return "Selecciona un alumno.";
            if (MontoTotal <= 0) return "El monto total debe ser mayor a 0.";
            if (!CargosPendientes.Any(c => c.Seleccionado && c.Monto > 0))
                return "Selecciona al menos un cargo y asigna un monto.";

            // Sin 'saldo a favor': debe aplicar TODO el monto
            if (TotalAplicado != MontoTotal)
                return "Debes aplicar exactamente el monto total del pago.";

            // Per-fila: los clamps ya evitan excedentes; aquí puedes añadir chequeos adicionales si quieres.
            return string.Empty;
        }

        [RelayCommand]
        private void CancelarPago()
        {
            if (_dialog.Confirmar("¿Cancelar la captura del pago?"))
            {
                AlumnoId = 0;
                AlumnoNombre = null;
                MontoTotal = 0;
                CargosPendientes.Clear();
                MensajeValidacion = null;
                PuedeIngresarMonto = false;
                PuedeSeleccionarMetodo = false;
            }
        }
    }

    // --------- VM por fila ----------
    public class CargoPagoItemVM : ObservableObject
    {
        private readonly Func<decimal> _getSaldoDisponiblePago;

        public CargoPagoItemVM(Func<decimal> getSaldoDisponiblePago)
        {
            _getSaldoDisponiblePago = getSaldoDisponiblePago;
        }

        // Identificadores
        public int CargoId { get; init; }
        public int? InscripcionId { get; init; }
        public int? ClaseId { get; init; }

        // Visuales
        public string Tipo { get; init; } = "";       // "Inscripción" | "Clase"
        public string Descripcion { get; init; } = "";

        // Saldos
        public decimal SaldoPendiente { get; init; }

        private bool seleccionado;
        public bool Seleccionado
        {
            get => seleccionado;
            set
            {
                if (SetProperty(ref seleccionado, value))
                {
                    // Si se marca y no hay monto, precarga el mínimo entre saldo del cargo y saldo disponible del pago.
                    if (seleccionado && Monto <= 0)
                    {
                        var max = Math.Min(SaldoPendiente, Math.Max(0, _getSaldoDisponiblePago()));
                        if (max > 0) Monto = max;
                    }
                    // Si se desmarca, limpia el monto
                    if (!seleccionado && Monto > 0)
                        Monto = 0;
                }
            }
        }

        private decimal monto;
        public decimal Monto
        {
            get => monto;
            set
            {
                // Máximo permitido: saldo del cargo y saldo disponible del pago + lo ya asignado en esta misma fila
                var maxPorCargo = Math.Max(0, SaldoPendiente);
                var maxPorPago = Math.Max(0, _getSaldoDisponiblePago() + monto); // permite reajuste
                var limpio = Math.Clamp(value, 0, Math.Min(maxPorCargo, maxPorPago));

                if (SetProperty(ref monto, limpio))
                {
                    if (monto > 0 && !Seleccionado) Seleccionado = true; // coherencia UX
                    OnPropertyChanged(nameof(SaldoResultante));
                }
            }
        }

        public decimal SaldoResultante => Math.Max(0, SaldoPendiente - Monto);

        // Para cuando cambie MontoTotal en el padre; no tocamos el valor, solo refresco si el UI necesita
        public void NotifyClampChange()
        {
            OnPropertyChanged(nameof(Monto));
            OnPropertyChanged(nameof(SaldoResultante));
        }
    }
}
