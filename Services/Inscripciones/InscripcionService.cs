using ControlTalleresMVP.Helpers.Dialogs;
using ControlTalleresMVP.Persistence.DataContext;
using ControlTalleresMVP.Persistence.ModelDTO;
using ControlTalleresMVP.Persistence.Models;
using ControlTalleresMVP.Services.Configuracion;
using ControlTalleresMVP.Services.Generaciones;
using Microsoft.EntityFrameworkCore;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Messaging;
using ControlTalleresMVP.Messages;

namespace ControlTalleresMVP.Services.Inscripciones
{
    public class InscripcionService : IInscripcionService
    {
        private readonly EscuelaContext _escuelaContext;
        private readonly IConfiguracionService _configuracionService;
        private readonly IGeneracionService _generacionService;
        private readonly IDialogService _dialogService;

        public ObservableCollection<InscripcionDTO> RegistrosInscripciones { get; set; } = new();
        public ObservableCollection<InscripcionRegistroDTO> RegistrosInscripcionesCompletos { get; set; } = new();

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

            // Validar cupo
            var taller = await _escuelaContext.Talleres.FirstOrDefaultAsync(t => t.TallerId == tallerId, ct)
                         ?? throw new InvalidOperationException("Taller no encontrado.");

            var generacion = await _escuelaContext.Generaciones.FirstOrDefaultAsync(g => g.GeneracionId == generacionId.GeneracionId, ct)
                             ?? throw new InvalidOperationException("Generación no encontrada.");

            // Validar que la fecha de inscripción esté dentro del rango de la generación
            var fechaInscripcion = fecha ?? DateTime.Now;
            if (fechaInscripcion < generacion.FechaInicio)
                throw new InvalidOperationException($"La fecha de inscripción no puede ser anterior al inicio de la generación ({generacion.FechaInicio:dd/MM/yyyy}).");
            
            if (generacion.FechaFin.HasValue && fechaInscripcion > generacion.FechaFin.Value)
                throw new InvalidOperationException($"La fecha de inscripción no puede ser posterior al fin de la generación ({generacion.FechaFin.Value:dd/MM/yyyy}).");

            var costo = _configuracionService.GetValor<int>("costo_inscripcion", 600);

            if (abonoInicial < 0 || abonoInicial > costo) throw new InvalidOperationException("Abono inicial inválido.");

            var saldo = costo - abonoInicial;
            var estado = saldo == 0 ? EstadoInscripcion.Pagada
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
                    SaldoActual = costo,
                    Fecha = now,
                    CreadoEn = now,
                    ActualizadoEn = now,
                    Eliminado = false,
                    Estado = EstadoCargo.Pendiente
                };

                _escuelaContext.Cargos.Add(cargo);
                await _escuelaContext.SaveChangesAsync(ct);

