using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.LogicalTree;
using System;
using System.Reactive;

namespace FullCrisis3;

[AutoLog]
public partial class MainWindow : Window
{
    private Button[] _quitDialogButtons = Array.Empty<Button>();
    private int _quitDialogSelectedIndex = 0;

    public MainWindow()
    {
        InitializeComponent();
        var viewModel = new MainWindowViewModel();
        viewModel.QuitDialogNavigation = HandleGamepadQuitDialogNavigation;
        DataContext = viewModel;
        Loaded += (s, e) => SetupQuitDialogButtons();
        
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
        };
    }

    private void ConfigureWindowManagerHints()
    {
        // Set window properties that help tiling window managers understand
        // this is a game/media application that should float
        
        // Fixed size window - hints to tiling WMs that this shouldn't be tiled
        CanResize = false;
        
        // Keep window on top for gaming experience (can be toggled by user if needed)
        // Topmost = true; // Commented out - too aggressive for most users
        
        // Additional hints through window startup location and size constraints
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

    private void SetupQuitDialogButtons()
    {
        _quitDialogButtons = new[]
        {
            this.FindControl<Button>("QuitButton")!,
            this.FindControl<Button>("KeepPlayingButton")!
        };

        for (int i = 0; i < _quitDialogButtons.Length; i++)
        {
            var index = i;
            _quitDialogButtons[i].PointerEntered += (s, e) => { _quitDialogSelectedIndex = index; _quitDialogButtons[_quitDialogSelectedIndex].Focus(); };
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
        if (_quitDialogButtons.Length == 0) return;

        switch (e.Key)
        {
            case Key.Left or Key.A:
                _quitDialogSelectedIndex = (_quitDialogSelectedIndex - 1 + _quitDialogButtons.Length) % _quitDialogButtons.Length;
                _quitDialogButtons[_quitDialogSelectedIndex].Focus();
                e.Handled = true;
                break;
            case Key.Right or Key.D:
                _quitDialogSelectedIndex = (_quitDialogSelectedIndex + 1) % _quitDialogButtons.Length;
                _quitDialogButtons[_quitDialogSelectedIndex].Focus();
                e.Handled = true;
                break;
            case Key.Enter or Key.Space:
                _quitDialogButtons[_quitDialogSelectedIndex].Command?.Execute(null);
                e.Handled = true;
                break;
            case Key.Escape:
                if (DataContext is MainWindowViewModel vm)
                {
                    vm.CancelQuitCommand.Execute(Unit.Default);
                    e.Handled = true;
                }
                break;
        }
    }

    private void SetInitialQuitDialogFocus()
    {
        if (_quitDialogButtons.Length > 0)
        {
            _quitDialogSelectedIndex = 0; // Start with first button (Quit)
            _quitDialogButtons[_quitDialogSelectedIndex].Focus();
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
        if (_quitDialogButtons.Length == 0) return;

        switch (input)
        {
            case "Left":
                _quitDialogSelectedIndex = (_quitDialogSelectedIndex - 1 + _quitDialogButtons.Length) % _quitDialogButtons.Length;
                _quitDialogButtons[_quitDialogSelectedIndex].Focus();
                break;
            case "Right":
                _quitDialogSelectedIndex = (_quitDialogSelectedIndex + 1) % _quitDialogButtons.Length;
                _quitDialogButtons[_quitDialogSelectedIndex].Focus();
                break;
            case "Confirm":
                _quitDialogButtons[_quitDialogSelectedIndex].Command?.Execute(null);
                break;
        }
    }
}

[AutoLog]
public partial class MainMenuView : UserControl
{
    private Button[] _buttons = Array.Empty<Button>();
    private int _selectedIndex = 0;

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

        for (int i = 0; i < _buttons.Length; i++)
        {
            var index = i;
            _buttons[i].PointerEntered += (s, e) => { _selectedIndex = index; _buttons[_selectedIndex].Focus(); };
        }

        if (_buttons.Length > 0) _buttons[0].Focus();
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (_buttons.Length == 0) return;

        switch (e.Key)
        {
            case Key.Up or Key.W:
                _selectedIndex = (_selectedIndex - 1 + _buttons.Length) % _buttons.Length;
                _buttons[_selectedIndex].Focus();
                e.Handled = true;
                break;
            case Key.Down or Key.S:
                _selectedIndex = (_selectedIndex + 1) % _buttons.Length;
                _buttons[_selectedIndex].Focus();
                e.Handled = true;
                break;
            case Key.Enter or Key.Space:
                _buttons[_selectedIndex].Command?.Execute(null);
                e.Handled = true;
                break;
        }
    }

    public void FocusQuitButton()
    {
        if (_buttons.Length > 3) // Quit button is at index 3
        {
            _selectedIndex = 3;
            _buttons[_selectedIndex].Focus();
        }
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