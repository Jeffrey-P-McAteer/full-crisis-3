using Avalonia.Controls;
using Avalonia.Input;
using System;
using System.Linq;

namespace FullCrisis3.Desktop.Views;

public partial class MainMenuView : UserControl
{
    private Button[] _menuButtons = Array.Empty<Button>();
    private int _selectedIndex = 0;

    public MainMenuView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        KeyDown += OnKeyDown;
    }

    private void OnLoaded(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        // Get all menu buttons
        _menuButtons = new[]
        {
            this.FindControl<Button>("NewGameButton")!,
            this.FindControl<Button>("LoadGameButton")!,
            this.FindControl<Button>("SettingsButton")!,
            this.FindControl<Button>("QuitButton")!
        };

        // Focus the first button
        if (_menuButtons.Length > 0)
        {
            _menuButtons[0].Focus();
        }
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (_menuButtons.Length == 0) return;

        switch (e.Key)
        {
            case Key.Up:
            case Key.W:
                NavigateUp();
                e.Handled = true;
                break;
                
            case Key.Down:
            case Key.S:
                NavigateDown();
                e.Handled = true;
                break;
                
            case Key.Enter:
            case Key.Space:
                ActivateSelectedButton();
                e.Handled = true;
                break;
        }
    }

    private void NavigateUp()
    {
        _selectedIndex = (_selectedIndex - 1 + _menuButtons.Length) % _menuButtons.Length;
        _menuButtons[_selectedIndex].Focus();
    }

    private void NavigateDown()
    {
        _selectedIndex = (_selectedIndex + 1) % _menuButtons.Length;
        _menuButtons[_selectedIndex].Focus();
    }

    private void ActivateSelectedButton()
    {
        if (_selectedIndex >= 0 && _selectedIndex < _menuButtons.Length)
        {
            _menuButtons[_selectedIndex].Command?.Execute(_menuButtons[_selectedIndex].CommandParameter);
        }
    }
}