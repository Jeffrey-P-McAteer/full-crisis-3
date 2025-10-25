using System;
using System.Reactive;
using ReactiveUI;

namespace FullCrisis3.Navigation;

/// <summary>
/// Enhanced base class for all view models with navigation support
/// </summary>
[AutoLog]
public abstract class NavigableViewModelBase : ReactiveObject
{
    protected readonly INavigationService NavigationService;

    protected NavigableViewModelBase()
    {
        NavigationService = new NavigationService();
        InitializeCommands();
    }

    protected NavigableViewModelBase(INavigationService navigationService)
    {
        NavigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
        InitializeCommands();
    }

    #region Common Commands

    /// <summary>
    /// Command to navigate back to previous view
    /// </summary>
    public ReactiveCommand<Unit, Unit> BackCommand { get; private set; } = null!;

    #endregion

    #region Initialization

    /// <summary>
    /// Initialize common commands - called from constructor
    /// </summary>
    private void InitializeCommands()
    {
        BackCommand = CommandFactory.CreateBackCommand(NavigationService);
        
        // Call derived class initialization
        InitializeViewModelCommands();
    }

    /// <summary>
    /// Override to initialize view model specific commands
    /// </summary>
    protected virtual void InitializeViewModelCommands() { }

    #endregion

    #region Navigation Helpers

    /// <summary>
    /// Navigate to a new view model instance
    /// </summary>
    protected void NavigateTo<T>() where T : ViewModelBase, new()
    {
        NavigationService.NavigateTo<T>();
    }

    /// <summary>
    /// Navigate to a specific view model instance
    /// </summary>
    protected void NavigateTo<T>(T viewModel) where T : ViewModelBase
    {
        NavigationService.NavigateTo(viewModel);
    }

    /// <summary>
    /// Navigate back to the previous view
    /// </summary>
    protected void NavigateBack()
    {
        NavigationService.NavigateBack();
    }

    /// <summary>
    /// Show a dialog with the specified view model
    /// </summary>
    protected void ShowDialog<T>(T dialogViewModel) where T : ViewModelBase
    {
        NavigationService.ShowDialog(dialogViewModel);
    }

    #endregion

    #region Command Creation Helpers

    /// <summary>
    /// Create a navigation command
    /// </summary>
    protected ReactiveCommand<Unit, Unit> CreateNavigationCommand<T>() where T : ViewModelBase, new()
    {
        return CommandFactory.CreateNavigationCommand<T>(NavigationService);
    }

    /// <summary>
    /// Create a navigation command with a specific instance
    /// </summary>
    protected ReactiveCommand<Unit, Unit> CreateNavigationCommand<T>(T viewModel) where T : ViewModelBase
    {
        return CommandFactory.CreateNavigationCommand(NavigationService, viewModel);
    }

    /// <summary>
    /// Create an action command
    /// </summary>
    protected ReactiveCommand<Unit, Unit> CreateActionCommand(Action action, string? actionName = null)
    {
        return CommandFactory.CreateActionCommand(action, actionName);
    }

    /// <summary>
    /// Create a parameterized action command
    /// </summary>
    protected ReactiveCommand<T, Unit> CreateActionCommand<T>(Action<T> action, string? actionName = null)
    {
        return CommandFactory.CreateActionCommand(action, actionName);
    }

    /// <summary>
    /// Create a conditional command
    /// </summary>
    protected ReactiveCommand<Unit, Unit> CreateConditionalCommand(Action action, IObservable<bool> canExecute, string? actionName = null)
    {
        return CommandFactory.CreateConditionalCommand(action, canExecute, actionName);
    }

    #endregion

    #region Lifecycle

    /// <summary>
    /// Called when the view model is being navigated to
    /// </summary>
    public virtual void OnNavigatedTo() { }

    /// <summary>
    /// Called when the view model is being navigated away from
    /// </summary>
    public virtual void OnNavigatedFrom() { }

    /// <summary>
    /// Called when the view model is being destroyed
    /// </summary>
    public virtual void OnDestroy() { }

    #endregion
}