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

namespace ControlTalleresMVP.Services.Promotores
{
    public class PromotorService: IPromotorService
    {
        public ObservableCollection<PromotorDTO> RegistrosPromotores { get; set; } = new();

        private readonly EscuelaContext _context;
        public PromotorService(EscuelaContext context)
        {
            _context = context;
        }

        public async Task GuardarAsync(Promotor promotor, CancellationToken ct = default)
        {
            _context.Promotores.Add(promotor);
            await _context.SaveChangesAsync(ct);
            await InicializarRegistros(ct);
        }

        public async Task EliminarAsync(int id, CancellationToken ct = default)
        {
            var promotor = await _context.Promotores.FirstOrDefaultAsync(p => p.IdPromotor== id);
            if (promotor is null) return;

            promotor.Eliminado = true;
            promotor.EliminadoEn = DateTimeOffset.Now;

            await _context.SaveChangesAsync(ct);
            await InicializarRegistros(ct);
        }

        public async Task ActualizarAsync(Promotor promotor, CancellationToken ct = default)
        {
            if (promotor.IdPromotor <= 0)
                throw new ArgumentException("El ID del alumno debe ser válido");

            var promotorExistente = await _context.Promotores
                .FirstOrDefaultAsync(p => p.IdPromotor == promotor.IdPromotor, ct);

            if (promotorExistente is null)
                throw new InvalidOperationException($"No se encontró el promotor con ID {promotor.IdPromotor}");

            // Solo actualizas campos que quieres
            promotorExistente.Nombre = promotor.Nombre;
            promotorExistente.ActualizadoEn = DateTimeOffset.Now;

            try
            {
                await _context.SaveChangesAsync(ct);
                await InicializarRegistros(ct);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al actualizar el promotor: " + ex.Message,
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        public async Task<List<PromotorDTO>> ObtenerPromotoresParaGridAsync(CancellationToken ct = default)
        {

            var datos = await _context.Promotores
                .AsNoTracking()
                .Where(a => !a.Eliminado)
                .Select(u => new
                {
                    u.IdPromotor,
                    u.Nombre,
                    u.CreadoEn
                })
                .ToListAsync(ct);

            return datos.Select(u => new PromotorDTO
            {
                Id = u.IdPromotor,
                Nombre = u.Nombre,
                CreadoEn = u.CreadoEn
            }).ToList();
        }


        public async Task InicializarRegistros(CancellationToken ct = default)
        {
            var promotores = await ObtenerPromotoresParaGridAsync(ct);

            RegistrosPromotores.Clear();

            foreach (var promotor in promotores)
            {
                RegistrosPromotores.Add(promotor);
            }
        }

        public List<Promotor> ObtenerTodos(CancellationToken ct = default)
        {
            return _context.Promotores
                .AsNoTracking()
                .Where(p => !p.Eliminado)
                .ToList();
        }
    }
}
