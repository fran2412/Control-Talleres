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

        public async Task<Promotor> GuardarAsync(Promotor promotor, CancellationToken ct = default)
        {
            _context.Promotores.Add(promotor);
            await _context.SaveChangesAsync(ct);

            var dto = MapToDto(promotor);
            InsertOrUpdateRegistro(dto);
            return promotor;
        }

        public async Task EliminarAsync(int id, CancellationToken ct = default)
        {
            var promotor = await _context.Promotores.FirstOrDefaultAsync(p => p.PromotorId== id);
            if (promotor is null) return;

            promotor.Eliminado = true;
            promotor.EliminadoEn = DateTime.Now;

            await _context.SaveChangesAsync(ct);

            var dto = RegistrosPromotores.FirstOrDefault(p => p.Id == id);
            if (dto is not null)
            {
                RegistrosPromotores.Remove(dto);
            }
        }

        public async Task ActualizarAsync(Promotor promotor, CancellationToken ct = default)
        {
            if (promotor.PromotorId <= 0)
                throw new ArgumentException("El ID del alumno debe ser válido");

            var promotorExistente = await _context.Promotores
                .FirstOrDefaultAsync(p => p.PromotorId == promotor.PromotorId, ct);

            if (promotorExistente is null)
                throw new InvalidOperationException($"No se encontró el promotor con ID {promotor.PromotorId}");

            // Solo actualizas campos que quieres
            promotorExistente.Nombre = promotor.Nombre;
            promotorExistente.Telefono = promotor.Telefono;
            promotorExistente.ActualizadoEn = DateTime.Now;

            try
            {
                await _context.SaveChangesAsync(ct);

                var dtoActualizado = MapToDto(promotorExistente);
                InsertOrUpdateRegistro(dtoActualizado);
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
                    u.PromotorId,
                    u.Nombre,
                    u.Telefono,
                    u.CreadoEn
                })
                .ToListAsync(ct);

            return datos.Select(u => new PromotorDTO
            {
                Id = u.PromotorId,
                Nombre = u.Nombre,
                Telefono = u.Telefono,
                CreadoEn = u.CreadoEn
            }).ToList();
        }


        public async Task InicializarRegistros(CancellationToken ct = default)
        {
            var promotores = await ObtenerPromotoresParaGridAsync(ct);

            // Ordenar por fecha de creación descendente (más recientes primero)
            var promotoresOrdenados = promotores.OrderByDescending(p => p.CreadoEn).ToList();

            RegistrosPromotores.Clear();

            foreach (var promotor in promotoresOrdenados)
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

        private static PromotorDTO MapToDto(Promotor promotor)
        {
            return new PromotorDTO
            {
                Id = promotor.PromotorId,
                Nombre = promotor.Nombre,
                Telefono = promotor.Telefono,
                CreadoEn = promotor.CreadoEn
            };
        }

        private void InsertOrUpdateRegistro(PromotorDTO dto)
        {
            var existente = RegistrosPromotores
                .Select((p, index) => new { Promotor = p, Index = index })
                .FirstOrDefault(x => x.Promotor.Id == dto.Id);

            if (existente is not null)
            {
                RegistrosPromotores.RemoveAt(existente.Index);
            }

            var insertIndex = 0;
            while (insertIndex < RegistrosPromotores.Count &&
                   RegistrosPromotores[insertIndex].CreadoEn >= dto.CreadoEn)
            {
                insertIndex++;
            }

            RegistrosPromotores.Insert(insertIndex, dto);
        }
    }
}
