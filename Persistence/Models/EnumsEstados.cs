namespace ControlTalleresMVP.Persistence.Models
{
    public enum EstadoInscripcion
    {
        Activa = 0,
        Cancelada= 1,
        Finalizada = 2,
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
}
