using ReactiveUI;
using System;
using System.Reactive;

namespace FullCrisis3.ViewModels;

public class MainMenuViewModel : ViewModelBase
{
    public MainMenuViewModel()
    {
        NewGameCommand = ReactiveCommand.Create(() => NavigateToSubMenu?.Invoke("New Game"));
        LoadGameCommand = ReactiveCommand.Create(() => NavigateToSubMenu?.Invoke("Load Game"));
        SettingsCommand = ReactiveCommand.Create(() => NavigateToSubMenu?.Invoke("Settings"));
        QuitCommand = ReactiveCommand.Create(() => ShowQuitDialog?.Invoke());
    }

    public ReactiveCommand<Unit, Unit> NewGameCommand { get; }
    public ReactiveCommand<Unit, Unit> LoadGameCommand { get; }
    public ReactiveCommand<Unit, Unit> SettingsCommand { get; }
    public ReactiveCommand<Unit, Unit> QuitCommand { get; }

    // Navigation delegates
    public Action<string>? NavigateToSubMenu { get; set; }
    public Action? ShowQuitDialog { get; set; }
}