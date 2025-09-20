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
        partial void OnAlumnoSeleccionadoChanged(Alumno? value)
        {
            ActualizarMostrarAvisoClasesPendientes();
            ActualizarInformacionFechasClases();
        }
        [ObservableProperty] private ObservableCollection<Taller> talleresDelAlumno = new();
        [ObservableProperty] private Taller? tallerSeleccionado;
        partial void OnTallerSeleccionadoChanged(Taller? value)
        {
            if (AlumnoSeleccionado != null)
            {
                RecalcularSugeridoYQuizasMonto(forceSetMonto: true);
            }
            ActualizarMostrarAvisoClasesPendientes();
            ActualizarInformacionFechasClases();
        }

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
            ActualizarInformacionFechasClases();
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
            
            // Calcular el límite máximo basado en la cantidad de clases seleccionadas
            var limiteMaximo = CantidadSeleccionada * CostoClase;
            if (v > limiteMaximo) v = limiteMaximo;
            
            if (MontoIngresado != v)
            {
                _ajustandoMonto = true;
                MontoIngresado = v;
                _ajustandoMonto = false;
            }
            
            // Actualizar mensaje de validación en tiempo real
            if (value.HasValue && value > 0m)
            {
                if (!MostrarControlesCantidad)
                {
                    // Hay clases pendientes: validar contra el monto sugerido
                    if (value > MontoSugerido)
                    {
                        MensajeValidacion = $"El monto no puede superar el necesario para pagar todas las clases pendientes ({MontoSugerido:C2}).";
                    }
                    else if (MensajeValidacion != null && MensajeValidacion.Contains("no puede superar"))
                    {
                        MensajeValidacion = null; // Limpiar mensaje si ya no aplica
                    }
                }
                else
                {
                    // No hay clases pendientes: limpiar mensajes de validación de límite
                    if (MensajeValidacion != null && MensajeValidacion.Contains("no puede superar"))
                    {
                        MensajeValidacion = null; // Limpiar mensaje si ya no aplica
                    }
                }
            }
            else if (MensajeValidacion != null && MensajeValidacion.Contains("no puede superar"))
            {
                MensajeValidacion = null; // Limpiar mensaje si ya no aplica
            }
            
            // Actualizar información de fechas cuando cambie el monto
            ActualizarInformacionFechasClases();
        }

        [ObservableProperty] private decimal montoSugerido;
        [ObservableProperty] private string? mensajeValidacion;
        [ObservableProperty] private string? notificacionTalleresSinPagar;
        [ObservableProperty] private bool tieneTalleresSinPagar;
        [ObservableProperty] private string? informacionClasesPendientes;
        [ObservableProperty] private bool mostrarControlesCantidad = true;
        [ObservableProperty] private string? avisoClasesPendientes;
        partial void OnAvisoClasesPendientesChanged(string? value)
        {
            ActualizarMostrarAvisoClasesPendientes();
        }
        
        [ObservableProperty] private bool mostrarAvisoClasesPendientes;
        [ObservableProperty] private string? informacionFechasClases;
        [ObservableProperty] private bool mostrarInformacionFechas;
        
        private void ActualizarMostrarAvisoClasesPendientes()
        {
            MostrarAvisoClasesPendientes = !string.IsNullOrEmpty(AvisoClasesPendientes) && AlumnoSeleccionado != null;
        }

        [RelayCommand]
