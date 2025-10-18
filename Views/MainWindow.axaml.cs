using Avalonia.Controls;
using Avalonia.Input;
using FullCrisis3.ViewModels;
using System;
using System.Linq;
using Avalonia.Interactivity;

namespace FullCrisis3.Views;

public partial class MainWindow : Window
{
    private MainWindowViewModel? _viewModel;

    public MainWindow()
    {
        InitializeComponent();
        _viewModel = new MainWindowViewModel();
        DataContext = _viewModel;
        
        // Set up gamepad action handlers
        SetupGamepadHandlers();
    }

    private void SetupGamepadHandlers()
    {
        if (_viewModel == null) return;

        _viewModel.GamepadConfirmAction = () =>
        {
            // For now, just trigger the focused button's command
            // This is a simplified approach that should work reliably
            System.Diagnostics.Debug.WriteLine("[GAMEPAD] Confirm action triggered");
        };

        _viewModel.GamepadNavigateAction = (direction) =>
        {
            // For now, just log the navigation attempt
            // The MainMenuView already handles keyboard navigation
            System.Diagnostics.Debug.WriteLine($"[GAMEPAD] Navigate {direction} triggered");
        };
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (DataContext is MainWindowViewModel viewModel)
        {
            if (e.Key == Key.Escape)
            {
                viewModel.HandleEscapeKey();
                e.Handled = true;
                return;
            }
        }
        
        base.OnKeyDown(e);
    }
}