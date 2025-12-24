namespace ControlTalleresMVP.Persistence.Models
{
    public class Configuracion
    {
        public string Clave { get; set; } = null!;
        public string Valor { get; set; } = null!;
        public string? Descripcion { get; set; }
    }
}