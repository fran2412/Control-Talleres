namespace ControlTalleresMVP.Messages
{
    public sealed record ClasesActualizadasMessage(int AlumnoId);
    public sealed record InscripcionesActualizadasMessage(int AlumnoId);
    public sealed record FechaClasesSeleccionadaCambiadaMessage(DateTime Fecha);
}
