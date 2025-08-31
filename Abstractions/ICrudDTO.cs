using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ControlTalleresMVP.Abstractions
{
    public interface ICrudDTO
    {
        public int Id { get; set; }
        public string Nombre { get; set; }
    }
}
