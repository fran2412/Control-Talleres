namespace ControlTalleresMVP.Persistence.Models
{
    public enum EstadoInscripcion
    {
        Pendiente = 0,
        Cancelada = 1,
        Pagada = 2,
    }

    public enum EstadoCargo
    {
        Pendiente = 0,   // Tiene saldo pendiente > 0
        Pagado = 1,      // SaldoActual == 0 (totalmente cubierto)
        Anulado = 2,     // Cancelado, ya no exigible
        Ajuste = 3       // Creado por corrección manual o nota
    }

    public enum EstadoAplicacionCargo
    {
        Vigente = 0,
        Anulada = 1,
    }

    public enum TipoCargo
    {
        Inscripcion = 1,
        Clase = 2,
    } 

    public enum MetodoPago
    {
        Efectivo = 1,
        TarjetaCredito = 2,
        Otro = 3,
    }
}
