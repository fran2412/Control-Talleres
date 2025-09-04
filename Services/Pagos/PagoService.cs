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

            // SIN saldo a favor: exige que ΣAplicaciones == MontoTotal
            var totalAplicado = captura.Aplicaciones.Sum(a => a.Monto);
            if (totalAplicado != captura.MontoTotal)
                throw new InvalidOperationException("El total aplicado debe ser exactamente igual al monto del pago.");

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
                    Estado = EstadoAplicacionCargo.Vigente,  // usa tu enum real
                };
                _ctx.PagoAplicaciones.Add(app);

                // Rebajar saldo del cargo
                cargo.SaldoActual -= apl.Monto;
                cargo.ActualizadoEn = ahora;

                // (Opcional) si tu cargo maneja estados:
                // if (cargo.SaldoActual == 0) cargo.Estado = EstadoCargo.Saldado;
            }

            await _ctx.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);

            return pago.PagoId;
        }
    }
}