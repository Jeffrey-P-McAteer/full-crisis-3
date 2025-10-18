using Avalonia.Controls;
using Avalonia.Input;
using System;
using System.Linq;
using FullCrisis3.Core.Input;
using FullCrisis3.Core.ViewModels;

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
        DataContextChanged += OnDataContextChanged;
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

        // Set up mouse enter/leave events for focus management
        for (int i = 0; i < _menuButtons.Length; i++)
        {
            var index = i; // Capture for closure
            _menuButtons[i].PointerEntered += (s, e) => {
                _selectedIndex = index;
                _menuButtons[_selectedIndex].Focus();
            };
        }

        // Focus the first button
        if (_menuButtons.Length > 0)
        {
            _selectedIndex = 0;
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

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        // Subscribe to gamepad input through the parent window's ViewModel
        if (this.Parent is ContentControl && 
            ((ContentControl)this.Parent).DataContext is MainWindowViewModel windowViewModel)
        {
            // Get the gamepad input service indirectly through reflection or a public property
            // For now, we'll handle gamepad input through the existing HandleGamepadInput method
            // in MainWindowViewModel which will update focus appropriately
        }
    }
}