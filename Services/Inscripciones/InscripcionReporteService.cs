using ControlTalleresMVP.Persistence.DataContext;
using ControlTalleresMVP.Persistence.ModelDTO;
using ControlTalleresMVP.Persistence.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

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
            CancellationToken ct = default)
        {
            var hoy = DateTime.Today;
            var hastaFiltro = hasta ?? hoy; // Si no se especifica hasta, usar fecha actual
            
            var query = from i in _escuelaContext.Inscripciones.AsNoTracking()
                        where !i.Eliminado
                              && (tallerId == null || i.TallerId == tallerId)
                              && (generacionId == null || i.GeneracionId == generacionId)
                              && (desde == null || i.Fecha >= desde.Value.Date)
                              && i.Fecha <= hastaFiltro.Date // Siempre filtrar hasta la fecha actual o la especificada
                        join a in _escuelaContext.Alumnos.AsNoTracking() on i.AlumnoId equals a.AlumnoId
                        join t in _escuelaContext.Talleres.AsNoTracking() on i.TallerId equals t.TallerId
                        join p in _escuelaContext.Promotores.AsNoTracking() on a.PromotorId equals p.PromotorId
                        join g in _escuelaContext.Generaciones.AsNoTracking() on i.GeneracionId equals g.GeneracionId
                        join s in _escuelaContext.Sedes.AsNoTracking() on a.SedeId equals s.SedeId into sedeGroup
                        from s in sedeGroup.DefaultIfEmpty()
                        select new InscripcionReporteDTO
                        {
                            InscripcionId = i.InscripcionId,
                            NombreAlumno = a.Nombre,
                            NombreTaller = t.Nombre,
                            NombreSede = s != null ? s.Nombre : "Sin Sede",
                            NombrePromotor = p.Nombre,
                            FechaInscripcion = i.Fecha,
                            Costo = i.Costo,
                            SaldoActual = i.SaldoActual,
                            Estado = i.Estado.ToString(),
                            DiaSemana = t.DiaSemana.ToString(),
                            FechaInicioTaller = t.FechaInicio,
                            FechaFinTaller = t.FechaFin,
                            NombreGeneracion = g.Nombre,
                            DiasTranscurridos = (DateTime.Today - i.Fecha).Days,
                            DiasRestantes = t.FechaFin.HasValue ? (t.FechaFin.Value - DateTime.Today).Days : null,
                            ProgresoPorcentaje = CalcularProgreso(i.Fecha, t.FechaInicio, t.FechaFin)
                        };

            // Filtrar por promotor si se especifica
            if (promotorId.HasValue)
            {
                query = query.Where(x => x.NombrePromotor == _escuelaContext.Promotores
                    .Where(p => p.PromotorId == promotorId.Value)
                    .Select(p => p.Nombre)
                    .FirstOrDefault());
            }

            return await query.OrderByDescending(x => x.FechaInscripcion).ToArrayAsync(ct);
        }

        public async Task<InscripcionEstadisticasDTO> ObtenerEstadisticasInscripcionesAsync(
            int? tallerId = null,
            int? generacionId = null,
            DateTime? desde = null,
            DateTime? hasta = null,
            CancellationToken ct = default)
        {
            var inscripciones = await ObtenerInscripcionesReporteAsync(tallerId, null, generacionId, desde, hasta, ct);

            var estadisticas = new InscripcionEstadisticasDTO
            {
                TotalInscripciones = inscripciones.Length,
                InscripcionesActivas = inscripciones.Count(i => i.Estado == "Pendiente"),
                InscripcionesCanceladas = inscripciones.Count(i => i.Estado == "Cancelada"),
                InscripcionesPagadas = inscripciones.Count(i => i.Estado == "Pagada"),
                
                MontoTotalInscripciones = inscripciones.Sum(i => i.Costo),
                MontoTotalRecaudado = inscripciones.Sum(i => i.Costo - i.SaldoActual),
                MontoTotalPendiente = inscripciones.Sum(i => i.SaldoActual),
                
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
    }
}
