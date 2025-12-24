using ControlTalleresMVP.Persistence.Models;

namespace ControlTalleresMVP.Services.Picker
{
    public interface IAlumnoPickerService
    {
        Alumno? Pick(bool excluirBecados = false);
        Task<Alumno?> PickConDeudasAsync(bool excluirBecados = false);
    }

}
