using ControlTalleresMVP.Helpers.Dialogs;
using ControlTalleresMVP.Persistence.DataContext;
using ControlTalleresMVP.Persistence.ModelDTO;
using ControlTalleresMVP.Persistence.Models;
using ControlTalleresMVP.Services.Cargos;
using ControlTalleresMVP.Services.Generaciones;
using ControlTalleresMVP.Services.Inscripciones;
using ControlTalleresMVP.Services.Sesion;
using ControlTalleresMVP.Validators;
using Microsoft.EntityFrameworkCore;
using System.Collections.ObjectModel;

namespace ControlTalleresMVP.Services.Alumnos
{
    public class AlumnoService : IAlumnoService
    {
        public ObservableCollection<AlumnoDTO> RegistrosAlumnos { get; set; } = new();

        private readonly EscuelaContext _context;
        private readonly ICargosService _cargosService;
        private readonly IGeneracionService _generacionService;
        private readonly IInscripcionService _inscripcionService;
        private readonly IDialogService _dialogService;
        private readonly IAlumnoValidator _alumnoValidator;
        private readonly ISesionService _sesionService;
        public AlumnoService(EscuelaContext context, IDialogService dialogService, ICargosService cargosService, IInscripcionService inscripcionService, IGeneracionService generacionService, IAlumnoValidator alumnoValidator, ISesionService sesionService)
        {
            _context = context;
            _cargosService = cargosService;
            _inscripcionService = inscripcionService;
            _dialogService = dialogService;
            _generacionService = generacionService;
            _alumnoValidator = alumnoValidator;
            _sesionService = sesionService;
        }

        public async Task<Alumno> GuardarAsync(Alumno alumno, CancellationToken ct = default)
        {
            //Quitar esto de aquí, no debe de ir en el service
            if (!_alumnoValidator.ConfirmarNombresSimilares(alumno.Nombre)) throw new OperationCanceledException("Operación cancelada por el usuario debido a nombres similares.");

            _context.Alumnos.Add(alumno);
            await _context.SaveChangesAsync(ct);

            await _context.Entry(alumno).Reference(a => a.Sede).LoadAsync(ct);
            await _context.Entry(alumno).Reference(a => a.Promotor).LoadAsync(ct);

            var alumnoDto = ConvertirADto(alumno);

            RegistrosAlumnos.Insert(0, alumnoDto);
            return alumno;
        }

        public async Task EliminarAsync(int id, CancellationToken ct = default)
        {
            var alumnoOC = RegistrosAlumnos.FirstOrDefault(a => a.Id == id);
            if (alumnoOC is null) return;

            var alumno = await _context.Alumnos.FirstOrDefaultAsync(a => a.AlumnoId == id);
            if (alumno is null) return;

            alumno.Eliminado = true;
            alumno.EliminadoEn = DateTime.Now;

            var cargos = await _cargosService.ObtenerCargosAsync(id);
            foreach (var cargo in cargos)
            {
                cargo.Estado = EstadoCargo.Anulado;
            }

            var inscripciones = await _inscripcionService.ObtenerInscripcionesAlumnoAsync(id);
            foreach (var inscripcion in inscripciones)
            {
                await _inscripcionService.CancelarAsync(inscripcion.InscripcionId, "Alumno dado de baja");
            }

            await _context.SaveChangesAsync(ct);
            RegistrosAlumnos.Remove(alumnoOC);
        }

        public async Task ActualizarAsync(Alumno alumno, CancellationToken ct = default)
        {
            if (alumno.AlumnoId <= 0)
                throw new ArgumentException("El ID del alumno debe ser válido");

            var existente = await _context.Alumnos
                .FirstOrDefaultAsync(a => a.AlumnoId == alumno.AlumnoId, ct);

            if (existente is null)
                throw new InvalidOperationException($"No se encontró el alumno con ID {alumno.AlumnoId}");

            existente.Nombre = (alumno.Nombre ?? "").Trim();
            existente.Telefono = alumno.Telefono;
            existente.PromotorId = alumno.PromotorId == 0 ? null : alumno.PromotorId;
            existente.DescuentoPorClase = alumno.DescuentoPorClase;
            existente.ActualizadoEn = DateTime.Now;

            await _context.SaveChangesAsync(ct);

            var dtoActualizado = await _context.Alumnos
                .AsNoTracking()
                .Where(a => a.AlumnoId == alumno.AlumnoId)
                .Select(a => new AlumnoDTO
                {
                    Id = a.AlumnoId,
                    Nombre = a.Nombre,
                    Telefono = a.Telefono,
                    Sede = a.Sede,
                    Promotor = a.Promotor,
                    CreadoEn = a.CreadoEn,
                    DescuentoPorClase = a.DescuentoPorClase
                })
                .FirstAsync(ct);

            ActualizarAlumnoPorIdReemplazo(alumno.AlumnoId, dtoActualizado);
        }

