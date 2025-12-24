namespace ControlTalleresMVP.Abstractions
{
    public interface ICrudService<TItem>
    {
        public Task<TItem> GuardarAsync(TItem item, CancellationToken ct = default);
        public Task EliminarAsync(int id, CancellationToken ct = default);
        public List<TItem> ObtenerTodos(CancellationToken ct = default);
        public Task ActualizarAsync(TItem item, CancellationToken ct = default);

    }
}
