using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;

namespace FullCrisis3.Navigation;

/// <summary>
/// Helper utilities for input handling
/// </summary>
[AutoLog]
public static class InputHelpers
{
    /// <summary>
    /// Checks if a text input control is currently focused
    /// </summary>
    public static bool IsTextInputFocused(Control view)
    {
        var focusedElement = TopLevel.GetTopLevel(view)?.FocusManager?.GetFocusedElement();
        return focusedElement is TextBox or ComboBox or AutoCompleteBox;
    }

    /// <summary>
    /// Checks if any dropdown/popup is currently open
    /// </summary>
    public static bool IsDropdownOpen(Control view)
    {
        var focusedElement = TopLevel.GetTopLevel(view)?.FocusManager?.GetFocusedElement();
        
        if (focusedElement is ComboBox comboBox)
        {
            return comboBox.IsDropDownOpen;
        }
        
        // Check for other popup types if needed
        return false;
    }

    /// <summary>
    /// Determines if a key should be handled by text input controls
    /// </summary>
    public static bool ShouldTextInputHandleKey(Key key)
    {
        return key switch
        {
            Key.Tab or Key.Enter or Key.Escape => false,
            Key.Up or Key.Down when IsDropdownNavigation() => false,
            _ => true
        };
    }

    /// <summary>
    /// Checks if the current context involves dropdown navigation
    /// </summary>
    private static bool IsDropdownNavigation()
    {
        // Could be enhanced to check current focus context
        return false;
    }

    /// <summary>
    /// Gets the input type from a key event
    /// </summary>
    public static FullCrisis3.InputManager.InputType GetInputType(KeyEventArgs e)
    {
        return e.KeyModifiers switch
        {
            KeyModifiers.None => FullCrisis3.InputManager.InputType.Keyboard,
            KeyModifiers.Shift => FullCrisis3.InputManager.InputType.Keyboard,
            _ => FullCrisis3.InputManager.InputType.Keyboard
        };
    }

    /// <summary>
    /// Converts gamepad input to navigation direction
    /// </summary>
    public static NavigationDirection? GetNavigationDirection(string gamepadInput)
    {
        return gamepadInput switch
        {
            "Up" => NavigationDirection.Up,
            "Down" => NavigationDirection.Down,
            "Left" => NavigationDirection.Left,
            "Right" => NavigationDirection.Right,
            _ => null
        };
    }

    /// <summary>
    /// Converts keyboard input to navigation direction
    /// </summary>
    public static NavigationDirection? GetNavigationDirection(Key key)
    {
        return key switch
        {
            Key.Up => NavigationDirection.Up,
            Key.Down => NavigationDirection.Down,
            Key.Left => NavigationDirection.Left,
            Key.Right => NavigationDirection.Right,
            Key.Tab => NavigationDirection.Next,
            _ => null
        };
    }
}

/// <summary>
/// Navigation directions
/// </summary>
public enum NavigationDirection
{
    Up,
    Down,
    Left,
    Right,
    Next,
    Previous
}