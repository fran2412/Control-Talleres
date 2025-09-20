using ControlTalleresMVP.Helpers.Dialogs;
using ControlTalleresMVP.Persistence.DataContext;
using ControlTalleresMVP.Persistence.ModelDTO;
using ControlTalleresMVP.Persistence.Models;
using ControlTalleresMVP.Services.Configuracion;
using ControlTalleresMVP.Services.Generaciones;
using ControlTalleresMVP.Messages;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.EntityFrameworkCore;

namespace ControlTalleresMVP.Services.Clases
{
    public class ClaseInfo
    {
        public DateTime Fecha { get; set; }
        public decimal Monto { get; set; }
        public decimal SaldoActual { get; set; }
        public EstadoCargo Estado { get; set; }
        public decimal MontoPagado { get; set; }
    }

    public class ClaseService : IClaseService
    {
        private readonly EscuelaContext _escuelaContext;
        private readonly IConfiguracionService _configuracionService;
        private readonly IDialogService _dialogService;
        private readonly IGeneracionService _generacionService;

        public ClaseService(EscuelaContext escuelaContext, IConfiguracionService configuracionService, IDialogService dialogService, IGeneracionService generacionService)
        {
            _escuelaContext = escuelaContext;
            _configuracionService = configuracionService;
            _dialogService = dialogService;
            _generacionService = generacionService;
        }

        // ====================
        // Registrar clase y cargo
        // ====================
        public async Task<RegistrarClaseResult> RegistrarClaseAsync(
    int alumnoId,
    int tallerId,
    DateTime fecha,
    decimal montoAbono,
    CancellationToken ct = default)
        {
            // 0) Resolver el día semanal del taller
            var tallerDia = await _escuelaContext.Talleres
                .Where(t => t.TallerId == tallerId && !t.Eliminado)
                .Select(t => (DayOfWeek?)t.DiaSemana)
                .FirstOrDefaultAsync(ct);

            if (tallerDia is null)
                throw new InvalidOperationException("Taller no encontrado o eliminado.");

            // 1) "Siguiente o el mismo" día semanal respecto a la fecha base
            //    Si hoy ya es el sábado (por ejemplo), ese mismo día cuenta.
            var day = SiguienteOElMismo(fecha.Date, tallerDia.Value, incluirSiHoyCoincide: true);

            // Validar que la fecha calculada coincida con el día de la semana del taller
            if (day.DayOfWeek != tallerDia.Value)
            {
                throw new InvalidOperationException(
                    $"La fecha de la clase ({day:dd/MM/yyyy}) no coincide con el día de la semana del taller ({ConvertirDiaSemanaASpanol(tallerDia.Value)}).");
            }

            var costoClase = Math.Max(1, _configuracionService.GetValor<int>("costo_clase", 150));

            bool claseCreada = false, cargoCreado = false, pagoCreado = false;
            decimal aplicado = 0m;
            bool yaPagado = false;
            decimal excedenteAplicado = 0m;

            // 2) Buscar o crear CLASE (única por Taller+Fecha)
            var clase = await _escuelaContext.Clases
                .FirstOrDefaultAsync(c => c.TallerId == tallerId && !c.Eliminado && c.Fecha.Date == day, ct);

            if (clase is null)
            {
                clase = new Clase { TallerId = tallerId, Fecha = day };
                _escuelaContext.Clases.Add(clase);
                await _escuelaContext.SaveChangesAsync(ct);
                claseCreada = true;
            }

            // 3) Buscar o crear CARGO (uno por alumno+clase)
            var cargo = await _escuelaContext.Cargos
                .FirstOrDefaultAsync(c => c.AlumnoId == alumnoId
                                       && c.ClaseId == clase.ClaseId
                                       && !c.Eliminado
                                       && c.Estado != EstadoCargo.Anulado, ct);

            if (cargo is null)
            {
                cargo = new Cargo
                {
                    AlumnoId = alumnoId,
                    ClaseId = clase.ClaseId,
                    Tipo = TipoCargo.Clase,
                    Fecha = day,               // ← usa la fecha "anclada" al día semanal
                    Monto = costoClase,
                    SaldoActual = costoClase,
                    Estado = EstadoCargo.Pendiente,
                    Eliminado = false
                };
                _escuelaContext.Cargos.Add(cargo);
                await _escuelaContext.SaveChangesAsync(ct);
                cargoCreado = true;
            }

            // 4) Aplicar abono (si hay)
            montoAbono = Math.Round(montoAbono, 2, MidpointRounding.AwayFromZero);
            if (montoAbono > 0m)
            {
                if (cargo.SaldoActual <= 0m)
                {
                    yaPagado = true;
                }
                else
                {
                    var aAplicar = Math.Min(montoAbono, cargo.SaldoActual);
                    if (aAplicar > 0m)
                    {
                        // Obtener información del taller para la descripción
                        var taller = await _escuelaContext.Talleres
                            .Where(t => t.TallerId == tallerId)
                            .Select(t => new { t.Nombre, t.DiaSemana })
                            .FirstOrDefaultAsync(ct);
                        
                        var diaSemana = ConvertirDiaSemanaASpanol(taller?.DiaSemana ?? DayOfWeek.Monday);
                        var descripcion = $"Pago de clase - {taller?.Nombre ?? "Taller"} ({diaSemana} {day:dd/MM/yyyy}) - Monto: ${aAplicar:F2}";
                        
                        var pago = new Pago
                        {
                            AlumnoId = alumnoId,
                            Fecha = DateTime.Now,
                            Metodo = MetodoPago.Efectivo, // ajusta según tu UI
                            MontoTotal = aAplicar,
                            Notas = descripcion
                        };
                        _escuelaContext.Pagos.Add(pago);
                        await _escuelaContext.SaveChangesAsync(ct);

                        var pa = new PagoAplicacion
                        {
                            PagoId = pago.PagoId,
                            CargoId = cargo.CargoId,
                            MontoAplicado = aAplicar,
                            Estado = EstadoAplicacionCargo.Vigente
                        };
                        _escuelaContext.PagoAplicaciones.Add(pa);

                        cargo.SaldoActual -= aAplicar;
                        cargo.Estado = cargo.SaldoActual == 0m
                            ? EstadoCargo.Pagado
                            : EstadoCargo.Pendiente;

                        await _escuelaContext.SaveChangesAsync(ct);

                        pagoCreado = true;
                        aplicado = aAplicar;

                        // 5) Verificar si hay excedente y aplicarlo a la siguiente clase
                        // El excedente es la diferencia entre lo que se pagó y lo que se aplicó a esta clase
                        var excedente = montoAbono - aAplicar;
                        if (excedente > 0m)
                        {
                            excedenteAplicado = await AplicarExcedenteASiguienteClaseAsync(
                                alumnoId, tallerId, day, excedente, taller?.Nombre ?? "Taller", ct);
                        }
                    }
                }
            }

            return new RegistrarClaseResult(
                Fecha: day,
                ClaseCreada: claseCreada,
                CargoCreado: cargoCreado,
                PagoCreado: pagoCreado,
                MontoAplicado: aplicado,
                CargoYaPagado: yaPagado,
                ExcedenteAplicado: excedenteAplicado
            );
        }


