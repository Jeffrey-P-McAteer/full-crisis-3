using Avalonia.Controls;
using Avalonia.Input;
using FullCrisis3.Core.ViewModels;

namespace FullCrisis3.Desktop.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainWindowViewModel();
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (DataContext is MainWindowViewModel viewModel)
        {
            if (e.Key == Key.Escape)
            {
                viewModel.HandleEscapeKey();
                e.Handled = true;
                return;
            }
        }
        
        base.OnKeyDown(e);
    }
}