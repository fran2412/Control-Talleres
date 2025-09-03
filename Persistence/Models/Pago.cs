namespace ControlTalleresMVP.Persistence.Models
{
    public class Pago
    {
        public int PagoId { get; set; }
        public DateTime Fecha { get; set; } = DateTime.Now; 
        
        public decimal MontoTotal { get; set; }
        public MetodoPago Metodo { get; set; } = MetodoPago.Efectivo;
        public string? Referencia { get; set; }
        public string? Notas { get; set; }

        // Auditoría
        public DateTime CreadoEn { get; set; } = DateTime.Now;
        public DateTime ActualizadoEn { get; set; } = DateTime.Now;
        public bool Eliminado { get; set; }
        public DateTime? EliminadoEn { get; set; }

        public int AlumnoId { get; set; }
        public Alumno Alumno { get; set; } = null!;

        public ICollection<PagoAplicacion> Aplicaciones { get; set; } = new List<PagoAplicacion>();
    }
}
