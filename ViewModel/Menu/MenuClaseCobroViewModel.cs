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
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ControlTalleresMVP.ViewModel.Menu
{
    public partial class MenuClaseCobroViewModel : ObservableObject
    {
        private const int MaxClasesFuturas = 4;
        private const decimal MinCostoClase = 1m;
        private const decimal MaxCostoClase = 10000m;

        private readonly IClaseService _claseService;
        private readonly IInscripcionService _inscripcionService;
        private readonly IDialogService _dialogService;
        private readonly IConfiguracionService _configuracionService;
        private readonly IAlumnoPickerService _alumnoPicker;

        private readonly decimal _costoClase;

        public MenuClaseCobroViewModel(
            IClaseService claseService,
            IInscripcionService inscripcionService,
            IDialogService dialogService,
            IConfiguracionService configuracionService,
            IAlumnoPickerService alumnoPicker)
        {
            _claseService = claseService;
            _inscripcionService = inscripcionService;
            _dialogService = dialogService;
            _configuracionService = configuracionService;
            _alumnoPicker = alumnoPicker;

            FechaDeHoy = DateTime.Now.ToString("dd/MM/yyyy");
            var costoCfg = _configuracionService.GetValor<int>("costo_clase", 150);
            _costoClase = ClampCosto(Math.Round((decimal)costoCfg, 2, MidpointRounding.AwayFromZero));
        }

        [ObservableProperty]
        private string fechaDeHoy = string.Empty;

        [ObservableProperty]
        private string? alumnoNombre;

        [ObservableProperty]
        private Alumno? alumnoSeleccionado;

        partial void OnAlumnoSeleccionadoChanged(Alumno? value)
        {
            if (value is null)
            {
                LimpiarClases();
            }
        }

        [ObservableProperty]
        private ObservableCollection<Taller> talleresDelAlumno = new();

        [ObservableProperty]
        private Taller? tallerSeleccionado;

        partial void OnTallerSeleccionadoChanged(Taller? value)
        {
            if (AlumnoSeleccionado is null || value is null)
            {
                LimpiarClases();
                return;
            }

            _ = CargarClasesDisponiblesAsync();
        }

        [ObservableProperty]
        private ObservableCollection<ClasePagoItem> clasesParaPago = new();

        [ObservableProperty]
        private bool hayClasesParaPago;

        [ObservableProperty]
        private int totalClasesSeleccionadas;

        [ObservableProperty]
        private decimal totalSeleccionado;

        [ObservableProperty]
        private string? mensajeValidacion;

        [ObservableProperty]
        private string? notificacionTalleresSinPagar;

        [ObservableProperty]
        private bool tieneTalleresSinPagar;

        [RelayCommand]
        private async Task BuscarAlumno()
        {
            var alumno = _alumnoPicker.Pick(excluirBecados: true);
    if (alumno is null) return;

    await ProcesarSeleccionAlumno(alumno);
}

public async Task BuscarAlumnoConAlumno(Alumno alumno)
{
    if (alumno is null) return;
    
    await ProcesarSeleccionAlumno(alumno);
}

private async Task ProcesarSeleccionAlumno(Alumno alumno)
{
    var inscripciones = await _inscripcionService.ObtenerInscripcionesAlumnoAsync(alumno.AlumnoId);
    var activas = inscripciones.Where(i => !i.Eliminado).ToArray();

    if (activas.Length == 0)
    {
        _dialogService.Alerta("Este alumno no está inscrito en ningún taller.\nInscríbalo en alguno para poder registrar sus pagos a clases.");
        return;
    }

    var inscripcionesPendientes = activas.Where(i => i.Estado == EstadoInscripcion.Pendiente).ToArray();

    var pendientesAgrupadas = inscripcionesPendientes
        .Where(i => i.Taller != null)
        .GroupBy(i => i.Taller!.Nombre)
        .Select(g => new
        {
            Taller = g.Key,
                    MontoPendiente = g.Sum(x => Math.Max(0m, x.SaldoActual))
        })
        .Where(x => x.MontoPendiente > 0m)
        .OrderBy(x => x.Taller)
        .ToList();

    if (pendientesAgrupadas.Count > 0)
    {
        var lista = string.Join(", ", pendientesAgrupadas.Select(p => $"{p.Taller} (${p.MontoPendiente:0.00})"));
        NotificacionTalleresSinPagar = $"⚠️ Inscripciones pendientes: {lista}";
        TieneTalleresSinPagar = true;
    }
    else
    {
        NotificacionTalleresSinPagar = null;
        TieneTalleresSinPagar = false;
    }

    var talleresDisponibles = new ObservableCollection<Taller>(
        activas
            .Where(i => i.Taller != null)
            .Select(i => i.Taller!)
            .GroupBy(t => t.TallerId)
            .Select(g => g.First())
            .OrderBy(t => t.Nombre)
                    .ToList());

    TalleresDelAlumno = talleresDisponibles;
    AlumnoSeleccionado = alumno;
    AlumnoNombre = alumno.Nombre;

    var ids = talleresDisponibles.Select(t => t.TallerId).ToArray();
    var estados = await _claseService.ObtenerEstadoPagoHoyAsync(alumno.AlumnoId, ids, DateTime.Today);
    var disponibles = estados.Where(e => e.PuedePagar).Select(e => e.TallerId).ToHashSet();

    TallerSeleccionado = TalleresDelAlumno.FirstOrDefault(t => disponibles.Contains(t.TallerId))
                       ?? TalleresDelAlumno.FirstOrDefault();

            // Forzar recarga del grid aunque TallerSeleccionado no cambie de referencia
            await CargarClasesDisponiblesAsync();
}

        [RelayCommand]
        private void LimpiarSeleccion()
        {
            NotificacionTalleresSinPagar = null;
            TieneTalleresSinPagar = false;
            MensajeValidacion = null;
            
            LimpiarClases();

            AlumnoSeleccionado = null;
            AlumnoNombre = string.Empty;
            TallerSeleccionado = null;
            TalleresDelAlumno = new();
        }

        [RelayCommand]
        private async Task GuardarPagoClases(CancellationToken ct)
        {
            try
            {
                MensajeValidacion = null;

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
                        
                var seleccion = ClasesParaPago
                    .Where(c => c.IsSeleccionada && c.EstaHabilitada && c.MontoAplicar > 0m)
                    .OrderBy(c => c.Fecha)
                    .ToList();

                if (seleccion.Count == 0)
                {
                    MensajeValidacion = "Selecciona al menos una clase y un monto mayor a 0.";
                            return;
                        }
                        
                var resultados = new List<RegistrarClaseResult>();

                foreach (var clase in seleccion)
                {
                    ct.ThrowIfCancellationRequested();

                    var monto = Math.Round(clase.MontoAplicar, 2, MidpointRounding.AwayFromZero);
                    if (monto <= 0m)
                    {
                        continue;
                    }
                            
                            var r = await _claseService.RegistrarClaseAsync(
                        AlumnoSeleccionado.AlumnoId,
                        TallerSeleccionado.TallerId,
                        clase.Fecha,
                        monto,
                        ct);

                            resultados.Add(r);
                        }
                        
                if (resultados.Count == 0)
                {
                    _dialogService.Alerta("No se registró ningún movimiento.", "Clases");
                    return;
                }

                var mensaje = ConstruirResumenPago(seleccion, resultados);
                _dialogService.Info(mensaje, "Pago registrado");

                WeakReferenceMessenger.Default.Send(new ClasesActualizadasMessage(AlumnoSeleccionado.AlumnoId));

                await CargarClasesDisponiblesAsync();

                var continuar = _dialogService.Confirmar("¿Desea registrar otro pago?", "Clases");
                if (!continuar)
                {
                    LimpiarSeleccion();
                }
            }
            catch (OperationCanceledException)
            {
                MensajeValidacion = "Operación cancelada.";
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

        private async Task CargarClasesDisponiblesAsync()
        {
            MensajeValidacion = null;
            LimpiarClases();

            if (AlumnoSeleccionado is null || TallerSeleccionado is null)
                return;

            try
            {
                var alumnoId = AlumnoSeleccionado.AlumnoId;
                var tallerId = TallerSeleccionado.TallerId;
                var hoy = DateTime.Today;

                var clasesFinancieras = await _claseService.ObtenerClasesFinancierasAsync(alumnoId, tallerId);

                var fechasConCargo = clasesFinancieras
                    .Select(c => c.FechaClase.Date)
                    .ToHashSet();

                var costoClaseAlumno = ObtenerCostoClaseAlumno();

                var clasesConSaldo = clasesFinancieras
                    .Where(c => c.SaldoActual > 0m)
                    .OrderBy(c => c.FechaClase)
                    .Select(c => new
                    {
                        Fecha = c.FechaClase.Date,
                        SaldoPendiente = NormalizarMonto(c.SaldoActual)
                    })
                    .ToList();

                bool hayPendientes = clasesConSaldo.Any(c => c.SaldoPendiente > 0m && c.Fecha <= hoy);
                bool permitirFuturas = !hayPendientes;

                var agregadas = new HashSet<DateTime>();

                // 1) PENDIENTES VENCIDAS O DE HOY (con cargo y saldo, fecha <= hoy) -> habilitadas y seleccionadas
                foreach (var clase in clasesConSaldo.Where(c => c.Fecha <= hoy))
                {
                    var fecha = clase.Fecha;
                    agregadas.Add(fecha);

                    if (clase.SaldoPendiente <= 0m)
                    {
                        continue;
                    }

                    var item = new ClasePagoItem(
                        fecha,
                        clase.SaldoPendiente,
                        esCargoExistente: true,
                        estaHabilitada: true);

                    AgregarClase(item, seleccionar: true);
                }

                // 2) FUTURAS CON SALDO (fecha > hoy)
                //    Si hay pendientes: NO se pueden ingresar (deshabilitadas y no seleccionadas).
                //    Si no hay pendientes: habilitadas y seleccionadas.
                foreach (var clase in clasesConSaldo.Where(c => c.Fecha > hoy))
                {
                    var fecha = clase.Fecha;
                    if (agregadas.Contains(fecha)) continue;

                    agregadas.Add(fecha);
                    if (clase.SaldoPendiente <= 0m)
                    {
                        continue;
                    }

                    var habilitada = permitirFuturas;      // false si hay pendientes
                    var seleccionada = permitirFuturas;    // false si hay pendientes

                    var item = new ClasePagoItem(
                        fecha,
                        clase.SaldoPendiente,
                        esCargoExistente: true,
                        estaHabilitada: habilitada);

                    AgregarClase(item, seleccionar: seleccionada);
                }

                // 3) PASADAS ESPERADAS SIN CARGO (desde inicio hasta hoy o fin si es antes)
                //    Estas sí se pueden pagar aun con pendientes.
                {
                    var inicio = TallerSeleccionado.FechaInicio.Date;
                    var limite = TallerSeleccionado.FechaFin?.Date;
                    var finCalculo = limite.HasValue && limite.Value < hoy ? limite.Value : hoy;

                    var fechaBase = AjustarAlDia(inicio, TallerSeleccionado.DiaSemana);
                    for (var f = fechaBase; f <= finCalculo; f = f.AddDays(7))
                    {
                        if (agregadas.Contains(f)) continue;
                        if (fechasConCargo.Contains(f)) continue; // ya existe cargo (pagado o no)

                        var item = new ClasePagoItem(
                            f,
                            costoClaseAlumno,
                            esCargoExistente: false,
                            estaHabilitada: true);

                        AgregarClase(item, seleccionar: true);
                        agregadas.Add(f);
                    }
                }

                // 4) FUTURAS ESPERADAS SIN CARGO
                //    Si hay pendientes: NO se pueden ingresar (deshabilitadas, no seleccionadas).
                //    Si no hay pendientes: habilitadas y seleccionadas.
                var futurasEsperadas = await GenerarClasesFuturasAsync();
                foreach (var fecha in futurasEsperadas)
                {
                    if (agregadas.Contains(fecha)) continue;
                    if (fechasConCargo.Contains(fecha)) continue;

                    var habilitada = permitirFuturas;      // false si hay pendientes
                    var seleccionada = permitirFuturas;    // false si hay pendientes

                    var item = new ClasePagoItem(
                        fecha,
                        costoClaseAlumno,
                        esCargoExistente: false,
                        estaHabilitada: habilitada);

                    AgregarClase(item, seleccionar: seleccionada);
                    agregadas.Add(fecha);
                }

                // Marcar por defecto solo la clase de HOY (si está habilitada)
                foreach (var item in ClasesParaPago)
                {
                    item.IsSeleccionada = item.EstaHabilitada && item.Fecha.Date == hoy;
                }

                // Nota: la clase de HOY ya queda cubierta:
                // - si tiene cargo con saldo -> bloque 1
                // - si no tiene cargo -> bloque 3 genera hoy al calcular hasta 'hoy'

                ActualizarTotales();
            }
            catch (Exception ex)
            {
                _dialogService.Error($"No se pudieron cargar las clases: {ex.Message}", "Clases");
            }
        }



        private async Task<DateTime[]> GenerarClasesFuturasAsync()
        {
            if (AlumnoSeleccionado is null || TallerSeleccionado is null)
            {
                return Array.Empty<DateTime>();
            }

            try
            {
                var clasesPagadas = await _claseService.ObtenerClasesPagadasAsync(
                    AlumnoSeleccionado.AlumnoId,
                    TallerSeleccionado.TallerId);

                DateTime inicioBase;
                if (clasesPagadas.Length > 0)
                {
                    var ultima = clasesPagadas
                        .OrderByDescending(c => c.Fecha)
                        .First()
                        .Fecha.Date;

                    inicioBase = ultima.AddDays(7);
                }
                else
                {
                    inicioBase = TallerSeleccionado.FechaInicio.Date;
                }

                var objetivo = TallerSeleccionado.DiaSemana;
                var fecha = AjustarAlDia(inicioBase, objetivo);
                        var hoy = DateTime.Today;
                var fin = TallerSeleccionado.FechaFin?.Date;

                var fechas = new List<DateTime>();

                while (fechas.Count < MaxClasesFuturas)
                {
                    if (fin.HasValue && fecha > fin.Value)
                    {
                        break;
                    }

                    if (fecha < hoy)
                    {
                        fecha = fecha.AddDays(7);
                        continue;
                    }

                    if (!fechas.Contains(fecha))
                    {
                        fechas.Add(fecha);
                    }

                    fecha = fecha.AddDays(7);
                }

                return fechas.ToArray();
                }
                catch
                {
                return Array.Empty<DateTime>();
            }
        }

        private void AgregarClase(ClasePagoItem item, bool seleccionar)
        {
            item.PropertyChanged += OnClaseItemPropertyChanged;
            item.InicializarSeleccion(seleccionar);
            ClasesParaPago.Add(item);
        }

        private void LimpiarClases()
        {
            foreach (var item in ClasesParaPago)
            {
                item.PropertyChanged -= OnClaseItemPropertyChanged;
            }

            ClasesParaPago = new ObservableCollection<ClasePagoItem>();
            HayClasesParaPago = false;
            TotalClasesSeleccionadas = 0;
            TotalSeleccionado = 0m;
        }

        private void OnClaseItemPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ClasePagoItem.IsSeleccionada) ||
                e.PropertyName == nameof(ClasePagoItem.MontoAplicar))
            {
                ActualizarTotales();
            }
        }

        private void ActualizarTotales()
        {
            HayClasesParaPago = ClasesParaPago.Count > 0;

            var seleccion = ClasesParaPago
                .Where(c => c.IsSeleccionada && c.EstaHabilitada && c.MontoAplicar > 0m)
                .ToList();

            TotalClasesSeleccionadas = seleccion.Count;
            TotalSeleccionado = Math.Round(seleccion.Sum(c => c.MontoAplicar), 2, MidpointRounding.AwayFromZero);

            if (TotalClasesSeleccionadas > 0 && MensajeValidacion != null && MensajeValidacion.Contains("Selecciona"))
            {
                MensajeValidacion = null;
            }
        }

        private bool FechaPerteneceAlTaller(DateTime fecha)
        {
            if (TallerSeleccionado is null)
            {
                return false;
            }

            var inicio = TallerSeleccionado.FechaInicio.Date;
            if (fecha.Date < inicio)
            {
                return false;
            }

            var fin = TallerSeleccionado.FechaFin?.Date;
            if (fin.HasValue && fecha.Date > fin.Value)
            {
                return false;
            }

            return TallerSeleccionado.DiaSemana == fecha.DayOfWeek;
        }

        private static DateTime AjustarAlDia(DateTime fecha, DayOfWeek objetivo)
        {
            var delta = ((int)objetivo - (int)fecha.DayOfWeek + 7) % 7;
            return fecha.AddDays(delta);
        }

        private string ConstruirResumenPago(IReadOnlyList<ClasePagoItem> seleccion, IReadOnlyList<RegistrarClaseResult> resultados)
        {
            var sb = new StringBuilder();
            for (int i = 0; i < seleccion.Count && i < resultados.Count; i++)
            {
                var clase = seleccion[i];
                var resultado = resultados[i];
                var fecha = clase.Fecha.ToString("dd/MM/yyyy");

                if (resultado.CargoYaPagado)
                {
                    sb.AppendLine($"- {fecha}: el cargo ya estaba pagado.");
                    continue;
                }

                if (resultado.MontoAplicado > 0m)
                {
                    sb.AppendLine($"- {fecha}: se aplicaron {resultado.MontoAplicado:C2}.");
                    if (resultado.ExcedenteAplicado > 0m)
                    {
                        sb.AppendLine($"  Excedente aplicado: {resultado.ExcedenteAplicado:C2}.");
                    }
                }
                else if (resultado.ClaseCreada || resultado.CargoCreado || resultado.PagoCreado)
                {
                    sb.AppendLine($"- {fecha}: movimiento registrado.");
                }
                else
                {
                    sb.AppendLine($"- {fecha}: sin cambios.");
                }
            }

            return sb.Length > 0 ? sb.ToString() : "Pago registrado.";
        }

        private decimal ObtenerCostoClaseAlumno()
        {
            var descuento = AlumnoSeleccionado?.DescuentoPorClase ?? 0m;
            var costoConDescuento = Math.Max(1m, _costoClase - descuento);
            return Math.Round(costoConDescuento, 2, MidpointRounding.AwayFromZero);
        }

        private static decimal NormalizarMonto(decimal valor)
        {
            return Math.Round(Math.Max(0m, valor), 2, MidpointRounding.AwayFromZero);
        }

        private static decimal ClampCosto(decimal valor)
        {
            if (valor < MinCostoClase) return MinCostoClase;
            if (valor > MaxCostoClase) return MaxCostoClase;
            return valor;
        }
    }

    public partial class ClasePagoItem : ObservableObject
    {
        private bool _ajustandoMonto;
        private bool _ajustandoSeleccion;

        public ClasePagoItem(DateTime fecha, decimal saldoPendiente, bool esCargoExistente, bool estaHabilitada)
        {
            Fecha = fecha.Date;
            SaldoPendiente = Math.Round(saldoPendiente, 2, MidpointRounding.AwayFromZero);
            SaldoMaximo = SaldoPendiente;
            EsCargoExistente = esCargoExistente;
            EstaHabilitada = estaHabilitada;
            montoAplicar = SaldoPendiente;
        }

        public DateTime Fecha { get; }
        public decimal SaldoPendiente { get; }
        public decimal SaldoMaximo { get; }
        public bool EsCargoExistente { get; }
        public bool EstaHabilitada { get; }

        public string FechaTexto => Fecha.ToString("dd/MM/yyyy");

        public string EstadoDescripcion
            => !EstaHabilitada
                ? "Futura bloqueada"
                : (Fecha.Date < DateTime.Today
                    ? "Pendiente"
                    : (Fecha.Date == DateTime.Today ? "Actual" : "Futura"));

        [ObservableProperty]
        private bool isSeleccionada;

        [ObservableProperty]
        private decimal montoAplicar;

        public bool PuedeEditarMonto => EstaHabilitada && IsSeleccionada;

        public void InicializarSeleccion(bool seleccionar)
        {
            _ajustandoSeleccion = true;
            _ajustandoSeleccion = false;
            OnPropertyChanged(nameof(PuedeEditarMonto));
        }

        partial void OnIsSeleccionadaChanged(bool value)
        {
            if (_ajustandoSeleccion) return;

            if (!EstaHabilitada && value)
            {
                _ajustandoSeleccion = true;
                IsSeleccionada = false;
                _ajustandoSeleccion = false;
                return;
            }

            OnPropertyChanged(nameof(PuedeEditarMonto));

            if (value && MontoAplicar <= 0m)
            {
                _ajustandoMonto = true;
                MontoAplicar = SaldoMaximo;
                _ajustandoMonto = false;
            }
        }

        partial void OnMontoAplicarChanged(decimal value)
        {
            var clamped = ClampMonto(value);
            if (clamped != value)
            {
                if (_ajustandoMonto) return;
                _ajustandoMonto = true;
                MontoAplicar = clamped;
                _ajustandoMonto = false;
                return;
            }

            if (MontoAplicar == 0m && IsSeleccionada)
            {
                _ajustandoSeleccion = true;
                IsSeleccionada = false;
                _ajustandoSeleccion = false;
                OnPropertyChanged(nameof(PuedeEditarMonto));
            }
        }

        private decimal ClampMonto(decimal valor)
        {
            if (valor < 0m) valor = 0m;
            if (valor > SaldoMaximo) valor = SaldoMaximo;
            return Math.Round(valor, 2, MidpointRounding.AwayFromZero);
        }
    }
}