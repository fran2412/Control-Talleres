using ControlTalleresMVP.Persistence.DataContext;
using ControlTalleresMVP.Persistence.ModelDTO;
using ControlTalleresMVP.Persistence.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

            // 1) Crear Pago
            var ahora = DateTime.Now;

            var pago = new Pago
            {
                AlumnoId = captura.AlumnoId,
                Fecha = ahora,
                MontoTotal = captura.MontoTotal,
                CreadoEn = ahora,
                ActualizadoEn = ahora
            };

            _ctx.Pagos.Add(pago);
            await _ctx.SaveChangesAsync(ct); // para tener PagoId

            // 🔹 Acumulador: inscId -> total aplicado a inscripción (según CAPTURA)
            var deltaPorInscripcion = new Dictionary<int, decimal>();

            // 2) Validar cargos y crear aplicaciones, rebajando saldos
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

                if (apl.Monto > cargo.SaldoActual + 0.0001m) // leve tolerancia por redondeos
                    throw new InvalidOperationException($"Monto a aplicar ({apl.Monto:C}) excede saldo del cargo #{apl.CargoId} ({cargo.SaldoActual:C}).");

                // (Opcional) coherencia con Inscripcion/Clase si el cargo está asociado:
                if (cargo.InscripcionId.HasValue && apl.InscripcionId.HasValue && cargo.InscripcionId != apl.InscripcionId)
                    throw new InvalidOperationException($"Inconsistencia: el cargo #{apl.CargoId} no corresponde a Inscripción #{apl.InscripcionId}.");
                if (cargo.ClaseId.HasValue && apl.ClaseId.HasValue && cargo.ClaseId != apl.ClaseId)
                    throw new InvalidOperationException($"Inconsistencia: el cargo #{apl.CargoId} no corresponde a Clase #{apl.ClaseId}.");

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

                // 🔹 Si la CAPTURA marca INSCRIPCIÓN (apl.InscripcionId y no ClaseId), acumula para Inscripción
                if (apl.InscripcionId.HasValue && !apl.ClaseId.HasValue)
                {
                    var inscId = apl.InscripcionId.Value;
                    if (!deltaPorInscripcion.TryAdd(inscId, apl.Monto))
                        deltaPorInscripcion[inscId] += apl.Monto;
                }

                // (Opcional) si tu cargo maneja estados:
                // if (cargo.SaldoActual == 0) cargo.Estado = EstadoCargo.Saldado;
            }

            await _ctx.SaveChangesAsync(ct);

            // 3) Aplicar delta acumulado a las Inscripciones afectadas
            if (deltaPorInscripcion.Count > 0)
            {
                var inscIds = deltaPorInscripcion.Keys.ToArray();

                var inscripciones = await _ctx.Inscripciones
                    .Where(i => inscIds.Contains(i.InscripcionId))
                    .ToListAsync(ct);

                foreach (var ins in inscripciones)
                {
                    var delta = deltaPorInscripcion[ins.InscripcionId];
                    ins.SaldoActual = Math.Max(0, ins.SaldoActual - delta);

                    if (ins.SaldoActual == 0) ins.Estado = EstadoInscripcion.Finalizada;
                    ins.ActualizadoEn = ahora;
                    // (Opcional) actualizar Estado según tu enum real:
                    // if (ins.SaldoActual <= 0) ins.Estado = EstadoInscripcion.Finalizada; // o Pagada
                    // else if (ins.SaldoActual < ins.Costo) ins.Estado = EstadoInscripcion.Activa; // Parcial si lo manejas
                    // else ins.Estado = EstadoInscripcion.Activa;
                }

                await _ctx.SaveChangesAsync(ct);
            }

            await tx.CommitAsync(ct);
            return pago.PagoId;
        }

    }
}