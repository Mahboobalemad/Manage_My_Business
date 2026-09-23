using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using ShopsManagement.UI.Services;

namespace ShopsManagement.UI.ViewModels;

public class SettingsViewModel : ViewModelBase
{
    private readonly ISettingsService _settingsService;

    // ── Backup frequency ──────────────────────────────────────────
    private string _backupFrequency;
    // ── Currency ──────────────────────────────────────────────────
    private string _currency;
    // ── Dark mode ─────────────────────────────────────────────────
    private bool _darkMode;
    // ── Status ────────────────────────────────────────────────────
    private string _statusMessage = string.Empty;

    public SettingsViewModel(ISettingsService settingsService)
    {
        _settingsService = settingsService;

        // Load persisted values
        _backupFrequency = settingsService.Current.BackupFrequency;
        _currency        = settingsService.Current.Currency;
        _darkMode        = settingsService.Current.DarkMode;

        // Available options
        BackupFrequencies = new ObservableCollection<FrequencyOption>
        {
            new("Daily",   "يومي   — نسخة احتياطية كل يوم"),
            new("Weekly",  "أسبوعي — نسخة احتياطية كل أسبوع"),
            new("Monthly", "شهري  — نسخة احتياطية كل شهر  ✅ (مُفضَّل)"),
            new("Yearly",  "سنوي  — نسخة احتياطية كل سنة"),
        };

        Currencies = new ObservableCollection<CurrencyOption>
        {
            new("YER", "ريال يمني",   "YER"),
            new("SAR", "ريال سعودي",  "SAR"),
            new("USD", "دولار أمريكي","USD"),
        };

        SaveCommand = new RelayCommand(SaveSettings);
    }

    // ── Collections ───────────────────────────────────────────────
    public ObservableCollection<FrequencyOption> BackupFrequencies { get; }
    public ObservableCollection<CurrencyOption>  Currencies        { get; }

    // ── Properties ────────────────────────────────────────────────
    public string BackupFrequency
    {
        get => _backupFrequency;
        set => SetProperty(ref _backupFrequency, value);
    }

    public string Currency
    {
        get => _currency;
        set
        {
            if (SetProperty(ref _currency, value) && value != _settingsService.Current.Currency)
            {
                // Notify user that exchange rate conversion is not yet supported
                if (!string.IsNullOrEmpty(_settingsService.Current.Currency) &&
                    value != _settingsService.Current.Currency)
                {
                    MessageBox.Show(
                        "للأسف، أسعار الصرف غير متاحة في التطبيق حالياً.\n" +
                        "سوف يتم تطويرها في النسخ القادمة.",
                        "تنبيه — تغيير العملة",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
            }
        }
    }

    public bool DarkMode
    {
        get => _darkMode;
        set
        {
            if (SetProperty(ref _darkMode, value))
                ApplyTheme(value);
        }
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    // ── Commands ──────────────────────────────────────────────────
    public ICommand SaveCommand { get; }

    // ── Backup frequency helpers (for RadioButton binding) ────────
    public bool IsDaily   { get => BackupFrequency == "Daily";   set { if (value) BackupFrequency = "Daily"; } }
    public bool IsWeekly  { get => BackupFrequency == "Weekly";  set { if (value) BackupFrequency = "Weekly"; } }
    public bool IsMonthly { get => BackupFrequency == "Monthly"; set { if (value) BackupFrequency = "Monthly"; } }
    public bool IsYearly  { get => BackupFrequency == "Yearly";  set { if (value) BackupFrequency = "Yearly"; } }

    // Override SetProperty on BackupFrequency to also notify helpers
    private new bool SetProperty<T>(ref T field, T value, [System.Runtime.CompilerServices.CallerMemberName] string? propertyName = null)
    {
        if (!base.SetProperty(ref field, value, propertyName)) return false;
        if (propertyName == nameof(BackupFrequency))
        {
            OnPropertyChanged(nameof(IsDaily));
            OnPropertyChanged(nameof(IsWeekly));
            OnPropertyChanged(nameof(IsMonthly));
            OnPropertyChanged(nameof(IsYearly));
        }
        return true;
    }

    // ── Currency helpers (for RadioButton binding) ─────────────────
    public bool IsYer { get => Currency == "YER"; set { if (value) Currency = "YER"; } }
    public bool IsSar { get => Currency == "SAR"; set { if (value) Currency = "SAR"; } }
    public bool IsUsd { get => Currency == "USD"; set { if (value) Currency = "USD"; } }

    // ── Private ───────────────────────────────────────────────────
    private void SaveSettings()
    {
        _settingsService.Current.BackupFrequency = BackupFrequency;
        _settingsService.Current.Currency        = Currency;
        _settingsService.Current.DarkMode        = DarkMode;
        _settingsService.Save();

        StatusMessage = $"✅ تم حفظ الإعدادات بنجاح — العملة: {Currency} | النسخ الاحتياطي: {GetFrequencyLabel()}";
    }

    private string GetFrequencyLabel() => BackupFrequency switch
    {
        "Daily"   => "يومي",
        "Weekly"  => "أسبوعي",
        "Monthly" => "شهري",
        "Yearly"  => "سنوي",
        _         => BackupFrequency
    };

    private static void ApplyTheme(bool dark)
    {
        // Placeholder — full dark theme requires ResourceDictionary swap
        // This is called whenever dark mode is toggled so future implementation can hook here
        _ = dark;
    }

    public Task InitializeAsync()
    {
        // Re-load in case settings changed from another path
        _backupFrequency = _settingsService.Current.BackupFrequency;
        _currency        = _settingsService.Current.Currency;
        _darkMode        = _settingsService.Current.DarkMode;
        OnPropertyChanged(nameof(BackupFrequency));
        OnPropertyChanged(nameof(Currency));
        OnPropertyChanged(nameof(DarkMode));
        OnPropertyChanged(nameof(IsDaily));
        OnPropertyChanged(nameof(IsWeekly));
        OnPropertyChanged(nameof(IsMonthly));
        OnPropertyChanged(nameof(IsYearly));
        OnPropertyChanged(nameof(IsYer));
        OnPropertyChanged(nameof(IsSar));
        OnPropertyChanged(nameof(IsUsd));
        return Task.CompletedTask;
    }
}

// ── Simple option models ──────────────────────────────────────────
public record FrequencyOption(string Key, string Label);
public record CurrencyOption(string Code, string Name, string Symbol);
