using ControlTalleresMVP.Persistence.DataContext;
using ControlTalleresMVP.Persistence.ModelDTO;
using Microsoft.EntityFrameworkCore;

namespace ControlTalleresMVP.Services.Inscripciones
{
    public class InscripcionReporteService : IInscripcionReporteService
    {
        private readonly EscuelaContext _escuelaContext;

        public InscripcionReporteService(EscuelaContext escuelaContext)
        {
            _escuelaContext = escuelaContext;
        }

        public async Task<InscripcionReporteDTO[]> ObtenerInscripcionesReporteAsync(
            int? tallerId = null,
            int? promotorId = null,
            int? generacionId = null,
            DateTime? desde = null,
            DateTime? hasta = null,
            bool incluirTalleresEliminados = false,
            CancellationToken ct = default)
        {
            var hoy = DateTime.Today;
            var hastaFiltro = hasta ?? hoy; // Si no se especifica hasta, usar fecha actual

            // Ajustar las fechas para incluir todo el día
            var desdeFiltro = desde?.Date ?? DateTime.MinValue;
            var hastaFiltroCompleto = hastaFiltro.Date.AddDays(1).AddTicks(-1); // Hasta el final del día


            var query = from i in _escuelaContext.Inscripciones.AsNoTracking()
                        where (incluirTalleresEliminados || !i.Eliminado)
                              && (tallerId == null || i.TallerId == tallerId)
                              && (generacionId == null || i.GeneracionId == generacionId)
                              && (desde == null || i.Fecha >= desdeFiltro)
                              && i.Fecha <= hastaFiltroCompleto // Incluir todo el día especificado
                        join a in _escuelaContext.Alumnos.AsNoTracking() on i.AlumnoId equals a.AlumnoId
                        join t in _escuelaContext.Talleres.AsNoTracking() on i.TallerId equals t.TallerId
                        where incluirTalleresEliminados || !t.Eliminado
                        join p in _escuelaContext.Promotores.AsNoTracking() on a.PromotorId equals p.PromotorId
                        join g in _escuelaContext.Generaciones.AsNoTracking() on i.GeneracionId equals g.GeneracionId
                        join s in _escuelaContext.Sedes.AsNoTracking() on a.SedeId equals s.SedeId into sedeGroup
                        from s in sedeGroup.DefaultIfEmpty()
                        select new InscripcionReporteDTO
                        {
                            InscripcionId = i.InscripcionId,
                            NombreAlumno = a.Eliminado ? $"{a.Nombre} (Eliminado)" : a.Nombre,
                            AlumnoEliminado = a.Eliminado,
                            TallerEliminado = t.Eliminado,
                            NombreTaller = t.Eliminado ? $"{t.Nombre} (Eliminado)" : t.Nombre,
                            NombreSede = s != null ? s.Nombre : "Sin Sede",
                            NombrePromotor = p.Nombre,
                            FechaInscripcion = i.Fecha,
                            Costo = i.Costo,
                            SaldoActual = i.SaldoActual,
                            Estado = i.Eliminado ? "Cancelada" : i.Estado.ToString(),
                            EstadoPago = CalcularEstadoPago(i.Costo, i.SaldoActual),
                            MontoPagado = i.Costo - i.SaldoActual,
                            DiaSemana = t.DiaSemana.ToString(),
                            FechaInicioTaller = t.FechaInicio,
                            FechaFinTaller = t.FechaFin,
                            NombreGeneracion = g.Nombre,
                            DiasTranscurridos = (DateTime.Today - i.Fecha).Days,
                            DiasRestantes = t.FechaFin.HasValue ? (t.FechaFin.Value - DateTime.Today).Days : null,
                            ProgresoPorcentaje = CalcularProgresoPago(i.Costo, i.SaldoActual)
                        };

            // Filtrar por promotor si se especifica
            if (promotorId.HasValue)
            {
                query = query.Where(x => x.NombrePromotor == _escuelaContext.Promotores
                    .Where(p => p.PromotorId == promotorId.Value)
                    .Select(p => p.Nombre)
                    .FirstOrDefault());
            }

            var resultados = await query
                .OrderBy(x => x.AlumnoEliminado) // Alumnos activos primero (false), eliminados al final (true)
                .ThenByDescending(x => x.FechaInscripcion) // Dentro de cada grupo, por fecha descendente
                .ToArrayAsync(ct);
            return resultados;
        }

