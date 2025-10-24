using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using System;
using System.Reactive;

namespace FullCrisis3;

public interface IGamepadNavigable
{
    bool HandleGamepadInput(string input);
}

[AutoLog]
public partial class MainWindow : Window
{
    private Button[] _quitDialogButtons = Array.Empty<Button>();
    private int _quitDialogSelectedIndex = 0;
    private readonly InputManager _quitDialogInputManager = new();

    public MainWindow()
    {
        InitializeComponent();
        var viewModel = new MainWindowViewModel();
        viewModel.QuitDialogNavigation = HandleGamepadQuitDialogNavigation;
        DataContext = viewModel;
        Loaded += (s, e) => SetupQuitDialogButtons();
        Loaded += this.OnWindowLoaded;
        
        // Configure window manager hints for better tiling WM support
        ConfigureWindowManagerHints();
        
        // Watch for quit dialog visibility changes to set initial focus
        viewModel.PropertyChanged += (s, e) => 
        {
            if (e.PropertyName == nameof(MainWindowViewModel.IsQuitDialogVisible))
            {
                if (viewModel.IsQuitDialogVisible)
                {
                    SetInitialQuitDialogFocus();
                }
                else
                {
                    RestoreFocusAfterQuitDialog();
                }
            }
            else if (e.PropertyName == nameof(MainWindowViewModel.CurrentBackgroundTheme))
            {
                UpdateBackgroundTheme(viewModel.CurrentBackgroundTheme);
            }
        };
    }

    private void ConfigureWindowManagerHints()
    {
        // Use CLI arguments for window configuration
        var args = GlobalArgs.Current;
        
        // Window size from CLI arguments
        Width = args.Width;
        Height = args.Height;
        
        // Window mode based on CLI arguments
        if (args.Fullscreen)
        {
            // Remove size constraints for fullscreen
            ClearValue(WidthProperty);
            ClearValue(HeightProperty);
            ClearValue(MinWidthProperty);
            ClearValue(MinHeightProperty);
            ClearValue(MaxWidthProperty);
            ClearValue(MaxHeightProperty);
            WindowState = Avalonia.Controls.WindowState.FullScreen;
        }
        else /*if (args.Windowed || args.DebugUI)*/
        {
            WindowState = Avalonia.Controls.WindowState.Normal;
            CanResize = true; // Default allows resizing at all times
        }
        
        WindowStartupLocation = Avalonia.Controls.WindowStartupLocation.CenterScreen;
        
        // These properties are already set in XAML but ensuring they're applied:
        // - Fixed dimensions (1280x720)
        // - CanResize=False 
        // - SystemDecorations=Full (shows title bar for better WM integration)
        // - SizeToContent=Manual (prevents auto-sizing)
        
        // Note: Tiling WM users can still override these with window rules like:
        // i3/sway: for_window [title="Full Crisis 3"] floating enable
        // or: for_window [class="FullCrisis3"] floating enable
    }

    private void OnWindowLoaded(object? sender, RoutedEventArgs e)
    {
        var args = GlobalArgs.Current;

        if (!args.Fullscreen)
        {
            // Clear constraints
            ClearValue(MinWidthProperty);
            ClearValue(MinHeightProperty);
            ClearValue(MaxWidthProperty);
            ClearValue(MaxHeightProperty);
        }
    }

    private void SetupQuitDialogButtons()
    {
        _quitDialogButtons = new[]
        {
            this.FindControl<Button>("QuitButton")!,
            this.FindControl<Button>("KeepPlayingButton")!
        };

        // Setup InputManager for quit dialog
        _quitDialogInputManager.ClearSelectables();
        
        for (int i = 0; i < _quitDialogButtons.Length; i++)
        {
            var index = i;
            var button = _quitDialogButtons[i];
            
            _quitDialogInputManager.RegisterSelectable(
                button, 
                tabIndex: i,
                onSelected: item => _quitDialogSelectedIndex = index
            );
        }
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (DataContext is MainWindowViewModel vm)
        {
            if (vm.IsQuitDialogVisible)
            {
                HandleQuitDialogNavigation(e);
            }
            else if (e.Key == Key.Escape)
            {
                vm.HandleEscapeKey();
                e.Handled = true;
            }
        }
        base.OnKeyDown(e);
    }