        // ====================
        // Cancelar clase
        // ====================
        public async Task CancelarAsync(int claseID, string? motivo = null, CancellationToken ct = default)
        {
            var clase = await _escuelaContext.Clases
                .FirstOrDefaultAsync(i => i.ClaseId == claseID, ct)
                      ?? throw new InvalidOperationException("Clase no encontrada.");

            if (motivo == null)
            {
                var motivoCancelacion = _dialogService.PedirTexto("Ingrese el motivo de la cancelación del pago de la clase\nOpcional");
                if (String.IsNullOrWhiteSpace(motivoCancelacion)) motivoCancelacion = "No especificado";
                motivo = motivoCancelacion;
            }

            // Marcar la clase como eliminada
            clase.Eliminado = true;
            clase.EliminadoEn = DateTime.Now;
            clase.ActualizadoEn = DateTime.Now;

            // Cancelar todos los cargos relacionados a esta clase
            var cargos = await _escuelaContext.Cargos
                .Where(c => c.ClaseId == claseID && !c.Eliminado)
                .ToListAsync(ct);

            foreach (var cargo in cargos)
            {
                cargo.Eliminado = true;
                cargo.Estado = EstadoCargo.Anulado;
                cargo.ActualizadoEn = DateTime.Now;
            }

            await _escuelaContext.SaveChangesAsync(ct);

            // Enviar mensaje de actualización para cada alumno afectado
            var alumnosAfectados = cargos.Select(c => c.AlumnoId).Distinct();
            foreach (var alumnoId in alumnosAfectados)
            {
                WeakReferenceMessenger.Default.Send(new ClasesActualizadasMessage(alumnoId));
            }
        }

        // ====================
        // Obtener clases de un alumno
        // ====================
        public async Task<Clase[]> ObtenerClasesDeAlumnoAsync(int alumnoId, CancellationToken ct = default)
        {
            var clases = await _escuelaContext.Cargos
                .Include(c => c.Clase)
                .Where(c => c.AlumnoId == alumnoId
                            && c.ClaseId != null
                            && !c.Eliminado
                            && c.Estado != EstadoCargo.Anulado)
                .Select(c => c.Clase!)
                .ToArrayAsync(ct);

            return clases;
        }

        public async Task<List<ClaseFinancieraDTO>> ObtenerClasesFinancierasAsync(
    int? alumnoId = null,
    int? tallerId = null,
    DateTime? desde = null,
    DateTime? hasta = null,
    CancellationToken ct = default)
        {
            var q = from ca in _escuelaContext.Cargos.AsNoTracking()
                    where ca.ClaseId != null
                          && !ca.Eliminado
                          && ca.Estado != EstadoCargo.Anulado
                          && (!alumnoId.HasValue || ca.AlumnoId == alumnoId.Value)
                    join cl in _escuelaContext.Clases.AsNoTracking() on ca.ClaseId equals cl.ClaseId
                    where (!tallerId.HasValue || cl.TallerId == tallerId.Value)
                       && (!desde.HasValue || cl.Fecha >= desde.Value.Date)
                       && (!hasta.HasValue || cl.Fecha <= hasta.Value.Date)
                    join ta in _escuelaContext.Talleres.AsNoTracking() on cl.TallerId equals ta.TallerId
                    join al in _escuelaContext.Alumnos.AsNoTracking() on ca.AlumnoId equals al.AlumnoId
                    join pa in _escuelaContext.PagoAplicaciones.AsNoTracking()
                            .Include(p => p.Pago) // para fecha y método
                        on ca.CargoId equals pa.CargoId into pagos
                    from pa in pagos.DefaultIfEmpty()
                    select new { ca, cl, ta, al, pa };

            // Agrupamos por cargo (una fila por clase/alumno)
            var lista = await (
                from x in q
                group x by new
                {
                    x.ca.CargoId,
                    x.cl.ClaseId,
                    x.cl.Fecha,
                    x.ta.TallerId,
                    TallerNombre = x.ta.Nombre,
                    x.al.AlumnoId,
                    AlumnoNombre = x.al.Nombre,
                    x.ca.Monto,
                    x.ca.SaldoActual,
                    x.ca.Estado
                }
                into g
                select new ClaseFinancieraDTO
                {
                    CargoId        = g.Key.CargoId,
                    ClaseId        = g.Key.ClaseId,
                    TallerId       = g.Key.TallerId,
                    AlumnoId       = g.Key.AlumnoId,
                    FechaClase     = g.Key.Fecha,
                    TallerNombre   = g.Key.TallerNombre,
                    AlumnoNombre   = g.Key.AlumnoNombre,
                    Monto          = g.Key.Monto,
                    SaldoActual    = g.Key.SaldoActual,
                    MontoPagado    = (decimal)((g.Where(z => z.pa != null)
                                                .Select(z => (double?)z.pa.MontoAplicado)
                                                .Sum()) ?? 0.0), // <- SQLite REAL
                    PorcentajePagado = g.Key.Monto > 0
                        ? (int)Math.Round((double)((g.Key.Monto - g.Key.SaldoActual) / g.Key.Monto * 100m))
                        : 0,
                    EstadoCargo    = g.Key.Estado,
                    EstadoTexto    = g.Key.Estado == EstadoCargo.Pagado ? "Pagada" :
                                     g.Key.Estado == EstadoCargo.Pendiente ? "Pendiente" : "Anulada",
                    UltimoPagoFecha  = g.Where(z => z.pa != null)
                                        .Max(z => (DateTime?)z.pa.Pago.Fecha)
                })
                .OrderByDescending(r => r.FechaClase)
                .ThenBy(r => r.TallerNombre)
                .ToListAsync(ct);

            // Redondeamos importes por consistencia visual
            foreach (var r in lista)
            {
                r.Monto        = Math.Round(r.Monto, 2, MidpointRounding.AwayFromZero);
                r.MontoPagado  = Math.Round(r.MontoPagado, 2, MidpointRounding.AwayFromZero);
                r.SaldoActual  = Math.Round(r.SaldoActual, 2, MidpointRounding.AwayFromZero);
            }

            return lista;
        }

