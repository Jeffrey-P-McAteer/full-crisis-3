using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Reactive;
using System.Reactive.Linq;
using FullCrisis3.Input;
using Avalonia.Threading;

namespace FullCrisis3.ViewModels;

public class MainWindowViewModel : ViewModelBase
{
    private readonly Stack<ViewModelBase> _viewStack = new();
    private ViewModelBase? _currentView;
    private bool _isQuitDialogVisible;
    private GamepadInputService? _gamepadInput;

    public MainWindowViewModel()
    {

        // Initialize with main menu
        var mainMenuViewModel = new MainMenuViewModel();
        mainMenuViewModel.NavigateToSubMenu = NavigateToSubMenu;
        mainMenuViewModel.ShowQuitDialog = ShowQuitDialog;
        
        CurrentView = mainMenuViewModel;

        // Setup commands
        ConfirmQuitCommand = ReactiveCommand.Create(ConfirmQuit);
        CancelQuitCommand = ReactiveCommand.Create(CancelQuit);
        
        // Initialize gamepad input after a short delay to ensure UI is ready
        Dispatcher.UIThread.Post(() =>
        {
            InitializeGamepadInput();
        }, DispatcherPriority.Background);
    }

    private void InitializeGamepadInput()
    {
        try
        {
            _gamepadInput = new GamepadInputService();
            _gamepadInput.InputObservable
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(HandleGamepadInput);
            _gamepadInput.DebugObservable
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(debug => 
                {
                    System.Diagnostics.Debug.WriteLine($"[GAMEPAD DEBUG] {debug}");
                    Console.WriteLine($"[GAMEPAD DEBUG] {debug}");
                });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[GAMEPAD ERROR] Failed to initialize gamepad input: {ex.Message}");
            Console.WriteLine($"[GAMEPAD ERROR] Failed to initialize gamepad input: {ex.Message}");
        }
    }

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

    public void HandleEscapeKey()
    {
        if (IsQuitDialogVisible)
        {
            CancelQuit();
            return;
        }

        if (_viewStack.Count > 0)
        {
            GoBack();
        }
        else
        {
            ShowQuitDialog();
        }
    }

    private void NavigateToSubMenu(string menuType)
    {
        if (CurrentView != null)
        {
            _viewStack.Push(CurrentView);
        }

        var subMenuViewModel = new SubMenuViewModel
        {
            Title = menuType,
            ContentText = $"Content area for {menuType}",
            BackCommand = ReactiveCommand.Create(GoBack)
        };

        CurrentView = subMenuViewModel;
    }

    private void GoBack()
    {
        if (_viewStack.Count > 0)
        {
            CurrentView = _viewStack.Pop();
        }
    }

    private void ShowQuitDialog()
    {
        IsQuitDialogVisible = true;
    }

    private void ConfirmQuit()
    {
        Environment.Exit(0);
    }

    private void CancelQuit()
    {
        IsQuitDialogVisible = false;
    }

    private void HandleGamepadInput(GamepadInput input)
    {
        switch (input)
        {
            case GamepadInput.Cancel:
                HandleEscapeKey();
                break;
            case GamepadInput.Confirm:
                // Handle confirm action based on current view
                if (IsQuitDialogVisible)
                {
                    ConfirmQuit();
                }
                else if (CurrentView is MainMenuViewModel mainMenu)
                {
                    // Trigger the currently focused menu item
                    // This will work with the focus-based system we implemented
                    GamepadConfirmAction?.Invoke();
                }
                break;
            case GamepadInput.NavigateUp:
                GamepadNavigateAction?.Invoke("Up");
                break;
            case GamepadInput.NavigateDown:
                GamepadNavigateAction?.Invoke("Down");
                break;
            case GamepadInput.NavigateLeft:
                GamepadNavigateAction?.Invoke("Left");
                break;
            case GamepadInput.NavigateRight:
                GamepadNavigateAction?.Invoke("Right");
                break;
        }
    }

    // Actions that Views can subscribe to for gamepad input
    public Action? GamepadConfirmAction { get; set; }
    public Action<string>? GamepadNavigateAction { get; set; }
}