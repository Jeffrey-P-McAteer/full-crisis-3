using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Reactive;
using Avalonia.Threading;

namespace FullCrisis3;

public class ViewModelBase : ReactiveObject { }

public class MainMenuViewModel : ViewModelBase
{
    public ReactiveCommand<Unit, Unit> NewGameCommand { get; }
    public ReactiveCommand<Unit, Unit> LoadGameCommand { get; }
    public ReactiveCommand<Unit, Unit> SettingsCommand { get; }
    public ReactiveCommand<Unit, Unit> QuitCommand { get; }

    public Action<string>? NavigateToSubMenu { get; set; }
    public Action? ShowQuitDialog { get; set; }

    public MainMenuViewModel()
    {
        Logger.LogMethod();
        NewGameCommand = ReactiveCommand.Create(() => { Logger.LogMethod("NewGameCommand"); NavigateToSubMenu?.Invoke("New Game"); });
        LoadGameCommand = ReactiveCommand.Create(() => { Logger.LogMethod("LoadGameCommand"); NavigateToSubMenu?.Invoke("Load Game"); });
        SettingsCommand = ReactiveCommand.Create(() => { Logger.LogMethod("SettingsCommand"); NavigateToSubMenu?.Invoke("Settings"); });
        QuitCommand = ReactiveCommand.Create(() => { Logger.LogMethod("QuitCommand"); ShowQuitDialog?.Invoke(); });
    }
}

public class SubMenuViewModel : ViewModelBase
{
    private string _title = string.Empty;
    private string _contentText = string.Empty;

    public string Title
    {
        get => _title;
        set => this.RaiseAndSetIfChanged(ref _title, value);
    }

    public string ContentText
    {
        get => _contentText;
        set => this.RaiseAndSetIfChanged(ref _contentText, value);
    }

    public ReactiveCommand<Unit, Unit>? BackCommand { get; set; }
}

public class MainWindowViewModel : ViewModelBase
{
    private readonly Stack<ViewModelBase> _viewStack = new();
    private ViewModelBase? _currentView;
    private bool _isQuitDialogVisible;
    private GamepadInput? _gamepadInput;

    public ViewModelBase? CurrentView
    {
        get => _currentView;
        set => this.RaiseAndSetIfChanged(ref _currentView, value);
    }

    public bool IsQuitDialogVisible
    {
        get => _isQuitDialogVisible;
        set => this.RaiseAndSetIfChanged(ref _isQuitDialogVisible, value);
    }

    public ReactiveCommand<Unit, Unit> ConfirmQuitCommand { get; }
    public ReactiveCommand<Unit, Unit> CancelQuitCommand { get; }

    public MainWindowViewModel()
    {
        Logger.LogMethod();
        var mainMenuViewModel = new MainMenuViewModel();
        mainMenuViewModel.NavigateToSubMenu = NavigateToSubMenu;
        mainMenuViewModel.ShowQuitDialog = () => { Logger.LogMethod("ShowQuitDialog"); IsQuitDialogVisible = true; };
        CurrentView = mainMenuViewModel;

        ConfirmQuitCommand = ReactiveCommand.Create(() => { Logger.LogMethod("ConfirmQuit"); Environment.Exit(0); });
        CancelQuitCommand = ReactiveCommand.Create(() => { Logger.LogMethod("CancelQuit"); IsQuitDialogVisible = false; });

        Dispatcher.UIThread.Post(() => _gamepadInput = new GamepadInput(HandleInput), DispatcherPriority.Background);
    }

    public void HandleEscapeKey()
    {
        Logger.LogMethod();
        if (IsQuitDialogVisible)
        {
            IsQuitDialogVisible = false;
            return;
        }

        if (_viewStack.Count > 0)
            CurrentView = _viewStack.Pop();
        else
            IsQuitDialogVisible = true;
    }

    private void NavigateToSubMenu(string menuType)
    {
        Logger.LogMethod(nameof(NavigateToSubMenu), menuType);
        if (CurrentView != null) _viewStack.Push(CurrentView);
        CurrentView = new SubMenuViewModel
        {
            Title = menuType,
            ContentText = $"Content area for {menuType}",
            BackCommand = ReactiveCommand.Create(() => { Logger.LogMethod("BackCommand"); CurrentView = _viewStack.Count > 0 ? _viewStack.Pop() : CurrentView; })
        };
    }

    private void HandleInput(string input)
    {
        Logger.LogMethod(nameof(HandleInput), input);
        Dispatcher.UIThread.Post(() =>
        {
            switch (input)
            {
                case "Cancel": HandleEscapeKey(); break;
                case "Confirm" when IsQuitDialogVisible: Environment.Exit(0); break;
            }
        });
    }
}