                // 3. Si hay abono inicial, crear el pago y su aplicación
                if (abonoInicial > 0m)
                {
                    // Obtener información del alumno y taller para la descripción
                    var alumno = await _escuelaContext.Alumnos
                        .Where(a => a.AlumnoId == alumnoId)
                        .Select(a => a.Nombre)
                        .FirstOrDefaultAsync(ct);
                    
                    var descripcion = $"Pago de inscripción - {alumno ?? "Alumno"} en {taller.Nombre} - Abono: ${abonoInicial:F2} de ${costo:F2} (Deuda pendiente: ${saldo:F2})";
                    
                    var pago = new Pago
                    {
                        AlumnoId = alumnoId,
                        Fecha = now,
                        MontoTotal = abonoInicial,
                        Metodo = MetodoPago.Efectivo,
                        Notas = descripcion,
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

                    cargo.SaldoActual -= abonoInicial;
                    cargo.ActualizadoEn = now;

                    // 🔹 Si se liquidó el cargo, marcar Pagado
                    if (cargo.SaldoActual == 0)
                        cargo.Estado = EstadoCargo.Pagado;

                    await _escuelaContext.SaveChangesAsync(ct);
                }

                await transaccion.CommitAsync(ct);
                await InicializarRegistros(ct);
                
                // Enviar mensaje de actualización para notificar a otros componentes
                WeakReferenceMessenger.Default.Send(new InscripcionesActualizadasMessage(alumnoId));
                
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
                .Include(i => i.Cargos.Where(c => !c.Eliminado))
                .FirstOrDefaultAsync(i => i.InscripcionId == inscripcionId, ct)
                      ?? throw new InvalidOperationException("Inscripción no encontrada.");

            if (motivo == null)
            {
                var motivoCancelacion = _dialogService.PedirTexto("Ingrese el motivo de la cancelación de la inscripción\nOpcional");
                if (String.IsNullOrWhiteSpace(motivoCancelacion)) motivoCancelacion = "No especificado";
                 motivo = motivoCancelacion;
            }

            // Cancelar la inscripción (mantener SaldoActual original)
            inscripcion.MotivoCancelacion = motivo;
            inscripcion.CanceladaEn = DateTime.Now;
            inscripcion.Eliminado = true;
            inscripcion.EliminadoEn = DateTime.Now;
            inscripcion.Estado = EstadoInscripcion.Cancelada;
            inscripcion.ActualizadoEn = DateTime.Now;

            // Cancelar todos los cargos relacionados a la inscripción
            var cargosRelacionados = inscripcion.Cargos.Where(c => !c.Eliminado).ToList();
            var clasesACancelar = new List<int>();

            foreach (var cargo in cargosRelacionados)
            {
                // Marcar el cargo como anulado
                cargo.Estado = EstadoCargo.Anulado;
                cargo.SaldoActual = 0;
                cargo.Eliminado = true;
                cargo.EliminadoEn = DateTime.Now;
                cargo.ActualizadoEn = DateTime.Now;

                // Si el cargo está relacionado con una clase, agregar la clase a la lista para cancelar
                if (cargo.ClaseId.HasValue && !clasesACancelar.Contains(cargo.ClaseId.Value))
                {
                    clasesACancelar.Add(cargo.ClaseId.Value);
                }
            }

            // Cancelar todas las clases relacionadas
            if (clasesACancelar.Any())
            {
                var clases = await _escuelaContext.Clases
                    .Where(c => clasesACancelar.Contains(c.ClaseId) && !c.Eliminado)
                    .ToListAsync(ct);

                foreach (var clase in clases)
                {
                    clase.Estado = EstadoClase.Cancelada;
                    clase.Eliminado = true;
                    clase.EliminadoEn = DateTime.Now;
                    clase.ActualizadoEn = DateTime.Now;
                }
            }

            await _escuelaContext.SaveChangesAsync(ct);

            // Enviar mensaje de actualización para notificar a otros componentes
            WeakReferenceMessenger.Default.Send(new InscripcionesActualizadasMessage(inscripcion.AlumnoId));
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
                .Where(i => !i.Eliminado && i.GeneracionId == genActualId && i.Estado != EstadoInscripcion.Cancelada)
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
                .OrderByDescending(i => i.CreadoEn)
                .ToListAsync(ct);

            return datos;
        }

        public async Task<Inscripcion[]> ObtenerInscripcionesAlumnoAsync(int alumnoId, CancellationToken ct = default)
        {
            return await _escuelaContext.Inscripciones
                .Include(i => i.Taller) // 🔹 Importante para que ya venga cargado
                .Where(i => i.AlumnoId == alumnoId
                         && !i.Eliminado
                         && i.Estado != EstadoInscripcion.Cancelada)
                .ToArrayAsync(ct);
        }

