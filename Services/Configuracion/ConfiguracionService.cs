using ControlTalleresMVP.Persistence.DataContext;
using ControlTalleresMVP.Persistence.Models;
using ControlTalleresMVP.Services.Sesion;

namespace ControlTalleresMVP.Services.Configuracion
{
    public class ConfiguracionService : IConfiguracionService
    {
        private readonly EscuelaContext _context;
        private readonly ISesionService _sesionService;

        public ConfiguracionService(EscuelaContext context, ISesionService sesionService)
        {
            _context = context;
            _sesionService = sesionService;
        }

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

        // =====================
        // Métodos para ConfiguracionSede (configuraciones por sede)
        // =====================

        public T GetValorSede<T>(string clave, T valorPorDefecto = default!)
        {
            if (string.IsNullOrWhiteSpace(clave))
                throw new ArgumentException("La clave de configuración no puede estar vacía.", nameof(clave));

            var sedeId = _sesionService.ObtenerIdSede();

            var config = _context.ConfiguracionesSede
                .FirstOrDefault(cs => cs.SedeId == sedeId && cs.Clave == clave);

            if (config is null)
            {
                config = new ConfiguracionSede
                {
                    SedeId = sedeId,
                    Clave = clave,
                    Valor = valorPorDefecto?.ToString() ?? string.Empty
                };
                _context.ConfiguracionesSede.Add(config);
                _context.SaveChanges();

                return valorPorDefecto;
            }

            return (T)Convert.ChangeType(config.Valor, typeof(T));
        }

        public bool SetValorSede(string clave, string valor)
        {
            try
            {
                var sedeId = _sesionService.ObtenerIdSede();

                var config = _context.ConfiguracionesSede
                    .FirstOrDefault(cs => cs.SedeId == sedeId && cs.Clave == clave);

                if (config is null)
                {
                    config = new ConfiguracionSede
                    {
                        SedeId = sedeId,
                        Clave = clave,
                        Valor = valor
                    };
                    _context.ConfiguracionesSede.Add(config);
                }
                else
                {
                    config.Valor = valor;
                }

                _context.SaveChanges();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
