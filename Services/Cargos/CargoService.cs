using ControlTalleresMVP.Persistence.DataContext;
using ControlTalleresMVP.Persistence.ModelDTO;
using ControlTalleresMVP.Persistence.Models;
using ControlTalleresMVP.Services.Generaciones;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace ControlTalleresMVP.Services.Cargos
{
    public class CargoService : ICargosService
    {
        private readonly EscuelaContext _escuelaContext;
        private readonly IGeneracionService _generacionService;

        public CargoService(EscuelaContext escuelaContext, IGeneracionService generacionService)
        {
            _escuelaContext = escuelaContext;
            _generacionService = generacionService;
        }

        public async Task<DestinoCargoDTO[]> ObtenerCargosPendientesActualesAsync(int alumnoId, CancellationToken ct = default)
        {
            // Todos los cargos con saldo > 0 del alumno (incluyendo alumnos dados de baja)
            // Los pagos deben mostrarse incluso si el alumno está dado de baja
            var generacion = _generacionService.ObtenerGeneracionActual();
            if (generacion == null) throw new ArgumentException($"No hay ninguna {nameof(generacion)} activa.");

            var query = _escuelaContext.Cargos
                .AsNoTracking()
                .Where(c => c.AlumnoId == alumnoId
                            && c.SaldoActual > 0
                            && c.Estado != EstadoCargo.Anulado
                            && (c.InscripcionId == null
                                || c.Inscripcion!.GeneracionId == generacion.GeneracionId));

            // Orden recomendable: Clases primero (si ClaseId != null), luego inscripciones, y por saldo descendente
            var list = await query
                .Include(c => c.Inscripcion!)
                    .ThenInclude(i => i.Taller!)
                .Include(c => c.Clase!)
                    .ThenInclude(cl => cl.Taller!)
                .OrderByDescending(c => c.ClaseId != null)
                .Select(c => new DestinoCargoDTO(
                    c.CargoId,
                    c.ClaseId != null ? "Clase"
                       : c.InscripcionId != null ? "Inscripción"
                       : "Cargo",
                    c.ClaseId != null
                       ? $"Clase del {c.Fecha:dd/MM/yyyy} - {c.Clase!.Taller!.Nombre}"
                       : c.InscripcionId != null
                           ? $"Inscripción - {c.Inscripcion!.Taller!.Nombre}"
                           : $"Cargo #{c.CargoId}",
                    c.SaldoActual,
                    c.InscripcionId,
                    c.ClaseId
                ))
                .ToArrayAsync(ct);

            return list;
        }

        public async Task<Cargo[]> ObtenerCargosAsync(int alumnoId, CancellationToken ct = default)
        {
            var cargos = await _escuelaContext.Cargos
                .Where(c => c.AlumnoId == alumnoId && c.Estado != EstadoCargo.Anulado)
                .ToArrayAsync(ct);

            return cargos;
        }
    }
}
