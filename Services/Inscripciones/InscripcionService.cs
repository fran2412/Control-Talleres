using ControlTalleresMVP.Helpers.Dialogs;
using ControlTalleresMVP.Persistence.DataContext;
using ControlTalleresMVP.Persistence.ModelDTO;
using ControlTalleresMVP.Persistence.Models;
using ControlTalleresMVP.Services.Configuracion;
using ControlTalleresMVP.Services.Generaciones;
using Microsoft.EntityFrameworkCore;
using System.Collections.ObjectModel;

namespace ControlTalleresMVP.Services.Inscripciones
{
    public class InscripcionService : IInscripcionService
    {
        private readonly EscuelaContext _escuelaContext;
        private readonly IConfiguracionService _configuracionService;
        private readonly IGeneracionService _generacionService;
        private readonly IDialogService _dialogService;

        public ObservableCollection<InscripcionDTO> RegistrosInscripciones { get; set; } = new();

        public InscripcionService(EscuelaContext escuelaContext, IConfiguracionService configuracionService, IGeneracionService generacionService, IDialogService dialogService)
        {
            _escuelaContext = escuelaContext;
            _configuracionService = configuracionService;
            _generacionService = generacionService;
            _dialogService = dialogService;
        }

        public async Task<bool> ExisteActivaAsync(int alumnoId, int tallerId, int generacionId, CancellationToken ct = default)
            => await _escuelaContext.Inscripciones.AnyAsync(i => i.AlumnoId == alumnoId
                                                  && i.TallerId == tallerId
                                                  && i.GeneracionId == generacionId
                                                  && !i.Eliminado, ct);

        public async Task<Inscripcion> InscribirAsync(
            int alumnoId, int tallerId,
            decimal abonoInicial = 0m,
            DateTime? fecha = null, CancellationToken ct = default)
        {

            var generacionId = _generacionService.ObtenerGeneracionActual();
            if (generacionId == null) throw new InvalidOperationException("No hay una generación activa.");

            // Validar duplicado
            if (await ExisteActivaAsync(alumnoId, tallerId, generacionId.GeneracionId, ct))
                throw new InvalidOperationException("Ya existe una inscripción activa para este alumno en el mismo taller y generación.");

            // (Opcional) validar cupo
            var taller = await _escuelaContext.Talleres.FirstOrDefaultAsync(t => t.TallerId == tallerId, ct)
                         ?? throw new InvalidOperationException("Taller no encontrado.");

            var generacion = await _escuelaContext.Generaciones.FirstOrDefaultAsync(g => g.GeneracionId == generacionId.GeneracionId, ct)
                             ?? throw new InvalidOperationException("Generación no encontrada.");

            var costo = _configuracionService.GetValor<int>("costo_inscripcion", 600);

            if (abonoInicial < 0 || abonoInicial > costo) throw new InvalidOperationException("Abono inicial inválido.");

            var saldo = costo - abonoInicial;
            var estado = saldo == 0 ? EstadoInscripcion.Pagada
                        : abonoInicial > 0 ? EstadoInscripcion.Pendiente
                        : EstadoInscripcion.Pendiente;

            var now = DateTime.Now;

            using var transaccion = await _escuelaContext.Database.BeginTransactionAsync(ct);
            try
            {
                // 1. Crear la inscripción
                var inscripcion = new Inscripcion
                {
                    AlumnoId = alumnoId,
                    TallerId = tallerId,
                    GeneracionId = generacionId.GeneracionId,
                    Fecha = fecha ?? now,
                    Costo = costo,
                    SaldoActual = saldo,
                    Estado = estado,
                    CreadoEn = now,
                    ActualizadoEn = now,
                    Eliminado = false
                };

                _escuelaContext.Inscripciones.Add(inscripcion);
                await _escuelaContext.SaveChangesAsync(ct);

                // 2. Crear el cargo por la inscripción
                var cargo = new Cargo
                {
                    AlumnoId = alumnoId,
                    InscripcionId = inscripcion.InscripcionId,
                    Tipo = TipoCargo.Inscripcion,
                    Monto = costo,
                    SaldoActual = saldo,
                    Fecha = now,
                    CreadoEn = now,
                    ActualizadoEn = now,
                    Eliminado = false,
                    Estado = EstadoCargo.Vigente
                };

                _escuelaContext.Cargos.Add(cargo);
                await _escuelaContext.SaveChangesAsync(ct);

                // 3. Si hay abono inicial, crear el pago y su aplicación
                if (abonoInicial > 0m)
                {
                    var pago = new Pago
                    {
                        AlumnoId = alumnoId,
                        Fecha = now,
                        MontoTotal = abonoInicial,
                        Metodo = MetodoPago.Efectivo,
                        Notas = "Abono inicial de inscripción",
                        CreadoEn = now,
                        ActualizadoEn = now,
                        Eliminado = false
                    };

                    _escuelaContext.Pagos.Add(pago);
                    await _escuelaContext.SaveChangesAsync(ct);

                    // Crear la aplicación del pago al cargo
                    var aplicacion = new PagoAplicacion
                    {
                        PagoId = pago.PagoId,
                        CargoId = cargo.CargoId,
                        MontoAplicado = abonoInicial,
                        Estado = EstadoAplicacionCargo.Vigente
                    };

                    _escuelaContext.PagoAplicaciones.Add(aplicacion);
                    await _escuelaContext.SaveChangesAsync(ct);
                }

                await transaccion.CommitAsync(ct);
                await InicializarRegistros(ct);
                return inscripcion;
            }
            catch
            {
                await transaccion.RollbackAsync(ct);
                throw;
            }
        }

