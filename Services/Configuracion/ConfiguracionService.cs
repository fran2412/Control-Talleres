using ControlTalleresMVP.Persistence.DataContext;
using ControlTalleresMVP.Persistence.Models;
using Microsoft.EntityFrameworkCore.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
                // Si no existe, lo insertamos
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

            // Si existe, intentamos convertirlo al tipo T
            return (T)Convert.ChangeType(config.Valor, typeof(T));
        }

        public void SetValor(string clave, string valor)
        {
            var config = _context.Configuraciones.First(c => c.Clave == clave);
            config.Valor = valor;
            _context.SaveChanges();
        }
    }
}
