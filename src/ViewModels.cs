using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Reactive;
using Avalonia.Threading;
using Avalonia.LogicalTree;

namespace FullCrisis3;

public class ViewModelBase : ReactiveObject { }

[AutoLog]
public class MainMenuViewModel : ViewModelBase
{
    public ReactiveCommand<Unit, Unit> NewGameCommand { get; }
    public ReactiveCommand<Unit, Unit> LoadGameCommand { get; }
    public ReactiveCommand<Unit, Unit> SettingsCommand { get; }
    public ReactiveCommand<Unit, Unit> QuitCommand { get; }

    public Action<string>? NavigateToSubMenu { get; set; }
    public Action? ShowQuitDialog { get; set; }
    public Action? RestoreFocusToQuit { get; set; }

    public MainMenuViewModel()
    {
        NewGameCommand = ReactiveCommand.Create(() => NavigateToSubMenu?.Invoke("New Game"));
        LoadGameCommand = ReactiveCommand.Create(() => NavigateToSubMenu?.Invoke("Load Game"));
        SettingsCommand = ReactiveCommand.Create(() => NavigateToSubMenu?.Invoke("Settings"));
        QuitCommand = ReactiveCommand.Create(() => ShowQuitDialog?.Invoke());
    }
}

[AutoLog]
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

[AutoLog]
public class SettingsViewModel : ViewModelBase
{
    private readonly AppSettings _settings;
    private bool _audioEnabled;
    private bool _backgroundMusicEnabled;

    public bool AudioEnabled
    {
        get => _audioEnabled;
        set
        {
            this.RaiseAndSetIfChanged(ref _audioEnabled, value);
            _settings.AudioEnabled = value;
            _settings.Save();
        }
    }

    public bool BackgroundMusicEnabled
    {
        get => _backgroundMusicEnabled;
        set
        {
            this.RaiseAndSetIfChanged(ref _backgroundMusicEnabled, value);
            _settings.BackgroundMusicEnabled = value;
            _settings.Save();
        }
    }

    public string ControllerName 
    { 
        get 
        {
            Logger.LogMethod("SettingsViewModel.ControllerName", "Getting controller name...");
            var name = SimpleGamepadInput.GetControllerName();
            Logger.LogMethod("SettingsViewModel.ControllerName", $"Controller name result: {name}");
            return name;
        }
    }
    public string AppDataLocation => AppSettings.AppDataLocation;

    public ReactiveCommand<Unit, Unit>? BackCommand { get; set; }

    public SettingsViewModel()
    {
        _settings = AppSettings.Load();
        _audioEnabled = _settings.AudioEnabled;
        _backgroundMusicEnabled = _settings.BackgroundMusicEnabled;
    }

    public void RefreshControllerName()
    {
        this.RaisePropertyChanged(nameof(ControllerName));
    }
}

[AutoLog]
public class MainWindowViewModel : ViewModelBase
{
    private readonly Stack<ViewModelBase> _viewStack = new();
    private ViewModelBase? _currentView;
    private bool _isQuitDialogVisible;
    private SimpleGamepadInput? _gamepadInput;

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
    
    public Action<string>? QuitDialogNavigation { get; set; }

    public MainWindowViewModel()
    {
        var mainMenuViewModel = new MainMenuViewModel();
        mainMenuViewModel.NavigateToSubMenu = NavigateToSubMenu;
        mainMenuViewModel.ShowQuitDialog = () => IsQuitDialogVisible = true;
        CurrentView = mainMenuViewModel;

        ConfirmQuitCommand = ReactiveCommand.Create(() => { Environment.Exit(0); });
        CancelQuitCommand = ReactiveCommand.Create(() => { IsQuitDialogVisible = false; });

        Logger.LogMethod("MainWindowViewModel Constructor", "About to initialize gamepad input...");
        Dispatcher.UIThread.Post(() => 
        {
            Logger.LogMethod("MainWindowViewModel Constructor", "Dispatcher callback executing - creating SimpleGamepadInput...");
            _gamepadInput = new SimpleGamepadInput(HandleInput, OnGamepadConnectionChanged);
            Logger.LogMethod("MainWindowViewModel Constructor", "SimpleGamepadInput created successfully");
        }, DispatcherPriority.Background);
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
        
        if (menuType == "Settings")
        {
            CurrentView = new SettingsViewModel
            {
                BackCommand = ReactiveCommand.Create(() => { CurrentView = _viewStack.Count > 0 ? _viewStack.Pop() : CurrentView; })
            };
        }
        else
        {
            CurrentView = new SubMenuViewModel
            {
                Title = menuType,
                ContentText = $"Content area for {menuType}",
                BackCommand = ReactiveCommand.Create(() => { CurrentView = _viewStack.Count > 0 ? _viewStack.Pop() : CurrentView; })
            };
        }
    }

    private void OnGamepadConnectionChanged(bool isConnected)
    {
        // Update settings display when gamepad is connected/disconnected
        Dispatcher.UIThread.Post(() =>
        {
            if (CurrentView is SettingsViewModel settingsVM)
            {
                // Force refresh of controller name property
                settingsVM.RefreshControllerName();
            }
        });
    }

    private void HandleInput(string input)
    {
        Dispatcher.UIThread.Post(() =>
        {
            if (IsQuitDialogVisible)
            {
                switch (input)
                {
                    case "Left":
                    case "Right":
                        QuitDialogNavigation?.Invoke(input);
                        break;
                    case "Confirm":
                        QuitDialogNavigation?.Invoke("Confirm");
                        break;
                    case "Cancel":
                        CancelQuitCommand.Execute(Unit.Default);
                        break;
                }
            }
            else
            {
                // Try to forward to current view first
                bool handled = false;
                if (CurrentView != null)
                {
                    // Find the associated UserControl for the current view model
                    var gamepadNavigableView = FindGamepadNavigableView();
                    if (gamepadNavigableView != null)
                    {
                        handled = gamepadNavigableView.HandleGamepadInput(input);
                    }
                }

                // If not handled by view, handle global actions
                if (!handled)
                {
                    switch (input)
                    {
                        case "Cancel": 
                            HandleEscapeKey(); 
                            break;
                    }
                }
            }
        });
    }

    private IGamepadNavigable? FindGamepadNavigableView()
    {
        // This is a simple approach - in a more complex app you might want
        // to maintain a reference to the current view control
        return CurrentView switch
        {
            MainMenuViewModel => FindControlInWindow<MainMenuView>(),
            SettingsViewModel => FindControlInWindow<SettingsView>(),
            _ => null
        };
    }

    private T? FindControlInWindow<T>() where T : class, IGamepadNavigable
    {
        // This is a workaround to find the view associated with the view model
        // In practice, you might want to pass the view reference through the view model
        if (Avalonia.Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
        {
            var mainWindow = desktop.MainWindow as MainWindow;
            return mainWindow?.FindLogicalDescendantOfType<T>();
        }
        return null;
    }
}
