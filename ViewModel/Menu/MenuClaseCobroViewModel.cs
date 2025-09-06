using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using ControlTalleresMVP.Helpers.Dialogs;
using ControlTalleresMVP.Messages;
using ControlTalleresMVP.Persistence.ModelDTO;
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
    public partial class MenuClaseCobroViewModel : ObservableObject
    {
        private readonly IClaseService _claseService;
        private readonly IInscripcionService _inscripcionService;
        private readonly IDialogService _dialogService;
        private readonly IConfiguracionService _configuracionService;
        private readonly IAlumnoPickerService _alumnoPicker;

        // límites y control de recursión
        private const int MaxClases = 4;
        private const decimal MinCostoClase = 1m;
        private const decimal MaxCostoClase = 10000m;
        private bool _ajustandoCantidad;
        private bool _ajustandoCosto;
        private bool _ajustandoMonto;

        public MenuClaseCobroViewModel(
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
            var costoCfg = _configuracionService.GetValor<int>("costo_clase", 150);
            CostoClase = ClampCosto(R2(costoCfg));
            RecalcularSugeridoYQuizasMonto();
        }

        [ObservableProperty] private string fechaDeHoy = "";
        [ObservableProperty] private string? alumnoNombre;
        [ObservableProperty] private Alumno? alumnoSeleccionado;
        [ObservableProperty] private ObservableCollection<Taller> talleresDelAlumno = new();
        [ObservableProperty] private Taller? tallerSeleccionado;

        [ObservableProperty] private int cantidadSeleccionada = 1;
        partial void OnCantidadSeleccionadaChanged(int value)
        {
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

        [ObservableProperty] private decimal costoClase;
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

        [ObservableProperty] private decimal? montoIngresado;
        partial void OnMontoIngresadoChanged(decimal? value)
        {
            if (_ajustandoMonto || value is null) return;
            var v = R2(value.Value);
            if (v < 0m) v = 0m;
            if (MontoSugerido > 0m && v > MontoSugerido) v = MontoSugerido;
            if (MontoIngresado != v)
            {
                _ajustandoMonto = true;
                MontoIngresado = v;
                _ajustandoMonto = false;
            }
        }

        [ObservableProperty] private decimal montoSugerido;
        [ObservableProperty] private string? mensajeValidacion;

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
                _dialogService.Alerta("Este alumno no está inscrito en ningún taller.\nInscríbalo en alguno para poder registrar sus pagos a clases.");
                return;
            }

            TalleresDelAlumno = talleresDisponibles;
            AlumnoSeleccionado = alumno;
            AlumnoNombre = alumno.Nombre;

            var ids = talleresDisponibles.Select(t => t.TallerId).ToArray();
            var estados = await _claseService.ObtenerEstadoPagoHoyAsync(alumno.AlumnoId, ids, DateTime.Today);

            var todosPagados = estados.Length > 0 && estados.All(e => e.EstaPagada);
            if (todosPagados)
            {
                var lista = string.Join("\n • ", talleresDisponibles.Select(t => t.Nombre));
                _dialogService.Alerta("El alumno ya tiene pagada la clase de HOY en todos sus talleres:\n" + $"• {lista}");
                return;
            }

            var disponibles = estados.Where(e => e.PuedePagar).Select(e => e.TallerId).ToHashSet();
            TallerSeleccionado = TalleresDelAlumno.FirstOrDefault(t => disponibles.Contains(t.TallerId))
                               ?? TalleresDelAlumno.First();
        }

        [RelayCommand]
        private void LimpiarSeleccion()
        {
            AlumnoSeleccionado = null;
            AlumnoNombre = string.Empty;
            TallerSeleccionado = null;
            TalleresDelAlumno = new();

            if (!_ajustandoCantidad)
            {
                _ajustandoCantidad = true;
                CantidadSeleccionada = 1;
                _ajustandoCantidad = false;
            }

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

                if (AlumnoSeleccionado is null) { MensajeValidacion = "Debes seleccionar un alumno."; return; }
                if (TallerSeleccionado is null) { MensajeValidacion = "Debes seleccionar un taller."; return; }
                if (MontoIngresado is null || MontoIngresado <= 0) { MensajeValidacion = "El monto debe ser mayor a 0."; return; }
                if (CantidadSeleccionada < 1) { MensajeValidacion = "La cantidad de clases debe ser al menos 1."; return; }

                var costo = Math.Round(CostoClase, 2, MidpointRounding.AwayFromZero);
                var totalIngresado = Math.Round(MontoIngresado.Value, 2, MidpointRounding.AwayFromZero);

                var clasesCompletas = (int)Math.Min(CantidadSeleccionada, Math.Truncate(totalIngresado / (costo == 0 ? 1 : costo)));
                var sobrante = totalIngresado - (clasesCompletas * costo);
                var cantidadFechas = Math.Max(clasesCompletas + (clasesCompletas > 0 && sobrante > 0m && clasesCompletas < CantidadSeleccionada ? 1 : 0), 1);
                var fechas = GenerarFechasSemanas(DateTime.Today, cantidadFechas);

                var resultados = new System.Collections.Generic.List<RegistrarClaseResult>();

                for (int i = 0; i < clasesCompletas; i++)
                {
                    var r = await _claseService.RegistrarClaseAsync(
                        AlumnoSeleccionado.AlumnoId, TallerSeleccionado.TallerId, fechas[i], costo, ct);
                    resultados.Add(r);
                }

                if (clasesCompletas > 0 && sobrante > 0m && clasesCompletas < CantidadSeleccionada)
                {
                    var abono = Math.Round(sobrante, 2, MidpointRounding.AwayFromZero);
                    var r = await _claseService.RegistrarClaseAsync(
                        AlumnoSeleccionado.AlumnoId, TallerSeleccionado.TallerId, fechas[clasesCompletas], abono, ct);
                    resultados.Add(r);
                }
                else if (clasesCompletas == 0 && totalIngresado > 0m)
                {
                    var r = await _claseService.RegistrarClaseAsync(
                        AlumnoSeleccionado.AlumnoId, TallerSeleccionado.TallerId, fechas[0], totalIngresado, ct);
                    resultados.Add(r);
                }

                string F(DateTime d) => d.ToString("dd/MM/yyyy");

                bool huboCambios = resultados.Any(r => r.PagoCreado || r.CargoCreado || r.ClaseCreada);
                if (!huboCambios)
                {
                    if (resultados.Count > 0 && resultados.All(r => r.CargoYaPagado))
                        _dialogService.Alerta("No se registró ningún movimiento: la(s) clase(s) ya estaban pagadas.");
                    else
                        _dialogService.Alerta("No se registró ningún movimiento.");
                    return;
                }

                var pagadasFull = resultados.Where(r => r.PagoCreado && r.MontoAplicado >= costo).Select(r => r.Fecha).OrderBy(d => d).ToList();
                var parciales = resultados.Where(r => r.PagoCreado && r.MontoAplicado > 0m && r.MontoAplicado < costo)
                                          .Select(r => new { r.Fecha, r.MontoAplicado }).OrderBy(x => x.Fecha).ToList();

                if (pagadasFull.Count > 0 && parciales.Count > 0)
                {
                    var listado = string.Join(", ", pagadasFull.Select(F));
                    var p = parciales.Last();
                    _dialogService.Info($"Se registraron {pagadasFull.Count} clase(s) pagada(s) ({listado}) y un abono de {p.MontoAplicado:0.00} para la clase de {F(p.Fecha)}.", "Clases");
                }
                else if (pagadasFull.Count == 1)
                {
                    _dialogService.Info($"Se registró la clase de {F(pagadasFull[0])} pagada por completo.", "Clases");
                }
                else if (pagadasFull.Count > 1)
                {
                    var listado = string.Join(", ", pagadasFull.Select(F));
                    _dialogService.Info($"Se registraron {pagadasFull.Count} clase(s) pagada(s): {listado}.", "Clases");
                }
                else if (parciales.Count > 0)
                {
                    var p = parciales.Last();
                    _dialogService.Info($"Se registró un abono de {p.MontoAplicado:0.00} para la clase de {F(p.Fecha)}.", "Clases");
                }

                // Notifica a quien muestre el registro que hay cambios
                WeakReferenceMessenger.Default.Send(new ClasesActualizadasMessage(AlumnoSeleccionado.AlumnoId));

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
            if (_dialogService.Confirmar("¿Está seguro de cancelar el registro del pago?", "Clases"))
            {
                LimpiarSeleccion();
                _dialogService.Info("Registro cancelado.", "Clases");
            }
        }

        // ==== helpers ====
        private void RecalcularSugeridoYQuizasMonto(bool forceSetMonto = false)
        {
            var nuevoSugerido = R2(CantidadSeleccionada * CostoClase);
            if (MontoSugerido != nuevoSugerido) MontoSugerido = nuevoSugerido;

            if (forceSetMonto || !MontoIngresado.HasValue || MontoIngresado > MontoSugerido)
            {
                if (!_ajustandoMonto)
                {
                    _ajustandoMonto = true;
                    MontoIngresado = MontoSugerido;
                    _ajustandoMonto = false;
                }
            }
            else if (MontoIngresado.HasValue)
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

        private static DateTime[] GenerarFechasSemanas(DateTime inicio, int cantidad)
        {
            if (cantidad < 1) return Array.Empty<DateTime>();
            var arr = new DateTime[cantidad];
            for (int i = 0; i < cantidad; i++) arr[i] = inicio.AddDays(7 * i);
            return arr;
        }

        private static decimal R2(decimal v) => Math.Round(v, 2, MidpointRounding.AwayFromZero);
        private static decimal ClampCosto(decimal v) => v < MinCostoClase ? MinCostoClase : (v > MaxCostoClase ? MaxCostoClase : v);
    }
}
