using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace FullCrisis3.Navigation;

/// <summary>
/// Base class for all navigable user controls
/// </summary>
[AutoLog]
public abstract class NavigableViewBase : UserControl, IGamepadNavigable
{
    protected readonly SpatialInputManager InputManager = new();
    protected readonly INavigationService NavigationService;
    private List<NavigationControlInfo> _navigationControls = new();
    private bool _isInitialized = false;

    protected NavigableViewBase()
    {
        NavigationService = new NavigationService();
        
        Loaded += OnViewLoaded;
        KeyDown += OnKeyDown;
        DataContextChanged += OnDataContextChanged;
    }

    #region Abstract/Virtual Methods

    /// <summary>
    /// Override to provide custom control discovery logic
    /// </summary>
    protected virtual List<NavigationControlInfo> GetNavigationControls()
    {
        // First try reflection-based discovery
        var controls = ControlFinder.FindNavigableControls(this);
        
        // If no attributed controls found, use auto-discovery
        if (controls.Count == 0)
        {
            controls = ControlFinder.AutoDiscoverControls(this);
        }
        
        // If still no controls, try legacy method
        if (controls.Count == 0)
        {
            var legacyControls = GetLegacyNavigableControls();
            if (legacyControls?.Length > 0)
            {
                controls = legacyControls.Select((control, index) => new NavigationControlInfo
                {
                    Control = control,
                    Order = index,
                    GridRow = index / 3,
                    GridColumn = index % 3,
                    IsDefault = index == 0
                }).ToList();
            }
        }

        return controls;
    }

    /// <summary>
    /// Legacy method for views that need custom control arrays
    /// </summary>
    protected virtual Control[]? GetLegacyNavigableControls() => null;

    /// <summary>
    /// Override to customize grid navigation behavior
    /// </summary>
    protected virtual bool UseGridNavigation => true;

    /// <summary>
    /// Override to handle custom initialization after controls are set up
    /// </summary>
    protected virtual void OnNavigationInitialized() { }

    /// <summary>
    /// Override to handle custom data context changes
    /// </summary>
    protected virtual void OnViewDataContextChanged() { }

    #endregion

    #region Event Handlers

    private void OnViewLoaded(object? sender, RoutedEventArgs e)
    {
        if (!_isInitialized)
        {
            InitializeNavigation();
            _isInitialized = true;
        }
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        OnViewDataContextChanged();
        
        // Re-initialize navigation if needed
        if (_isInitialized)
        {
            InitializeNavigation();
        }
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        // Handle text input focus
        if (InputHelpers.IsTextInputFocused(this))
        {
            // Only intercept specific keys when text input is focused
            if (InputHelpers.ShouldTextInputHandleKey(e.Key))
            {
                return; // Let text input handle it
            }
        }

        // Handle navigation
        var direction = InputHelpers.GetNavigationDirection(e.Key);
        if (direction.HasValue)
        {
            var handled = HandleNavigationDirection(direction.Value, e.KeyModifiers);
            if (handled)
            {
                e.Handled = true;
                return;
            }
        }

        // Fallback to InputManager
        InputManager.HandleKeyInput(e);
    }

    #endregion

    #region Navigation Logic

    private void InitializeNavigation()
    {
        try
        {
            Logger.Debug($"Initializing navigation for {GetType().Name}");
            
            // Clear previous setup
            InputManager.ClearSelectables();
            InputManager.SetGridNavigation(UseGridNavigation);

            // Get navigation controls
            _navigationControls = GetNavigationControls();
            
            Logger.Debug($"Found {_navigationControls.Count} navigation controls");

            // Register all controls at once using the new system
            InputManager.RegisterSelectables(_navigationControls);

            // Call derived class initialization
            OnNavigationInitialized();
        }
        catch (Exception ex)
        {
            Logger.Debug($"Error initializing navigation: {ex.Message}");
        }
    }

    private bool HandleNavigationDirection(NavigationDirection direction, KeyModifiers modifiers)
    {
        try
        {
            return direction switch
            {
                NavigationDirection.Up => InputManager.SelectUp(),
                NavigationDirection.Down => InputManager.SelectDown(),
                NavigationDirection.Left => InputManager.SelectLeft(),
                NavigationDirection.Right => InputManager.SelectRight(),
                NavigationDirection.Next => InputManager.SelectNext(),
                NavigationDirection.Previous => InputManager.SelectPrevious(),
                _ => false
            };
        }
        catch (Exception ex)
        {
            Logger.Debug($"Error handling navigation direction {direction}: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Called when a control is selected
    /// </summary>
    protected virtual void OnControlSelected(NavigationControlInfo controlInfo, int index)
    {
        Logger.Debug($"Selected control: {controlInfo.Control.GetType().Name} at index {index}");
    }

    #endregion

    #region IGamepadNavigable Implementation

    public virtual bool HandleGamepadInput(string input)
    {
        try
        {
            Logger.Debug($"Handling gamepad input: {input}");
            
            // Handle common gamepad inputs
            switch (input)
            {
                case "Confirm":
                    InputManager.ActivateSelected();
                    return true;
                
                case "Cancel":
                    return HandleCancelInput();
            }

            // Fallback to InputManager
            return InputManager.HandleGamepadInput(input);
        }
        catch (Exception ex)
        {
            Logger.Debug($"Error handling gamepad input {input}: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Handle cancel/back input - override for custom behavior
    /// </summary>
    protected virtual bool HandleCancelInput()
    {
        // Default behavior - could be overridden
        return false;
    }

    #endregion

    #region Public API

    /// <summary>
    /// Manually refresh navigation setup
    /// </summary>
    public void RefreshNavigation()
    {
        InitializeNavigation();
    }

    /// <summary>
    /// Get currently selected control
    /// </summary>
    public Control? GetSelectedControl()
    {
        var selectedItem = InputManager.SelectedItem;
        return selectedItem?.Control;
    }

    /// <summary>
    /// Select a specific control by index
    /// </summary>
    public bool SelectControl(int index)
    {
        return InputManager.SelectItem(index);
    }

    #endregion
}