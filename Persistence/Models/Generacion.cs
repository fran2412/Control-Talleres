namespace ControlTalleresMVP.Persistence.Models
{
    public class Generacion
    {
        public int GeneracionId { get; set; }
        public string Nombre { get; set; } = null!;

        public int SedeId { get; set; }
        public virtual Sede Sede { get; set; } = null!;

        public DateTime FechaInicio { get; set; }
        public DateTime? FechaFin { get; set; }

        public DateTime CreadoEn { get; set; }
        public DateTime ActualizadoEn { get; set; }
        public bool Eliminado { get; set; } = false;
        public DateTime? EliminadoEn { get; set; }

        public ICollection<Inscripcion> Inscripciones { get; set; } = new List<Inscripcion>();
    }
}
