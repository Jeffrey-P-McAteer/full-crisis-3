using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Reactive;
using System.Reactive.Linq;

namespace FullCrisis3.Core.ViewModels;

public class MainWindowViewModel : ViewModelBase
{
    private readonly Stack<ViewModelBase> _viewStack = new();
    private ViewModelBase? _currentView;
    private bool _isQuitDialogVisible;

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
}