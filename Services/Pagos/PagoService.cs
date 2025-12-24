using ControlTalleresMVP.Persistence.DataContext;
using ControlTalleresMVP.Persistence.ModelDTO;
using ControlTalleresMVP.Persistence.Models;
using Microsoft.EntityFrameworkCore;

namespace ControlTalleresMVP.Services.Pagos
{
    public class PagoService : IPagoService
    {
        private readonly EscuelaContext _ctx;

        public PagoService(EscuelaContext ctx)
        {
            _ctx = ctx;
        }

        public async Task<int> GuardarPagoAsync(PagoCapturaDTO captura, CancellationToken ct = default)
        {
            if (captura is null) throw new ArgumentNullException(nameof(captura));
            if (captura.AlumnoId <= 0) throw new InvalidOperationException("Alumno inválido.");
            if (captura.MontoTotal <= 0) throw new InvalidOperationException("El monto total debe ser mayor a 0.");
            if (captura.Aplicaciones is null || captura.Aplicaciones.Length == 0)
                throw new InvalidOperationException("Debes proporcionar al menos una aplicación.");

            using var tx = await _ctx.Database.BeginTransactionAsync(ct);

            var ahora = DateTime.Now;

            // 1) Crear el pago
            var pago = new Pago
            {
                AlumnoId = captura.AlumnoId,
                Fecha = ahora,
                MontoTotal = captura.MontoTotal,
                CreadoEn = ahora,
                ActualizadoEn = ahora
            };

            _ctx.Pagos.Add(pago);
            await _ctx.SaveChangesAsync(ct); // para obtener PagoId

            // 🔹 Acumulador: inscId -> total aplicado en esta captura
            var deltaPorInscripcion = new Dictionary<int, decimal>();

            // 2) Procesar aplicaciones
            foreach (var apl in captura.Aplicaciones)
            {
                if (apl.Monto <= 0)
                    throw new InvalidOperationException("Hay aplicaciones con monto ≤ 0.");

                var cargo = await _ctx.Cargos
                    .FirstOrDefaultAsync(c => c.CargoId == apl.CargoId, ct)
                    ?? throw new InvalidOperationException($"Cargo #{apl.CargoId} no existe.");

                if (cargo.Eliminado)
                    throw new InvalidOperationException($"Cargo #{apl.CargoId} está marcado como eliminado.");

                if (cargo.AlumnoId != captura.AlumnoId)
                    throw new InvalidOperationException($"El cargo #{apl.CargoId} no pertenece al alumno #{captura.AlumnoId}.");

                if (apl.Monto > cargo.SaldoActual + 0.0001m)
                    throw new InvalidOperationException(
                        $"Monto a aplicar ({apl.Monto:C}) excede saldo del cargo #{apl.CargoId} ({cargo.SaldoActual:C}).");

                if (cargo.InscripcionId.HasValue && apl.InscripcionId.HasValue && cargo.InscripcionId != apl.InscripcionId)
                    throw new InvalidOperationException(
                        $"Inconsistencia: el cargo #{apl.CargoId} no corresponde a Inscripción #{apl.InscripcionId}.");
                if (cargo.ClaseId.HasValue && apl.ClaseId.HasValue && cargo.ClaseId != apl.ClaseId)
                    throw new InvalidOperationException(
                        $"Inconsistencia: el cargo #{apl.CargoId} no corresponde a Clase #{apl.ClaseId}.");

                // Crear la aplicación
                var app = new PagoAplicacion
                {
                    PagoId = pago.PagoId,
                    CargoId = cargo.CargoId,
                    MontoAplicado = apl.Monto,
                    Estado = EstadoAplicacionCargo.Vigente,
                };
                _ctx.PagoAplicaciones.Add(app);

                // Rebajar saldo del cargo
                cargo.SaldoActual -= apl.Monto;
                cargo.ActualizadoEn = ahora;

                // 🔹 Marcar cargo como Pagado si se liquidó
                if (cargo.SaldoActual == 0)
                    cargo.Estado = EstadoCargo.Pagado;

                // 🔹 Acumular delta por inscripción
                if (apl.InscripcionId.HasValue && !apl.ClaseId.HasValue)
                {
                    var inscId = apl.InscripcionId.Value;
                    if (!deltaPorInscripcion.TryAdd(inscId, apl.Monto))
                        deltaPorInscripcion[inscId] += apl.Monto;
                }
            }

            // 3) Aplicar deltas a inscripciones
            if (deltaPorInscripcion.Count > 0)
            {
                var inscIds = deltaPorInscripcion.Keys.ToArray();
                var inscripciones = await _ctx.Inscripciones
                    .Where(i => inscIds.Contains(i.InscripcionId))
                    .ToListAsync(ct);

                foreach (var ins in inscripciones)
                {
                    if (deltaPorInscripcion.TryGetValue(ins.InscripcionId, out var delta))
                    {
                        ins.SaldoActual = Math.Max(0, ins.SaldoActual - delta);
                        ins.ActualizadoEn = ahora;

                        if (ins.SaldoActual == 0)
                            ins.Estado = EstadoInscripcion.Pagada;
                    }
                }
            }

            await _ctx.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);

            return pago.PagoId;
        }


    }
}