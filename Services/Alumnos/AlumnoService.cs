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

namespace ControlTalleresMVP.Services.Alumnos
{
    public class AlumnoService: IAlumnoService
    {
        public ObservableCollection<AlumnoDTO> RegistrosAlumnos { get; set; } = new();

        private readonly EscuelaContext _context;
        public AlumnoService(EscuelaContext context)
        {
            _context = context;
        }

        public async Task<Alumno> GuardarAsync(Alumno alumno, CancellationToken ct = default)
        {
            _context.Alumnos.Add(alumno);
            await _context.SaveChangesAsync(ct);
            await InicializarRegistros(ct);
            return alumno;
        }

        public async Task EliminarAsync(int id, CancellationToken ct = default)
        {
            var alumno = await _context.Alumnos.FirstOrDefaultAsync(a => a.AlumnoId == id);
            if (alumno is null) return;

            alumno.Eliminado = true;
            alumno.EliminadoEn = DateTime.Now;

            await _context.SaveChangesAsync(ct);
            await InicializarRegistros(ct);
        }

        public async Task ActualizarAsync(Alumno alumno, CancellationToken ct = default)
        {
            if (alumno.AlumnoId <= 0)
                throw new ArgumentException("El ID del alumno debe ser válido");

            var alumnoExistente = await _context.Alumnos
                .FirstOrDefaultAsync(a => a.AlumnoId == alumno.AlumnoId, ct);

            if (alumnoExistente is null)
                throw new InvalidOperationException($"No se encontró el alumno con ID {alumno.AlumnoId}");

            // Solo actualizas campos que quieres
            alumnoExistente.Nombre = alumno.Nombre;
            alumnoExistente.Telefono = alumno.Telefono;
            alumnoExistente.SedeId = alumno.SedeId == 0 ? null : alumno.SedeId;
            alumnoExistente.PromotorId = alumno.PromotorId == 0 ? null : alumno.PromotorId;
            alumnoExistente.ActualizadoEn = DateTime.Now;

            try
            {
                await _context.SaveChangesAsync(ct);
                await InicializarRegistros(ct);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al actualizar el alumno: " + ex.Message,
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public async Task<List<AlumnoDTO>> ObtenerAlumnosParaGridAsync(CancellationToken ct = default)
        {

            var datos = await _context.Alumnos
                .AsNoTracking()
                .Where(a => !a.Eliminado)
                .Select(u => new
                {
                    u.AlumnoId,
                    u.Nombre,
                    u.Telefono,
                    u.Sede,
                    u.Promotor,
                    u.CreadoEn
                })
                .ToListAsync(ct);

            return datos.Select(u => new AlumnoDTO
            {
                Id = u.AlumnoId,
                Nombre = u.Nombre,
                Telefono = u.Telefono,
                Sede = u.Sede,
                Promotor = u.Promotor,
                CreadoEn = u.CreadoEn
            }).ToList();
        }


        public async Task InicializarRegistros(CancellationToken ct = default)
        {
            var alumnos = await ObtenerAlumnosParaGridAsync(ct);

            RegistrosAlumnos.Clear();

            foreach (var alumno in alumnos)
            {
                RegistrosAlumnos.Add(alumno);
            }
        }

        public List<Alumno> ObtenerTodos(CancellationToken ct = default)
        {
            return _context.Alumnos
                .AsNoTracking()
                .Where(a => !a.Eliminado)
                .ToList();
        }

    }
}
