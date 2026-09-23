using System.IO;
using System.Text.Json;

namespace ShopsManagement.UI.Services;

/// <summary>Application settings persisted to a local JSON file.</summary>
public class AppSettings
{
    /// <summary>Auto-backup frequency: Daily | Weekly | Monthly | Yearly</summary>
    public string BackupFrequency { get; set; } = "Weekly";

    /// <summary>Preferred currency code: YER | SAR | USD</summary>
    public string Currency { get; set; } = "YER";

    /// <summary>Dark mode enabled flag</summary>
    public bool DarkMode { get; set; } = false;

    /// <summary>Date of last automatic backup (used to decide if a new one should run)</summary>
    public DateTime? LastAutoBackupDate { get; set; } = null;
}

public interface ISettingsService
{
    AppSettings Current { get; }
    void Save();
    void Load();

    /// <summary>Returns true if enough time has passed to run an automatic backup.</summary>
    bool ShouldRunAutoBackup();

    /// <summary>Records that an automatic backup was just completed.</summary>
    void RecordAutoBackupDone();
}

public class SettingsService : ISettingsService
{
    private static readonly string SettingsPath = Path.Combine(
        AppDomain.CurrentDomain.BaseDirectory, "app_settings.json");

    private AppSettings _current = new();

    public AppSettings Current => _current;

    public SettingsService()
    {
        Load();
    }

    public void Load()
    {
        try
        {
            if (File.Exists(SettingsPath))
            {
                var json = File.ReadAllText(SettingsPath);
                _current = JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
            }
        }
        catch
        {
            _current = new AppSettings();
        }
    }

    public void Save()
    {
        try
        {
            var json = JsonSerializer.Serialize(_current, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(SettingsPath, json);
        }
        catch
        {
            // Non-blocking
        }
    }

    /// <summary>
    /// Checks whether enough time has elapsed since the last automatic backup,
    /// based on the configured frequency (default: Weekly).
    /// </summary>
    public bool ShouldRunAutoBackup()
    {
        if (_current.LastAutoBackupDate == null) return true; // Never backed up

        var last = _current.LastAutoBackupDate.Value;
        var now  = DateTime.Now;

        return (_current.BackupFrequency ?? "Weekly") switch
        {
            "Daily"   => (now - last).TotalHours  >= 24,
            "Weekly"  => (now - last).TotalDays   >= 7,
            "Monthly" => (now - last).TotalDays   >= 30,
            "Yearly"  => (now - last).TotalDays   >= 365,
            _         => (now - last).TotalDays   >= 7,   // fallback = weekly
        };
    }

    public void RecordAutoBackupDone()
    {
        _current.LastAutoBackupDate = DateTime.Now;
        Save();
    }
}