        // ====================
        // Verificación de duplicados
        // ====================
        private async Task<bool> ExisteCargoClaseAsync(int alumnoId, int claseId, DateTime fecha, CancellationToken ct = default)
            => await _escuelaContext.Cargos.AnyAsync(i =>
                   i.AlumnoId == alumnoId
                && i.ClaseId == claseId
                && i.Fecha.Date == fecha.Date
                && !i.Eliminado, ct);

        public async Task<ClasePagoEstadoDTO[]> ObtenerEstadoPagoHoyAsync(
            int alumnoId,
            int[] tallerIds,
            DateTime fecha,
            CancellationToken ct = default)
        {
            var ids = (tallerIds ?? Array.Empty<int>()).Distinct().ToArray();
            if (alumnoId <= 0 || ids.Length == 0) return Array.Empty<ClasePagoEstadoDTO>();

            var day = fecha.Date;

            var resumen = await (
                from clases in _escuelaContext.Clases.AsNoTracking()
                where clases.Fecha == day && ids.Contains(clases.TallerId)
                join cargos in _escuelaContext.Cargos.AsNoTracking()
                    on clases.ClaseId equals cargos.ClaseId into caGroup
                from cargos in caGroup
                    .Where(c => c.AlumnoId == alumnoId
                                && c.Estado != EstadoCargo.Anulado)
                    .DefaultIfEmpty()
                group cargos by clases.TallerId into g
                select new
                {
                    TallerId = g.Key,
                    TieneCargo = g.Where(x => x != null).Any(),

                    Saldo = (decimal)((g.Where(x => x != null)
                                        .Select(x => (double?)x!.SaldoActual)
                                        .Sum()) ?? 0.0)
                })
                .ToListAsync(ct);

            // Armar mapa por taller
            var dict = resumen.ToDictionary(
                x => x.TallerId,
                x => new ClasePagoEstadoDTO(
                        x.TallerId,
                        x.TieneCargo,
                        x.TieneCargo && x.Saldo == 0m,
                        !x.TieneCargo || x.Saldo > 0m));

            // Si no existe aún la clase de hoy para algún taller, se puede pagar
            foreach (var id in ids)
            {
                if (!dict.ContainsKey(id))
                {
                    dict[id] = new ClasePagoEstadoDTO(
                        id,
                        TieneCargo: false,
                        EstaPagada: false,
                        PuedePagar: true);
                }
            }

            return dict.Values.OrderBy(v => v.TallerId).ToArray();
        }

        public async Task<Clase[]> ObtenerClasesPagadasAsync(int alumnoId, int tallerId, CancellationToken ct = default)
        {
            var clases = await _escuelaContext.Cargos
                .Include(c => c.Clase)
                .Where(c => c.AlumnoId == alumnoId
                            && c.ClaseId != null
                            && c.Clase!.TallerId == tallerId
                            && c.Estado == EstadoCargo.Pagado)
                .Select(c => c.Clase!)
                .ToArrayAsync(ct);

            return clases;
        }

