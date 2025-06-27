using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace FileUpdaterClient;

public class MainViewModel : INotifyPropertyChanged
{
    private string _title = Settings.Title;
    private string _subTitle = Settings.Subtitle;
    private string _titleColor = Settings.TitleColor;
    private string _errorMessage = string.Empty;
    private string _subtitleColor = Settings.SubtitleColor;
    private double _progress;
    private string _progressText = "Checking for updates..";
    public event PropertyChangedEventHandler? PropertyChanged;

    public string TitleColor
    {
        get => _titleColor;
        set => SetField(ref _titleColor, value);
    }

    public string Title
    {
        get => _title;
        set => SetField(ref _title, value);
    }

    public string SubtitleColor
    {
        get => _subtitleColor;
        set => SetField(ref _subtitleColor, value);
    }

    public string Subtitle
    {
        get => _subTitle;
        set => SetField(ref _subTitle, value);
    }

    public string ErrorMessage
    {
        get => _errorMessage;
        set => SetField(ref _errorMessage, value);
    }

    public double Progress
    {
        get => _progress;
        set => SetField(ref _progress, value);
    }

    public string ProgressText
    {
        get => _progressText;
        set => SetField(ref _progressText, value);
    }

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
 
    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}