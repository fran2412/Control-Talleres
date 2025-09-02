using ControlTalleresMVP.Persistence.DataContext;
using ControlTalleresMVP.Persistence.ModelDTO;
using ControlTalleresMVP.Persistence.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace ControlTalleresMVP.Services.Sedes
{
    public class SedeService: ISedeService
    {
        public ObservableCollection<SedeDTO> RegistrosSedes { get; set; } = new();

        private readonly EscuelaContext _context;
        public SedeService(EscuelaContext context)
        {
            _context = context;
        }

        public async Task GuardarAsync(Sede sede, CancellationToken ct = default)
        {
            _context.Sedes.Add(sede);
            await _context.SaveChangesAsync(ct);
            await InicializarRegistros(ct);
        }

        public async Task EliminarAsync(int id, CancellationToken ct = default)
        {
            var sede = await _context.Sedes.FirstOrDefaultAsync(p => p.SedeId == id);
            if (sede is null) return;

            sede.Eliminado = true;
            sede.EliminadoEn = DateTime.Now;

            await _context.SaveChangesAsync(ct);
            await InicializarRegistros(ct);
        }

        public async Task ActualizarAsync(Sede sede, CancellationToken ct = default)
        {
            if (sede.SedeId <= 0)
                throw new ArgumentException("El ID de la sede debe ser válida");

            var sedeExistente = await _context.Sedes
                .FirstOrDefaultAsync(s => s.SedeId == sede.SedeId, ct);

            if (sedeExistente is null)
                throw new InvalidOperationException($"No se encontró a la sede con ID {sede.SedeId}");

            // Solo actualizas campos que quieres
            sedeExistente.Nombre = sede.Nombre;
            sedeExistente.ActualizadoEn = DateTime.Now;

            try
            {
                await _context.SaveChangesAsync(ct);
                await InicializarRegistros(ct);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al actualizar la sede: " + ex.Message,
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public async Task<List<SedeDTO>> ObtenerSedesParaGridAsync(CancellationToken ct = default)
        {

            var datos = await _context.Sedes
                .AsNoTracking()
                .Where(a => !a.Eliminado)
                .Select(u => new
                {
                    u.SedeId,
                    u.Nombre,
                    u.CreadoEn
                })
                .ToListAsync(ct);

            return datos.Select(u => new SedeDTO
            {
                Id = u.SedeId,
                Nombre = u.Nombre,
                CreadoEn = u.CreadoEn
            }).ToList();
        }


        public async Task InicializarRegistros(CancellationToken ct = default)
        {
            var sedes = await ObtenerSedesParaGridAsync(ct);

            RegistrosSedes.Clear();

            foreach (var sede in sedes)
            {
                RegistrosSedes.Add(sede);
            }
        }

        public List<Sede> ObtenerTodos(CancellationToken ct = default)
        {
            return _context.Sedes
                .AsNoTracking()
                .Where(p => !p.Eliminado)
                .ToList();
        }
    }
}
