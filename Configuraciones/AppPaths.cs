using System.IO;

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
