namespace ControlTalleresMVP.Persistence.Models
{
    public class Configuracion
    {
        public string Clave { get; set; } = null!;
        public string Valor { get; set; } = null!;
        public string? Descripcion { get; set; }

        public int SedeId { get; set; }
        public virtual Sede Sede { get; set; } = null!;
    }
}