using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ControlTalleresMVP.Helpers.Dialogs;
using ControlTalleresMVP.Persistence.ModelDTO;
using ControlTalleresMVP.Persistence.Models;
using ControlTalleresMVP.Services.Cargos;
using ControlTalleresMVP.Services.Pagos;
using ControlTalleresMVP.Services.Picker;
using Microsoft.Extensions.Logging.Abstractions;
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

        public ObservableCollection<CargoPagoItemVM> CargosPendientes { get; } = new();

        [ObservableProperty] private string? mensajeValidacion;

        public decimal TotalAplicado => CargosPendientes.Sum(c => c.Monto);


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
            GuardarPagoCommand.NotifyCanExecuteChanged();
            MensajeValidacion = Validar();
        }

        [RelayCommand]
        private async Task BuscarAlumnoAsync()
        {
            try
            {
                var seleccionado = _alumnoPicker.Pick();
                if (seleccionado is null)
                {
                    return;
                }

                var cargos = await _cargosService.ObtenerCargosPendientesActualesAsync(seleccionado.AlumnoId);

                if (cargos.Length == 0)
                {
                    _dialog.Alerta("Este alumno no tiene cargos pendientes.");
                    return;
                }

                CargarCargos(cargos);

                AlumnoId = seleccionado.AlumnoId;
                AlumnoNombre = seleccionado.Nombre;

                RecalcularResumen();
            }
            catch (Exception ex)
            {
                _dialog.Error($"Error al buscar alumno: {ex.Message}");
            }
        }

        public void CargarCargos(IEnumerable<DestinoCargoDTO> cargos)
        {
            CargosPendientes.Clear();

            foreach (var dto in cargos)
            {
                var vm = new CargoPagoItemVM
                {
                    CargoId = dto.CargoId,
                    Tipo = dto.Tipo,
                    Descripcion = dto.Descripcion,
                    SaldoPendiente = dto.SaldoPendiente,
                    InscripcionId = dto.InscripcionId,
                    ClaseId = dto.ClaseId
                };

                vm.PropertyChanged += ItemOnPropertyChanged;
                CargosPendientes.Add(vm);
            }
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
                TotalAplicado,
                aplicaciones,
                MetodoPago.Efectivo
            );

            try
            {
                var pagoId = await _pagoService.GuardarPagoAsync(captura);
                _dialog.Info($"Pago #{pagoId} guardado correctamente.");
                LimpiarDistribucion();

                var cargos = await _cargosService.ObtenerCargosPendientesActualesAsync(AlumnoId);

                if (cargos.Length == 0)
                {
                    LimpiarTodo();
                    _dialog.Info("Este alumno ya no tiene cargos pendientes.");
                    return;
                }

                CargarCargos(cargos);

                RecalcularResumen();

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
            if (!CargosPendientes.Any(c => c.Seleccionado && c.Monto > 0))
                return "Selecciona al menos un cargo y asigna un monto.";

            // Cada fila ya clampa a SaldoPendiente, así que en teoría no se excede.
            // Si quieres doble validación explícita:
            var excedido = CargosPendientes.Any(c => c.Monto > c.SaldoPendiente);
            if (excedido) return "Hay montos que exceden el saldo del cargo.";

            return string.Empty;
        }

        [RelayCommand]
        private void CancelarPago()
        {
            if (_dialog.Confirmar("¿Cancelar la captura del pago?"))
            {
                LimpiarTodo();
            }
        }

        private void LimpiarTodo()
        {
            AlumnoId = 0;
            AlumnoNombre = null;
            CargosPendientes.Clear();
            MensajeValidacion = null;
        }
    }

    // --------- VM por fila ----------
    public class CargoPagoItemVM : ObservableObject
    {
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
                // Máximo permitido: el saldo del cargo
                var maxPorCargo = Math.Max(0, SaldoPendiente);
                var limpio = Math.Clamp(value, 0, maxPorCargo);

                if (SetProperty(ref monto, limpio))
                {
                    if (monto > 0 && !Seleccionado) Seleccionado = true; // coherencia UX
                    OnPropertyChanged(nameof(SaldoResultante));
                }
            }
        }

        public decimal SaldoResultante => Math.Max(0, SaldoPendiente - Monto);
    }
}
