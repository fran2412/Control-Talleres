using ControlTalleresMVP.Persistence.DataContext;
using ControlTalleresMVP.Persistence.ModelDTO;
using ControlTalleresMVP.Persistence.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace ControlTalleresMVP.Services.Talleres
{
    internal class TallerService : ITallerService
    {
        public ObservableCollection<TallerDTO> RegistrosTalleres { get; set; } = new();

        private readonly EscuelaContext _context;
        public TallerService(EscuelaContext context)
        {
            _context = context;
        }

        public async Task<Taller> GuardarAsync(Taller taller, CancellationToken ct = default)
        {
            // Validar que el horario de fin sea posterior al de inicio
            if (taller.HorarioFin <= taller.HorarioInicio)
            {
                throw new InvalidOperationException("El horario de fin debe ser posterior al horario de inicio");
            }
            
            _context.Talleres.Add(taller);
            await _context.SaveChangesAsync(ct);

            var dto = await ConstruirDtoAsync(taller, ct);
            InsertarOrdenado(dto);
            return taller;
        }

        public async Task EliminarAsync(int tallerId, CancellationToken ct = default)
        {
            var now = DateTime.Now;
            using var transaccion = await _context.Database.BeginTransactionAsync(ct);

            var taller = await _context.Talleres.FirstOrDefaultAsync(t => t.TallerId == tallerId, ct)
                         ?? throw new InvalidOperationException("Taller no encontrado.");

            // 1) Soft-delete del taller
            taller.Eliminado = true;
            taller.EliminadoEn = now;

            // 2) Inscripciones del taller (no eliminadas)
            var inscripciones = await _context.Inscripciones
                .Where(i => i.TallerId == tallerId && !i.Eliminado && i.Estado != EstadoInscripcion.Cancelada)
                .ToListAsync(ct);

            var inscripcionesIds = inscripciones.Select(i => i.InscripcionId).ToArray();

            // 3) Cargos ligados a esas inscripciones y vigentes
            var cargosInscripcion = await _context.Cargos
                .Where(c => c.InscripcionId != null
                            && inscripcionesIds.Contains(c.InscripcionId!.Value)
                            && !c.Eliminado
                            && c.Estado != EstadoCargo.Anulado)
                .ToListAsync(ct);


            // 4) Anular cargos y perdonar saldo
            foreach (var c in cargosInscripcion)
            {
                c.Estado = EstadoCargo.Anulado;
                c.SaldoActual = 0m;
                c.ActualizadoEn = now;
            }

            // 5) Cancelar inscripciones (mantener SaldoActual original)
            foreach (var i in inscripciones)
            {
                i.Estado = EstadoInscripcion.Cancelada;
                i.CanceladaEn = now;
                i.MotivoCancelacion = "Taller eliminado";
                // Mantener SaldoActual original - no se perdona la deuda en la inscripción
                i.ActualizadoEn = now;
                // i.Eliminado = true;
                // i.EliminadoEn = now;
            }

            // NOTA: PagoAplicacion se mantiene en Estado = Vigente (no reembolsos)

            await _context.SaveChangesAsync(ct);
            await transaccion.CommitAsync(ct);

            var registro = RegistrosTalleres.FirstOrDefault(r => r.Id == tallerId);
            if (registro != null)
            {
                if (RegistrosTalleres.Any(r => r.Eliminado))
                {
                    registro.Eliminado = true;
                    registro.EliminadoEn = now;
                }
                else
                {
                    RegistrosTalleres.Remove(registro);
                }
            }
        }

        public async Task ActualizarAsync(Taller taller, CancellationToken ct = default)
        {
            if (taller.TallerId <= 0)
                throw new ArgumentException("El ID del taller debe ser válida");

            var tallerExistente = await _context.Talleres
                .FirstOrDefaultAsync(s => s.TallerId == taller.TallerId, ct);

            if (tallerExistente is null)
                throw new InvalidOperationException($"No se encontró al taller con ID {taller.TallerId}");

            // Validar que el horario de fin sea posterior al de inicio
            if (taller.HorarioFin <= taller.HorarioInicio)
            {
                throw new InvalidOperationException("El horario de fin debe ser posterior al horario de inicio");
            }
            
            // Solo actualizas campos que quieres
            tallerExistente.Nombre = taller.Nombre;
            tallerExistente.HorarioInicio = taller.HorarioInicio;
            tallerExistente.HorarioFin = taller.HorarioFin;
            tallerExistente.DiaSemana = taller.DiaSemana;
            tallerExistente.FechaInicio = taller.FechaInicio;
            tallerExistente.FechaFin = taller.FechaFin;
            tallerExistente.SedeId = taller.SedeId;
            tallerExistente.ActualizadoEn = DateTime.Now;

            try
            {
                await _context.SaveChangesAsync(ct);

                await _context.Entry(tallerExistente).ReloadAsync(ct);
                await _context.Entry(tallerExistente).Reference(t => t.Sede).LoadAsync(ct);

                var registro = RegistrosTalleres.FirstOrDefault(r => r.Id == tallerExistente.TallerId);
                if (registro != null)
                {
                    ActualizarDto(registro, tallerExistente);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al actualizar el taller: " + ex.Message,
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public async Task<List<TallerDTO>> ObtenerTalleresParaGridAsync(CancellationToken ct = default)
        {
            return await ObtenerTalleresParaGridAsync(false, ct);
        }

        public async Task<List<TallerDTO>> ObtenerTalleresParaGridAsync(bool incluirEliminados, CancellationToken ct = default)
        {
            var query = _context.Talleres.AsNoTracking();
            
            if (!incluirEliminados)
            {
                query = query.Where(a => !a.Eliminado);
            }

            var datos = await query
                .Include(t => t.Sede)
                .Select(u => new
                {
                    u.TallerId,
                    u.HorarioInicio,
                    u.HorarioFin,
                    u.DiaSemana,
                    u.Nombre,
                    u.FechaInicio,
                    u.FechaFin,
                    u.SedeId,
                    SedeNombre = u.Sede.Nombre,
                    u.CreadoEn,
                    u.Eliminado,
                    u.EliminadoEn
                })
                .OrderBy(t => t.Eliminado) // Talleres activos primero
                .ThenBy(t => t.Nombre)     // Luego ordenados por nombre
                .ToListAsync(ct);

            return datos.Select(u => new TallerDTO
            {
                Id = u.TallerId,
                Nombre = u.Nombre,
                HorarioInicio = u.HorarioInicio,
                HorarioFin = u.HorarioFin,
                DiaSemana = ConvertirDiaSemanaASpanol(u.DiaSemana),
                FechaInicio = u.FechaInicio,
                FechaFin = u.FechaFin,
                SedeId = u.SedeId,
                NombreSede = u.SedeNombre,
                CreadoEn = u.CreadoEn,
                Eliminado = u.Eliminado,
                EliminadoEn = u.EliminadoEn
            }).ToList();
        }


        public async Task InicializarRegistros(CancellationToken ct = default)
        {
            var talleres = await ObtenerTalleresParaGridAsync(ct);

            // Ordenar por fecha de creación descendente (más recientes primero)
            var talleresOrdenados = talleres.OrderByDescending(t => t.CreadoEn).ToList();

            RegistrosTalleres.Clear();

            foreach (var taller in talleresOrdenados)
            {
                RegistrosTalleres.Add(taller);
            }
        }

        public List<Taller> ObtenerTodos(CancellationToken ct = default)
        {
            return _context.Talleres
                .AsNoTracking()
                .Where(p => !p.Eliminado)
                .ToList();
        }

        private static string ConvertirDiaSemanaASpanol(DayOfWeek diaSemana)
        {
            return diaSemana switch
            {
                DayOfWeek.Monday => "Lunes",
                DayOfWeek.Tuesday => "Martes",
                DayOfWeek.Wednesday => "Miércoles",
                DayOfWeek.Thursday => "Jueves",
                DayOfWeek.Friday => "Viernes",
                DayOfWeek.Saturday => "Sábado",
                DayOfWeek.Sunday => "Domingo",
                _ => "Lunes"
            };
        }

        private async Task<TallerDTO> ConstruirDtoAsync(Taller taller, CancellationToken ct)
        {
            await _context.Entry(taller).ReloadAsync(ct);
            await _context.Entry(taller).Reference(t => t.Sede).LoadAsync(ct);

            return CrearDtoDesdeTaller(taller);
        }

        private static TallerDTO CrearDtoDesdeTaller(Taller taller)
        {
            return new TallerDTO
            {
                Id = taller.TallerId,
                Nombre = taller.Nombre,
                HorarioInicio = taller.HorarioInicio,
                HorarioFin = taller.HorarioFin,
                DiaSemana = ConvertirDiaSemanaASpanol(taller.DiaSemana),
                FechaInicio = taller.FechaInicio,
                FechaFin = taller.FechaFin,
                SedeId = taller.SedeId,
                NombreSede = taller.Sede?.Nombre ?? string.Empty,
                CreadoEn = taller.CreadoEn,
                Eliminado = taller.Eliminado,
                EliminadoEn = taller.EliminadoEn
            };
        }

        private static void ActualizarDto(TallerDTO destino, Taller origen)
        {
            destino.Nombre = origen.Nombre;
            destino.HorarioInicio = origen.HorarioInicio;
            destino.HorarioFin = origen.HorarioFin;
            destino.DiaSemana = ConvertirDiaSemanaASpanol(origen.DiaSemana);
            destino.FechaInicio = origen.FechaInicio;
            destino.FechaFin = origen.FechaFin;
            destino.SedeId = origen.SedeId;
            destino.NombreSede = origen.Sede?.Nombre ?? string.Empty;
            destino.Eliminado = origen.Eliminado;
            destino.EliminadoEn = origen.EliminadoEn;
        }

        private void InsertarOrdenado(TallerDTO nuevo)
        {
            var indice = 0;
            while (indice < RegistrosTalleres.Count && RegistrosTalleres[indice].CreadoEn >= nuevo.CreadoEn)
            {
                indice++;
            }

            RegistrosTalleres.Insert(indice, nuevo);
        }

    }
}
