using Avalonia.Controls;
using Avalonia.Input;
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
        
        // Watch for quit dialog visibility changes to set initial focus
        viewModel.PropertyChanged += (s, e) => 
        {
            if (e.PropertyName == nameof(MainWindowViewModel.IsQuitDialogVisible) && viewModel.IsQuitDialogVisible)
            {
                SetInitialQuitDialogFocus();
            }
        };
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
}

[AutoLog]
public partial class SubMenuView : UserControl
{
    public SubMenuView() 
    { 
        InitializeComponent(); 
    }
}