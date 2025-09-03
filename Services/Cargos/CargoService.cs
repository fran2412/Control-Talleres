using ControlTalleresMVP.Persistence.DataContext;
using ControlTalleresMVP.Persistence.ModelDTO;
using ControlTalleresMVP.Persistence.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ControlTalleresMVP.Services.Cargos
{
    public class CargoService : ICargosService
    {
        private readonly EscuelaContext _escuelaContext;

        public CargoService(EscuelaContext escuelaContext)
        {
            _escuelaContext = escuelaContext;
        }

        public async Task<DestinoCargoDTO[]> ObtenerCargosPendientesAsync(int alumnoId, CancellationToken ct = default)
        {
            // Todos los cargos con saldo > 0 del alumno (sin importar si son inscripción o clase)
            // Si quieres excluir inscripciones canceladas, agrega filtro por navegación si la tienes (i.Eliminado == false).
            var query = _escuelaContext.Cargos
                .AsNoTracking()
                .Where(c => c.AlumnoId == alumnoId
                            && !c.Eliminado
                            && c.SaldoActual > 0
                            && c.Estado != EstadoCargo.Anulado);

            // Orden recomendable: Clases primero (si ClaseId != null), luego inscripciones, y por saldo descendente
            var list = await query
                .OrderByDescending(c => c.ClaseId != null)
                .ThenByDescending(c => (double) c.SaldoActual)
                .Select(c => new DestinoCargoDTO(
                    c.CargoId,
                    c.ClaseId != null ? "Clase"
                       : c.InscripcionId != null ? "Inscripción"
                       : "Cargo",
                    c.ClaseId != null
                       ? $"Clase #{c.ClaseId}"
                       : c.InscripcionId != null
                           ? $"Inscripción #{c.InscripcionId}"
                           : $"Cargo #{c.CargoId}",
                    c.SaldoActual,
                    c.InscripcionId,
                    c.ClaseId
                ))
                .ToArrayAsync(ct);

            return list;
        }
    }
}