        public async Task<List<InscripcionRegistroDTO>> ObtenerInscripcionesCompletasAsync(
            int? generacionId = null,
            int? tallerId = null,
            int? alumnoId = null,
            DateTime? desde = null,
            DateTime? hasta = null,
            string? filtro = null,
            bool incluirTalleresEliminados = false,
            CancellationToken ct = default)
        {
            var genActualId = generacionId ?? _generacionService.ObtenerGeneracionActual()?.GeneracionId;
            if (genActualId == null) 
            {
                System.Diagnostics.Debug.WriteLine("No hay generación actual");
                return new List<InscripcionRegistroDTO>();
            }
            
            System.Diagnostics.Debug.WriteLine($"Buscando inscripciones para generación {genActualId}");

            // Query base con joins necesarios
            var q = from i in _escuelaContext.Inscripciones.AsNoTracking()
                    where !i.Eliminado
                       && i.GeneracionId == genActualId
                       && (!tallerId.HasValue || i.TallerId == tallerId.Value)
                       && (!alumnoId.HasValue || i.AlumnoId == alumnoId.Value)
                       && (!desde.HasValue || i.Fecha >= desde.Value.Date)
                       && (!hasta.HasValue || i.Fecha <= hasta.Value.Date)
                    join ta in _escuelaContext.Talleres.AsNoTracking() on i.TallerId equals ta.TallerId
                    join al in _escuelaContext.Alumnos.AsNoTracking() on i.AlumnoId equals al.AlumnoId
                    join ge in _escuelaContext.Generaciones.AsNoTracking() on i.GeneracionId equals ge.GeneracionId
                    join ca in _escuelaContext.Cargos.AsNoTracking() on i.InscripcionId equals ca.InscripcionId into cargos
                    from ca in cargos.DefaultIfEmpty()
                    join pa in _escuelaContext.PagoAplicaciones.AsNoTracking()
                            .Include(p => p.Pago) // para fecha y método
                        on ca.CargoId equals pa.CargoId into pagos
                    from pa in pagos.DefaultIfEmpty()
                    select new { i, ta, al, ge, ca, pa };

            // Aplicar filtro de texto si se proporciona
            if (!string.IsNullOrWhiteSpace(filtro))
            {
                var filtroLower = filtro.ToLower();
                q = q.Where(x => x.al.Nombre.ToLower().Contains(filtroLower) ||
                                 x.ta.Nombre.ToLower().Contains(filtroLower) ||
                                 x.i.Estado.ToString().ToLower().Contains(filtroLower));
            }

            // Aplicar filtro de talleres eliminados
            if (!incluirTalleresEliminados)
            {
                q = q.Where(x => !x.ta.Eliminado);
            }

            // Agrupamos por inscripción (una fila por inscripción)
            var lista = await (
                from x in q
                group x by new
                {
                    x.i.InscripcionId,
                    x.i.TallerId,
                    x.i.AlumnoId,
                    x.i.GeneracionId,
                    x.i.Fecha,
                    x.i.Costo,
                    x.i.SaldoActual,
                    x.i.Estado,
                    x.i.MotivoCancelacion,
                    x.i.CanceladaEn,
                    TallerNombre = x.ta.Nombre,
                    TallerEliminado = x.ta.Eliminado,
                    AlumnoNombre = x.al.Nombre,
                    GeneracionNombre = x.ge.Nombre
                }
                into g
                select new InscripcionRegistroDTO
                {
                    InscripcionId = g.Key.InscripcionId,
                    TallerId = g.Key.TallerId,
                    AlumnoId = g.Key.AlumnoId,
                    GeneracionId = g.Key.GeneracionId,
                    FechaInscripcion = g.Key.Fecha,
                    TallerNombre = g.Key.TallerNombre,
                    TallerEliminado = g.Key.TallerEliminado,
                    AlumnoNombre = g.Key.AlumnoNombre,
                    GeneracionNombre = g.Key.GeneracionNombre,
                    Monto = g.Key.Costo,
                    SaldoActual = g.Key.SaldoActual,
                    MontoPagado = (decimal)((g.Where(z => z.pa != null)
                                            .Select(z => (double?)z.pa.MontoAplicado)
                                            .Sum()) ?? 0.0), // <- SQLite REAL
                    PorcentajePagado = g.Key.Costo > 0
                        ? (int)Math.Round((double)((g.Key.Costo - g.Key.SaldoActual) / g.Key.Costo * 100m))
                        : 0,
                    EstadoInscripcion = g.Key.Estado,
                    EstadoTexto = g.Key.Estado == EstadoInscripcion.Pagada ? "Pagada" :
                                 g.Key.Estado == EstadoInscripcion.Pendiente ? "Pendiente" : "Cancelada",
                    UltimoPagoFecha = g.Where(z => z.pa != null)
                                      .Max(z => (DateTime?)z.pa.Pago.Fecha),
                    MotivoCancelacion = g.Key.MotivoCancelacion,
                    CanceladaEn = g.Key.CanceladaEn
                })
                .OrderByDescending(r => r.FechaInscripcion)
                .ThenBy(r => r.TallerNombre)
                .ThenBy(r => r.AlumnoNombre)
                .ToListAsync(ct);

            // Redondeamos importes por consistencia visual
            foreach (var r in lista)
            {
                r.Monto = Math.Round(r.Monto, 2, MidpointRounding.AwayFromZero);
                r.MontoPagado = Math.Round(r.MontoPagado, 2, MidpointRounding.AwayFromZero);
                r.SaldoActual = Math.Round(r.SaldoActual, 2, MidpointRounding.AwayFromZero);
            }

            System.Diagnostics.Debug.WriteLine($"Encontrados {lista.Count} registros de inscripciones");
            return lista;
        }

        public async Task InicializarRegistrosCompletos(CancellationToken ct = default)
        {
            var inscripciones = await ObtenerInscripcionesCompletasAsync(ct: ct);

            RegistrosInscripcionesCompletos.Clear();

            foreach (var inscripcion in inscripciones)
            {
                RegistrosInscripcionesCompletos.Add(inscripcion);
            }
        }

        public async Task<decimal> ObtenerSaldoPendienteAsync(int alumnoId, int tallerId, CancellationToken ct = default)
        {
            var generacion = _generacionService.ObtenerGeneracionActual();
            if (generacion == null) return 0m;

            var inscripcion = await _escuelaContext.Inscripciones
                .AsNoTracking()
                .Where(i => i.AlumnoId == alumnoId 
                         && i.TallerId == tallerId 
                         && i.GeneracionId == generacion.GeneracionId
                         && !i.Eliminado)
                .Select(i => i.SaldoActual)
                .FirstOrDefaultAsync(ct);

            return inscripcion;
        }
    }
}
