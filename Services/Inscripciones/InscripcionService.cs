using ControlTalleresMVP.Persistence.DataContext;
using ControlTalleresMVP.Persistence.Models;
using ControlTalleresMVP.Services.Configuracion;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace ControlTalleresMVP.Services.Inscripciones
{
    public class InscripcionService : IInscripcionService
    {
        private readonly EscuelaContext _escuelaContext;
        private readonly IConfiguracionService _configuracionService;

        public InscripcionService(EscuelaContext escuelaContext, IConfiguracionService configuracionService)
        {
            _escuelaContext = escuelaContext;
            _configuracionService = configuracionService;
        }

        public async Task<bool> ExisteActivaAsync(int alumnoId, int tallerId, int generacionId, CancellationToken ct = default)
            => await _escuelaContext.Inscripciones.AnyAsync(i => i.AlumnoId == alumnoId
                                                  && i.TallerId == tallerId
                                                  && i.GeneracionId == generacionId
                                                  && !i.Eliminado, ct);

        public async Task<Inscripcion> InscribirAsync(
            int alumnoId, int tallerId, int generacionId,
            decimal abonoInicial = 0m,
            DateTime? fecha = null, CancellationToken ct = default)
        {
            // Validar duplicado
            if (await ExisteActivaAsync(alumnoId, tallerId, generacionId, ct))
                throw new InvalidOperationException("Ya existe una inscripción activa para este alumno en el mismo taller y generación.");

            // (Opcional) validar cupo
            var taller = await _escuelaContext.Talleres.FirstOrDefaultAsync(t => t.TallerId == tallerId, ct)
                         ?? throw new InvalidOperationException("Taller no encontrado.");

            var generacion = await _escuelaContext.Generaciones.FirstOrDefaultAsync(g => g.GeneracionId == generacionId, ct)
                             ?? throw new InvalidOperationException("Generación no encontrada.");

            var costo = _configuracionService.GetValor<int>("costo_inscripcion", 600);

            if (abonoInicial < 0 || abonoInicial > costo) throw new InvalidOperationException("Abono inicial inválido.");

            var saldo = costo - abonoInicial;
            var estado = saldo == 0 ? EstadoInscripcion.Pagado
                        : abonoInicial > 0 ? EstadoInscripcion.Parcial
                        : EstadoInscripcion.Pendiente;

            var now = DateTime.Now;

            using var transaccion = await _escuelaContext.Database.BeginTransactionAsync(ct);
            try
            {
                var inscripcion = new Inscripcion
                {
                    AlumnoId = alumnoId,
                    TallerId = tallerId,
                    GeneracionId = generacionId,
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

                // (Opcional) Crear Pago inicial si corresponde
                if (abonoInicial > 0m)
                {
                    var pago = new Pago
                    {
                        InscripcionId = inscripcion.InscripcionId,
                        Fecha = now,
                        Monto = abonoInicial,
                        Concepto = "Abono inicial",
                        CreadoEn = now
                    };
                    _escuelaContext.Pago.Add(pago);
                    await _escuelaContext.SaveChangesAsync(ct);
                }

                await transaccion.CommitAsync(ct);
                return inscripcion;
            }
            catch
            {
                await transaccion.RollbackAsync(ct);
                throw;
            }
        }

        public async Task RecalcularEstadoAsync(int inscripcionId, CancellationToken ct = default)
        {
            var ins = await _escuelaContext.Inscripciones.FirstOrDefaultAsync(i => i.InscripcionId == inscripcionId, ct)
                      ?? throw new InvalidOperationException("Inscripción no encontrada.");

            if (ins.Eliminado) { ins.Estado = EstadoInscripcion.Cancelado; }
            else
            {
                var totalPagos = await _escuelaContext.Pagos
                    .Where(p => p.InscripcionId == inscripcionId)
                    .SumAsync(p => (decimal?)p.Monto, ct) ?? 0m;

                ins.SaldoActual = Math.Max(0m, ins.Costo - totalPagos);
                ins.Estado = ins.SaldoActual == 0 ? EstadoInscripcion.Pagado
                           : totalPagos > 0 ? EstadoInscripcion.Parcial
                           : EstadoInscripcion.Pendiente;
                ins.ActualizadoEn = DateTime.Now;
            }

            await _escuelaContext.SaveChangesAsync(ct);
        }

        public async Task CancelarAsync(int inscripcionId, string? motivo = null, CancellationToken ct = default)
        {
            var ins = await _escuelaContext.Inscripciones.FirstOrDefaultAsync(i => i.InscripcionId == inscripcionId, ct)
                      ?? throw new InvalidOperationException("Inscripción no encontrada.");

            ins.Eliminado = true;
            ins.EliminadoEn = DateTime.Now;
            ins.Estado = EstadoInscripcion.Cancelado;
            ins.ActualizadoEn = DateTime.Now;

            // (Opcional) guardar motivo en una tabla de bitácora
            await _escuelaContext.SaveChangesAsync(ct);
        }
    }

}
