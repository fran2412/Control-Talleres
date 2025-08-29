using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ControlTalleresMVP.Configuraciones
{
    public static class AppPaths
    {
        public static string BaseDir
        {
            get
            {
                var roaming = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                return Path.Combine(roaming, "ControlTalleres");
            }
        }

        public static string DbPath => Path.Combine(BaseDir, "sistemaescuelatalleres.db");

        public static void EnsureAppFolder()
        {
            Directory.CreateDirectory(BaseDir);
        }
    }
}