    private void HandleQuitDialogNavigation(KeyEventArgs e)
    {
        // Handle escape key specifically
        if (e.Key == Key.Escape)
        {
            if (DataContext is MainWindowViewModel vm)
            {
                vm.CancelQuitCommand.Execute(Unit.Default);
                e.Handled = true;
            }
            return;
        }

        // Let InputManager handle all other navigation
        if (_quitDialogInputManager.HandleKeyInput(e))
        {
            // InputManager handled the key
            return;
        }
    }

    private void SetInitialQuitDialogFocus()
    {
        if (_quitDialogButtons.Length > 0)
        {
            _quitDialogSelectedIndex = 0; // Start with first button (Quit)
            _quitDialogInputManager.SelectItem(0);
        }
    }

    private void RestoreFocusAfterQuitDialog()
    {
        // Try to focus the quit button in the main menu after a short delay
        // This allows the UI to update and the MainMenuView to be available
        if (DataContext is MainWindowViewModel vm && vm.CurrentView is MainMenuViewModel mainMenuVM)
        {
            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                // Set focus back to quit button using the callback we added to MainMenuViewModel
                if (mainMenuVM.RestoreFocusToQuit != null)
                {
                    mainMenuVM.RestoreFocusToQuit.Invoke();
                }
            }, Avalonia.Threading.DispatcherPriority.Background);
        }
    }

    private void HandleGamepadQuitDialogNavigation(string input)
    {
        _quitDialogInputManager.HandleGamepadInput(input);
    }
    
    private void UpdateBackgroundTheme(BackgroundThemeConfig? newTheme)
    {
        if (newTheme == null) return;
        
        // Find the AnimatedBackground control
        var animatedBackground = this.FindLogicalDescendantOfType<AnimatedBackground>();
        if (animatedBackground != null)
        {
            animatedBackground.SetTheme(newTheme);
        }
    }
}

[AutoLog]
public partial class MainMenuView : UserControl, IGamepadNavigable
{
    private Button[] _buttons = Array.Empty<Button>();
    private int _selectedIndex = 0;
    private readonly InputManager _inputManager = new();

    public MainMenuView()
    {
        InitializeComponent();
        Loaded += (s, e) => SetupButtons();
        KeyDown += OnKeyDown;
        
        // Wire up the focus restoration callback
        DataContextChanged += (s, e) =>
        {
            if (DataContext is MainMenuViewModel vm)
            {
                vm.RestoreFocusToQuit = FocusQuitButton;
            }
        };
    }

    private void SetupButtons()
    {
        _buttons = new[]
        {
            this.FindControl<Button>("NewGameButton")!,
            this.FindControl<Button>("LoadGameButton")!,
            this.FindControl<Button>("SettingsButton")!,
            this.FindControl<Button>("QuitButton")!
        };

        // Setup InputManager for main menu
        _inputManager.ClearSelectables();
        
        for (int i = 0; i < _buttons.Length; i++)
        {
            var index = i;
            var button = _buttons[i];
            
            _inputManager.RegisterSelectable(
                button, 
                tabIndex: i,
                onSelected: item => _selectedIndex = index
            );
        }

        if (_buttons.Length > 0) _inputManager.SelectItem(0);
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        _inputManager.HandleKeyInput(e);
    }

    public void FocusQuitButton()
    {
        if (_buttons.Length > 3) // Quit button is at index 3
        {
            _selectedIndex = 3;
            _inputManager.SelectItem(3);
        }
    }

    public bool HandleGamepadInput(string input)
    {
        return _inputManager.HandleGamepadInput(input);
    }
}