private async Task BuscarAlumno()
{
    var alumno = _alumnoPicker.Pick();
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
    var inscripciones = await _inscripcionService.ObtenerInscripcionesAsync(alumno.AlumnoId);

    // Activas (no eliminadas)
    var activas = inscripciones.Where(i => !i.Eliminado).ToArray();

    // ÚNICO caso con diálogo: sin inscripciones
    if (activas.Length == 0)
    {
        _dialogService.Alerta("Este alumno no está inscrito en ningún taller.\nInscríbalo en alguno para poder registrar sus pagos a clases.");
        return;
    }

    // Particionar por estado
    var inscripcionesPagadas    = activas.Where(i => i.Estado == EstadoInscripcion.Pagada).ToArray();
    var inscripcionesPendientes = activas.Where(i => i.Estado == EstadoInscripcion.Pendiente).ToArray();

    // ---- Notificación (no bloquea) de inscripciones pendientes con monto ----
    // Si existiera más de una inscripción al mismo taller, agrupamos y sumamos saldos.
    var pendientesAgrupadas = inscripcionesPendientes
        .Where(i => i.Taller != null)
        .GroupBy(i => i.Taller!.Nombre)
        .Select(g => new
        {
            Taller = g.Key,
            MontoPendiente = g.Sum(x => Math.Max(0m, x.SaldoActual)) // usa SaldoActual de tu modelo
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

    // Talleres disponibles: incluye pagadas y pendientes (no bloquea pagos de clase)
    var talleresDisponibles = new ObservableCollection<Taller>(
        activas
            .Where(i => i.Taller != null)
            .Select(i => i.Taller!)
            .GroupBy(t => t.TallerId)
            .Select(g => g.First())
            .OrderBy(t => t.Nombre)
            .ToList()
    );

    TalleresDelAlumno = talleresDisponibles;
    AlumnoSeleccionado = alumno;
    AlumnoNombre = alumno.Nombre;
    ActualizarMostrarAvisoClasesPendientes();

    // Sugerir por defecto uno que "pueda pagar clase hoy" (opcional, no muestra diálogos)
    var ids = talleresDisponibles.Select(t => t.TallerId).ToArray();
    var estados = await _claseService.ObtenerEstadoPagoHoyAsync(alumno.AlumnoId, ids, DateTime.Today);
    var disponibles = estados.Where(e => e.PuedePagar).Select(e => e.TallerId).ToHashSet();

    TallerSeleccionado = TalleresDelAlumno.FirstOrDefault(t => disponibles.Contains(t.TallerId))
                       ?? TalleresDelAlumno.FirstOrDefault();

    // Recalcular monto sugerido basado en clases pendientes
    RecalcularSugeridoYQuizasMonto(forceSetMonto: true);
}



        [RelayCommand]
        private void LimpiarSeleccion()
        {
            // Limpiar notificaciones primero
            NotificacionTalleresSinPagar = null;
            TieneTalleresSinPagar = false;
            InformacionClasesPendientes = null;
            MostrarControlesCantidad = true;
            AvisoClasesPendientes = null;
            
            // Limpiar selecciones
            AlumnoSeleccionado = null;
            AlumnoNombre = string.Empty;
            TallerSeleccionado = null;
            TalleresDelAlumno = new();

            // Actualizar visibilidad del aviso
            ActualizarMostrarAvisoClasesPendientes();

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

                var costo = Math.Round(CostoClase, 2, MidpointRounding.AwayFromZero);
                var totalIngresado = Math.Round(MontoIngresado.Value, 2, MidpointRounding.AwayFromZero);

                // Obtener clases pendientes de pago desde la fecha de inicio del taller
                var clasesPendientes = await ObtenerClasesPendientesPagoAsync(AlumnoSeleccionado.AlumnoId, TallerSeleccionado.TallerId);

                var resultados = new List<RegistrarClaseResult>();

                if (clasesPendientes.Length > 0)
                {
                    // HAY clases pendientes: permitir pagos con excedentes
                    var montoNecesario = R2(clasesPendientes.Length * CostoClase);
                    
                    // Validar que el monto sea mayor a 0
                    if (totalIngresado <= 0)
                    {
                        MensajeValidacion = "El monto debe ser mayor a 0.";
                        return;
                    }
                    
                    // Aplicar pago a las clases pendientes en orden cronológico
                    var montoRestante = totalIngresado;
                    for (int i = 0; i < clasesPendientes.Length && montoRestante > 0; i++)
                    {
                        var montoAPagar = Math.Min(costo, montoRestante);
                        var r = await _claseService.RegistrarClaseAsync(
                            AlumnoSeleccionado.AlumnoId, TallerSeleccionado.TallerId, clasesPendientes[i], montoAPagar, ct);
                        resultados.Add(r);
                        montoRestante -= montoAPagar;
                    }
                    
                    // Si queda monto restante después de pagar todas las clases pendientes, aplicarlo como excedente
                    if (montoRestante > 0m)
                    {
                        // Calcular la siguiente clase después de la última clase pendiente
                        var ultimaClasePendiente = clasesPendientes[clasesPendientes.Length - 1];
                        var siguienteFecha = ultimaClasePendiente.AddDays(7);
                        
                        // Ajustar al día de la semana correcto del taller
                        var objetivo = TallerSeleccionado.DiaSemana;
                        int delta = ((int)objetivo - (int)siguienteFecha.DayOfWeek + 7) % 7;
                        var fechaSiguienteClase = siguienteFecha.AddDays(delta);
                        
                        // Aplicar el excedente a la siguiente clase
                        var r = await _claseService.RegistrarClaseAsync(
                            AlumnoSeleccionado.AlumnoId, TallerSeleccionado.TallerId, fechaSiguienteClase, montoRestante, ct);
                        resultados.Add(r);
                    }
                }
                else
                {
                    // NO hay clases pendientes: aplicar reglas según fecha de fin del taller
                    // Permitir pagos parciales sin validar cantidad de clases

                    // Verificar si el taller tiene fecha de fin
                    var fechaFin = TallerSeleccionado.FechaFin?.Date;
                    var hoy = DateTime.Today;
                    
                    if (fechaFin.HasValue)
                    {
                        // CON fecha de fin: permitir pagos en conjunto hasta la fecha de fin
                        if (hoy > fechaFin.Value)
                        {
                            MensajeValidacion = "El taller ya terminó. No se pueden registrar pagos para clases futuras.";
                            return;
                        }
                        
                        // Generar fechas a partir de la última clase pagada
                        var fechas = await CalcularFechasDesdeUltimaClasePagada();
                        if (fechas.Length == 0)
                        {
                            MensajeValidacion = "No se pueden generar clases futuras dentro del período del taller.";
                            return;
                        }
                        
                        // Validar que el monto no exceda las clases disponibles (permitir pagos parciales)
                        var montoMaximo = R2(fechas.Length * CostoClase);
                        if (totalIngresado > montoMaximo)
                        {
                            MensajeValidacion = $"El monto no puede superar el necesario para pagar {fechas.Length} clases disponibles ({montoMaximo:C2}).";
                            return;
                        }
                        
                        // Validar que el monto sea mayor a 0
                        if (totalIngresado <= 0)
                        {
                            MensajeValidacion = "El monto debe ser mayor a 0.";
                            return;
                        }
                        
                        // Procesar pagos para las fechas generadas
                        var clasesCompletas = (int)Math.Min(fechas.Length, Math.Truncate(totalIngresado / (costo == 0 ? 1 : costo)));
                        var sobrante = totalIngresado - (clasesCompletas * costo);
                        
                        for (int i = 0; i < clasesCompletas; i++)
                        {
                            var r = await _claseService.RegistrarClaseAsync(
                                AlumnoSeleccionado.AlumnoId, TallerSeleccionado.TallerId, fechas[i], costo, ct);
                            resultados.Add(r);
                        }

                        if (clasesCompletas > 0 && sobrante > 0m && clasesCompletas < fechas.Length)
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
                        else if (clasesCompletas > 0 && sobrante > 0m && clasesCompletas >= fechas.Length)
                        {
                            // Si se pagaron todas las clases disponibles y hay sobrante, aplicarlo a la siguiente clase
                            var ultimaFecha = fechas[fechas.Length - 1];
                            var siguienteFecha = ultimaFecha.AddDays(7);
                            
                            // Ajustar al día de la semana correcto del taller
                            var objetivo = TallerSeleccionado.DiaSemana;
                            int delta = ((int)objetivo - (int)siguienteFecha.DayOfWeek + 7) % 7;
                            var fechaSiguienteClase = siguienteFecha.AddDays(delta);
                            
                            var r = await _claseService.RegistrarClaseAsync(
                                AlumnoSeleccionado.AlumnoId, TallerSeleccionado.TallerId, fechaSiguienteClase, sobrante, ct);
                            resultados.Add(r);
                        }
                    }
                    else
                    {
                        // SIN fecha de fin: permitir pagos en paquetes como talleres con fecha fin
                        // Permitir pagos parciales sin validar cantidad de clases

                        // Generar fechas a partir de la última clase pagada
                        var fechas = await CalcularFechasDesdeUltimaClasePagada();
                        if (fechas.Length == 0)
                        {
                            MensajeValidacion = "No se pueden generar clases futuras para este taller.";
                            return;
                        }
                        
                        // Validar que el monto no exceda las clases solicitadas (permitir pagos parciales)
                        var montoMaximo = R2(fechas.Length * CostoClase);
                        if (totalIngresado > montoMaximo)
                        {
                            MensajeValidacion = $"El monto no puede superar el necesario para pagar {fechas.Length} clases ({montoMaximo:C2}).";
                            return;
                        }
                        
                        // Validar que el monto sea mayor a 0
                        if (totalIngresado <= 0)
                        {
                            MensajeValidacion = "El monto debe ser mayor a 0.";
                            return;
                        }
                        
                        // Procesar pagos para las fechas generadas
                        var clasesCompletas = (int)Math.Min(fechas.Length, Math.Truncate(totalIngresado / (costo == 0 ? 1 : costo)));
                        var sobrante = totalIngresado - (clasesCompletas * costo);
                        
                        for (int i = 0; i < clasesCompletas; i++)
                        {
                            var r = await _claseService.RegistrarClaseAsync(
                                AlumnoSeleccionado.AlumnoId, TallerSeleccionado.TallerId, fechas[i], costo, ct);
                            resultados.Add(r);
                        }
                        
                        // Si hay sobrante, aplicarlo a la siguiente clase
                        if (sobrante > 0 && clasesCompletas > 0)
                        {
                            var ultimaClase = fechas[clasesCompletas - 1];
                            var siguienteFecha = ultimaClase.AddDays(7);
                            
                            // Ajustar al día de la semana correcto del taller
                            var objetivo = TallerSeleccionado.DiaSemana;
                            int delta = ((int)objetivo - (int)siguienteFecha.DayOfWeek + 7) % 7;
                            var fechaSiguienteClase = siguienteFecha.AddDays(delta);
                            
                            var r = await _claseService.RegistrarClaseAsync(
                                AlumnoSeleccionado.AlumnoId, TallerSeleccionado.TallerId, fechaSiguienteClase, sobrante, ct);
                            resultados.Add(r);
                        }
                        else if (clasesCompletas == 0 && totalIngresado > 0m)
                        {
                            var r = await _claseService.RegistrarClaseAsync(
                                AlumnoSeleccionado.AlumnoId, TallerSeleccionado.TallerId, fechas[0], totalIngresado, ct);
                            resultados.Add(r);
                        }
                    }
                }

                string F(DateTime d) => d.ToString("dd/MM/yyyy");

                bool huboCambios = resultados.Any(r => r.PagoCreado || r.CargoCreado || r.ClaseCreada || r.MontoAplicado > 0);
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
                var excedentes = resultados.Where(r => r.ExcedenteAplicado > 0m)
                                          .Select(r => new { r.Fecha, r.ExcedenteAplicado }).OrderBy(x => x.Fecha).ToList();

                // Construir mensaje de confirmación
                var mensaje = new List<string>();

                if (pagadasFull.Count > 0)
                {
                    var listado = string.Join(", ", pagadasFull.Select(F));
                    mensaje.Add($"Se registraron {pagadasFull.Count} clase(s) pagada(s) ({listado})");
                }

                if (parciales.Count > 0)
                {
                    var p = parciales.Last();
                    mensaje.Add($"Se registró un abono de {p.MontoAplicado:0.00} para la clase de {F(p.Fecha)}");
                }

                if (excedentes.Count > 0)
                {
                    var totalExcedente = excedentes.Sum(e => e.ExcedenteAplicado);
                    mensaje.Add($"Se aplicó un excedente de ${totalExcedente:0.00} a la siguiente clase");
                }

                if (mensaje.Count > 0)
                {
                    _dialogService.Info(string.Join(".\n", mensaje) + ".", "Clases");
                }

                // Notifica a quien muestre el registro que hay cambios
                WeakReferenceMessenger.Default.Send(new ClasesActualizadasMessage(AlumnoSeleccionado.AlumnoId));

                // Preguntar si quiere registrar otro pago
                var continuar = _dialogService.Confirmar("¿Desea registrar otro pago?", "Pago Registrado");
                if (continuar)
                {
                    // Mantener el alumno seleccionado y limpiar solo los campos de pago
                    TallerSeleccionado = null;
                    CantidadSeleccionada = 1;
                    MontoIngresado = null;
                    MontoSugerido = 0;
                    MensajeValidacion = null;
                    InformacionClasesPendientes = null;
                    AvisoClasesPendientes = null;
                    ActualizarMostrarAvisoClasesPendientes();
                    RecalcularSugeridoYQuizasMonto(forceSetMonto: true);
                }
                else
                {
                    // Limpiar toda la selección
                    LimpiarSeleccion();
                }
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
        private async void RecalcularSugeridoYQuizasMonto(bool forceSetMonto = false)
        {
            decimal nuevoSugerido = 0m;
            
            // Si hay un taller seleccionado, verificar si hay clases pendientes
            if (TallerSeleccionado != null && AlumnoSeleccionado != null)
            {
                try
                {
                    var clasesPendientes = await ObtenerClasesPendientesPagoAsync(AlumnoSeleccionado.AlumnoId, TallerSeleccionado.TallerId);
                    
                    if (clasesPendientes.Length > 0)
                    {
                        // HAY clases pendientes: actualizar la cantidad automáticamente
                        if (!_ajustandoCantidad)
                        {
                            _ajustandoCantidad = true;
                            CantidadSeleccionada = clasesPendientes.Length;
                            _ajustandoCantidad = false;
                        }
                        
                        // Sugerir monto para pagar todas las clases pendientes
                        nuevoSugerido = R2(clasesPendientes.Length * CostoClase);
                        
                        var fechas = clasesPendientes.Select(f => f.ToString("dd/MM/yyyy")).ToArray();
                        var fechasTexto = string.Join(", ", fechas);
                        InformacionClasesPendientes = $"Clases pendientes ({clasesPendientes.Length}): {fechasTexto}";
                        MostrarControlesCantidad = true; // Mostrar controles de cantidad para que el usuario vea la actualización
                        
                        // Mostrar aviso de que se están pagando clases no pagadas
                        AvisoClasesPendientes = "⚠️ ATENCIÓN: Se están pagando clases que no fueron pagadas o no se terminaron de pagar anteriormente. El pago se aplicará a las clases pendientes en orden cronológico.";
                        ActualizarMostrarAvisoClasesPendientes();
                    }
                    else
                    {
                        // NO hay clases pendientes: aplicar reglas según fecha de fin del taller
                        var fechaFin = TallerSeleccionado.FechaFin?.Date;
                        var hoy = DateTime.Today;
                        
                        if (fechaFin.HasValue)
                        {
                            // CON fecha de fin: permitir pagos en conjunto hasta la fecha de fin
                            if (hoy <= fechaFin.Value)
                            {
                                // Generar fechas disponibles a partir de la última clase pagada
                                var fechasDisponibles = await CalcularFechasDesdeUltimaClasePagada();
                                if (fechasDisponibles.Length > 0)
                                {
                                    nuevoSugerido = R2(fechasDisponibles.Length * CostoClase);
                                    InformacionClasesPendientes = $"Clases futuras disponibles ({fechasDisponibles.Length}): {string.Join(", ", fechasDisponibles.Select(f => f.ToString("dd/MM/yyyy")))}";
                                }
                                else
                                {
                                    nuevoSugerido = 0;
                                    InformacionClasesPendientes = "No hay clases futuras disponibles dentro del período del taller.";
                                }
                            }
                            else
                            {
                                nuevoSugerido = 0;
                                InformacionClasesPendientes = "El taller ya terminó. No se pueden registrar pagos para clases futuras.";
                            }
                            MostrarControlesCantidad = true; // Mostrar controles de cantidad
                            AvisoClasesPendientes = null; // Limpiar aviso
                        }
                        else
                        {
                            // SIN fecha de fin: permitir pagos en paquetes a partir de la última clase pagada
                            var fechas = await CalcularFechasDesdeUltimaClasePagada();
                            if (fechas.Length > 0)
                            {
                                nuevoSugerido = R2(fechas.Length * CostoClase);
                                InformacionClasesPendientes = $"Taller sin fecha de fin - se pueden pagar {fechas.Length} clases";
                            }
                            else
                            {
                                nuevoSugerido = 0;
                                InformacionClasesPendientes = "No se pueden generar clases futuras para este taller.";
                            }
                            MostrarControlesCantidad = true; // Mostrar controles de cantidad
                            AvisoClasesPendientes = null; // Limpiar aviso
                        }
                        ActualizarMostrarAvisoClasesPendientes();
                    }
                }
                catch
                {
                    // Si hay error, usar la lógica anterior
                    nuevoSugerido = R2(CantidadSeleccionada * CostoClase);
                    InformacionClasesPendientes = null;
                    AvisoClasesPendientes = null;
                    ActualizarMostrarAvisoClasesPendientes();
                }
            }
            else
            {
                // Lógica anterior para cuando no hay taller seleccionado
                nuevoSugerido = R2(CantidadSeleccionada * CostoClase);
                InformacionClasesPendientes = null;
                AvisoClasesPendientes = null;
                ActualizarMostrarAvisoClasesPendientes();
            }

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
            else if (MontoIngresado.HasValue && MontoIngresado.Value != MontoSugerido)
            {
                // Si el monto ingresado no coincide con el sugerido, actualizarlo
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
            
            // Actualizar información de fechas después de recalcular
            ActualizarInformacionFechasClases();
        }

        private DateTime[] GenerarFechasSemanas(DateTime desde, int cantidad)
        {
            if (TallerSeleccionado is null || cantidad <= 0)
                return Array.Empty<DateTime>();

            var objetivo = TallerSeleccionado.DiaSemana; // DayOfWeek del taller
            var start = desde.Date;
            var fechaFin = TallerSeleccionado.FechaFin?.Date;

            // Próximo (o mismo) día objetivo respecto a 'desde'
            int delta = ((int)objetivo - (int)start.DayOfWeek + 7) % 7;
            var primera = start.AddDays(delta); // incluye 'hoy' si coincide

            var clases = new List<DateTime>();
            var fechaActual = primera;
            
            // Generar clases hasta la cantidad solicitada o hasta la fecha de fin (si existe)
            for (int i = 0; i < cantidad; i++)
            {
                // Si hay fecha de fin, verificar que no la excedamos
                if (fechaFin.HasValue && fechaActual > fechaFin.Value)
                    break;
                    
                clases.Add(fechaActual.Date);
                fechaActual = fechaActual.AddDays(7);
            }

            return clases.ToArray();
        }

        private DateTime[] GenerarFechasDesdeInicioTaller(int cantidad)
        {
            if (TallerSeleccionado is null || cantidad <= 0)
                return Array.Empty<DateTime>();

            var objetivo = TallerSeleccionado.DiaSemana;
            var fechaInicio = TallerSeleccionado.FechaInicio.Date;
            var hoy = DateTime.Today;

            // Calcular la primera clase desde la fecha de inicio del taller
            int delta = ((int)objetivo - (int)fechaInicio.DayOfWeek + 7) % 7;
            var primeraClase = fechaInicio.AddDays(delta);

            // Generar todas las clases desde la primera hasta hoy (o más si se especifica)
            var clases = new List<DateTime>();
            var fechaActual = primeraClase;
            
            while (fechaActual <= hoy && clases.Count < cantidad)
            {
                clases.Add(fechaActual);
                fechaActual = fechaActual.AddDays(7);
            }

            return clases.ToArray();
        }

        private async Task<DateTime[]> ObtenerClasesPendientesPagoAsync(int alumnoId, int tallerId)
        {
            if (TallerSeleccionado is null) return Array.Empty<DateTime>();

            var objetivo = TallerSeleccionado.DiaSemana;
            var fechaInicio = TallerSeleccionado.FechaInicio.Date;
            var hoy = DateTime.Today;

            // Calcular la primera clase desde la fecha de inicio del taller
            int delta = ((int)objetivo - (int)fechaInicio.DayOfWeek + 7) % 7;
            var primeraClase = fechaInicio.AddDays(delta);

            // Generar todas las clases desde la primera hasta hoy
            var todasLasClases = new List<DateTime>();
            var fechaActual = primeraClase;
            
            while (fechaActual <= hoy)
            {
                todasLasClases.Add(fechaActual);
                fechaActual = fechaActual.AddDays(7);
            }

            // Obtener el estado de pago de cada clase
            var estados = await _claseService.ObtenerEstadoPagoHoyAsync(alumnoId, new[] { tallerId }, hoy);
            var estadoTaller = estados.FirstOrDefault(e => e.TallerId == tallerId);

            // Si no hay estado, significa que no hay clases registradas aún
            if (estadoTaller == null)
            {
                return todasLasClases.ToArray();
            }

            // Obtener clases ya pagadas desde la base de datos
            var clasesPagadas = await _claseService.ObtenerClasesPagadasAsync(alumnoId, tallerId);
            var fechasPagadas = clasesPagadas.Select(c => c.Fecha.Date).ToHashSet();

            // Filtrar las clases pendientes
            var clasesPendientes = todasLasClases
                .Where(fecha => !fechasPagadas.Contains(fecha))
                .ToArray();

            return clasesPendientes;
        }

        private async void ActualizarInformacionFechasClases()
        {
            if (AlumnoSeleccionado == null || TallerSeleccionado == null)
            {
                InformacionFechasClases = null;
                MostrarInformacionFechas = false;
                return;
            }

            try
            {
                // Verificar si hay clases pendientes
                var clasesPendientes = await ObtenerClasesPendientesPagoAsync(AlumnoSeleccionado.AlumnoId, TallerSeleccionado.TallerId);
                
                if (clasesPendientes.Length > 0)
                {
                    // Hay clases pendientes: mostrar las fechas de las clases pendientes
                    var fechasTexto = string.Join(", ", clasesPendientes.Select(f => f.ToString("dd/MM/yyyy")));
                    
                    // Obtener el monto pendiente de la clase más lejana
                    var montoPendienteClaseLejana = await ObtenerMontoPendienteClaseAsync(clasesPendientes[clasesPendientes.Length - 1]);
                    
                    if (montoPendienteClaseLejana > 0)
                    {
                        var fechaLejana = clasesPendientes[clasesPendientes.Length - 1].ToString("dd/MM/yyyy");
                        InformacionFechasClases = $"Pagando clases del día: {fechasTexto}\nMonto pendiente de la clase del {fechaLejana}: ${montoPendienteClaseLejana:F2}";
                    }
                    else
                    {
                        InformacionFechasClases = $"Pagando clases del día: {fechasTexto}";
                    }
                    MostrarInformacionFechas = true;
                }
                else
                {
                    // No hay clases pendientes: calcular fechas futuras a partir de la última clase pagada
                    var fechasFuturas = await CalcularFechasDesdeUltimaClasePagada();
                    
                    if (fechasFuturas.Length == 0)
                    {
                        InformacionFechasClases = "No se pueden generar clases para este taller.";
                        MostrarInformacionFechas = true;
                        return;
                    }

                    // Calcular el monto total y el monto por clase
                    var montoTotal = CantidadSeleccionada * CostoClase;
                    var montoIngresado = MontoIngresado ?? 0m;
                    
                    if (montoIngresado >= montoTotal)
                    {
                        // Pago completo de todas las clases
                        var fechasTexto = string.Join(", ", fechasFuturas.Select(f => f.ToString("dd/MM/yyyy")));
                        
                        // Verificar si hay monto pendiente en la última clase
                        var ultimaFecha = fechasFuturas.Last();
                        var montoPendienteUltima = await ObtenerMontoPendienteClaseAsync(ultimaFecha);
                        
                        // Calcular si habrá excedente
                        var excedente = montoIngresado - montoTotal;
                        
                        if (montoPendienteUltima > 0)
                        {
                            var mensaje = $"Pagando clases del día: {fechasTexto}\nMonto pendiente real de la clase del {ultimaFecha:dd/MM/yyyy}: ${montoPendienteUltima:F2}";
                            if (excedente > 0)
                            {
                                var siguienteFecha = ultimaFecha.AddDays(7);
                                var objetivo = TallerSeleccionado.DiaSemana;
                                int delta = ((int)objetivo - (int)siguienteFecha.DayOfWeek + 7) % 7;
                                var fechaSiguienteClase = siguienteFecha.AddDays(delta);
                                mensaje += $"\nExcedente de ${excedente:F2} se aplicará a la clase del {fechaSiguienteClase:dd/MM/yyyy}";
                            }
                            InformacionFechasClases = mensaje;
                        }
                        else
                        {
                            var mensaje = $"Pagando clases del día: {fechasTexto}";
                            if (excedente > 0)
                            {
                                var siguienteFecha = ultimaFecha.AddDays(7);
                                var objetivo = TallerSeleccionado.DiaSemana;
                                int delta = ((int)objetivo - (int)siguienteFecha.DayOfWeek + 7) % 7;
                                var fechaSiguienteClase = siguienteFecha.AddDays(delta);
                                mensaje += $"\nExcedente de ${excedente:F2} se aplicará a la clase del {fechaSiguienteClase:dd/MM/yyyy}";
                            }
                            InformacionFechasClases = mensaje;
                        }
                        MostrarInformacionFechas = true;
                    }
                    else if (montoIngresado > 0)
                    {
                        // Pago parcial
                        var clasesCompletas = (int)Math.Truncate(montoIngresado / CostoClase);
                        var montoRestante = montoIngresado - (clasesCompletas * CostoClase);
                        
                        if (clasesCompletas > 0)
                        {
                            var fechasCompletas = fechasFuturas.Take(clasesCompletas).Select(f => f.ToString("dd/MM/yyyy"));
                            var fechasTexto = string.Join(", ", fechasCompletas);
                            
                            if (montoRestante > 0 && clasesCompletas < fechasFuturas.Length)
                            {
                                var fechaParcial = fechasFuturas[clasesCompletas].ToString("dd/MM/yyyy");
                                var montoPendienteReal = await ObtenerMontoPendienteClaseAsync(fechasFuturas[clasesCompletas]);
                                
                                var mensaje = $"Pagando clases del día: {fechasTexto} y ${montoRestante:F2} de la clase del día {fechaParcial}";
                                if (montoPendienteReal > 0)
                                {
                                    mensaje += $"\nMonto pendiente real de la clase del {fechaParcial}: ${montoPendienteReal:F2}";
                                }
                                
                                // Verificar si hay excedente después de aplicar el monto restante
                                var excedente = montoRestante - montoPendienteReal;
                                if (excedente > 0)
                                {
                                    mensaje += $"\nExcedente de ${excedente:F2} se aplicará a la siguiente clase";
                                }
                                
                                InformacionFechasClases = mensaje;
                            }
                            else
                            {
                                // Verificar si hay monto pendiente en la última clase completa
                                var ultimaFechaCompleta = fechasCompletas.Last();
                                var montoPendienteUltimaCompleta = await ObtenerMontoPendienteClaseAsync(DateTime.ParseExact(ultimaFechaCompleta, "dd/MM/yyyy", null));
                                
                                var mensaje = $"Pagando clases del día: {fechasTexto}";
                                if (montoPendienteUltimaCompleta > 0)
                                {
                                    mensaje += $"\nMonto pendiente real de la clase del {ultimaFechaCompleta}: ${montoPendienteUltimaCompleta:F2}";
                                }
                                
                                // Verificar si hay excedente
                                var excedente = montoIngresado - (clasesCompletas * CostoClase);
                                if (excedente > 0)
                                {
                                    mensaje += $"\nExcedente de ${excedente:F2} se aplicará a la siguiente clase";
                                }
                                
                                InformacionFechasClases = mensaje;
                            }
                            MostrarInformacionFechas = true;
                        }
                        else
                        {
                            // Solo pago parcial de la primera clase
                            var fechaParcial = fechasFuturas[0].ToString("dd/MM/yyyy");
                            var montoPendienteReal = await ObtenerMontoPendienteClaseAsync(fechasFuturas[0]);
                            
                            var mensaje = $"Pagando ${montoIngresado:F2} de la clase del día {fechaParcial}";
                            if (montoPendienteReal > 0)
                            {
                                mensaje += $"\nMonto pendiente real: ${montoPendienteReal:F2}";
                                
                                // Verificar si hay excedente después de aplicar el monto
                                var excedente = montoIngresado - montoPendienteReal;
                                if (excedente > 0)
                                {
                                    mensaje += $"\nExcedente de ${excedente:F2} se aplicará a la siguiente clase";
                                }
                            }
                            else
                            {
                                // Si no hay monto pendiente, todo el monto es excedente
                                var excedente = montoIngresado;
                                if (excedente > 0)
                                {
                                    mensaje += $"\nExcedente de ${excedente:F2} se aplicará a la siguiente clase";
                                }
                            }
                            
                            InformacionFechasClases = mensaje;
                            MostrarInformacionFechas = true;
                        }
                    }
                    else
                    {
                        // Sin monto ingresado, mostrar las fechas que se pagarían
                        var fechasTexto = string.Join(", ", fechasFuturas.Select(f => f.ToString("dd/MM/yyyy")));
                        InformacionFechasClases = $"Se pagarán clases del día: {fechasTexto}";
                        MostrarInformacionFechas = true;
                    }
                }
            }
            catch (Exception ex)
            {
                InformacionFechasClases = $"Error al calcular fechas: {ex.Message}";
                MostrarInformacionFechas = true;
            }
        }

        private async Task<DateTime[]> CalcularFechasDesdeUltimaClasePagada()
        {
            if (AlumnoSeleccionado == null || TallerSeleccionado == null)
                return Array.Empty<DateTime>();

            try
            {
                // Obtener las clases ya pagadas del alumno para este taller
                var clasesPagadas = await _claseService.ObtenerClasesPagadasAsync(
                    AlumnoSeleccionado.AlumnoId, 
                    TallerSeleccionado.TallerId);

                DateTime fechaInicio;
                
                if (clasesPagadas.Length > 0)
                {
                    // Encontrar la última clase pagada
                    var ultimaClasePagada = clasesPagadas
                        .OrderByDescending(c => c.Fecha)
                        .First();
                    
                    // Calcular la siguiente clase después de la última pagada
                    var objetivo = TallerSeleccionado.DiaSemana;
                    var ultimaFecha = ultimaClasePagada.Fecha.Date;
                    
                    // Calcular la siguiente clase (7 días después de la última)
                    var siguienteClase = ultimaFecha.AddDays(7);
                    
                    // Ajustar al día de la semana correcto si es necesario
                    int delta = ((int)objetivo - (int)siguienteClase.DayOfWeek + 7) % 7;
                    fechaInicio = siguienteClase.AddDays(delta);
                }
                else
                {
                    // No hay clases pagadas, empezar desde la fecha de inicio del taller
                    var objetivo = TallerSeleccionado.DiaSemana;
                    var fechaInicioTaller = TallerSeleccionado.FechaInicio.Date;
                    
                    // Calcular la primera clase desde la fecha de inicio
                    int delta = ((int)objetivo - (int)fechaInicioTaller.DayOfWeek + 7) % 7;
                    fechaInicio = fechaInicioTaller.AddDays(delta);
                }

                // Generar las fechas futuras desde la fecha calculada
                return GenerarFechasSemanas(fechaInicio, CantidadSeleccionada);
            }
            catch (Exception)
            {
                // En caso de error, usar la lógica anterior (desde hoy)
                return GenerarFechasSemanas(DateTime.Today, CantidadSeleccionada);
            }
        }

        private async Task<decimal> ObtenerMontoPendienteClaseAsync(DateTime fechaClase)
        {
            try
            {
                if (AlumnoSeleccionado == null || TallerSeleccionado == null)
                    return 0m;

                // Obtener las clases financieras para encontrar el monto pendiente
                var clasesFinancieras = await _claseService.ObtenerClasesFinancierasAsync(
                    AlumnoSeleccionado.AlumnoId,
                    TallerSeleccionado.TallerId,
                    fechaClase,
                    fechaClase);

                var clase = clasesFinancieras.FirstOrDefault(c => c.FechaClase.Date == fechaClase.Date);
                return clase?.SaldoActual ?? 0m;
            }
            catch
            {
                return 0m;
            }
        }


        private static decimal R2(decimal v) => Math.Round(v, 2, MidpointRounding.AwayFromZero);
        private static decimal ClampCosto(decimal v) => v < MinCostoClase ? MinCostoClase : (v > MaxCostoClase ? MaxCostoClase : v);
    }
}
