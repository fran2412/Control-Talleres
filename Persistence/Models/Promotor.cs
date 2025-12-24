namespace ControlTalleresMVP.Persistence.Models
{
    public class Promotor
    {
        public int PromotorId { get; set; }
        public string Nombre { get; set; } = null!;
        public string? Telefono { get; set; }
        public DateTime CreadoEn { get; set; }
        public DateTime ActualizadoEn { get; set; }

        public bool Eliminado { get; set; } = false;
        public DateTime? EliminadoEn { get; set; }


        // Relación inversa: un promotor puede tener muchos alumnos
        public ICollection<Alumno> Alumnos { get; set; } = new List<Alumno>();
    }
}
