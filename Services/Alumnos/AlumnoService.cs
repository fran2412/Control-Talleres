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

        public async Task GuardarAsync(Alumno alumno, CancellationToken ct = default)
        {
            _context.Alumnos.Add(alumno);
            await _context.SaveChangesAsync(ct);
            await InicializarRegistros(ct);
        }

        public void EditarAlumno(Alumno alumno)
        {
            var existente = _context.Alumnos
                           .FirstOrDefault(a => a.IdAlumno == alumno.IdAlumno);

            if (existente == null)
                throw new InvalidOperationException("El alumno no existe");

            // Actualizar propiedades necesarias
            existente.Nombre = alumno.Nombre;
            existente.Telefono = alumno.Telefono;
            existente.IdSede = alumno.IdSede;
            existente.IdPromotor = alumno.IdPromotor;
            existente.ActualizadoEn = DateTimeOffset.UtcNow;

            _context.SaveChanges();
        }

        public async Task EliminarAsync(int id, CancellationToken ct = default)
        {
            var alumno = await _context.Alumnos.FirstOrDefaultAsync(a => a.IdAlumno == id);
            if (alumno is null) return;

            alumno.Eliminado = true;
            alumno.EliminadoEn = DateTimeOffset.Now;

            await _context.SaveChangesAsync(ct);
            await InicializarRegistros(ct);
        }

        public async Task ActualizarAsync(Alumno alumno, CancellationToken ct = default)
        {
            alumno.ActualizadoEn = DateTimeOffset.UtcNow;
            _context.Alumnos.Update(alumno);
            await _context.SaveChangesAsync(ct);
            await InicializarRegistros(ct);
        }

        public async Task<List<AlumnoDTO>> ObtenerAlumnosParaGridAsync(CancellationToken ct = default)
        {

            var datos = await _context.Alumnos
                .AsNoTracking()
                .Where(a => !a.Eliminado)
                .Select(u => new
                {
                    u.IdAlumno,
                    u.Nombre,
                    u.Telefono,
                    u.Sede,
                    u.Promotor,
                    u.CreadoEn
                })
                .ToListAsync(ct);

            return datos.Select(u => new AlumnoDTO
            {
                Id = u.IdAlumno,
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
    }
}
