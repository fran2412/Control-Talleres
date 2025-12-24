using ControlTalleresMVP.Persistence.DataContext;

namespace ControlTalleresMVP.Services.Configuracion
{
    public class ConfiguracionService : IConfiguracionService
    {
        private readonly EscuelaContext _context;
        public ConfiguracionService(EscuelaContext context) => _context = context;

        public string GetValor(string clave, string valorPorDefecto = "")
        {
            var config = _context.Configuraciones.FirstOrDefault(c => c.Clave == clave);

            if (string.IsNullOrWhiteSpace(clave))
                throw new ArgumentException("La clave no puede estar vacía.", nameof(clave));

            if (config is null)
            {
                config = new Persistence.Models.Configuracion
                {
                    Clave = clave,
                    Valor = valorPorDefecto,
                    Descripcion = "" // opcional
                };
                _context.Configuraciones.Add(config);
                _context.SaveChanges();
                return valorPorDefecto;
            }

            return config.Valor;
        }

        public T GetValor<T>(string clave, T valorPorDefecto = default!)
        {
            if (string.IsNullOrWhiteSpace(clave))
                throw new ArgumentException("La clave de configuración no puede estar vacía.", nameof(clave));

            var config = _context.Configuraciones.FirstOrDefault(c => c.Clave == clave);

            if (config is null)
            {
                config = new Persistence.Models.Configuracion
                {
                    Clave = clave,
                    Valor = valorPorDefecto?.ToString() ?? string.Empty,
                    Descripcion = ""
                };
                _context.Configuraciones.Add(config);
                _context.SaveChanges();

                return valorPorDefecto;
            }

            return (T)Convert.ChangeType(config.Valor, typeof(T));
        }

        public bool SetValor(string clave, string valor)
        {
            try
            {
                var config = _context.Configuraciones.First(c => c.Clave == clave);
                if (config is null) return false;

                config.Valor = valor;
                _context.SaveChanges();
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }
    }
}