        public async Task<InscripcionEstadisticasDTO> ObtenerEstadisticasInscripcionesAsync(
            int? tallerId = null,
            int? generacionId = null,
            DateTime? desde = null,
            DateTime? hasta = null,
            bool incluirTalleresEliminados = false,
            CancellationToken ct = default)
        {
            var inscripciones = await ObtenerInscripcionesReporteAsync(tallerId, null, generacionId, desde, hasta, incluirTalleresEliminados, ct);

            var estadisticas = new InscripcionEstadisticasDTO
            {
                TotalInscripciones = inscripciones.Length,
                InscripcionesActivas = inscripciones.Count(i => i.Estado == "Pendiente"),
                InscripcionesCanceladas = inscripciones.Count(i => i.Estado == "Cancelada"),
                InscripcionesPagadas = inscripciones.Count(i => i.Estado == "Pagada"),

                MontoTotalInscripciones = inscripciones.Sum(i => i.Costo),
                MontoTotalRecaudado = inscripciones.Sum(i => i.Costo - i.SaldoActual),
                MontoTotalPendiente = inscripciones.Where(i => !i.TallerEliminado).Sum(i => i.SaldoActual),

                TalleresConInscripciones = inscripciones.Select(i => i.NombreTaller).Distinct().Count(),

                LunesInscripciones = inscripciones.Count(i => i.DiaSemana == "Monday"),
                MartesInscripciones = inscripciones.Count(i => i.DiaSemana == "Tuesday"),
                MiercolesInscripciones = inscripciones.Count(i => i.DiaSemana == "Wednesday"),
                JuevesInscripciones = inscripciones.Count(i => i.DiaSemana == "Thursday"),
                ViernesInscripciones = inscripciones.Count(i => i.DiaSemana == "Friday"),
                SabadoInscripciones = inscripciones.Count(i => i.DiaSemana == "Saturday"),
                DomingoInscripciones = inscripciones.Count(i => i.DiaSemana == "Sunday")
            };

            // Calcular promedios y porcentajes
            estadisticas.PromedioCostoInscripcion = estadisticas.TotalInscripciones > 0
                ? estadisticas.MontoTotalInscripciones / estadisticas.TotalInscripciones
                : 0;

            // Taller más popular
            var tallerMasPopular = inscripciones
                .GroupBy(i => i.NombreTaller)
                .OrderByDescending(g => g.Count())
                .FirstOrDefault();

            if (tallerMasPopular != null)
            {
                estadisticas.TallerMasPopular = tallerMasPopular.Key;
                estadisticas.InscripcionesTallerMasPopular = tallerMasPopular.Count();
            }

            // Calcular tasa de retención (simplificado)
            estadisticas.TasaRetencion = estadisticas.TotalInscripciones > 0
                ? (decimal)estadisticas.InscripcionesActivas / estadisticas.TotalInscripciones * 100
                : 0;

            return estadisticas;
        }

        private static decimal CalcularProgreso(DateTime fechaInscripcion, DateTime fechaInicioTaller, DateTime? fechaFinTaller)
        {
            var hoy = DateTime.Today;
            var fechaFin = fechaFinTaller ?? hoy;
            var fechaLimite = fechaFin < hoy ? fechaFin : hoy;

            var diasTotales = (fechaFin - fechaInicioTaller).Days;
            var diasTranscurridos = (fechaLimite - fechaInicioTaller).Days;

            if (diasTotales <= 0) return 0;

            var progreso = (decimal)diasTranscurridos / diasTotales * 100;
            return Math.Max(0, Math.Min(100, progreso));
        }

        private static decimal CalcularProgresoPago(decimal costo, decimal saldoActual)
        {
            if (costo <= 0) return 0;

            var montoPagado = costo - saldoActual;
            var progreso = (montoPagado / costo) * 100;

            return Math.Max(0, Math.Min(100, progreso));
        }

        private static string CalcularEstadoPago(decimal costo, decimal saldoActual)
        {
            var montoPagado = costo - saldoActual;

            if (saldoActual <= 0)
            {
                return "✅ Completamente pagado";
            }
            else if (montoPagado > 0)
            {
                return "⚠️ Parcialmente pagado";
            }
            else
            {
                return "❌ Sin pagos";
            }
        }
    }
}