[AutoLog]
public partial class SubMenuView : UserControl
{
    public SubMenuView() 
    { 
        InitializeComponent(); 
    }
}

[AutoLog]
public partial class SettingsView : UserControl, IGamepadNavigable
{
    private readonly InputManager _inputManager = new();

    public SettingsView()
    {
        InitializeComponent();
        Loaded += (s, e) => SetupControls();
        KeyDown += OnKeyDown;
    }

    private void SetupControls()
    {
        var controls = new Control[]
        {
            this.FindControl<CheckBox>("AudioEnabledCheckBox")!,
            this.FindControl<CheckBox>("BackgroundMusicCheckBox")!,
            this.FindControl<Button>("BackButton")!
        };

        _inputManager.ClearSelectables();
        
        for (int i = 0; i < controls.Length; i++)
        {
            _inputManager.RegisterSelectable(controls[i], tabIndex: i);
        }

        if (controls.Length > 0) _inputManager.SelectItem(0);
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        _inputManager.HandleKeyInput(e);
    }

    public bool HandleGamepadInput(string input)
    {
        return _inputManager.HandleGamepadInput(input);
    }
}

[AutoLog]
public partial class LoadGameView : UserControl, IGamepadNavigable
{
    private readonly InputManager _inputManager = new();
    private LoadGameViewModel? _viewModel;
    
    public LoadGameView()
    {
        InitializeComponent();
        Loaded += (s, e) => SetupControls();
        KeyDown += OnKeyDown;
        DataContextChanged += OnDataContextChanged;
    }
    
    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        _viewModel = DataContext as LoadGameViewModel;
    }
    
    private void SetupControls()
    {
        var controls = new Control[]
        {
            this.FindControl<ListBox>("SaveGamesList") ?? new ListBox(),
            this.FindControl<Button>("DeleteButton")!,
            this.FindControl<Button>("RefreshButton")!,
            this.FindControl<Button>("BackButton")!
        };
        
        _inputManager.ClearSelectables();
        
        for (int i = 0; i < controls.Length; i++)
        {
            if (controls[i] != null)
            {
                _inputManager.RegisterSelectable(controls[i], tabIndex: i);
            }
        }
        
        if (controls.Length > 0) _inputManager.SelectItem(0);
    }
    
    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        // Check if focus is on a text input element
        if (IsTextInputFocused())
        {
            // Let text input handle the key naturally, only handle navigation keys
            if (e.Key == Key.Tab || e.Key == Key.Enter || e.Key == Key.Escape)
            {
                _inputManager.HandleKeyInput(e);
            }
            return;
        }
        
        // Handle Enter key for loading selected save game
        if (e.Key == Key.Enter && _viewModel?.SelectedSave != null)
        {
            var saveGamesList = this.FindControl<ListBox>("SaveGamesList");
            if (saveGamesList?.IsFocused == true)
            {
                _viewModel.PlaySaveCommand.Execute(_viewModel.SelectedSave);
                e.Handled = true;
                return;
            }
        }
        
        _inputManager.HandleKeyInput(e);
    }
    
    private bool IsTextInputFocused()
    {
        var focusedElement = TopLevel.GetTopLevel(this)?.FocusManager?.GetFocusedElement();
        return focusedElement is TextBox || focusedElement is ComboBox;
    }
    
    public bool HandleGamepadInput(string input)
    {
        // Handle A button (Confirm) for loading selected save game
        if (input == "Confirm" && _viewModel?.SelectedSave != null)
        {
            var saveGamesList = this.FindControl<ListBox>("SaveGamesList");
            if (saveGamesList?.IsFocused == true)
            {
                _viewModel.PlaySaveCommand.Execute(_viewModel.SelectedSave);
                return true;
            }
        }
        
        return _inputManager.HandleGamepadInput(input);
    }
    
    private void SaveGameButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is SaveGameData saveData && _viewModel != null)
        {
            _viewModel.PlaySaveCommand.Execute(saveData);
        }
    }
}
