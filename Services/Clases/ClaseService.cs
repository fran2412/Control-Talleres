using ControlTalleresMVP.Helpers.Dialogs;
using ControlTalleresMVP.Persistence.DataContext;
using ControlTalleresMVP.Persistence.Models;
using ControlTalleresMVP.Services.Configuracion;
using Microsoft.EntityFrameworkCore;

namespace ControlTalleresMVP.Services.Clases
{
    public class ClaseService : IClaseService
    {
        private readonly EscuelaContext _escuelaContext;
        private readonly IConfiguracionService _configuracionService;
        private readonly IDialogService _dialogService;

        public ClaseService(EscuelaContext escuelaContext, IConfiguracionService configuracionService, IDialogService dialogService)
        {
            _escuelaContext = escuelaContext;
            _configuracionService = configuracionService;
            _dialogService = dialogService;
        }

        // ====================
        // Registrar clase y cargo
        // ====================
        public async Task<Clase> RegistrarClaseAsync(
            int alumnoId, int tallerId, DateTime fecha,
            decimal abonoInicial = 0m,
            CancellationToken ct = default)
        {
            var now = DateTime.Now;

            // Verificar si ya existe clase en esa fecha para el taller
            var clase = await _escuelaContext.Clases
                .FirstOrDefaultAsync(c =>
                    c.TallerId == tallerId &&
                    c.Fecha.Date == fecha.Date &&
                    !c.Eliminado, ct);

            if (clase is null)
            {
                clase = new Clase
                {
                    TallerId = tallerId,
                    Fecha = fecha.Date,
                    CreadoEn = now,
                    ActualizadoEn = now,
                    Eliminado = false
                };

                _escuelaContext.Clases.Add(clase);
                await _escuelaContext.SaveChangesAsync(ct);
            }

            // Evitar duplicado de cargo
            if (await ExisteCargoClaseAsync(alumnoId, clase.ClaseId, fecha, ct))
                throw new InvalidOperationException("Ya existe un cargo activo para este alumno en la misma clase.");

            using var transaccion = await _escuelaContext.Database.BeginTransactionAsync(ct);
            try
            {
                // Crear el cargo
                var costoClase = _configuracionService.GetValor<int>("costo_clase", 150);
                var cargo = new Cargo
                {
                    AlumnoId = alumnoId,
                    ClaseId = clase.ClaseId,
                    Tipo = TipoCargo.Clase,
                    Monto = costoClase,
                    SaldoActual = costoClase,
                    Fecha = now,
                    Estado = EstadoCargo.Pendiente,
                    CreadoEn = now,
                    ActualizadoEn = now,
                    Eliminado = false
                };

                _escuelaContext.Cargos.Add(cargo);
                await _escuelaContext.SaveChangesAsync(ct);

                // Abono inicial opcional
                if (abonoInicial > 0m)
                {
                    var pago = new Pago
                    {
                        AlumnoId = alumnoId,
                        Fecha = now,
                        MontoTotal = abonoInicial,
                        Metodo = MetodoPago.Efectivo,
                        Notas = "Pago de clase",
                        CreadoEn = now,
                        ActualizadoEn = now,
                        Eliminado = false
                    };

                    _escuelaContext.Pagos.Add(pago);
                    await _escuelaContext.SaveChangesAsync(ct);

                    var aplicacion = new PagoAplicacion
                    {
                        PagoId = pago.PagoId,
                        CargoId = cargo.CargoId,
                        MontoAplicado = abonoInicial,
                        Estado = EstadoAplicacionCargo.Vigente
                    };

                    _escuelaContext.PagoAplicaciones.Add(aplicacion);

                    cargo.SaldoActual -= abonoInicial;
                    cargo.ActualizadoEn = now;

                    if (cargo.SaldoActual == 0)
                        cargo.Estado = EstadoCargo.Pagado;

                    await _escuelaContext.SaveChangesAsync(ct);
                }

                await transaccion.CommitAsync(ct);
                return clase;
            }
            catch
            {
                await transaccion.RollbackAsync(ct);
                throw;
            }
        }

        // ====================
        // Cancelar clase
        // ====================
        public async Task CancelarAsync(int claseID, string? motivo = null, CancellationToken ct = default)
        {
            var clase = await _escuelaContext.Clases
                .FirstOrDefaultAsync(i => i.ClaseId == claseID, ct)
                      ?? throw new InvalidOperationException("Clase no encontrada.");

            if (motivo == null)
            {
                var motivoCancelacion = _dialogService.PedirTexto("Ingrese el motivo de la cancelación del pago de la clase\nOpcional");
                if (String.IsNullOrWhiteSpace(motivoCancelacion)) motivoCancelacion = "No especificado";
                motivo = motivoCancelacion;
            }
            clase.Eliminado = true;
            clase.EliminadoEn = DateTime.Now;
            clase.ActualizadoEn = DateTime.Now;

            await _escuelaContext.SaveChangesAsync(ct);
        }

        // ====================
        // Obtener clases de un alumno
        // ====================
        public async Task<Clase[]> ObtenerClasesDeAlumnoAsync(int alumnoId, CancellationToken ct = default)
        {
            var clases = await _escuelaContext.Cargos
                .Include(c => c.Clase)
                .Where(c => c.AlumnoId == alumnoId
                            && c.ClaseId != null
                            && !c.Eliminado
                            && c.Estado != EstadoCargo.Anulado)
                .Select(c => c.Clase!)
                .ToArrayAsync(ct);

            return clases;
        }

        // ====================
        // Verificación de duplicados
        // ====================
        private async Task<bool> ExisteCargoClaseAsync(int alumnoId, int claseId, DateTime fecha, CancellationToken ct = default)
            => await _escuelaContext.Cargos.AnyAsync(i =>
                   i.AlumnoId == alumnoId
                && i.ClaseId == claseId
                && i.Fecha.Date == fecha.Date
                && !i.Eliminado, ct);
    }
}
