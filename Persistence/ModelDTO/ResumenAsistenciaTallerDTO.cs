namespace ControlTalleresMVP.Persistence.ModelDTO
{
    public class ResumenAsistenciaTallerDTO
    {
        public int TallerId { get; set; }
        public string TallerNombre { get; set; } = string.Empty;
        public string DiaSemana { get; set; } = string.Empty;
        public int AlumnosInscritos { get; set; }    // Total inscritos activos
        public int AlumnosAsistentes { get; set; }   // Con clase registrada hoy
    }
}
