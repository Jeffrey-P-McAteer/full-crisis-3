using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;

namespace FullCrisis3;

public partial class App : Application
{
    public override void Initialize() 
    { 
        Logger.LogMethod(); 
        AvaloniaXamlLoader.Load(this); 
    }

    public override void OnFrameworkInitializationCompleted()
    {
        Logger.LogMethod();
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            desktop.MainWindow = new MainWindow();
        base.OnFrameworkInitializationCompleted();
    }
}