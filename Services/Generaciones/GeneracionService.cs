using ControlTalleresMVP.Persistence.DataContext;
using ControlTalleresMVP.Persistence.ModelDTO;
using ControlTalleresMVP.Persistence.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace ControlTalleresMVP.Services.Generaciones
{
    public class GeneracionService : IGeneracionService
    {

        public ObservableCollection<GeneracionDTO> RegistrosGeneraciones { get; set; } = new();

        private readonly EscuelaContext _context;
        public GeneracionService(EscuelaContext context)
        {
            _context = context;
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

            var datos = await _context.Generaciones
                .AsNoTracking()
                .Where(a => !a.Eliminado)
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

            var count = _context.Generaciones
                .Count(g => g.FechaInicio.Year == año);

            string nombre = $"GEN-{año}-{(count + 1):00}";

            var ultimaGeneracion = _context.Generaciones
                .OrderByDescending(g => g.FechaInicio)
                .FirstOrDefault();

            if (ultimaGeneracion == null || ultimaGeneracion.FechaFin != null)
                return;

            ultimaGeneracion.FechaFin = DateTime.Now;

            var dtoUltima = RegistrosGeneraciones
                .FirstOrDefault(g => g.Id == ultimaGeneracion.GeneracionId);

            if (dtoUltima is null) return;

            dtoUltima.FechaFin = ultimaGeneracion.FechaFin;


            // Crear nueva generación
            var nuevaGeneracion = new Generacion
            {
                Nombre = nombre,
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

            int ultimoIndice = RegistrosGeneraciones.Count();
            RegistrosGeneraciones.Insert(ultimoIndice, nuevoDto);
        }

        public Generacion? ObtenerGeneracionActual()
        {
            return _context.Generaciones
                .Where(g => g.FechaFin == null)
                .OrderByDescending(g => g.FechaInicio)
                .FirstOrDefault();
        }
    }
}
