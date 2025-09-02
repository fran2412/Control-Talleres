using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ControlTalleresMVP.Persistence.Models
{
    public class Configuracion
    {
        public string Clave { get; set; } = null!;
        public string Valor { get; set; } = null!;
        public string? Descripcion { get; set; }
    }
}