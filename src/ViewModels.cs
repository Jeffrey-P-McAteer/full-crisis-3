using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Reactive;
using Avalonia.Threading;
using Avalonia.LogicalTree;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Controls;

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
    public Action<object>? NavigateToView { get; set; }
    public Action? ShowQuitDialog { get; set; }
    public Action? RestoreFocusToQuit { get; set; }

    public MainMenuViewModel()
    {
        NewGameCommand = ReactiveCommand.Create(() => {
            var newGameViewModel = new NewGameViewModel();
            newGameViewModel.NavigateToView = NavigateToView;
            NavigateToView?.Invoke(newGameViewModel);
        });
        LoadGameCommand = ReactiveCommand.Create(() => {
            var loadGameViewModel = new LoadGameViewModel();
            loadGameViewModel.NavigateToView = NavigateToView;
            NavigateToView?.Invoke(loadGameViewModel);
        });
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
    private BackgroundThemeConfig? _currentBackgroundTheme;

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
    
    public BackgroundThemeConfig? CurrentBackgroundTheme
    {
        get => _currentBackgroundTheme;
        private set => this.RaiseAndSetIfChanged(ref _currentBackgroundTheme, value);
    }

    public ReactiveCommand<Unit, Unit> ConfirmQuitCommand { get; }
    public ReactiveCommand<Unit, Unit> CancelQuitCommand { get; }
    
    public Action<string>? QuitDialogNavigation { get; set; }

    public MainWindowViewModel()
    {
        var mainMenuViewModel = new MainMenuViewModel();
        mainMenuViewModel.NavigateToSubMenu = NavigateToSubMenu;
        mainMenuViewModel.NavigateToView = NavigateToView;
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

    private void NavigateToView(object viewModel)
    {
        if (CurrentView != null) _viewStack.Push(CurrentView);
        
        if (viewModel is ViewModelBase vm)
        {
            CurrentView = vm;
            
            // Set up navigation for specific view models
            if (vm is NewGameViewModel newGameVM)
            {
                newGameVM.NavigateToView = NavigateToView;
            }
            else if (vm is LoadGameViewModel loadGameVM)
            {
                loadGameVM.NavigateToView = NavigateToView;
            }
            else if (vm is GameViewModel gameVM)
            {
                gameVM.NavigateToView = NavigateToView;
                CurrentBackgroundTheme = gameVM.BackgroundTheme;
                
                // Listen for theme changes
                gameVM.PropertyChanged += (s, e) =>
                {
                    if (e.PropertyName == nameof(GameViewModel.BackgroundTheme))
                    {
                        CurrentBackgroundTheme = gameVM.BackgroundTheme;
                    }
                };
            }
            else if (vm is MainMenuViewModel mainMenuVM)
            {
                // Reset navigation for main menu
                mainMenuVM.NavigateToSubMenu = NavigateToSubMenu;
                mainMenuVM.NavigateToView = NavigateToView;
                mainMenuVM.ShowQuitDialog = () => IsQuitDialogVisible = true;
                
                // Clear the view stack when returning to main menu
                _viewStack.Clear();
                
                // Reset to default theme
                CurrentBackgroundTheme = AnimatedBackground.BackgroundThemes.Cityscape;
            }
        }
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
                    case "LeftButton":
                    case "RightButton":
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
                // Convert gamepad inputs to keyboard equivalents for consistent PC-style navigation
                bool handled = false;
                switch (input)
                {
                    case "Left":
                        // Simulate Shift+Tab (navigate backward)
                        handled = SimulateTabNavigation(shift: true);
                        break;
                    case "Right":
                        // Simulate Tab (navigate forward)
                        handled = SimulateTabNavigation(shift: false);
                        break;
                    case "LeftButton":
                        // LB button - Simulate Shift+Tab (navigate backward)
                        Logger.Debug("LeftButton (LB) pressed - simulating Shift+Tab navigation");
                        handled = SimulateTabNavigation(shift: true);
                        break;
                    case "RightButton":
                        // RB button - Simulate Tab (navigate forward)
                        Logger.Debug("RightButton (RB) pressed - simulating Tab navigation");
                        handled = SimulateTabNavigation(shift: false);
                        break;
                    case "Confirm":
                        // A button - Simulate Enter key
                        Logger.Debug("Confirm (A button) pressed - simulating Enter key");
                        handled = SimulateEnterKey();
                        break;
                }

                // If not handled by Tab/Enter simulation, try to forward to current view
                if (!handled && CurrentView != null)
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
                            // B button - Handle as Escape key
                            Logger.Debug("Cancel (B button) pressed - handling as Escape key");
                            HandleEscapeKey(); 
                            break;
                    }
                }
            }
        });
    }

    private bool SimulateTabNavigation(bool shift)
    {
        try
        {
            // Get the current focused element from the main window
            if (Avalonia.Application.Current?.ApplicationLifetime is not Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
                return false;
                
            var mainWindow = desktop.MainWindow;
            if (mainWindow == null) return false;
            
            var focusManager = mainWindow.FocusManager;
            if (focusManager == null) return false;

            var currentFocus = focusManager.GetFocusedElement();
            
            // If no element is focused, focus the first focusable element
            if (currentFocus == null)
            {
                Logger.Debug("SimulateTabNavigation: No element focused, attempting to focus first focusable element");
                
                // Try to move focus to the first focusable element
                var firstFocusable = FindFirstFocusableElement(mainWindow);
                if (firstFocusable != null)
                {
                    firstFocusable.Focus();
                    Logger.Debug($"SimulateTabNavigation: Focused first element: {firstFocusable.GetType().Name}");
                    return true;
                }
                
                Logger.Debug("SimulateTabNavigation: No focusable elements found");
                return false;
            }

            // Create a realistic Tab key event exactly as if Tab was pressed
            var keyEventArgs = new KeyEventArgs
            {
                Key = Key.Tab,
                KeyModifiers = shift ? KeyModifiers.Shift : KeyModifiers.None,
                RoutedEvent = InputElement.KeyDownEvent,
                Source = currentFocus
            };
            
            // Send the Tab key event to the focused element first, then let it bubble up
            if (currentFocus is InputElement inputElement)
            {
                inputElement.RaiseEvent(keyEventArgs);
                Logger.Debug($"SimulateTabNavigation: {(shift ? "Shift+Tab" : "Tab")} event sent to {currentFocus.GetType().Name}, handled: {keyEventArgs.Handled}");
                return keyEventArgs.Handled;
            }

            // Fallback: send to main window
            mainWindow.RaiseEvent(keyEventArgs);
            Logger.Debug($"SimulateTabNavigation: {(shift ? "Shift+Tab" : "Tab")} event sent to MainWindow, handled: {keyEventArgs.Handled}");
            return keyEventArgs.Handled;
        }
        catch (Exception ex)
        {
            Logger.Debug($"SimulateTabNavigation: Error simulating tab navigation: {ex.Message}");
        }
        
        return false;
    }

    private bool SimulateEnterKey()
    {
        try
        {
            // Get the current focused element from the main window
            if (Avalonia.Application.Current?.ApplicationLifetime is not Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
                return false;
                
            var mainWindow = desktop.MainWindow;
            if (mainWindow == null) return false;
            
            var focusManager = mainWindow.FocusManager;
            if (focusManager == null) return false;

            var currentFocus = focusManager.GetFocusedElement();
            
            // If no element is focused, focus the first focusable element
            if (currentFocus == null)
            {
                Logger.Debug("SimulateEnterKey: No element focused, attempting to focus first focusable element");
                
                // Try to move focus to the first focusable element
                var firstFocusable = FindFirstFocusableElement(mainWindow);
                if (firstFocusable != null)
                {
                    firstFocusable.Focus();
                    Logger.Debug($"SimulateEnterKey: Focused first element: {firstFocusable.GetType().Name}");
                    // Now simulate Enter on the newly focused element
                    currentFocus = firstFocusable;
                }
                else
                {
                    Logger.Debug("SimulateEnterKey: No focusable elements found");
                    return false;
                }
            }

            // Create a realistic Enter key event exactly as if Enter was pressed
            var keyEventArgs = new KeyEventArgs
            {
                Key = Key.Enter,
                KeyModifiers = KeyModifiers.None,
                RoutedEvent = InputElement.KeyDownEvent,
                Source = currentFocus
            };
            
            // Send the Enter key event to the focused element first, then let it bubble up
            if (currentFocus is InputElement inputElement)
            {
                inputElement.RaiseEvent(keyEventArgs);
                Logger.Debug($"SimulateEnterKey: Enter event sent to {currentFocus.GetType().Name}, handled: {keyEventArgs.Handled}");
                return keyEventArgs.Handled;
            }

            // Fallback: send to main window
            mainWindow.RaiseEvent(keyEventArgs);
            Logger.Debug($"SimulateEnterKey: Enter event sent to MainWindow, handled: {keyEventArgs.Handled}");
            return keyEventArgs.Handled;
        }
        catch (Exception ex)
        {
            Logger.Debug($"SimulateEnterKey: Error simulating enter key: {ex.Message}");
        }
        
        return false;
    }

    private Control? FindFirstFocusableElement(Window window)
    {
        try
        {
            // Recursively search for the first focusable control in the visual tree
            return FindFirstFocusableElementRecursive(window);
        }
        catch (Exception ex)
        {
            Logger.Debug($"FindFirstFocusableElement: Error finding focusable element: {ex.Message}");
            return null;
        }
    }

    private Control? FindFirstFocusableElementRecursive(Control parent)
    {
        // Check if the current control is focusable
        if (parent.Focusable && parent.IsEnabled && parent.IsVisible && parent.IsEffectivelyVisible)
        {
            // Prefer interactive controls
            if (parent is Button or TextBox or ComboBox or CheckBox or ListBox)
            {
                return parent;
            }
        }

        // Search children using logical tree
        if (parent.GetLogicalChildren() != null)
        {
            foreach (var child in parent.GetLogicalChildren())
            {
                if (child is Control childControl)
                {
                    var focusable = FindFirstFocusableElementRecursive(childControl);
                    if (focusable != null)
                    {
                        return focusable;
                    }
                }
            }
        }

        // If no interactive control found, return the first focusable control
        if (parent.Focusable && parent.IsEnabled && parent.IsVisible && parent.IsEffectivelyVisible)
        {
            return parent;
        }

        return null;
    }

    private IGamepadNavigable? FindGamepadNavigableView()
    {
        // This is a simple approach - in a more complex app you might want
        // to maintain a reference to the current view control
        return CurrentView switch
        {
            MainMenuViewModel => FindControlInWindow<MainMenuView>(),
            SettingsViewModel => FindControlInWindow<SettingsView>(),
            NewGameViewModel => FindControlInWindow<NewGameView>(),
            LoadGameViewModel => FindControlInWindow<LoadGameView>(),
            GameViewModel => FindControlInWindow<GameView>(),
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
