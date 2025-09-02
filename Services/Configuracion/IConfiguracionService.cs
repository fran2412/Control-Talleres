using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ControlTalleresMVP.Services.Configuracion
{
    public interface IConfiguracionService
    {
        string GetValor(string clave, string valorPorDefecto = "");
        public T GetValor<T>(string clave, T valorPorDefecto = default!);
        void SetValor(string clave, string valor);
    }
}
