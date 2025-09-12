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
                    Fecha = day,               // ← usa la fecha “anclada” al día semanal
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
                        var pago = new Pago
                        {
                            AlumnoId = alumnoId,
                            Fecha = DateTime.Now,
                            Metodo = MetodoPago.Efectivo, // ajusta según tu UI
                            MontoTotal = aAplicar
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
                    }
                }
            }

            return new RegistrarClaseResult(
                Fecha: day,
                ClaseCreada: claseCreada,
                CargoCreado: cargoCreado,
                PagoCreado: pagoCreado,
                MontoAplicado: aplicado,
                CargoYaPagado: yaPagado
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
            CancellationToken ct = default)
        {
            var hoy = DateTime.Today;
            var generacion = _generacionService.ObtenerGeneracionActual();
            if (generacion == null) return Array.Empty<EstadoPagoAlumnoDTO>();

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
            var resultados = new List<EstadoPagoAlumnoDTO>();

            foreach (var taller in talleres)
            {
                // Calcular fechas de clases hasta hoy o fecha fin
                var fechaFin = taller.FechaFin?.Date ?? hoy;
                var fechaLimite = fechaFin < hoy ? fechaFin : hoy;
                
                // Generar fechas de clases desde FechaInicio hasta fechaLimite
                var fechasClases = GenerarFechasClases(taller.FechaInicio, fechaLimite, taller.DiaSemana);
                
                if (fechasClases.Count == 0) continue;

                // Obtener alumnos inscritos en este taller (incluyendo cancelados si el taller está eliminado)
                var alumnosQuery = _escuelaContext.Inscripciones
                    .AsNoTracking()
                    .Where(i => i.TallerId == taller.TallerId
                                && i.GeneracionId == generacion.GeneracionId
                                && !i.Eliminado
                                && (taller.Eliminado || i.Estado != EstadoInscripcion.Cancelada)
                                && (alumnoId == null || i.AlumnoId == alumnoId))
                    .Select(i => new { i.AlumnoId, i.Alumno!.Nombre, i.Estado, i.SaldoActual });

                var alumnos = await alumnosQuery.ToListAsync(ct);

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

                    // Para inscripciones canceladas, no considerar cargos en cálculos de pagos
                    // pero sí mostrar la información histórica
                    var clasesPagadas = inscripcionCancelada 
                        ? cargosClases.Where(c => false).ToList() // No hay clases pagadas si la inscripción está cancelada
                        : taller.Eliminado 
                            ? cargosClases.Where(c => c.MontoPagado > 0 && c.Estado != EstadoCargo.Anulado).ToList()
                            : cargosClases.Where(c => c.MontoPagado > 0).ToList();
                    
                    var clasesPendientes = inscripcionCancelada 
                        ? cargosClases.Where(c => false).ToList() // No hay clases pendientes si la inscripción está cancelada
                        : taller.Eliminado 
                            ? cargosClases.Where(c => c.SaldoActual > 0 && c.Estado != EstadoCargo.Anulado).ToList()
                            : cargosClases.Where(c => c.SaldoActual > 0).ToList();

                    // Calcular estadísticas
                    var clasesPagadasCount = clasesPagadas.Count;
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
                    
                    // Calcular clases completamente pagadas vs parcialmente pagadas
                    // Si el taller está eliminado, no considerar cargos anulados como completamente pagados
                    var clasesCompletamentePagadas = taller.Eliminado 
                        ? cargosClases.Count(c => c.SaldoActual == 0 && c.Estado != EstadoCargo.Anulado)
                        : cargosClases.Count(c => c.SaldoActual == 0);
                    var clasesParcialmentePagadas = cargosClases.Count(c => c.MontoPagado > 0 && c.SaldoActual > 0);
                    var clasesSinPagos = cargosClases.Count(c => c.MontoPagado == 0);
                    
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

    }
}
