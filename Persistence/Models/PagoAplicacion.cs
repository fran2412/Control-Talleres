namespace ControlTalleresMVP.Persistence.Models
{
    public class PagoAplicacion
    {
        public int PagoAplicacionId { get; set; }

        public decimal MontoAplicado { get; set; }

        public int PagoId { get; set; }
        public Pago Pago { get; set; } = null!;

        public Cargo Cargo { get; set; } = null!;
        public int CargoId { get; set; }

    }
}
