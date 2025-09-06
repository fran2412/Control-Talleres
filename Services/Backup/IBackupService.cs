using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ControlTalleresMVP.Services.Backup
{
    public interface IBackupService
    {
        /// <summary>
        /// Crea un backup de la base de datos
        /// </summary>
        /// <param name="backupName">Nombre personalizado para el backup (opcional)</param>
        /// <param name="ct">Token de cancelación</param>
        /// <returns>Ruta del archivo de backup creado</returns>
        Task<string> CreateBackupAsync(string? backupName = null, CancellationToken ct = default);

        /// <summary>
        /// Crea un backup automático con timestamp
        /// </summary>
        /// <param name="ct">Token de cancelación</param>
        /// <returns>Ruta del archivo de backup creado</returns>
        Task<string> CreateAutomaticBackupAsync(CancellationToken ct = default);

        /// <summary>
        /// Restaura la base de datos desde un backup
        /// </summary>
        /// <param name="backupPath">Ruta del archivo de backup</param>
        /// <param name="ct">Token de cancelación</param>
        /// <returns>True si la restauración fue exitosa</returns>
        Task<bool> RestoreFromBackupAsync(string backupPath, CancellationToken ct = default);

        /// <summary>
        /// Obtiene la lista de backups disponibles
        /// </summary>
        /// <returns>Lista de información de backups</returns>
        Task<List<BackupInfo>> GetAvailableBackupsAsync();

        /// <summary>
        /// Elimina un backup específico
        /// </summary>
        /// <param name="backupPath">Ruta del archivo de backup</param>
        /// <returns>True si se eliminó correctamente</returns>
        Task<bool> DeleteBackupAsync(string backupPath);

        /// <summary>
        /// Limpia backups antiguos (más de X días)
        /// </summary>
        /// <param name="daysToKeep">Días a mantener (por defecto 30)</param>
        /// <returns>Número de backups eliminados</returns>
        Task<int> CleanupOldBackupsAsync(int daysToKeep = 30);

        /// <summary>
        /// Verifica la integridad de la base de datos actual
        /// </summary>
        /// <returns>True si la base de datos está íntegra</returns>
        Task<bool> VerifyDatabaseIntegrityAsync();

        /// <summary>
        /// Obtiene el tamaño de la base de datos actual
        /// </summary>
        /// <returns>Tamaño en bytes</returns>
        Task<long> GetDatabaseSizeAsync();
    }

    public class BackupInfo
    {
        public string FilePath { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; }
        public long SizeBytes { get; set; }
        public string SizeFormatted { get; set; } = string.Empty;
        public bool IsValid { get; set; }
    }
}
