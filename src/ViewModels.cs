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
        NewGameCommand = ReactiveCommand.Create(() => NavigateToSubMenu?.Invoke("New Game"));
        LoadGameCommand = ReactiveCommand.Create(() => NavigateToSubMenu?.Invoke("Load Game"));
        SettingsCommand = ReactiveCommand.Create(() => NavigateToSubMenu?.Invoke("Settings"));
        QuitCommand = ReactiveCommand.Create(() => ShowQuitDialog?.Invoke());
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
        var mainMenuViewModel = new MainMenuViewModel();
        mainMenuViewModel.NavigateToSubMenu = NavigateToSubMenu;
        mainMenuViewModel.ShowQuitDialog = () => IsQuitDialogVisible = true;
        CurrentView = mainMenuViewModel;

        ConfirmQuitCommand = ReactiveCommand.Create(() => { Environment.Exit(0); });
        CancelQuitCommand = ReactiveCommand.Create(() => { IsQuitDialogVisible = false; });

        Dispatcher.UIThread.Post(() => _gamepadInput = new GamepadInput(HandleInput), DispatcherPriority.Background);
    }

    public void HandleEscapeKey()
    {
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
        if (CurrentView != null) _viewStack.Push(CurrentView);
        CurrentView = new SubMenuViewModel
        {
            Title = menuType,
            ContentText = $"Content area for {menuType}",
            BackCommand = ReactiveCommand.Create(() => { CurrentView = _viewStack.Count > 0 ? _viewStack.Pop() : CurrentView; })
        };
    }

    private void HandleInput(string input)
    {
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