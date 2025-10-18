using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ControlTalleresMVP.Services.Backup
{
    public interface IBackupService
    {
        Task<string> CreateBackupAsync(string? backupName = null, CancellationToken ct = default);

        Task<string> CreateAutomaticBackupAsync(CancellationToken ct = default);

        Task<bool> RestoreFromBackupAsync(string backupPath, CancellationToken ct = default);

        Task<List<BackupInfo>> GetAvailableBackupsAsync();

        Task<bool> DeleteBackupAsync(string backupPath);

        Task<int> CleanupOldBackupsAsync(int daysToKeep = 30);

        Task<bool> VerifyDatabaseIntegrityAsync();

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
