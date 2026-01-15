namespace ControlTalleresMVP.Persistence.Models
{
    public class ConfiguracionSede
    {
        public int ConfiguracionSedeId { get; set; }
        public int SedeId { get; set; }
        public string Clave { get; set; } = null!;
        public string Valor { get; set; } = null!;

        public virtual Sede Sede { get; set; } = null!;
    }
}
