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
        Vigente = 0,
        Anulado = 1,
        Ajuste = 2,
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
