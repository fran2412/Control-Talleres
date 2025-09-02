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

namespace ControlTalleresMVP.Services.Talleres
{
    internal class TallerService : ITallerService
    {
        public ObservableCollection<TallerDTO> RegistrosTalleres { get; set; } = new();

        private readonly EscuelaContext _context;
        public TallerService(EscuelaContext context)
        {
            _context = context;
        }

        public async Task<Taller> GuardarAsync(Taller taller, CancellationToken ct = default)
        {
            _context.Talleres.Add(taller);
            await _context.SaveChangesAsync(ct);
            await InicializarRegistros(ct);
            return taller;
        }

        public async Task EliminarAsync(int id, CancellationToken ct = default)
        {
            var taller = await _context.Talleres.FirstOrDefaultAsync(p => p.TallerId == id);
            if (taller is null) return;

            taller.Eliminado = true;
            taller.EliminadoEn = DateTime.Now;

            await _context.SaveChangesAsync(ct);
            await InicializarRegistros(ct);
        }

        public async Task ActualizarAsync(Taller taller, CancellationToken ct = default)
        {
            if (taller.TallerId <= 0)
                throw new ArgumentException("El ID del taller debe ser válida");

            var tallerExistente = await _context.Talleres
                .FirstOrDefaultAsync(s => s.TallerId == taller.TallerId, ct);

            if (tallerExistente is null)
                throw new InvalidOperationException($"No se encontró al taller con ID {taller.TallerId}");

            // Solo actualizas campos que quieres
            tallerExistente.Nombre = taller.Nombre;
            tallerExistente.Horario = taller.Horario;
            tallerExistente.ActualizadoEn = DateTime.Now;

            try
            {
                await _context.SaveChangesAsync(ct);
                await InicializarRegistros(ct);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al actualizar el taller: " + ex.Message,
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public async Task<List<TallerDTO>> ObtenerTalleresParaGridAsync(CancellationToken ct = default)
        {

            var datos = await _context.Talleres
                .AsNoTracking()
                .Where(a => !a.Eliminado)
                .Select(u => new
                {
                    u.TallerId,
                    u.Horario,
                    u.Nombre,
                    u.CreadoEn
                })
                .ToListAsync(ct);

            return datos.Select(u => new TallerDTO
            {
                Id = u.TallerId,
                Nombre = u.Nombre,
                Horario = u.Horario,
                CreadoEn = u.CreadoEn
            }).ToList();
        }


        public async Task InicializarRegistros(CancellationToken ct = default)
        {
            var talleres = await ObtenerTalleresParaGridAsync(ct);

            RegistrosTalleres.Clear();

            foreach (var taller in talleres)
            {
                RegistrosTalleres.Add(taller);
            }
        }

        public List<Taller> ObtenerTodos(CancellationToken ct = default)
        {
            return _context.Talleres
                .AsNoTracking()
                .Where(p => !p.Eliminado)
                .ToList();
        }
    }
}
