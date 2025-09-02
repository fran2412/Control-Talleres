using ControlTalleresMVP.Persistence.DataContext;
using ControlTalleresMVP.Persistence.Models;
using ControlTalleresMVP.Services.Configuracion;
using ControlTalleresMVP.Services.Generaciones;
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
        private readonly IGeneracionService _generacionService;

        public InscripcionService(EscuelaContext escuelaContext, IConfiguracionService configuracionService, IGeneracionService generacionService)
        {
            _escuelaContext = escuelaContext;
            _configuracionService = configuracionService;
            _generacionService = generacionService;
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
            var estado = saldo == 0 ? EstadoInscripcion.Pagado
                        : abonoInicial > 0 ? EstadoInscripcion.Parcial
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
                    Eliminado = false
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
                        Metodo = "Efectivo",
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
                        MontoAplicado = abonoInicial
                    };

                    _escuelaContext.PagoAplicaciones.Add(aplicacion);
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
    }
}