        public async Task<List<AlumnoDTO>> ObtenerAlumnosDto(CancellationToken ct = default)
        {
            var sedeId = _sesionService.ObtenerIdSede();

            var alumnos = await _context.Alumnos
                .AsNoTracking()
                .Where(a => !a.Eliminado && a.SedeId == sedeId)
                .Select(u => new
                {
                    u.AlumnoId,
                    u.Nombre,
                    u.Telefono,
                    u.Sede,
                    u.Promotor,
                    u.CreadoEn,
                    u.DescuentoPorClase
                })
                .ToListAsync(ct);

            return alumnos.Select(u => new AlumnoDTO
            {
                Id = u.AlumnoId,
                Nombre = u.Nombre,
                Telefono = u.Telefono,
                Sede = u.Sede,
                Promotor = u.Promotor,
                CreadoEn = u.CreadoEn,
                DescuentoPorClase = u.DescuentoPorClase
            }).ToList();
        }

        public AlumnoDTO ConvertirADto(Alumno alumno)
        {
            return new AlumnoDTO
            {
                Id = alumno.AlumnoId,
                Nombre = alumno.Nombre,
                Telefono = alumno.Telefono,
                Promotor = alumno.Promotor,
                Sede = alumno.Sede,
                CreadoEn = alumno.CreadoEn,
                DescuentoPorClase = alumno.DescuentoPorClase
            };
        }

        public async Task InicializarRegistros(CancellationToken ct = default)
        {
            var alumnos = await ObtenerAlumnosDto(ct);

            var alumnosOrdenados = alumnos.OrderByDescending(a => a.CreadoEn).ToList();

            RegistrosAlumnos.Clear();

            foreach (var alumno in alumnosOrdenados)
            {
                RegistrosAlumnos.Add(alumno);
            }
        }

        public List<Alumno> ObtenerTodos(CancellationToken ct = default)
        {
            var sedeId = _sesionService.ObtenerIdSede();
            return _context.Alumnos
                .AsNoTracking()
                .Where(a => !a.Eliminado && a.SedeId == sedeId)
                .ToList();
        }

        public async Task<List<Alumno>> ObtenerAlumnosConDeudasPendientesAsync(CancellationToken ct = default)
        {
            var sedeId = _sesionService.ObtenerIdSede();
            var generacion = _generacionService.ObtenerGeneracionActual();
            if (generacion == null) return new List<Alumno>();

            var alumnos = await _context.Cargos
                .AsNoTracking()
                .Where(c => c.SaldoActual > 0
                           && c.Estado != EstadoCargo.Anulado
                           && !c.Eliminado
                           && (c.InscripcionId == null
                               || c.Inscripcion!.GeneracionId == generacion.GeneracionId))
                .Select(c => c.Alumno)
                .Distinct()
                .Where(a => !a.Eliminado && a.SedeId == sedeId)
                .Include(a => a.Sede)
                .Include(a => a.Promotor)
                .OrderBy(a => a.Nombre)
                .ToListAsync(ct);

            return alumnos;
        }

        public void ActualizarAlumnoPorIdReemplazo(int id, AlumnoDTO nuevoAlumno)
        {
            var alumnoOC = RegistrosAlumnos.FirstOrDefault(a => a.Id == id);

            if (alumnoOC is null) return;

            var index = RegistrosAlumnos.IndexOf(alumnoOC);

            if (index >= 0)
            {
                RegistrosAlumnos[index] = nuevoAlumno; // reemplazo completo
            }
        }

        public ObservableCollection<Alumno> ObtenerPorInicial(char inicial)
        {
            string inicialLower = inicial.ToString().ToLower();

            return new ObservableCollection<Alumno>(_context.Alumnos
                .Where(a => a.Nombre.ToLower().StartsWith(inicialLower))
                .AsNoTracking()
                .ToList());
        }

    }
}