        public async Task CancelarAsync(int inscripcionId, string? motivo = null, CancellationToken ct = default)
        {
            var inscripcion = await _escuelaContext.Inscripciones
                .FirstOrDefaultAsync(i => i.InscripcionId == inscripcionId, ct)
                      ?? throw new InvalidOperationException("Inscripción no encontrada.");

            if (motivo == null)
            {
                var motivoCancelacion = _dialogService.PedirTexto("Ingrese el motivo de la cancelación de la inscripción\nOpcional");
                if (String.IsNullOrWhiteSpace(motivoCancelacion)) motivoCancelacion = "No especificado";
                 motivo = motivoCancelacion;
            }
            inscripcion.MotivoCancelacion = motivo;
            inscripcion.CanceladaEn = DateTime.Now;
            inscripcion.Eliminado = true;
            inscripcion.EliminadoEn = DateTime.Now;
            inscripcion.Estado = EstadoInscripcion.Cancelada;
            inscripcion.ActualizadoEn = DateTime.Now;

            await _escuelaContext.SaveChangesAsync(ct);
        }


        public async Task InicializarRegistros(CancellationToken ct = default)
        {
            var inscripciones = await ObtenerInscripcionesParaGridAsync(ct);

            RegistrosInscripciones.Clear();

            foreach (var inscripcion in inscripciones)
            {
                RegistrosInscripciones.Add(inscripcion);
            }
        }

        public async Task<List<InscripcionDTO>> ObtenerInscripcionesParaGridAsync(CancellationToken ct = default)
        {
            var genActualId = _generacionService.ObtenerGeneracionActual()?.GeneracionId
                ?? throw new InvalidOperationException("No hay generación actual.");

            var datos = await _escuelaContext.Inscripciones
                .AsNoTracking()
                .Where(i => !i.Eliminado && i.GeneracionId == genActualId)
                .Select(i => new InscripcionDTO
                {
                    Id          = i.InscripcionId,
                    Nombre      = i.Alumno.Nombre,  // ← string plano
                    Taller      = i.Taller.Nombre,  // ← string plano
                    Costo       = i.Costo,
                    SaldoActual = i.SaldoActual,
                    Estado      = i.Estado,
                    CreadoEn    = i.Fecha
                })
                .ToListAsync(ct);

            return datos;
        }

        public async Task<Inscripcion[]> ObtenerInscripcionesAsync(int alumnoId, CancellationToken ct = default)
        {
            var cargos = await _escuelaContext.Inscripciones
                .Where(c => c.AlumnoId == alumnoId && !c.Eliminado && c.Estado != EstadoInscripcion.Cancelada)
                .ToArrayAsync(ct);

            return cargos;
        }
    }
}
