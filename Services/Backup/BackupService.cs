using ControlTalleresMVP.Configuraciones;
using ControlTalleresMVP.Persistence.DataContext;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ControlTalleresMVP.Services.Backup
{
    public class BackupService : IBackupService
    {
        private readonly EscuelaContext _context;
        private readonly string _backupDirectory;

        public BackupService(EscuelaContext context)
        {
            _context = context;
            _backupDirectory = Path.Combine(Path.GetDirectoryName(AppPaths.DbPath)!, "backups");
            
            // Crear directorio de backups si no existe
            if (!Directory.Exists(_backupDirectory))
            {
                Directory.CreateDirectory(_backupDirectory);
            }
        }

        public async Task<string> CreateBackupAsync(string? backupName = null, CancellationToken ct = default)
        {
            try
            {
                // Generar nombre del backup
                var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                var fileName = backupName != null 
                    ? $"{backupName}_{timestamp}.db"
                    : $"backup_{timestamp}.db";
                
                var backupPath = Path.Combine(_backupDirectory, fileName);

                // Crear backup copiando el archivo de la base de datos
                await Task.Run(() => File.Copy(AppPaths.DbPath, backupPath, true), ct);

                // Verificar que el backup se creó correctamente
                if (File.Exists(backupPath))
                {
                    var backupSize = new FileInfo(backupPath).Length;
                    const long MINIMUM_BACKUP_SIZE = 50 * 1024; // 50 KB
                    
                    if (backupSize < MINIMUM_BACKUP_SIZE)
                    {
                        System.Diagnostics.Debug.WriteLine($"⚠️ Backup demasiado pequeño ({backupSize} bytes). Cerrando aplicación para aplicar WAL.");
                        
                        // Eliminar backup inválido
                        File.Delete(backupPath);
                        
                        // Cerrar la aplicación para forzar aplicación del WAL
                        Environment.Exit(1);
                    }
                    
                    System.Diagnostics.Debug.WriteLine($"Backup creado exitosamente: {backupPath} ({backupSize} bytes)");
                    return backupPath;
                }
                else
                {
                    throw new InvalidOperationException("No se pudo crear el backup");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al crear backup: {ex.Message}");
                throw;
            }
        }

        public async Task<string> CreateAutomaticBackupAsync(CancellationToken ct = default)
        {
            try
            {
                var today = DateTime.Now.ToString("yyyyMMdd");
                var backupName = $"auto_{today}";
                
                // Verificar si ya existe un backup automático del día actual
                var existingBackups = await GetAvailableBackupsAsync();
                var todayBackup = existingBackups.FirstOrDefault(b => 
                    b.FileName.StartsWith($"auto_{today}") && 
                    b.CreatedDate.Date == DateTime.Now.Date);
                
                if (todayBackup != null)
                {
                    System.Diagnostics.Debug.WriteLine($"Ya existe un backup automático del día {today}. No se creará uno nuevo.");
                    return todayBackup.FilePath;
                }
                
                // Crear backup automático solo si no existe uno del día actual
                System.Diagnostics.Debug.WriteLine($"Creando backup automático del día {today}");
                return await CreateBackupAsync(backupName, ct);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al crear backup automático: {ex.Message}");
                throw;
            }
        }

        public async Task<bool> RestoreFromBackupAsync(string backupPath, CancellationToken ct = default)
        {
            try
            {
                if (!File.Exists(backupPath))
                {
                    throw new FileNotFoundException($"El archivo de backup no existe: {backupPath}");
                }

                // Cerrar la conexión actual
                await _context.Database.CloseConnectionAsync();

                // Crear backup de la base de datos actual antes de restaurar
                var currentBackup = await CreateBackupAsync("pre_restore", ct);

                // Restaurar desde el backup
                await Task.Run(() => File.Copy(backupPath, AppPaths.DbPath, true), ct);

                // Verificar integridad de la base de datos restaurada
                var isIntegrityOk = await VerifyDatabaseIntegrityAsync();
                
                if (!isIntegrityOk)
                {
                    // Si la integridad falla, restaurar el backup anterior
                    await Task.Run(() => File.Copy(currentBackup, AppPaths.DbPath, true), ct);
                    throw new InvalidOperationException("La base de datos restaurada no es válida. Se restauró el estado anterior.");
                }

                System.Diagnostics.Debug.WriteLine($"Base de datos restaurada exitosamente desde: {backupPath}");
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al restaurar backup: {ex.Message}");
                return false;
            }
        }

        public async Task<List<BackupInfo>> GetAvailableBackupsAsync()
        {
            try
            {
                var backupFiles = Directory.GetFiles(_backupDirectory, "*.db")
                    .OrderByDescending(f => File.GetCreationTime(f))
                    .ToList();

                var backups = new List<BackupInfo>();

                foreach (var filePath in backupFiles)
                {
                    var fileInfo = new FileInfo(filePath);
                    var backupInfo = new BackupInfo
                    {
                        FilePath = filePath,
                        FileName = fileInfo.Name,
                        CreatedDate = fileInfo.CreationTime,
                        SizeBytes = fileInfo.Length,
                        SizeFormatted = FormatFileSize(fileInfo.Length),
                        IsValid = await IsValidBackupAsync(filePath)
                    };

                    backups.Add(backupInfo);
                }

                return backups;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al obtener lista de backups: {ex.Message}");
                return new List<BackupInfo>();
            }
        }

        public async Task<bool> DeleteBackupAsync(string backupPath)
        {
            try
            {
                if (File.Exists(backupPath))
                {
                    await Task.Run(() => File.Delete(backupPath));
                    System.Diagnostics.Debug.WriteLine($"Backup eliminado: {backupPath}");
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al eliminar backup: {ex.Message}");
                return false;
            }
        }

        public async Task<int> CleanupOldBackupsAsync(int daysToKeep = 30)
        {
            try
            {
                var cutoffDate = DateTime.Now.AddDays(-daysToKeep);
                var oldBackups = Directory.GetFiles(_backupDirectory, "*.db")
                    .Where(f => File.GetCreationTime(f) < cutoffDate)
                    .ToList();

                int deletedCount = 0;
                foreach (var backupPath in oldBackups)
                {
                    if (await DeleteBackupAsync(backupPath))
                    {
                        deletedCount++;
                    }
                }

                System.Diagnostics.Debug.WriteLine($"Se eliminaron {deletedCount} backups antiguos");
                return deletedCount;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al limpiar backups antiguos: {ex.Message}");
                return 0;
            }
        }

        public async Task<bool> VerifyDatabaseIntegrityAsync()
        {
            try
            {
                // Verificar que la base de datos existe y es accesible
                if (!File.Exists(AppPaths.DbPath))
                {
                    return false;
                }

                // Verificar conexión y consulta básica
                var canConnect = await _context.Database.CanConnectAsync();
                if (!canConnect)
                {
                    return false;
                }

                // Verificar que se pueden ejecutar consultas básicas
                var count = await _context.Alumnos.CountAsync();
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al verificar integridad: {ex.Message}");
                return false;
            }
        }

        public async Task<long> GetDatabaseSizeAsync()
        {
            try
            {
                if (File.Exists(AppPaths.DbPath))
                {
                    var fileInfo = new FileInfo(AppPaths.DbPath);
                    return await Task.FromResult(fileInfo.Length);
                }
                return 0;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al obtener tamaño de base de datos: {ex.Message}");
                return 0;
            }
        }

        private async Task<bool> IsValidBackupAsync(string backupPath)
        {
            try
            {
                // Verificar que el archivo existe y tiene contenido
                if (!File.Exists(backupPath))
                {
                    return false;
                }

                var fileInfo = new FileInfo(backupPath);
                if (fileInfo.Length == 0)
                {
                    return false;
                }

                // Verificar que es un archivo SQLite válido (verificar header)
                using var fileStream = new FileStream(backupPath, FileMode.Open, FileAccess.Read);
                var header = new byte[16];
                await fileStream.ReadAsync(header, 0, 16);
                
                // SQLite files start with "SQLite format 3"
                var sqliteHeader = System.Text.Encoding.ASCII.GetString(header);
                return sqliteHeader.StartsWith("SQLite format 3");
            }
            catch
            {
                return false;
            }
        }

        private string FormatFileSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB" };
            double len = bytes;
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }
            return $"{len:0.##} {sizes[order]}";
        }
    }
}
