using ControlTalleresMVP.Helpers.Dialogs;
using ControlTalleresMVP.Migrations;
using ControlTalleresMVP.Persistence.DataContext;
using ControlTalleresMVP.Persistence.ModelDTO;
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
        public async Task<RegistrarClaseResult> RegistrarClaseAsync(
            int alumnoId,
            int tallerId,
            DateTime fecha,
            decimal montoAbono,
            CancellationToken ct = default)
        {
            var day = fecha.Date;
            var costoClase = Math.Max(1, _configuracionService.GetValor<int>("costo_clase", 150));

            bool claseCreada = false, cargoCreado = false, pagoCreado = false;
            decimal aplicado = 0m;
            bool yaPagado = false;

            // 1) Buscar o crear CLASE
            var clase = await _escuelaContext.Clases
                .FirstOrDefaultAsync(c => c.TallerId == tallerId && !c.Eliminado && c.Fecha == day, ct);

            if (clase is null)
            {
                clase = new Clase { TallerId = tallerId, Fecha = day };
                _escuelaContext.Clases.Add(clase);
                await _escuelaContext.SaveChangesAsync(ct);
                claseCreada = true;
            }

            // 2) Buscar o crear CARGO (único por alumno+clase)
            var cargo = await _escuelaContext.Cargos
                .FirstOrDefaultAsync(c => c.AlumnoId == alumnoId
                                       && c.ClaseId == clase.ClaseId
                                       && !c.Eliminado
                                       && c.Estado != EstadoCargo.Anulado, ct);

            if (cargo is null)
            {
                cargo = new Cargo
                {
                    AlumnoId = alumnoId,
                    ClaseId = clase.ClaseId,
                    Tipo = TipoCargo.Clase,
                    Fecha = day,
                    Monto = costoClase,
                    SaldoActual = costoClase,
                    Estado = EstadoCargo.Pendiente,
                    Eliminado = false
                };
                _escuelaContext.Cargos.Add(cargo);
                await _escuelaContext.SaveChangesAsync(ct);
                cargoCreado = true;
            }

            // 3) Aplicar ABONO (si hay)
            montoAbono = Math.Round(montoAbono, 2, MidpointRounding.AwayFromZero);
            if (montoAbono > 0m)
            {
                if (cargo.SaldoActual <= 0m)
                {
                    yaPagado = true; // no hacer nada más
                }
                else
                {
                    var aAplicar = Math.Min(montoAbono, cargo.SaldoActual);
                    if (aAplicar > 0m)
                    {
                        var pago = new Pago
                        {
                            AlumnoId = alumnoId,
                            Fecha = DateTime.Now,
                            Metodo = MetodoPago.Efectivo, // ajusta según tu UI
                            MontoTotal = aAplicar
                        };
                        _escuelaContext.Pagos.Add(pago);
                        await _escuelaContext.SaveChangesAsync(ct);

                        var pa = new PagoAplicacion
                        {
                            PagoId = pago.PagoId,
                            CargoId = cargo.CargoId,
                            MontoAplicado = aAplicar,
                            Estado = EstadoAplicacionCargo.Vigente
                        };
                        _escuelaContext.PagoAplicaciones.Add(pa);

                        cargo.SaldoActual -= aAplicar;
                        cargo.Estado = cargo.SaldoActual == 0m
                            ? EstadoCargo.Pagado
                            : (cargo.SaldoActual < cargo.Monto ? EstadoCargo.Pendiente : EstadoCargo.Pendiente);

                        await _escuelaContext.SaveChangesAsync(ct);

                        pagoCreado = true;
                        aplicado = aAplicar;
                    }
                }
            }

            return new RegistrarClaseResult(
                Fecha: day,
                ClaseCreada: claseCreada,
                CargoCreado: cargoCreado,
                PagoCreado: pagoCreado,
                MontoAplicado: aplicado,
                CargoYaPagado: yaPagado
            );
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

        public async Task<ClasePagoEstadoDTO[]> ObtenerEstadoPagoHoyAsync(
            int alumnoId,
            int[] tallerIds,
            DateTime fecha,
            CancellationToken ct = default)
        {
            var ids = (tallerIds ?? Array.Empty<int>()).Distinct().ToArray();
            if (alumnoId <= 0 || ids.Length == 0) return Array.Empty<ClasePagoEstadoDTO>();

            var day = fecha.Date;

            var resumen = await (
                from clases in _escuelaContext.Clases.AsNoTracking()
                where clases.Fecha == day && ids.Contains(clases.TallerId)
                join cargos in _escuelaContext.Cargos.AsNoTracking()
                    on clases.ClaseId equals cargos.ClaseId into caGroup
                from cargos in caGroup
                    .Where(c => c.AlumnoId == alumnoId
                                && !c.Eliminado
                                && c.Estado != EstadoCargo.Anulado)
                    .DefaultIfEmpty()
                group cargos by clases.TallerId into g
                select new
                {
                    TallerId = g.Key,
                    TieneCargo = g.Where(x => x != null).Any(),

                    Saldo = (decimal)((g.Where(x => x != null)
                                        .Select(x => (double?)x!.SaldoActual)
                                        .Sum()) ?? 0.0)
                })
                .ToListAsync(ct);

            // Armar mapa por taller
            var dict = resumen.ToDictionary(
                x => x.TallerId,
                x => new ClasePagoEstadoDTO(
                        x.TallerId,
                        x.TieneCargo,
                        x.TieneCargo && x.Saldo == 0m,
                        !x.TieneCargo || x.Saldo > 0m));

            // Si no existe aún la clase de hoy para algún taller, se puede pagar
            foreach (var id in ids)
            {
                if (!dict.ContainsKey(id))
                {
                    dict[id] = new ClasePagoEstadoDTO(
                        id,
                        TieneCargo: false,
                        EstaPagada: false,
                        PuedePagar: true);
                }
            }

            return dict.Values.OrderBy(v => v.TallerId).ToArray();
        }
    }
}