        public async Task<EstadoPagoAlumnoDTO[]> ObtenerEstadoPagoAlumnosAsync(
            int? tallerId = null,
            int? alumnoId = null,
            int? generacionId = null,
            CancellationToken ct = default)
        {
            var hoy = DateTime.Today;
            
            // Si no se especifica generación, traer datos de todas las generaciones
            if (!generacionId.HasValue)
            {
                return await ObtenerEstadosPagoTodasGeneracionesAsync(null, null, ct);
            }
            
            // Usar la generación especificada
            var generacion = _escuelaContext.Generaciones.FirstOrDefault(g => g.GeneracionId == generacionId.Value);
            if (generacion == null) 
            {
                return Array.Empty<EstadoPagoAlumnoDTO>();
            }

            // Obtener costo de clase desde configuración
            var costoClase = Math.Max(1, _configuracionService.GetValor<int>("costo_clase", 150));

            // Obtener talleres con sus fechas de inicio y fin (incluyendo eliminados)
            var talleresQuery = _escuelaContext.Talleres
                .AsNoTracking()
                .Where(t => tallerId == null || t.TallerId == tallerId)
                .Select(t => new
                {
                    t.TallerId,
                    t.Nombre,
                    t.FechaInicio,
                    t.FechaFin,
                    t.DiaSemana,
                    t.Eliminado
                });

            var talleres = await talleresQuery.ToListAsync(ct);
            System.Diagnostics.Debug.WriteLine($"✅ Talleres encontrados: {talleres.Count}");
            
            var resultados = new List<EstadoPagoAlumnoDTO>();

            foreach (var taller in talleres)
            {
                System.Diagnostics.Debug.WriteLine($"🔍 Procesando taller: {taller.Nombre} (ID: {taller.TallerId})");
                
                // Calcular fechas de clases - usar fecha fin del taller o una fecha futura razonable
                var fechaFin = taller.FechaFin?.Date ?? hoy.AddMonths(6); // Si no tiene fecha fin, asumir 6 meses
                var fechaLimite = fechaFin < hoy ? fechaFin : hoy;
                
                // Generar fechas de clases desde FechaInicio hasta fechaLimite
                var fechasClases = GenerarFechasClases(taller.FechaInicio, fechaLimite, taller.DiaSemana);
                System.Diagnostics.Debug.WriteLine($"📅 Fechas de clases generadas: {fechasClases.Count} (desde {taller.FechaInicio:yyyy-MM-dd} hasta {fechaLimite:yyyy-MM-dd})");
                
                // Si no hay fechas de clases generadas, crear al menos una fecha para mostrar el taller
                if (fechasClases.Count == 0)
                {
                    // Si el taller no ha comenzado, usar la fecha de inicio
                    if (taller.FechaInicio > hoy)
                    {
                        fechasClases = new List<DateTime> { taller.FechaInicio };
                    }
                    else
                    {
                        // Si ya comenzó pero no hay fechas, usar hoy
                        fechasClases = new List<DateTime> { hoy };
                    }
                }

                // Obtener alumnos inscritos en este taller (incluyendo cancelados si el taller está eliminado)
                var alumnosQuery = _escuelaContext.Inscripciones
                    .AsNoTracking()
                    .Where(i => i.TallerId == taller.TallerId
                                && i.GeneracionId == generacion.GeneracionId
                                && !i.Eliminado
                                && (taller.Eliminado || i.Estado != EstadoInscripcion.Cancelada)
                                && (alumnoId == null || i.AlumnoId == alumnoId))
                    .Select(i => new { 
                        i.AlumnoId, 
                        i.Alumno!.Nombre, 
                        i.Estado, 
                        i.SaldoActual,
                        SedeId = i.Alumno.SedeId,
                        NombreSede = i.Alumno.Sede!.Nombre
                    });

                var alumnos = await alumnosQuery.ToListAsync(ct);
                System.Diagnostics.Debug.WriteLine($"👥 Alumnos inscritos en {taller.Nombre}: {alumnos.Count}");
                
                // Si no hay alumnos inscritos, mostrar un mensaje informativo
                if (alumnos.Count == 0)
                {
                    // Crear un registro informativo para talleres sin inscripciones
                    var resultadoVacio = new EstadoPagoAlumnoDTO
                    {
                        AlumnoId = 0,
                        NombreAlumno = "Sin alumnos inscritos",
                        TallerId = taller.TallerId,
                        NombreTaller = taller.Eliminado ? $"{taller.Nombre} (Eliminado)" : taller.Nombre,
                        SedeId = 0,
                        NombreSede = "N/A",
                        FechaInicio = taller.FechaInicio,
                        FechaFin = taller.FechaFin,
                        ClasesTotales = fechasClases.Count,
                        ClasesPagadas = 0,
                        ClasesPendientes = 0,
                        MontoTotal = fechasClases.Count * costoClase,
                        MontoPagado = 0,
                        MontoPendiente = 0,
                        TodasLasClasesPagadas = false,
                        EstadoPago = "ℹ️ Sin inscripciones",
                        FechaUltimaClase = fechasClases.LastOrDefault(),
                        FechaHoy = hoy
                    };
                    resultados.Add(resultadoVacio);
                    continue;
                }

                foreach (var alumno in alumnos)
                {
                    // Verificar si la inscripción está cancelada
                    var inscripcionCancelada = alumno.Estado == EstadoInscripcion.Cancelada;
                    
                    // Obtener todos los cargos de clases de este alumno en este taller
                    var cargosClases = await _escuelaContext.Cargos
                        .AsNoTracking()
                        .Where(c => c.AlumnoId == alumno.AlumnoId
                                    && c.ClaseId != null
                                    && c.Clase!.TallerId == taller.TallerId
                                    && (taller.Eliminado || c.Estado != EstadoCargo.Anulado))
                        .Select(c => new { 
                            c.Clase!.Fecha, 
                            c.Monto, 
                            c.SaldoActual,
                            c.Estado,
                            MontoPagado = c.Monto - c.SaldoActual
                        })
                        .ToListAsync(ct);

                    // Crear un diccionario de cargos por fecha para facilitar la búsqueda
                    var cargosPorFecha = cargosClases.ToDictionary(c => c.Fecha.Date, c => c);

                    // Calcular clases pagadas y pendientes basándose en las fechas esperadas
                    var clasesPagadas = new List<ClaseInfo>();
                    var clasesPendientes = new List<ClaseInfo>();
                    
                    if (!inscripcionCancelada)
                    {
                        // Primero, procesar las clases esperadas del taller
                        foreach (var fechaClase in fechasClases)
                        {
                            if (cargosPorFecha.TryGetValue(fechaClase.Date, out var cargo))
                            {
                                // Hay un cargo para esta clase
                                var claseInfo = new ClaseInfo
                                {
                                    Fecha = cargo.Fecha,
                                    Monto = cargo.Monto,
                                    SaldoActual = cargo.SaldoActual,
                                    Estado = cargo.Estado,
                                    MontoPagado = cargo.MontoPagado
                                };

                                if (taller.Eliminado)
                                {
                                    if (cargo.MontoPagado > 0 && cargo.Estado != EstadoCargo.Anulado)
                                    {
                                        clasesPagadas.Add(claseInfo);
                                    }
                                    if (cargo.SaldoActual > 0 && cargo.Estado != EstadoCargo.Anulado)
                                    {
                                        clasesPendientes.Add(claseInfo);
                                    }
                                }
                                else
                                {
                                    if (cargo.MontoPagado > 0)
                                    {
                                        clasesPagadas.Add(claseInfo);
                                    }
                                    if (cargo.SaldoActual > 0)
                                    {
                                        clasesPendientes.Add(claseInfo);
                                    }
                                }
                            }
                            else
                            {
                                // No hay cargo para esta clase, es una clase pendiente
                                var clasePendiente = new ClaseInfo
                                {
                                    Fecha = fechaClase,
                                    Monto = costoClase,
                                    SaldoActual = costoClase,
                                    Estado = EstadoCargo.Pendiente,
                                    MontoPagado = 0m
                                };
                                clasesPendientes.Add(clasePendiente);
                            }
                        }

                        // Luego, agregar las clases pagadas de más (excedentes) que no están en las fechas esperadas
                        var fechasEsperadas = fechasClases.Select(f => f.Date).ToHashSet();
                        foreach (var cargo in cargosClases)
                        {
                            if (!fechasEsperadas.Contains(cargo.Fecha.Date) && cargo.MontoPagado > 0)
                            {
                                // Esta es una clase pagada de más (excedente)
                                var claseExcedente = new ClaseInfo
                                {
                                    Fecha = cargo.Fecha,
                                    Monto = cargo.Monto,
                                    SaldoActual = cargo.SaldoActual,
                                    Estado = cargo.Estado,
                                    MontoPagado = cargo.MontoPagado
                                };
                                clasesPagadas.Add(claseExcedente);
                            }
                        }
                    }

                    // Calcular estadísticas
                    // Solo contar como "pagadas" las clases completamente pagadas (saldo = 0)
                    var clasesCompletamentePagadas = clasesPagadas.Count(c => c.SaldoActual == 0);
                    var clasesPagadasCount = clasesCompletamentePagadas;
                    var clasesPendientesCount = clasesPendientes.Count;
                    var clasesTotales = fechasClases.Count;
                    
                    // Calcular monto pagado
                    var montoPagado = inscripcionCancelada 
                        ? 0 // No hay pagos si la inscripción está cancelada
                        : clasesPagadas.Sum(c => c.MontoPagado);
                    
                    // Calcular monto pendiente
                    var montoPendiente = inscripcionCancelada 
                        ? 0 // No hay deuda pendiente si la inscripción está cancelada
                        : taller.Eliminado ? 0 : clasesPendientes.Sum(c => c.SaldoActual);
                    
                    var montoTotal = clasesTotales * costoClase;
                    
                    // Calcular clases parcialmente pagadas (tienen pago pero no están completas)
                    var clasesParcialmentePagadas = clasesPagadas.Count(c => c.MontoPagado > 0 && c.SaldoActual > 0);
                    var clasesSinPagos = clasesTotales - clasesPagadasCount;
                    
                    // Verificar si se pagaron al menos todas las clases que se deben
                    var todasLasClasesPagadas = clasesCompletamentePagadas >= clasesTotales && clasesPendientesCount == 0;
                    // Verificar si se pagó más de lo debido
                    var pagoExcedido = clasesCompletamentePagadas > clasesTotales;

                    // Determinar estado del pago
                    string estadoPago;
                    if (inscripcionCancelada)
                    {
                        // Para inscripciones canceladas, mostrar estado específico
                        estadoPago = "🚫 Inscripción cancelada";
                    }
                    else if (taller.Eliminado)
                    {
                        // Para talleres eliminados, mostrar solo lo que realmente pagó
                        if (montoPagado > 0)
                        {
                            var porcentajePagado = montoTotal > 0 ? (montoPagado / montoTotal) * 100 : 0;
                            estadoPago = $"⚠️ Taller eliminado - Pagó {montoPagado:C2} ({porcentajePagado:F1}%)";
                        }
                        else
                        {
                            estadoPago = "❌ Taller eliminado - Sin pagos";
                        }
                    }
                    else if (todasLasClasesPagadas && !pagoExcedido)
                    {
                        estadoPago = "✅ Todas las clases pagadas";
                    }
                    else if (todasLasClasesPagadas && pagoExcedido)
                    {
                        estadoPago = "✅ Todas las clases pagadas (con exceso)";
                    }
                    else if (clasesCompletamentePagadas > 0 || clasesParcialmentePagadas > 0)
                    {
                        var totalConPagos = clasesCompletamentePagadas + clasesParcialmentePagadas;
                        if (clasesParcialmentePagadas > 0)
                        {
                            estadoPago = $"⚠️ Parcialmente pagado ({clasesCompletamentePagadas} completas, {clasesParcialmentePagadas} parciales de {clasesTotales})";
                        }
                        else
                        {
                            estadoPago = $"⚠️ Parcialmente pagado ({clasesCompletamentePagadas}/{clasesTotales})";
                        }
                    }
                    else
                    {
                        estadoPago = "❌ Sin pagos";
                    }

                    // Si el taller tiene fecha fin y ya terminó, ajustar el estado (solo si no está eliminado)
                    if (!taller.Eliminado && taller.FechaFin.HasValue && hoy > taller.FechaFin.Value)
                    {
                        if (todasLasClasesPagadas && !pagoExcedido)
                        {
                            estadoPago = "✅ Completado - Todas las clases pagadas";
                        }
                        else if (todasLasClasesPagadas && pagoExcedido)
                        {
                            estadoPago = "✅ Completado - Todas las clases pagadas (con exceso)";
                        }
                        else
                        {
                            estadoPago = $"❌ Incompleto - Faltan {clasesPendientesCount} clases";
                        }
                    }

                    var resultado = new EstadoPagoAlumnoDTO
                    {
                        AlumnoId = alumno.AlumnoId,
                        NombreAlumno = alumno.Nombre,
                        TallerId = taller.TallerId,
                        NombreTaller = taller.Eliminado ? $"{taller.Nombre} (Eliminado)" : taller.Nombre,
                        SedeId = alumno.SedeId ?? 0,
                        NombreSede = alumno.NombreSede ?? "Sin sede",
                        FechaInicio = taller.FechaInicio,
                        FechaFin = taller.FechaFin,
                        ClasesTotales = clasesTotales,
                        ClasesPagadas = clasesPagadasCount,
                        ClasesPendientes = clasesPendientesCount,
                        MontoTotal = montoTotal,
                        MontoPagado = montoPagado,
                        MontoPendiente = montoPendiente,
                        TodasLasClasesPagadas = todasLasClasesPagadas,
                        EstadoPago = estadoPago,
                        FechaUltimaClase = fechasClases.LastOrDefault(),
                        FechaHoy = hoy
                    };

                    resultados.Add(resultado);
                }
            }

            return resultados.OrderBy(r => r.NombreTaller)
                            .ThenBy(r => r.NombreAlumno)
                            .ToArray();
        }

