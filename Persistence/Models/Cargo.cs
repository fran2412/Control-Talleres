namespace ControlTalleresMVP.Persistence.Models
{
    public class Cargo
    {
        public int CargoId { get; set; }

        public decimal Monto { get; set; }
        public decimal SaldoActual { get; set; }

        public TipoCargo Tipo { get; set; }
        public DateTime Fecha { get; set; } = DateTime.Now;
        public EstadoCargo Estado { get; set; } = EstadoCargo.Pendiente;

        public DateTime CreadoEn { get; set; } = DateTime.Now;
        public DateTime ActualizadoEn { get; set; } = DateTime.Now;
        public bool Eliminado { get; set; }
        public DateTime? EliminadoEn { get; set; }

        public int AlumnoId { get; set; }
        public Alumno Alumno { get; set; } = null!;

        public int? InscripcionId { get; set; }
        public Inscripcion? Inscripcion { get; set; }

        public int? ClaseId { get; set; }
        //Pendiente clase id

        public ICollection<PagoAplicacion> Aplicaciones { get; set; } = new List<PagoAplicacion>();
    }
}