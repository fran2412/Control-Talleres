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

        //TODO Eliminar lo que no se usa
        public ObservableCollection<GeneracionDTO> RegistrosGeneraciones { get; set; } = new();

        private readonly EscuelaContext _context;
        public GeneracionService(EscuelaContext context)
        {
            _context = context;
        }

        public async Task<Generacion> GuardarAsync(Generacion generacion, CancellationToken ct = default)
        {
            _context.Generaciones.Add(generacion);
            await _context.SaveChangesAsync(ct);
            return generacion;
        }

        public async Task EliminarAsync(int id, CancellationToken ct = default)
        {
            var generacion = await _context.Generaciones.FirstOrDefaultAsync(a => a.GeneracionId == id);
            if (generacion is null) return;

            generacion.Eliminado = true;
            generacion.EliminadoEn = DateTime.Now;

            await _context.SaveChangesAsync(ct);
            await InicializarRegistros(ct);
        }

        public async Task ActualizarAsync(Generacion generacion, CancellationToken ct = default)
        {
            if (generacion.GeneracionId <= 0)
                throw new ArgumentException("El ID de la generación debe ser válido");

            var generacionExistente = await _context.Generaciones
                .FirstOrDefaultAsync(a => a.GeneracionId == generacion.GeneracionId, ct);

            if (generacionExistente is null)
                throw new InvalidOperationException($"No se encontró la generación con ID {generacion.GeneracionId}");

            // Solo actualizas campos que quieres
            generacionExistente.Nombre = generacion.Nombre;
            generacionExistente.FechaInicio = generacion.FechaInicio;
            generacionExistente.FechaFin = generacion.FechaFin;
            generacionExistente.ActualizadoEn = DateTime.Now;

            try
            {
                await _context.SaveChangesAsync(ct);
                await InicializarRegistros(ct);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al actualizar la generación: " + ex.Message,
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public async Task InicializarRegistros(CancellationToken ct = default)
        {
            var generaciones = await ObtenerGeneracionesParaGridAsync(ct);

            RegistrosGeneraciones.Clear();

            foreach (var generacion in generaciones)
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

        public List<Generacion> ObtenerTodos(CancellationToken ct = default)
        {
            return _context.Generaciones
                .AsNoTracking()
                .OrderByDescending(g => g.FechaInicio)
                .ToList();
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

            if (ultimaGeneracion != null && ultimaGeneracion.FechaFin == null)
            {
                ultimaGeneracion.FechaFin = DateTime.Now;
            }

            // Crear nueva generación
            var nuevaGeneracion = new Generacion
            {
                Nombre = nombre,
                FechaInicio = DateTime.Now,
                FechaFin = null,
                CreadoEn = DateTime.Now
            };

            await _context.Generaciones.AddAsync(nuevaGeneracion);
            await _context.SaveChangesAsync();
            await InicializarRegistros(ct);
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