        private List<DateTime> GenerarFechasClases(DateTime fechaInicio, DateTime fechaLimite, DayOfWeek diaSemana)
        {
            var fechas = new List<DateTime>();
            var fechaActual = fechaInicio.Date;
            
            // Encontrar el primer día de la semana del taller
            while (fechaActual.DayOfWeek != diaSemana)
            {
                fechaActual = fechaActual.AddDays(1);
            }

            // Generar fechas de clases hasta la fecha límite
            while (fechaActual <= fechaLimite)
            {
                fechas.Add(fechaActual);
                fechaActual = fechaActual.AddDays(7);
            }

            // Si no se generaron fechas (por ejemplo, si la fecha límite es muy cercana),
            // al menos incluir la fecha de inicio
            if (fechas.Count == 0 && fechaInicio <= fechaLimite)
            {
                fechas.Add(fechaInicio);
            }

            return fechas;
        }

        private static DateTime SiguienteOElMismo(DateTime desde, DayOfWeek objetivo, bool incluirSiHoyCoincide)
        {
            desde = desde.Date;
            int delta = ((int)objetivo - (int)desde.DayOfWeek + 7) % 7;
            if (delta == 0 && !incluirSiHoyCoincide) delta = 7;
            return desde.AddDays(delta);
        }

        private static string ConvertirDiaSemanaASpanol(DayOfWeek diaSemana)
        {
            return diaSemana switch
            {
                DayOfWeek.Monday => "Lunes",
                DayOfWeek.Tuesday => "Martes",
                DayOfWeek.Wednesday => "Miércoles",
                DayOfWeek.Thursday => "Jueves",
                DayOfWeek.Friday => "Viernes",
                DayOfWeek.Saturday => "Sábado",
                DayOfWeek.Sunday => "Domingo",
                _ => "Lunes"
            };
        }

