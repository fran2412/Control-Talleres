using ControlTalleresMVP.Abstractions;
using ControlTalleresMVP.Persistence.ModelDTO;
using ControlTalleresMVP.Persistence.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ControlTalleresMVP.Services.Inscripciones
{
    public interface IInscripcionService
    {
        public ObservableCollection<InscripcionDTO> RegistrosInscripciones { get; set; }
        public ObservableCollection<InscripcionRegistroDTO> RegistrosInscripcionesCompletos { get; set; }
        Task<bool> ExisteActivaAsync(int alumnoId, int tallerId, int generacionId, CancellationToken ct = default);
        Task<Inscripcion> InscribirAsync(
            int alumnoId, int tallerId,
            decimal abonoInicial = 0m,
            DateTime? fecha = null, CancellationToken ct = default);
        public Task InicializarRegistros(CancellationToken ct = default);
        public Task InicializarRegistrosCompletos(CancellationToken ct = default);
        public Task CancelarAsync(int inscripcionId, string? motivo = null, CancellationToken ct = default);
        public Task<Inscripcion[]> ObtenerInscripcionesAsync(int alumnoId, CancellationToken ct = default);
        public Task<List<InscripcionRegistroDTO>> ObtenerInscripcionesCompletasAsync(
            int? generacionId = null,
            int? tallerId = null,
            int? alumnoId = null,
            DateTime? desde = null,
            DateTime? hasta = null,
            string? filtro = null,
            CancellationToken ct = default);
    }
}
