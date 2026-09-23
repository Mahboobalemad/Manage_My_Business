namespace ShopsManagement.UI.ViewModels;

public class MonthTabItemViewModel : ViewModelBase
{
    private bool _isSelected;

    public MonthTabItemViewModel(int monthNumber, string monthName)
    {
        MonthNumber = monthNumber;
        MonthName = monthName;
    }

    public int MonthNumber { get; }
    public string MonthName { get; }

    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }
}