        // ====================
        // Aplicar excedente a la siguiente clase
        // ====================
        private async Task<decimal> AplicarExcedenteASiguienteClaseAsync(
            int alumnoId,
            int tallerId,
            DateTime fechaClaseActual,
            decimal excedente,
            string nombreTaller,
            CancellationToken ct = default)
        {
            try
            {
                // Calcular la siguiente clase (7 días después)
                var siguienteFecha = fechaClaseActual.AddDays(7);
                
                // Obtener información del taller para validar el día de la semana
                var taller = await _escuelaContext.Talleres
                    .Where(t => t.TallerId == tallerId && !t.Eliminado)
                    .Select(t => new { t.DiaSemana, t.FechaFin })
                    .FirstOrDefaultAsync(ct);

                if (taller == null)
                    return 0m;

                // Ajustar la fecha al día correcto de la semana
                var diaSemana = taller.DiaSemana;
                int delta = ((int)diaSemana - (int)siguienteFecha.DayOfWeek + 7) % 7;
                var fechaSiguienteClase = siguienteFecha.AddDays(delta);

                // Verificar si el taller tiene fecha de fin y si la siguiente clase está dentro del rango
                if (taller.FechaFin.HasValue && fechaSiguienteClase > taller.FechaFin.Value)
                {
                    // Si la siguiente clase está fuera del rango del taller, no aplicar excedente
                    return 0m;
                }

                // Buscar o crear la clase siguiente
                var claseSiguiente = await _escuelaContext.Clases
                    .FirstOrDefaultAsync(c => c.TallerId == tallerId && !c.Eliminado && c.Fecha.Date == fechaSiguienteClase, ct);

                if (claseSiguiente == null)
                {
                    claseSiguiente = new Clase { TallerId = tallerId, Fecha = fechaSiguienteClase };
                    _escuelaContext.Clases.Add(claseSiguiente);
                    await _escuelaContext.SaveChangesAsync(ct);
                }

                // Buscar o crear el cargo para la clase siguiente
                var cargoSiguiente = await _escuelaContext.Cargos
                    .FirstOrDefaultAsync(c => c.AlumnoId == alumnoId
                                           && c.ClaseId == claseSiguiente.ClaseId
                                           && !c.Eliminado
                                           && c.Estado != EstadoCargo.Anulado, ct);

                if (cargoSiguiente == null)
                {
                    var costoClase = Math.Max(1, _configuracionService.GetValor<int>("costo_clase", 150));
                    cargoSiguiente = new Cargo
                    {
                        AlumnoId = alumnoId,
                        ClaseId = claseSiguiente.ClaseId,
                        Tipo = TipoCargo.Clase,
                        Fecha = fechaSiguienteClase,
                        Monto = costoClase,
                        SaldoActual = costoClase,
                        Estado = EstadoCargo.Pendiente,
                        Eliminado = false
                    };
                    _escuelaContext.Cargos.Add(cargoSiguiente);
                    await _escuelaContext.SaveChangesAsync(ct);
                }

                // Si la clase siguiente ya está pagada, no aplicar excedente
                if (cargoSiguiente.SaldoActual <= 0m)
                    return 0m;

                // Aplicar el excedente a la clase siguiente
                var montoAplicar = Math.Min(excedente, cargoSiguiente.SaldoActual);
                if (montoAplicar <= 0m)
                    return 0m;

                // Crear el pago para el excedente
                var diaSemanaTexto = ConvertirDiaSemanaASpanol(diaSemana);
                var descripcion = $"Excedente aplicado a clase - {nombreTaller} ({diaSemanaTexto} {fechaSiguienteClase:dd/MM/yyyy}) - Monto: ${montoAplicar:F2}";
                
                var pagoExcedente = new Pago
                {
                    AlumnoId = alumnoId,
                    Fecha = DateTime.Now,
                    Metodo = MetodoPago.Efectivo,
                    MontoTotal = montoAplicar,
                    Notas = descripcion
                };
                _escuelaContext.Pagos.Add(pagoExcedente);
                await _escuelaContext.SaveChangesAsync(ct);

                // Crear la aplicación del pago
                var paExcedente = new PagoAplicacion
                {
                    PagoId = pagoExcedente.PagoId,
                    CargoId = cargoSiguiente.CargoId,
                    MontoAplicado = montoAplicar,
                    Estado = EstadoAplicacionCargo.Vigente
                };
                _escuelaContext.PagoAplicaciones.Add(paExcedente);

                // Actualizar el saldo del cargo
                cargoSiguiente.SaldoActual -= montoAplicar;
                cargoSiguiente.Estado = cargoSiguiente.SaldoActual == 0m
                    ? EstadoCargo.Pagado
                    : EstadoCargo.Pendiente;

                await _escuelaContext.SaveChangesAsync(ct);

                // Verificar si hay excedente restante y aplicarlo recursivamente
                var excedenteRestante = excedente - montoAplicar;
                if (excedenteRestante > 0m)
                {
                    // Aplicar el excedente restante a la siguiente clase
                    var excedenteAdicional = await AplicarExcedenteASiguienteClaseAsync(
                        alumnoId, tallerId, fechaSiguienteClase, excedenteRestante, nombreTaller, ct);
                    return montoAplicar + excedenteAdicional;
                }

                return montoAplicar;
            }
            catch (Exception)
            {
                // En caso de error, retornar 0 para no afectar el flujo principal
                return 0m;
            }
        }

