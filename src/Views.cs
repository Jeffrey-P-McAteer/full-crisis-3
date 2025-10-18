using Avalonia.Controls;
using Avalonia.Input;
using System;

namespace FullCrisis3;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        Logger.LogMethod();
        InitializeComponent();
        DataContext = new MainWindowViewModel();
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        Logger.LogMethod(nameof(OnKeyDown), e.Key.ToString());
        if (e.Key == Key.Escape && DataContext is MainWindowViewModel vm)
        {
            vm.HandleEscapeKey();
            e.Handled = true;
        }
        base.OnKeyDown(e);
    }
}

public partial class MainMenuView : UserControl
{
    private Button[] _buttons = Array.Empty<Button>();
    private int _selectedIndex = 0;

    public MainMenuView()
    {
        Logger.LogMethod();
        InitializeComponent();
        Loaded += (s, e) => SetupButtons();
        KeyDown += OnKeyDown;
    }

    private void SetupButtons()
    {
        Logger.LogMethod();
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
            _buttons[i].PointerEntered += (s, e) => { Logger.Debug($"Button {index} selected"); _selectedIndex = index; _buttons[_selectedIndex].Focus(); };
        }

        if (_buttons.Length > 0) _buttons[0].Focus();
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        Logger.LogMethod(nameof(OnKeyDown), e.Key.ToString());
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

public partial class SubMenuView : UserControl
{
    public SubMenuView() 
    { 
        Logger.LogMethod(); 
        InitializeComponent(); 
    }
}