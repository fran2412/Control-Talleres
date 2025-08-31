using ControlTalleresMVP.Persistence.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ControlTalleresMVP.Abstractions
{
    public interface ICrudService<TItem>
    {
        public Task GuardarAsync(TItem item, CancellationToken ct = default);
        public Task EliminarAsync(int id, CancellationToken ct = default);
        public List<TItem> ObtenerTodos(CancellationToken ct = default);
        public Task ActualizarAsync(TItem item, CancellationToken ct = default);

    }
}