        private async Task<EstadoPagoAlumnoDTO[]> ObtenerEstadosPagoTodasGeneracionesAsync(
            int? tallerId = null,
            int? alumnoId = null,
            CancellationToken ct = default)
        {
            try
            {
                var hoy = DateTime.Today;
                var costoClase = Math.Max(1, _configuracionService.GetValor<int>("costo_clase", 150));
                var resultados = new List<EstadoPagoAlumnoDTO>();

                // Obtener TODAS las inscripciones sin filtros
                var inscripciones = await _escuelaContext.Inscripciones
                    .AsNoTracking()
                    .Include(i => i.Alumno)
                        .ThenInclude(a => a.Sede)
                    .Include(i => i.Taller)
                    .Include(i => i.Generacion)
                    .Where(i => (tallerId == null || i.TallerId == tallerId) &&
                               (alumnoId == null || i.AlumnoId == alumnoId))
                    .ToListAsync(ct);

                if (inscripciones.Count == 0)
                {
                    return Array.Empty<EstadoPagoAlumnoDTO>();
                }

                // Procesar cada inscripción
                foreach (var inscripcion in inscripciones)
                {
                    // Calcular fechas de clases solo hasta hoy (no futuras)
                    var fechaInicio = inscripcion.Taller.FechaInicio;
                    var fechaFin = inscripcion.Taller.FechaFin ?? hoy.AddMonths(6);
                    
                    // Usar la fecha más temprana entre la fecha fin del taller y hoy
                    var fechaLimite = fechaFin < hoy ? fechaFin : hoy;
                    
                    // Solo generar fechas si el taller ya comenzó
                    var fechasClases = new List<DateTime>();
                    if (fechaInicio <= hoy)
                    {
                        fechasClases = GenerarFechasClases(fechaInicio, fechaLimite, inscripcion.Taller.DiaSemana);
                    }
                    
                    if (fechasClases.Count == 0)
                    {
                        fechasClases.Add(fechaInicio);
                    }

                    // Obtener cargos de clases de este alumno para este taller (no todos los cargos)
                    var cargosClases = await _escuelaContext.Cargos
                        .AsNoTracking()
                        .Where(c => c.AlumnoId == inscripcion.AlumnoId
                                    && c.ClaseId != null
                                    && c.Clase!.TallerId == inscripcion.TallerId
                                    && (inscripcion.Taller.Eliminado || c.Estado != EstadoCargo.Anulado))
                        .Select(c => new { 
                            c.Clase!.Fecha, 
                            c.Monto, 
                            c.SaldoActual,
                            c.Estado,
                            MontoPagado = c.Monto - c.SaldoActual
                        })
                        .ToListAsync(ct);

                    // Verificar si la inscripción está cancelada
                    var inscripcionCancelada = inscripcion.Estado == EstadoInscripcion.Cancelada;
                    
                    // Crear un diccionario de cargos por fecha para facilitar la búsqueda
                    var cargosPorFecha = cargosClases.ToDictionary(c => c.Fecha.Date, c => c);

                    // Calcular clases pagadas y pendientes basándose en las fechas esperadas
                    var clasesPagadas = new List<ClaseInfo>();
                    var clasesPendientes = new List<ClaseInfo>();
                    
                    if (!inscripcionCancelada)
                    {
                        // Primero, procesar las clases esperadas del taller
                        foreach (var fechaClase in fechasClases)
                        {
                            if (cargosPorFecha.TryGetValue(fechaClase.Date, out var cargo))
                            {
                                // Hay un cargo para esta clase
                                var claseInfo = new ClaseInfo
                                {
                                    Fecha = cargo.Fecha,
                                    Monto = cargo.Monto,
                                    SaldoActual = cargo.SaldoActual,
                                    Estado = cargo.Estado,
                                    MontoPagado = cargo.MontoPagado
                                };

                                if (inscripcion.Taller.Eliminado)
                                {
                                    if (cargo.MontoPagado > 0 && cargo.Estado != EstadoCargo.Anulado)
                                    {
                                        clasesPagadas.Add(claseInfo);
                                    }
                                    if (cargo.SaldoActual > 0 && cargo.Estado != EstadoCargo.Anulado)
                                    {
                                        clasesPendientes.Add(claseInfo);
                                    }
                                }
                                else
                                {
                                    if (cargo.MontoPagado > 0)
                                    {
                                        clasesPagadas.Add(claseInfo);
                                    }
                                    if (cargo.SaldoActual > 0)
                                    {
                                        clasesPendientes.Add(claseInfo);
                                    }
                                }
                            }
                            else
                            {
                                // No hay cargo para esta clase, es una clase pendiente
                                var clasePendiente = new ClaseInfo
                                {
                                    Fecha = fechaClase,
                                    Monto = costoClase,
                                    SaldoActual = costoClase,
                                    Estado = EstadoCargo.Pendiente,
                                    MontoPagado = 0m
                                };
                                clasesPendientes.Add(clasePendiente);
                            }
                        }

                        // Luego, agregar las clases pagadas de más (excedentes) que no están en las fechas esperadas
                        var fechasEsperadas = fechasClases.Select(f => f.Date).ToHashSet();
                        foreach (var cargo in cargosClases)
                        {
                            if (!fechasEsperadas.Contains(cargo.Fecha.Date) && cargo.MontoPagado > 0)
                            {
                                // Esta es una clase pagada de más (excedente)
                                var claseExcedente = new ClaseInfo
                                {
                                    Fecha = cargo.Fecha,
                                    Monto = cargo.Monto,
                                    SaldoActual = cargo.SaldoActual,
                                    Estado = cargo.Estado,
                                    MontoPagado = cargo.MontoPagado
                                };
                                clasesPagadas.Add(claseExcedente);
                            }
                        }
                    }

                    // Calcular estadísticas
                    // Solo contar como "pagadas" las clases completamente pagadas (saldo = 0)
                    var clasesCompletamentePagadas = clasesPagadas.Count(c => c.SaldoActual == 0);
                    var clasesPagadasCount = clasesCompletamentePagadas;
                    var clasesPendientesCount = clasesPendientes.Count;
                    var clasesTotales = fechasClases.Count;
                    
                    // Calcular monto pagado
                    var montoPagado = inscripcionCancelada 
                        ? 0 // No hay pagos si la inscripción está cancelada
                        : clasesPagadas.Sum(c => c.MontoPagado);
                    
                    // Calcular monto pendiente
                    var montoPendiente = inscripcionCancelada 
                        ? 0 // No hay deuda pendiente si la inscripción está cancelada
                        : inscripcion.Taller.Eliminado ? 0 : clasesPendientes.Sum(c => c.SaldoActual);
                    
                    var montoTotal = clasesTotales * costoClase;
                    
                    // Calcular clases parcialmente pagadas (tienen pago pero no están completas)
                    var clasesParcialmentePagadas = clasesPagadas.Count(c => c.MontoPagado > 0 && c.SaldoActual > 0);
                    
                    // Verificar si se pagaron al menos todas las clases que se deben
                    var todasLasClasesPagadas = clasesCompletamentePagadas >= clasesTotales && clasesPendientesCount == 0;
                    // Verificar si se pagó más de lo debido
                    var pagoExcedido = clasesCompletamentePagadas > clasesTotales;

                    // Determinar estado del pago usando la misma lógica que el método principal
                    string estadoPago;
                    if (inscripcionCancelada)
                    {
                        estadoPago = "🚫 Inscripción cancelada";
                    }
                    else if (inscripcion.Taller.Eliminado)
                    {
                        if (montoPagado > 0)
                        {
                            var porcentajePagado = montoTotal > 0 ? (montoPagado / montoTotal) * 100 : 0;
                            estadoPago = $"⚠️ Taller eliminado - Pagó {montoPagado:C2} ({porcentajePagado:F1}%)";
                        }
                        else
                        {
                            estadoPago = "❌ Taller eliminado - Sin pagos";
                        }
                    }
                    else if (todasLasClasesPagadas && !pagoExcedido)
                    {
                        estadoPago = "✅ Todas las clases pagadas";
                    }
                    else if (todasLasClasesPagadas && pagoExcedido)
                    {
                        estadoPago = "✅ Todas las clases pagadas (con exceso)";
                    }
                    else if (clasesCompletamentePagadas > 0 || clasesParcialmentePagadas > 0)
                    {
                        if (clasesParcialmentePagadas > 0)
                        {
                            estadoPago = $"⚠️ Parcialmente pagado ({clasesCompletamentePagadas} completas, {clasesParcialmentePagadas} parciales de {clasesTotales})";
                        }
                        else
                        {
                            estadoPago = $"⚠️ Parcialmente pagado ({clasesCompletamentePagadas}/{clasesTotales})";
                        }
                    }
                    else
                    {
                        estadoPago = "❌ Sin pagos";
                    }

                    // Si el taller tiene fecha fin y ya terminó, ajustar el estado (solo si no está eliminado)
                    if (!inscripcion.Taller.Eliminado && inscripcion.Taller.FechaFin.HasValue && hoy > inscripcion.Taller.FechaFin.Value)
                    {
                        if (todasLasClasesPagadas && !pagoExcedido)
                        {
                            estadoPago = "✅ Completado - Todas las clases pagadas";
                        }
                        else if (todasLasClasesPagadas && pagoExcedido)
                        {
                            estadoPago = "✅ Completado - Todas las clases pagadas (con exceso)";
                        }
                        else
                        {
                            estadoPago = $"❌ Incompleto - Faltan {clasesPendientesCount} clases";
                        }
                    }

                    var estado = new EstadoPagoAlumnoDTO
                    {
                        AlumnoId = inscripcion.AlumnoId,
                        NombreAlumno = inscripcion.Alumno.Nombre,
                        TallerId = inscripcion.TallerId,
                        NombreTaller = inscripcion.Taller.Eliminado ? $"{inscripcion.Taller.Nombre} (Eliminado)" : inscripcion.Taller.Nombre,
                        SedeId = inscripcion.Alumno.SedeId ?? 0,
                        NombreSede = inscripcion.Alumno.Sede?.Nombre ?? "Sin sede",
                        FechaInicio = fechaInicio,
                        FechaFin = inscripcion.Taller.FechaFin,
                        ClasesTotales = clasesTotales,
                        ClasesPagadas = clasesPagadasCount,
                        ClasesPendientes = clasesPendientesCount,
                        MontoTotal = montoTotal,
                        MontoPagado = montoPagado,
                        MontoPendiente = montoPendiente,
                        TodasLasClasesPagadas = todasLasClasesPagadas,
                        EstadoPago = estadoPago,
                        FechaUltimaClase = fechasClases.LastOrDefault(),
                        FechaHoy = hoy
                    };

                    resultados.Add(estado);
                }

                return resultados.OrderBy(r => r.NombreTaller)
                                .ThenBy(r => r.NombreAlumno)
                                .ToArray();
            }
            catch
            {
                return Array.Empty<EstadoPagoAlumnoDTO>();
            }
        }

    }
}
