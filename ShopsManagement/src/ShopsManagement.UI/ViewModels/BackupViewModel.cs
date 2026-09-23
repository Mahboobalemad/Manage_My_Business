using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using Microsoft.Win32;
using ShopsManagement.Domain.Entities;
using ShopsManagement.Domain.Enums;
using ShopsManagement.Domain.Services;
using ShopsManagement.UI.Services;

namespace ShopsManagement.UI.ViewModels;

public class BackupViewModel : ViewModelBase
{
    private readonly IBackupService    _backupService;
    private readonly ISettingsService  _settingsService;

    private string _statusMessage = string.Empty;
    private bool   _isUploading   = false;

    public BackupViewModel(IBackupService backupService, ISettingsService settingsService)
    {
        _backupService   = backupService;
        _settingsService = settingsService;

        BackupLogs = new ObservableCollection<BackupLog>();

        CreateManualBackupCommand  = new RelayCommand(async () => await CreateManualBackupAsync());
        UploadToGoogleDriveCommand = new RelayCommand(async () => await UploadToGoogleDriveAsync());
        RestoreBackupCommand       = new RelayCommand(async () => await RestoreBackupAsync());
        RefreshLogsCommand         = new RelayCommand(async () => await LoadLogsAsync());
    }

    // ── Exposed Collections ─────────────────────────────────────────
    public ObservableCollection<BackupLog> BackupLogs { get; }

    // ── Properties ──────────────────────────────────────────────────
    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    public bool IsUploading
    {
        get => _isUploading;
        set => SetProperty(ref _isUploading, value);
    }

    // Current auto-backup frequency label (shown in UI)
    public string BackupFrequencyLabel
    {
        get
        {
            try
            {
                var freq = _settingsService?.Current?.BackupFrequency ?? "Weekly";
                return freq switch
                {
                    "Daily"   => "يومي",
                    "Weekly"  => "أسبوعي",
                    "Monthly" => "شهري",
                    "Yearly"  => "سنوي",
                    _         => "أسبوعي"
                };
            }
            catch
            {
                return "أسبوعي";
            }
        }
    }

    public DateTime? LastAutoBackupDate
    {
        get
        {
            try
            {
                return _settingsService?.Current?.LastAutoBackupDate;
            }
            catch
            {
                return null;
            }
        }
    }

    public string LastAutoBackupDateText
    {
        get
        {
            try
            {
                var dt = LastAutoBackupDate;
                return dt.HasValue ? dt.Value.ToString("yyyy/MM/dd HH:mm") : "لم يتم بعد";
            }
            catch
            {
                return "لم يتم بعد";
            }
        }
    }

    // ── Commands ────────────────────────────────────────────────────
    public ICommand CreateManualBackupCommand  { get; }
    public ICommand UploadToGoogleDriveCommand { get; }
    public ICommand RestoreBackupCommand       { get; }
    public ICommand RefreshLogsCommand         { get; }

    // ── Lifecycle ───────────────────────────────────────────────────
    public async Task InitializeAsync()
    {
        try
        {
            OnPropertyChanged(nameof(BackupFrequencyLabel));
            OnPropertyChanged(nameof(LastAutoBackupDate));
            OnPropertyChanged(nameof(LastAutoBackupDateText));
            await LoadLogsAsync();
        }
        catch (Exception ex)
        {
            StatusMessage = $"تنبيه: تعذر تحميل سجل النسخ الاحتياطي: {ex.Message}";
        }
    }

    public async Task LoadLogsAsync()
    {
        try
        {
            if (_backupService == null) return;
            var logs = await _backupService.GetBackupLogsAsync();
            BackupLogs.Clear();
            if (logs != null)
            {
                foreach (var log in logs) BackupLogs.Add(log);
            }
            StatusMessage = $"تم تحميل {BackupLogs.Count} سجل — آخر نسخة تلقائية: {LastAutoBackupDateText}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"تنبيه: {ex.Message}";
        }
    }

    // ── Manual backup ───────────────────────────────────────────────
    private async Task CreateManualBackupAsync()
    {
        var dialog = new SaveFileDialog
        {
            Filter   = "ملف قاعدة البيانات (*.db)|*.db|جميع الملفات (*.*)|*.*",
            FileName = $"ShopsBackup_{DateTime.Now:yyyyMMdd_HHmmss}.db"
        };

        string? customPath = null;
        if (dialog.ShowDialog() == true) customPath = dialog.FileName;

        try
        {
            StatusMessage = "⏳ جارٍ إنشاء النسخة الاحتياطية...";
            var result = await _backupService.CreateBackupAsync(BackupType.Manual, customPath);

            StatusMessage = result.Status == BackupStatus.Success
                ? $"✅ تمت النسخة الاحتياطية بنجاح: {result.FilePathOrDriveFileId}"
                : $"❌ فشلت النسخة الاحتياطية: {result.Notes}";

            await LoadLogsAsync();
        }
        catch (Exception ex)
        {
            StatusMessage = $"❌ خطأ: {ex.Message}";
        }
    }

