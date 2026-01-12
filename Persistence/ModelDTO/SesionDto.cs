namespace ControlTalleresMVP.Persistence.ModelDTO
{
    /// <summary>
    /// DTO para almacenar la información de la sesión actual del usuario,
    /// incluyendo la sede seleccionada.
    /// </summary>
    public class SesionDto
    {
        public int SedeId { get; set; }
        public string SedeNombre { get; set; } = string.Empty;
    }
}
