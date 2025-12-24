using ControlTalleresMVP.Persistence.ModelDTO;

namespace ControlTalleresMVP.Services.Pagos
{
    public interface IPagoService
    {
        Task<int> GuardarPagoAsync(PagoCapturaDTO captura, CancellationToken ct = default);
    }
}