    // ── Google Drive upload ─────────────────────────────────────────
    private async Task UploadToGoogleDriveAsync()
    {
        if (IsUploading) return;

        try
        {
            IsUploading = true;
            StatusMessage = "⏳ جارٍ إنشاء نسخة احتياطية للرفع...";

            // Step 1: Create a local backup first
            var result = await _backupService.CreateBackupAsync(BackupType.Manual);
            if (result.Status != BackupStatus.Success)
            {
                StatusMessage = $"❌ فشل إنشاء النسخة الاحتياطية قبل الرفع: {result.Notes}";
                return;
            }

            string localBackupPath = result.FilePathOrDriveFileId;

            // Step 2: Open Google Drive in browser
            Process.Start(new ProcessStartInfo
            {
                FileName        = "https://drive.google.com/drive/my-drive",
                UseShellExecute = true
            });

            // Step 3: Ask user if they want to open the backup folder too
            var openFolder = MessageBox.Show(
                $"تم فتح Google Drive في المتصفح.\n\n" +
                $"مسار الملف الاحتياطي للرفع اليدوي:\n{localBackupPath}\n\n" +
                "هل تريد فتح مجلد النسخة الاحتياطية لتسهيل الرفع؟",
                "رفع إلى Google Drive",
                MessageBoxButton.YesNo,
                MessageBoxImage.Information);

            if (openFolder == MessageBoxResult.Yes)
            {
                var folder = System.IO.Path.GetDirectoryName(localBackupPath);
                if (!string.IsNullOrEmpty(folder) && System.IO.Directory.Exists(folder))
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName        = folder,
                        UseShellExecute = true
                    });
                }
            }

            StatusMessage = $"✅ تم تجهيز النسخة وفتح Google Drive — قم بسحب الملف ورفعه: {System.IO.Path.GetFileName(localBackupPath)}";
            await LoadLogsAsync();
        }
        catch (Exception ex)
        {
            StatusMessage = $"❌ خطأ أثناء العملية: {ex.Message}";
        }
        finally
        {
            IsUploading = false;
        }
    }

    // ── Restore with confirmation ───────────────────────────────────
    private async Task RestoreBackupAsync()
    {
        var openDialog = new OpenFileDialog
        {
            Filter = "ملف قاعدة البيانات (*.db)|*.db|جميع الملفات (*.*)|*.*",
            Title  = "اختر ملف النسخة الاحتياطية لاستعادتها"
        };

        if (openDialog.ShowDialog() != true) return;

        string selectedFile = openDialog.FileName;

        // ── Confirmation dialog ─────────────────────────────────────
        var confirm = MessageBox.Show(
            $"⚠️ تحذير: تأكيد الاستعادة\n\n" +
            $"سيتم استبدال قاعدة البيانات الحالية بالنسخة الاحتياطية المحددة:\n" +
            $"{System.IO.Path.GetFileName(selectedFile)}\n\n" +
            "❗ سيتم فقدان أي بيانات تمت إضافتها بعد تاريخ هذه النسخة.\n\n" +
            "هل أنت متأكد من المتابعة؟",
            "تأكيد استعادة النسخة الاحتياطية",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (confirm != MessageBoxResult.Yes) return;

        try
        {
            StatusMessage = "⏳ جارٍ استعادة النسخة الاحتياطية...";
            var ok = await _backupService.RestoreBackupAsync(selectedFile);
            if (ok)
            {
                StatusMessage = "✅ تمت الاستعادة بنجاح! يُفضَّل إعادة تشغيل التطبيق لتطبيق التغييرات.";
                MessageBox.Show(
                    "✅ تمت استعادة قاعدة البيانات بنجاح!\n\nيُرجى إعادة تشغيل التطبيق لتطبيق التغييرات.",
                    "تمت الاستعادة",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"❌ خطأ أثناء الاستعادة: {ex.Message}";
        }
    }

    /// <summary>
    /// Called by other ViewModels before deleting critical records
    /// </summary>
    public async Task CreatePreDeletionBackupAsync(string entityType)
    {
        try
        {
            if (_backupService != null)
            {
                var result = await _backupService.CreateBackupAsync(BackupType.Automatic);
                if (result.Status == BackupStatus.Success)
                {
                    StatusMessage = $"✅ تم إنشاء نسخة احتياطية تلقائية قبل حذف {entityType}";
                }
            }
        }
        catch
        {
            // Non-blocking
        }
    }
}
