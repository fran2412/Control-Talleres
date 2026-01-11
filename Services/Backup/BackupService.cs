using ControlTalleresMVP.Configuraciones;
using ControlTalleresMVP.Persistence.DataContext;
using Microsoft.EntityFrameworkCore;
using System.IO;

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
            return await CreateBackupAsync(backupName, true, ct);
        }

        public async Task<string> CreateBackupAsync(string? backupName, bool optimizeDatabase, CancellationToken ct = default)
        {
            try
            {
                // Generar nombre del backup
                var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                var fechaFormateada = DateTime.Now.ToString("dd-MM-yyyy_HH");
                var fileName = backupName != null
                    ? $"{backupName}.db"
                    : $"Copia_Seguridad_Manual_{fechaFormateada}.db";

                var backupPath = Path.Combine(_backupDirectory, fileName);

                // Ejecutar PRAGMA solo si se solicita optimización (backups manuales)
                if (optimizeDatabase)
                {
                    await OptimizeDatabaseForBackupAsync(ct);
                }

                // Crear backup copiando el archivo de la base de datos
                await Task.Run(() => File.Copy(AppPaths.DbPath, backupPath, true), ct);

                // Verificar que el backup se creó correctamente
                if (File.Exists(backupPath))
                {
                    var backupSize = new FileInfo(backupPath).Length;
                    const long MINIMUM_BACKUP_SIZE = 50 * 1024; // 50 KB

                    if (backupSize < MINIMUM_BACKUP_SIZE)
                    {
                        File.Delete(backupPath);
                        RestartApplication();
                    }
                    return backupPath;
                }
                else
                {
                    throw new InvalidOperationException("No se pudo crear el backup");
                }
            }
            catch
            {
                throw;
            }
        }

        public async Task<string> CreateAutomaticBackupAsync(CancellationToken ct = default)
        {
            try
            {
                var today = DateTime.Now.ToString("yyyyMMdd");
                var fechaFormateada = DateTime.Now.ToString("dd-MM-yyyy_HH");
                var backupName = $"Copia_Seguridad_Automatica_{fechaFormateada}";

                // Verificar si ya existe un backup automático del día actual
                var existingBackups = await GetAvailableBackupsAsync();
                var todayBackup = existingBackups.FirstOrDefault(b =>
                    b.FileName.StartsWith($"Copia_Seguridad_Automatica_{DateTime.Now.ToString("dd-MM-yyyy")}") &&
                    b.CreatedDate.Date == DateTime.Now.Date);

                if (todayBackup != null)
                {
                    return todayBackup.FilePath;
                }

                return await CreateBackupAsync(backupName, ct);
            }
            catch
            {
                throw;
            }
        }

        public async Task<bool> RestoreFromBackupAsync(string backupPath, CancellationToken ct = default)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(backupPath) || !File.Exists(backupPath))
                {
                    return false;
                }

                var dbPath = AppPaths.DbPath;
                var dbDir = Path.GetDirectoryName(dbPath)!;
                var tempPath = Path.Combine(dbDir, $"{Path.GetFileNameWithoutExtension(dbPath)}.__restore_tmp{Path.GetExtension(dbPath)}");

                string? preRestore = null;

                try
                {
                    await _context.Database.CloseConnectionAsync();
                    _context.ChangeTracker.Clear();

                    try
                    {
                        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
                    }
                    catch
                    {
                    }

                    // 2) Eliminar archivos auxiliares si existen
                    TryDeleteIfExists(dbPath + "-wal");
                    TryDeleteIfExists(dbPath + "-shm");
                    TryDeleteIfExists(dbPath + "-journal");

                    // 3) Respaldo de seguridad para rollback (sin optimización para evitar conflictos)
                    var fechaRestore = DateTime.Now.ToString("dd-MM-yyyy_HH");
                    preRestore = await CreateBackupAsync($"Copia_Seguridad_Pre_Restauracion_{fechaRestore}", false, ct);

                    // 4) Copiar el backup a un archivo temporal en el MISMO directorio (misma unidad)
                    await Task.Run(() => File.Copy(backupPath, tempPath, true), ct);

                    // 5) Reemplazo atómico del archivo activo
                    if (File.Exists(dbPath))
                    {
                        // Reemplaza el destino con el temp (operación atómica en el mismo volumen)
                        File.Replace(tempPath, dbPath, null);
                    }
                    else
                    {
                        // Si por alguna razón no existe aún la BD, mueve el temp
                        File.Move(tempPath, dbPath);
                    }

                    // 6) Verificar integridad de la BD restaurada con conexión FRESCA
                    var isIntegrityOk = await VerifyDatabaseIntegrityAsync(ct);
                    if (!isIntegrityOk)
                    {
                        // Rollback: volver al respaldo pre_restore (también de forma atómica)
                        var rollbackTmp = Path.Combine(dbDir, $"{Path.GetFileNameWithoutExtension(dbPath)}.__rollback_tmp{Path.GetExtension(dbPath)}");
                        await Task.Run(() => File.Copy(preRestore!, rollbackTmp, true), ct);

                        if (File.Exists(dbPath))
                        {
                            File.Replace(rollbackTmp, dbPath, null);
                        }
                        else
                        {
                            File.Move(rollbackTmp, dbPath);
                        }

                        return false;
                    }


                    RestartApplication();

                    return true;
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    // Limpieza del temporal si quedó colgado
                    TryDeleteIfExists(tempPath);
                    return false;
                }
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        /// <summary>
        /// Intenta eliminar un archivo si existe, sin lanzar excepciones
        /// </summary>
        private static void TryDeleteIfExists(string path)
        {
            try
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
            catch
            {
            }
        }

        /// <summary>
        /// Verifica la integridad de la base de datos usando una conexión fresca
        /// </summary>
        private async Task<bool> VerifyDatabaseIntegrityAsync(CancellationToken ct = default)
        {
            try
            {
                // Verificar que la base de datos existe y es accesible
                if (!File.Exists(AppPaths.DbPath))
                {
                    return false;
                }

                // Verificar conexión y consulta básica
                var canConnect = await _context.Database.CanConnectAsync(ct);
                if (!canConnect)
                {
                    return false;
                }

                // Verificar que se pueden ejecutar consultas básicas
                var count = await _context.Alumnos.CountAsync(ct);
                return true;
            }
            catch
            {
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

                // Ordenar por fecha de creación (más recientes primero) para el DataGrid
                return backups.OrderByDescending(b => b.CreatedDate).ToList();
            }
            catch
            {
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
                    return true;
                }
                return false;
            }
            catch
            {
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

                return deletedCount;
            }
            catch
            {
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
            catch
            {
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
            catch
            {
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

        /// <summary>
        /// Fuerza el checkpoint del WAL para que los archivos auxiliares reflejen los cambios en el .db
        /// </summary>
        private async Task OptimizeDatabaseForBackupAsync(CancellationToken ct = default)
        {
            try
            {

                // Forzar checkpoint del WAL para incluir todos los cambios pendientes
                await _context.Database.ExecuteSqlRawAsync("PRAGMA wal_checkpoint(FULL)", ct);

            }
            catch
            {
            }
        }

        /// <summary>
        /// Reinicia la aplicación para aplicar el WAL y crear un backup válido
        /// </summary>
        private void RestartApplication()
        {
            try
            {
                // Obtener la ruta del ejecutable actual
                var currentExecutable = System.Reflection.Assembly.GetExecutingAssembly().Location;
                var executablePath = currentExecutable.Replace(".dll", ".exe");

                // Si no existe el .exe, usar el .dll
                if (!File.Exists(executablePath))
                {
                    executablePath = currentExecutable;
                }

                // Iniciar una nueva instancia de la aplicación
                var startInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = executablePath,
                    UseShellExecute = true,
                    WorkingDirectory = Path.GetDirectoryName(executablePath)
                };

                System.Diagnostics.Process.Start(startInfo);

                // Cerrar la aplicación actual
                Environment.Exit(0);
            }
            catch
            {
                // Si falla el reinicio, cerrar normalmente
                Environment.Exit(1);
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
