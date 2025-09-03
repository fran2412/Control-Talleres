using ControlTalleresMVP.Persistence.ModelDTO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ControlTalleresMVP.Services.Pagos
{
    public interface IPagoService
    {
        Task<int> GuardarPagoAsync(PagoCapturaDTO captura, CancellationToken ct = default);
    }
}
