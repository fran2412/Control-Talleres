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

        public async Task<Sede> GuardarAsync(Sede sede, CancellationToken ct = default)
        {
            _context.Sedes.Add(sede);
            await _context.SaveChangesAsync(ct);

            InsertarSedeOrdenada(new SedeDTO
            {
                Id = sede.SedeId,
                Nombre = sede.Nombre,
                CreadoEn = sede.CreadoEn
            });
            return sede;
        }

        public async Task EliminarAsync(int id, CancellationToken ct = default)
        {
            var sede = await _context.Sedes.FirstOrDefaultAsync(p => p.SedeId == id);
            if (sede is null) return;

            // Validar que no tenga alumnos asociados
            var tieneAlumnos = await _context.Alumnos
                .AnyAsync(a => a.SedeId == id && !a.Eliminado, ct);
            
            if (tieneAlumnos)
                throw new InvalidOperationException("No se puede eliminar una sede que tiene alumnos registrados.");

            sede.Eliminado = true;
            sede.EliminadoEn = DateTime.Now;

            await _context.SaveChangesAsync(ct);

            var dto = RegistrosSedes.FirstOrDefault(s => s.Id == id);
            if (dto is not null)
            {
                RegistrosSedes.Remove(dto);
            }
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

                var indice = ObtenerIndicePorId(sedeExistente.SedeId);
                var sedeDtoActualizada = new SedeDTO
                {
                    Id = sedeExistente.SedeId,
                    Nombre = sedeExistente.Nombre,
                    CreadoEn = sedeExistente.CreadoEn
                };

                if (indice >= 0)
                {
                    RegistrosSedes[indice] = sedeDtoActualizada;
                }
                else
                {
                    InsertarSedeOrdenada(sedeDtoActualizada);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al actualizar la sede: " + ex.Message,
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void InsertarSedeOrdenada(SedeDTO sedeDto)
        {
            var indiceExistente = ObtenerIndicePorId(sedeDto.Id);
            if (indiceExistente >= 0)
            {
                RegistrosSedes.RemoveAt(indiceExistente);
            }

            var index = 0;
            while (index < RegistrosSedes.Count && RegistrosSedes[index].CreadoEn >= sedeDto.CreadoEn)
            {
                index++;
            }

            RegistrosSedes.Insert(index, sedeDto);
        }

        private int ObtenerIndicePorId(int id)
        {
            for (var i = 0; i < RegistrosSedes.Count; i++)
            {
                if (RegistrosSedes[i].Id == id)
                    return i;
            }

            return -1;
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

            // Ordenar por fecha de creación descendente (más recientes primero)
            var sedesOrdenadas = sedes.OrderByDescending(s => s.CreadoEn).ToList();

            RegistrosSedes.Clear();

            foreach (var sede in sedesOrdenadas)
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
