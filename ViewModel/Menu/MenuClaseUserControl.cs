using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ControlTalleresMVP.Helpers.Dialogs;
using ControlTalleresMVP.Persistence.Models;
using ControlTalleresMVP.Services.Clases;
using ControlTalleresMVP.Services.Configuracion;
using ControlTalleresMVP.Services.Inscripciones;
using ControlTalleresMVP.Services.Picker;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ControlTalleresMVP.ViewModel.Menu
{
    public partial class MenuClaseUserControl : ObservableObject
    {
        private readonly IClaseService _claseService;
        private readonly IInscripcionService _inscripcionService;
        private readonly IDialogService _dialogService;
        private readonly IConfiguracionService _configuracionService;
        private readonly IAlumnoPickerService _alumnoPicker;

        // Límites y control de recursión
        private const int MaxClases = 4;
        private const decimal MinCostoClase = 1m;
        private const decimal MaxCostoClase = 10000m;

        private bool _ajustandoCantidad;
        private bool _ajustandoCosto;
        private bool _ajustandoMonto;

        public MenuClaseUserControl(
            IClaseService claseService,
            IInscripcionService inscripcionService,
            IDialogService dialogService,
            IConfiguracionService configuracionService,
            IAlumnoPickerService alumnoPicker)
        {
            _claseService = claseService;
            _dialogService = dialogService;
            _alumnoPicker = alumnoPicker;
            _configuracionService = configuracionService;
            _inscripcionService = inscripcionService;

            FechaDeHoy = DateTime.Now.ToString("dd/MM/yyyy");

            // Inicializa costo y monta el sugerido + ingresado
            var costoCfg = _configuracionService.GetValor<int>("costo_clase", 150);
            CostoClase = ClampCosto(R2(costoCfg));
            RecalcularSugeridoYQuizasMonto();
        }

        // ====================
        // PROPIEDADES BINDING
        // ====================

        [ObservableProperty]
        private string fechaDeHoy;

        [ObservableProperty]
        private string? alumnoNombre;

        [ObservableProperty]
        private Alumno? alumnoSeleccionado;

        [ObservableProperty]
        private ObservableCollection<Taller> talleresDelAlumno = new();

        [ObservableProperty]
        private Taller? tallerSeleccionado;

        [ObservableProperty]
        private int cantidadSeleccionada = 1;
        partial void OnCantidadSeleccionadaChanged(int value)
        {
            // Clamp real: 1..MaxClases
            var clamped = value < 1 ? 1 : (value > MaxClases ? MaxClases : value);
            if (clamped != value)
            {
                if (_ajustandoCantidad) return;
                _ajustandoCantidad = true;
                CantidadSeleccionada = clamped;
                _ajustandoCantidad = false;
                return;
            }

            RecalcularSugeridoYQuizasMonto();
        }

        [ObservableProperty]
        private decimal costoClase;
        partial void OnCostoClaseChanged(decimal value)
        {
            var clamped = ClampCosto(R2(value));
            if (clamped != value)
            {
                if (_ajustandoCosto) return;
                _ajustandoCosto = true;
                CostoClase = clamped;
                _ajustandoCosto = false;
                return;
            }

            RecalcularSugeridoYQuizasMonto();
        }

        [ObservableProperty]
        private decimal? montoIngresado;
        partial void OnMontoIngresadoChanged(decimal? value)
        {
            if (_ajustandoMonto) return;
            if (value is null) return;

            var v = R2(value.Value);
            // Clamp: 0..MontoSugerido
            if (v < 0m) v = 0m;
            if (MontoSugerido > 0m && v > MontoSugerido) v = MontoSugerido;

            if (MontoIngresado != v)
            {
                _ajustandoMonto = true;
                MontoIngresado = v;
                _ajustandoMonto = false;
            }
        }

        [ObservableProperty]
        private decimal montoSugerido;

        [ObservableProperty]
        private string? mensajeValidacion;

        // ====================
        // COMANDOS
        // ====================

        [RelayCommand]
        private async Task BuscarAlumno()
        {
            var alumno = _alumnoPicker.Pick();
            if (alumno is null) return;

            var inscripciones = await _inscripcionService.ObtenerInscripcionesAsync(alumno.AlumnoId);

            var talleresDisponibles = new ObservableCollection<Taller>(
                inscripciones
                    .Where(i => i.Taller != null)
                    .Select(i => i.Taller!)
                    .GroupBy(t => t.TallerId)
                    .Select(g => g.First())
                    .ToList()
            );

            if (talleresDisponibles.Count == 0)
            {
                _dialogService.Alerta("Este alumno no está inscrito en ningún taller.\n" +
                    "Inscríbalo en alguno para poder registrar sus pagos a clases.");
                return;
            }

            TalleresDelAlumno = talleresDisponibles;
            TallerSeleccionado = TalleresDelAlumno.First();
            AlumnoSeleccionado = alumno;
            AlumnoNombre = alumno.Nombre;
        }

        [RelayCommand]
        private void LimpiarSeleccion()
        {
            AlumnoSeleccionado = null;
            AlumnoNombre = string.Empty;
            TallerSeleccionado = null;
            TalleresDelAlumno = new();

            // Reinicia cantidad y montos
            if (!_ajustandoCantidad)
            {
                _ajustandoCantidad = true;
                CantidadSeleccionada = 1;
                _ajustandoCantidad = false;
            }

            // Recalcula y fija al sugerido
            RecalcularSugeridoYQuizasMonto(forceSetMonto: true);
            MensajeValidacion = null;
        }

        [RelayCommand]
        private void UsarMontoSugerido()
        {
            _ajustandoMonto = true;
            MontoIngresado = MontoSugerido;
            _ajustandoMonto = false;
        }

        [RelayCommand]
        private async Task GuardarPagoClases(CancellationToken ct)
        {
            try
            {
                MensajeValidacion = null;

                // Validaciones básicas
                if (AlumnoSeleccionado is null)
                {
                    MensajeValidacion = "Debes seleccionar un alumno.";
                    return;
                }

                if (TallerSeleccionado is null)
                {
                    MensajeValidacion = "Debes seleccionar un taller.";
                    return;
                }

                if (MontoIngresado is null || MontoIngresado <= 0)
                {
                    MensajeValidacion = "El monto debe ser mayor a 0.";
                    return;
                }

                if (CantidadSeleccionada < 1)
                {
                    MensajeValidacion = "La cantidad de clases debe ser al menos 1.";
                    return;
                }

                // 1) Fechas semanales (hoy + 7d * i)
                var fechas = Enumerable.Range(0, CantidadSeleccionada)
                                       .Select(i => DateTime.Today.AddDays(7 * i))
                                       .ToArray();

                // 2) Distribución del pago entre las N clases
                //    (si prefieres TODO a la primera, comenta este bloque y deja tu lógica)
                decimal total = Math.Round(MontoIngresado.Value, 2, MidpointRounding.AwayFromZero);
                decimal baseMonto = Math.Round(total / CantidadSeleccionada, 2, MidpointRounding.AwayFromZero);
                decimal acumulado = 0m;

                for (int i = 0; i < CantidadSeleccionada; i++)
                {
                    // Última clase: ajusta para cuadrar centavos
                    decimal montoEstaClase = (i == CantidadSeleccionada - 1)
                        ? total - acumulado
                        : baseMonto;

                    acumulado += montoEstaClase;

                    await _claseService.RegistrarClaseAsync(
                        AlumnoSeleccionado.AlumnoId,
                        TallerSeleccionado.TallerId,
                        fechas[i],
                        montoEstaClase, // ⇐ pago distribuido por clase
                        ct);
                }

                _dialogService.Info("Pago registrado correctamente.", "Clases");
                LimpiarSeleccion();
            }
            catch (Exception ex)
            {
                _dialogService.Error($"Error al registrar el pago: {ex.Message}", "Clases");
            }
        }

        [RelayCommand]
        private void CancelarPagoClases()
        {
            // Asumiendo que Confirmar devuelve bool
            if (_dialogService.Confirmar("¿Está seguro de cancelar el registro del pago?", "Clases"))
            {
                LimpiarSeleccion();
                _dialogService.Info("Registro cancelado.", "Clases");
            }
        }

        // ====================
        // Helpers
        // ====================

        private (bool ok, string? msg) Validar()
        {
            if (AlumnoSeleccionado is null)
                return (false, "Debes seleccionar un alumno.");

            if (TallerSeleccionado is null)
                return (false, "Debes seleccionar un taller.");

            if (CantidadSeleccionada < 1 || CantidadSeleccionada > MaxClases)
                return (false, $"La cantidad debe estar entre 1 y {MaxClases}.");

            if (CostoClase < MinCostoClase || CostoClase > MaxCostoClase)
                return (false, $"El costo de la clase debe estar entre {MinCostoClase:0.##} y {MaxCostoClase:0.##}.");

            if (MontoSugerido <= 0)
                return (false, "El monto sugerido no es válido.");

            if (!MontoIngresado.HasValue || MontoIngresado <= 0)
                return (false, "El monto ingresado debe ser mayor a 0.");

            if (MontoIngresado > MontoSugerido)
                return (false, "El monto ingresado no puede exceder el monto sugerido.");

            return (true, null);
        }

        private void RecalcularSugeridoYQuizasMonto(bool forceSetMonto = false)
        {
            var nuevoSugerido = R2(CantidadSeleccionada * CostoClase);

            if (MontoSugerido != nuevoSugerido)
                MontoSugerido = nuevoSugerido;

            // Si no hay monto, si nos pasamos, o si se fuerza, alinear al sugerido
            if (forceSetMonto || !MontoIngresado.HasValue || MontoIngresado > MontoSugerido)
            {
                if (!_ajustandoMonto)
                {
                    _ajustandoMonto = true;
                    MontoIngresado = MontoSugerido;
                    _ajustandoMonto = false;
                }
            }
            else
            {
                // Si existe monto pero con más de 2 decimales o negativo, normaliza
                if (MontoIngresado.HasValue)
                {
                    var norm = MontoIngresado.Value;
                    if (norm < 0m) norm = 0m;
                    norm = R2(norm);

                    if (!_ajustandoMonto && norm != MontoIngresado.Value)
                    {
                        _ajustandoMonto = true;
                        MontoIngresado = norm;
                        _ajustandoMonto = false;
                    }
                }
            }
        }

        private static decimal R2(decimal v) =>
            Math.Round(v, 2, MidpointRounding.AwayFromZero);

        private static decimal ClampCosto(decimal v)
        {
            if (v < MinCostoClase) return MinCostoClase;
            if (v > MaxCostoClase) return MaxCostoClase;
            return v;
        }
    }
}
