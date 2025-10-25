using System;
using System.Collections.Generic;
using ReactiveUI;

namespace FullCrisis3.Navigation;

/// <summary>
/// Centralized navigation service
/// </summary>
public interface INavigationService
{
    void NavigateTo<T>() where T : ViewModelBase, new();
    void NavigateTo<T>(T viewModel) where T : ViewModelBase;
    void NavigateBack();
    void ShowDialog<T>(T dialogViewModel) where T : ViewModelBase;
    void SetNavigationCallback(Action<object> navigationCallback);
}

[AutoLog]
public class NavigationService : INavigationService
{
    private readonly Stack<ViewModelBase> _navigationStack = new();
    private Action<object>? _navigationCallback;

    public void SetNavigationCallback(Action<object> navigationCallback)
    {
        _navigationCallback = navigationCallback;
    }

    public void NavigateTo<T>() where T : ViewModelBase, new()
    {
        NavigateTo(new T());
    }

    public void NavigateTo<T>(T viewModel) where T : ViewModelBase
    {
        if (viewModel == null) throw new ArgumentNullException(nameof(viewModel));

        Logger.Debug($"Navigating to {typeof(T).Name}");
        
        _navigationCallback?.Invoke(viewModel);
    }

    public void NavigateBack()
    {
        if (_navigationStack.Count > 0)
        {
            var previousViewModel = _navigationStack.Pop();
            Logger.Debug($"Navigating back to {previousViewModel.GetType().Name}");
            _navigationCallback?.Invoke(previousViewModel);
        }
        else
        {
            Logger.Debug("No previous view to navigate back to");
        }
    }

    public void ShowDialog<T>(T dialogViewModel) where T : ViewModelBase
    {
        // For now, treat dialogs the same as navigation
        // Could be extended for actual dialog behavior
        NavigateTo(dialogViewModel);
    }

    public void PushToStack(ViewModelBase viewModel)
    {
        _navigationStack.Push(viewModel);
    }

    public void ClearStack()
    {
        _navigationStack.Clear();
    }
}