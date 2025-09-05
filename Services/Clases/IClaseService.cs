using ControlTalleresMVP.Persistence.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ControlTalleresMVP.Services.Clases
{
    public interface IClaseService
    {
        public Task<Clase> RegistrarClaseAsync(
            int alumnoId, int tallerId, DateTime fecha,
            decimal abonoInicial = 0m,
            CancellationToken ct = default);

        Task<bool> ExisteCargoClaseAsync(int alumnoId, int claseId, DateTime fecha, CancellationToken ct = default);

        Task<Clase[]> ObtenerClasesDeAlumnoAsync(int alumnoId, CancellationToken ct = default);

        Task CancelarClaseAsync(int claseId, CancellationToken ct = default);

    }
}
