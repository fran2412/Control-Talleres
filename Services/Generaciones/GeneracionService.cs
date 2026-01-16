using ControlTalleresMVP.Persistence.DataContext;
using ControlTalleresMVP.Persistence.ModelDTO;
using ControlTalleresMVP.Persistence.Models;
using ControlTalleresMVP.Services.Sesion;
using Microsoft.EntityFrameworkCore;
using System.Collections.ObjectModel;

namespace ControlTalleresMVP.Services.Generaciones
{
    public class GeneracionService : IGeneracionService
    {

        public ObservableCollection<GeneracionDTO> RegistrosGeneraciones { get; set; } = new();

        private readonly EscuelaContext _context;
        private readonly ISesionService _sesionService;

        public GeneracionService(EscuelaContext context, ISesionService sesionService)
        {
            _context = context;
            _sesionService = sesionService;
        }

        public async Task InicializarRegistros(CancellationToken ct = default)
        {
            var generaciones = await ObtenerGeneracionesParaGridAsync(ct);

            var generacionesOrdenadas = generaciones.OrderByDescending(g => g.FechaInicio).ToList();

            RegistrosGeneraciones.Clear();

            foreach (var generacion in generacionesOrdenadas)
            {
                RegistrosGeneraciones.Add(generacion);
            }
        }

        public async Task<List<GeneracionDTO>> ObtenerGeneracionesParaGridAsync(CancellationToken ct = default)
        {
            var sedeId = _sesionService.ObtenerIdSede();

            var datos = await _context.Generaciones
                .AsNoTracking()
                .Where(a => !a.Eliminado && a.SedeId == sedeId)
                .Select(u => new
                {
                    u.GeneracionId,
                    u.Nombre,
                    u.FechaInicio,
                    u.FechaFin,
                })
                .ToListAsync(ct);

            return datos.Select(u => new GeneracionDTO
            {
                Id = u.GeneracionId,
                Nombre = u.Nombre,
                FechaInicio = u.FechaInicio,
                FechaFin = u.FechaFin,
            }).ToList();
        }

        public async Task NuevaGeneracion(CancellationToken ct = default)
        {
            var año = DateTime.Now.Year;
            var sedeId = _sesionService.ObtenerIdSede();

            // Contar generaciones del año para esta sede específica
            var count = await _context.Generaciones
                .CountAsync(g => g.FechaInicio.Year == año && g.SedeId == sedeId, ct);

            string nombre = $"GEN-{año}-{(count + 1):00}";

            // Cerrar la última generación abierta de esta sede
            var ultimaGeneracion = await _context.Generaciones
                .Where(g => g.SedeId == sedeId)
                .OrderByDescending(g => g.FechaInicio)
                .FirstOrDefaultAsync(ct);

            if (ultimaGeneracion != null && ultimaGeneracion.FechaFin == null)
            {
                ultimaGeneracion.FechaFin = DateTime.Now;

                var dtoUltima = RegistrosGeneraciones
                    .FirstOrDefault(g => g.Id == ultimaGeneracion.GeneracionId);

                if (dtoUltima != null)
                {
                    dtoUltima.FechaFin = ultimaGeneracion.FechaFin;
                }
            }

            var nuevaGeneracion = new Generacion
            {
                Nombre = nombre,
                SedeId = sedeId,
                FechaInicio = DateTime.Now,
                FechaFin = null,
                CreadoEn = DateTime.Now
            };

            await _context.Generaciones.AddAsync(nuevaGeneracion, ct);
            await _context.SaveChangesAsync(ct);

            var nuevoDto = new GeneracionDTO
            {
                Id = nuevaGeneracion.GeneracionId,
                Nombre = nuevaGeneracion.Nombre,
                FechaInicio = nuevaGeneracion.FechaInicio,
                FechaFin = nuevaGeneracion.FechaFin,
            };

            RegistrosGeneraciones.Insert(0, nuevoDto);
        }

        public Generacion? ObtenerGeneracionActual()
        {
            var sedeId = _sesionService.ObtenerIdSede();

            return _context.Generaciones
                .Where(g => g.FechaFin == null && g.SedeId == sedeId)
                .OrderByDescending(g => g.FechaInicio)
                .FirstOrDefault();
        }
    }
}

