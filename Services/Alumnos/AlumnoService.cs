using ControlTalleresMVP.Persistence.DataContext;
using ControlTalleresMVP.Persistence.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ControlTalleresMVP.Services.Alumnos
{
    public class AlumnoService: IAlumnoService
    {
        private readonly EscuelaContext _context;
        public AlumnoService(EscuelaContext context)
        {
            _context = context;
        }

        public void AgregarAlumno(Alumno alumno)
        {
            alumno.CreadoEn = DateTimeOffset.UtcNow;
            alumno.ActualizadoEn = DateTimeOffset.UtcNow;

            _context.Alumnos.Add(alumno);
            _context.SaveChanges();
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

        public void EliminarAlumno(int id)
        {
            var alumno = _context.Alumnos
                            .FirstOrDefault(a => a.IdAlumno == id);

            if (alumno == null)
                throw new InvalidOperationException("El alumno no existe");

            // 🔹 Eliminación lógica
            alumno.Eliminado = true;
            alumno.EliminadoEn = DateTimeOffset.UtcNow;
            alumno.ActualizadoEn = DateTimeOffset.UtcNow;

            _context.SaveChanges();
        }
    }
}
