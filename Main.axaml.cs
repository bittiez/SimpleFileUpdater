using Avalonia.Controls;

namespace FileUpdaterClient;

public partial class Main : Window
{
    private MainViewModel _data;
    public Main()
    {
        InitializeComponent();
        DataContext = _data = new MainViewModel();
        Title = _data.Title;
        UpdateHandler.HandleUpdates(_data);
    }
} 