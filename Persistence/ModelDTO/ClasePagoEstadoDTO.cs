namespace ControlTalleresMVP.Persistence.ModelDTO
{
    public record ClasePagoEstadoDTO(
        int TallerId,
        bool TieneCargo,
        bool EstaPagada,
        bool PuedePagar
    );

    public record RegistrarClaseResult(
        DateTime Fecha,
        bool ClaseCreada,
        bool CargoCreado,
        bool PagoCreado,
        decimal MontoAplicado,
        bool CargoYaPagado,
        decimal ExcedenteAplicado = 0m
        );
}
