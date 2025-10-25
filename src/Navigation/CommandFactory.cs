using System;
using System.Reactive;
using ReactiveUI;

namespace FullCrisis3.Navigation;

/// <summary>
/// Factory for creating common reactive commands
/// </summary>
[AutoLog]
public static class CommandFactory
{
    /// <summary>
    /// Creates a navigation command that navigates to a new instance of the specified view model
    /// </summary>
    public static ReactiveCommand<Unit, Unit> CreateNavigationCommand<T>(INavigationService navigationService) 
        where T : ViewModelBase, new()
    {
        return ReactiveCommand.Create(() =>
        {
            Logger.Debug($"Executing navigation command to {typeof(T).Name}");
            navigationService.NavigateTo<T>();
        });
    }

    /// <summary>
    /// Creates a navigation command with a specific view model instance
    /// </summary>
    public static ReactiveCommand<Unit, Unit> CreateNavigationCommand<T>(INavigationService navigationService, T viewModel) 
        where T : ViewModelBase
    {
        return ReactiveCommand.Create(() =>
        {
            Logger.Debug($"Executing navigation command to {typeof(T).Name} instance");
            navigationService.NavigateTo(viewModel);
        });
    }

    /// <summary>
    /// Creates a back navigation command
    /// </summary>
    public static ReactiveCommand<Unit, Unit> CreateBackCommand(INavigationService navigationService)
    {
        return ReactiveCommand.Create(() =>
        {
            Logger.Debug("Executing back navigation command");
            navigationService.NavigateBack();
        });
    }

    /// <summary>
    /// Creates a generic action command
    /// </summary>
    public static ReactiveCommand<Unit, Unit> CreateActionCommand(Action action, string? actionName = null)
    {
        return ReactiveCommand.Create(() =>
        {
            Logger.Debug($"Executing action command: {actionName ?? "unnamed"}");
            action.Invoke();
        });
    }

    /// <summary>
    /// Creates a parameterized action command
    /// </summary>
    public static ReactiveCommand<T, Unit> CreateActionCommand<T>(Action<T> action, string? actionName = null)
    {
        return ReactiveCommand.Create<T>(parameter =>
        {
            Logger.Debug($"Executing parameterized action command: {actionName ?? "unnamed"} with parameter: {parameter}");
            action.Invoke(parameter);
        });
    }

    /// <summary>
    /// Creates a conditional command that's only enabled when the condition is true
    /// </summary>
    public static ReactiveCommand<Unit, Unit> CreateConditionalCommand(Action action, IObservable<bool> canExecute, string? actionName = null)
    {
        return ReactiveCommand.Create(action, canExecute);
    }
}