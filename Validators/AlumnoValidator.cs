using ControlTalleresMVP.Helpers.Dialogs;
using ControlTalleresMVP.Persistence.DataContext;
using F23.StringSimilarity;
using Microsoft.EntityFrameworkCore;

namespace ControlTalleresMVP.Validators
{
    public class AlumnoValidator : IAlumnoValidator
    {
        // Refactorizar esto luego, el context no debería estar aquí
        private readonly IDialogService _dialogService;
        private readonly EscuelaContext _context;

        public AlumnoValidator(IDialogService dialogService, EscuelaContext context)
        {
            _dialogService = dialogService;
            _context = context;
        }

        public bool ConfirmarNombresSimilares(string nombreAlumno)
        {
            char inicialAlumno = nombreAlumno[0];

            var alumnosConMismaInicial = _context.Alumnos
                .Where(a => EF.Functions.Like(a.Nombre, $"{inicialAlumno}%"))
                .AsNoTracking()
                .ToList();

            var alumnosAValidar = alumnosConMismaInicial
                .Where(a => CalcularSimilitud(a.Nombre, nombreAlumno) >= 0.9)
                .ToList();

            if (!alumnosAValidar.Any())
                return true;

            int total = alumnosAValidar.Count;
            string alumnosListados = string.Join("\n", alumnosAValidar.Select(a => $"- {a.Nombre}"));

            string mensaje = total == 1
                ? $"Se ha encontrado {total} alumno con un nombre similar\n{alumnosListados}\n¿Desea continuar?"
                : $"Se han encontrado {total} alumnos con nombres similares\n{alumnosListados}\n¿Desea continuar?";

            return _dialogService.Confirmar(mensaje);
        }

        private static double CalcularSimilitud(string nuevoAlumno, string alumnoRegistrado)
        {
            var jw = new JaroWinkler();
            return jw.Similarity(nuevoAlumno, alumnoRegistrado);
        }
    }
}
