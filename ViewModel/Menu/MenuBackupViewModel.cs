using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ControlTalleresMVP.Helpers.Dialogs;
using ControlTalleresMVP.Services.Backup;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace ControlTalleresMVP.ViewModel.Menu
{
    public partial class MenuBackupViewModel : ObservableObject
    {
        private readonly IBackupService _backupService;
        private readonly IDialogService _dialogService;

        [ObservableProperty] private ObservableCollection<BackupInfo> backups = new();
        [ObservableProperty] private BackupInfo? backupSeleccionado;
        [ObservableProperty] private bool isCreatingBackup;
        [ObservableProperty] private bool isRestoringBackup;
        [ObservableProperty] private long databaseSizeBytes;
        [ObservableProperty] private string databaseSizeFormatted = string.Empty;
        [ObservableProperty] private bool databaseIntegrityOk;
        [ObservableProperty] private string? backupName;

        public MenuBackupViewModel(IBackupService backupService, IDialogService dialogService)
        {
            _backupService = backupService;
            _dialogService = dialogService;
            
            _ = LoadBackupsAsync();
            _ = UpdateDatabaseInfoAsync();
        }

        [RelayCommand]
        private async Task LoadBackupsAsync()
        {
            try
            {
                var backupList = await _backupService.GetAvailableBackupsAsync();
                Backups.Clear();
                
                foreach (var backup in backupList)
                {
                    Backups.Add(backup);
                }
            }
            catch (Exception ex)
            {
                _dialogService.Error($"Error al cargar backups: {ex.Message}");
            }
        }

        [RelayCommand]
        private async Task CreateBackupAsync()
        {
            if (IsCreatingBackup) return;

            try
            {
                IsCreatingBackup = true;
                
                var backupPath = string.IsNullOrWhiteSpace(BackupName) 
                    ? await _backupService.CreateBackupAsync("Respaldo_Manual")
                    : await _backupService.CreateBackupAsync(BackupName);

                _dialogService.Info($"Backup creado exitosamente:\n{Path.GetFileName(backupPath)}");
                BackupName = string.Empty;
                
                await LoadBackupsAsync();
                await UpdateDatabaseInfoAsync();
            }
            catch (Exception ex)
            {
                _dialogService.Error($"Error al crear backup: {ex.Message}");
            }
            finally
            {
                IsCreatingBackup = false;
            }
        }

        [RelayCommand]
        private async Task RestoreBackupAsync()
        {
            System.Diagnostics.Debug.WriteLine("RestoreBackupAsync - Comando ejecutado");
            
            if (BackupSeleccionado == null) 
            {
                System.Diagnostics.Debug.WriteLine("RestoreBackupAsync - No hay backup seleccionado");
                _dialogService.Alerta("Por favor selecciona un backup para restaurar.");
                return;
            }

            if (IsRestoringBackup) 
            {
                System.Diagnostics.Debug.WriteLine("RestoreBackupAsync - Ya se está restaurando un backup");
                return;
            }

            System.Diagnostics.Debug.WriteLine($"RestoreBackupAsync - Backup seleccionado: {BackupSeleccionado.FileName}");

            var confirmacion = _dialogService.Confirmar(
                $"¿Estás seguro de que quieres restaurar el backup?\n\n" +
                $"Archivo: {BackupSeleccionado.FileName}\n" +
                $"Fecha: {BackupSeleccionado.CreatedDate:dd/MM/yyyy HH:mm}\n\n" +
                $"⚠️ Esta acción reemplazará la base de datos actual.");
            
            if (!confirmacion) 
            {
                System.Diagnostics.Debug.WriteLine("RestoreBackupAsync - Usuario canceló la restauración");
                return;
            }

            try
            {
                System.Diagnostics.Debug.WriteLine($"RestoreBackupAsync - Restaurando backup: {BackupSeleccionado.FilePath}");
                IsRestoringBackup = true;
                
                var success = await _backupService.RestoreFromBackupAsync(BackupSeleccionado.FilePath);
                
                if (success)
                {
                    System.Diagnostics.Debug.WriteLine("RestoreBackupAsync - Backup restaurado exitosamente");
                    _dialogService.Info("Base de datos restaurada exitosamente.\nLa aplicación se reiniciará.");
                    Application.Current.Shutdown();
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("RestoreBackupAsync - Error al restaurar backup");
                    _dialogService.Error("Error al restaurar el backup.");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"RestoreBackupAsync - Excepción: {ex.Message}");
                _dialogService.Error($"Error al restaurar backup: {ex.Message}");
            }
            finally
            {
                IsRestoringBackup = false;
            }
        }

        [RelayCommand]
        private async Task DeleteBackupAsync()
        {
            System.Diagnostics.Debug.WriteLine("DeleteBackupAsync - Comando ejecutado");
            
            if (BackupSeleccionado == null) 
            {
                System.Diagnostics.Debug.WriteLine("DeleteBackupAsync - No hay backup seleccionado");
                _dialogService.Alerta("Por favor selecciona un backup para eliminar.");
                return;
            }

            System.Diagnostics.Debug.WriteLine($"DeleteBackupAsync - Backup seleccionado: {BackupSeleccionado.FileName}");

            var confirmacion = _dialogService.Confirmar(
                $"¿Estás seguro de que quieres eliminar este backup?\n\n" +
                $"Archivo: {BackupSeleccionado.FileName}\n" +
                $"Fecha: {BackupSeleccionado.CreatedDate:dd/MM/yyyy HH:mm}");
            
            if (!confirmacion) 
            {
                System.Diagnostics.Debug.WriteLine("DeleteBackupAsync - Usuario canceló la eliminación");
                return;
            }

            try
            {
                System.Diagnostics.Debug.WriteLine($"DeleteBackupAsync - Eliminando backup: {BackupSeleccionado.FilePath}");
                var success = await _backupService.DeleteBackupAsync(BackupSeleccionado.FilePath);
                
                if (success)
                {
                    System.Diagnostics.Debug.WriteLine("DeleteBackupAsync - Backup eliminado exitosamente");
                    _dialogService.Info("Backup eliminado exitosamente.");
                    await LoadBackupsAsync();
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("DeleteBackupAsync - Error al eliminar backup");
                    _dialogService.Error("Error al eliminar el backup.");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"DeleteBackupAsync - Excepción: {ex.Message}");
                _dialogService.Error($"Error al eliminar backup: {ex.Message}");
            }
        }

        [RelayCommand]
        private async Task CleanupOldBackupsAsync()
        {
            var confirmacion = _dialogService.Confirmar(
                "¿Eliminar backups antiguos (más de 30 días)?\n\n" +
                "Esta acción no se puede deshacer.");
            
            if (!confirmacion) return;

            try
            {
                var deletedCount = await _backupService.CleanupOldBackupsAsync(30);
                _dialogService.Info($"Se eliminaron {deletedCount} backups antiguos.");
                await LoadBackupsAsync();
            }
            catch (Exception ex)
            {
                _dialogService.Error($"Error al limpiar backups: {ex.Message}");
            }
        }

        [RelayCommand]
        private async Task VerifyDatabaseAsync()
        {
            try
            {
                var isIntegrityOk = await _backupService.VerifyDatabaseIntegrityAsync();
                DatabaseIntegrityOk = isIntegrityOk;
                
                if (isIntegrityOk)
                {
                    _dialogService.Info("✅ La base de datos está íntegra y funcionando correctamente.");
                }
                else
                {
                    _dialogService.Error("❌ Se detectaron problemas en la integridad de la base de datos.\nSe recomienda crear un backup inmediatamente.");
                }
            }
            catch (Exception ex)
            {
                _dialogService.Error($"Error al verificar integridad: {ex.Message}");
            }
        }

        [RelayCommand]
        private async Task UpdateDatabaseInfoAsync()
        {
            try
            {
                DatabaseSizeBytes = await _backupService.GetDatabaseSizeAsync();
                DatabaseSizeFormatted = FormatFileSize(DatabaseSizeBytes);
                DatabaseIntegrityOk = await _backupService.VerifyDatabaseIntegrityAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al actualizar información de base de datos: {ex.Message}");
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
