using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ControlTalleresMVP.Persistence.ModelDTO
{
    public class TallerDTO
    {
        [ScaffoldColumn(false)]
        public int Id { get; set; }

        [Display(Name = "Nombre completo")]
        public string Nombre { get; set; } = "";

    }
}
