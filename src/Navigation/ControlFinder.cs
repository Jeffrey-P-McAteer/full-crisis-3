using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Avalonia.Controls;
using Avalonia.LogicalTree;

namespace FullCrisis3.Navigation;

/// <summary>
/// Utility for finding and discovering controls using reflection
/// </summary>
[AutoLog]
public static class ControlFinder
{
    /// <summary>
    /// Finds controls by their x:Name attribute
    /// </summary>
    public static T? FindControlByName<T>(UserControl view, string name) where T : Control
    {
        return view.FindControl<T>(name);
    }

    /// <summary>
    /// Finds all controls marked with NavigationOrder attribute using reflection
    /// </summary>
    public static List<NavigationControlInfo> FindNavigableControls(UserControl view)
    {
        var controls = new List<NavigationControlInfo>();
        var viewType = view.GetType();

        // Get all fields and properties
        var members = viewType.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
            .Cast<MemberInfo>()
            .Concat(viewType.GetProperties(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public))
            .Where(m => m.GetCustomAttribute<NavigationOrderAttribute>() != null);

        foreach (var member in members)
        {
            var attr = member.GetCustomAttribute<NavigationOrderAttribute>()!;
            Control? control = null;

            try
            {
                if (member is FieldInfo field && typeof(Control).IsAssignableFrom(field.FieldType))
                {
                    control = field.GetValue(view) as Control;
                }
                else if (member is PropertyInfo prop && typeof(Control).IsAssignableFrom(prop.PropertyType))
                {
                    control = prop.GetValue(view) as Control;
                }

                if (control != null)
                {
                    controls.Add(new NavigationControlInfo
                    {
                        Control = control,
                        Order = attr.Order,
                        GridRow = attr.GridRow >= 0 ? attr.GridRow : CalculateGridRow(controls.Count),
                        GridColumn = attr.GridColumn >= 0 ? attr.GridColumn : CalculateGridColumn(controls.Count),
                        IsDefault = attr.IsDefault
                    });
                }
            }
            catch (Exception ex)
            {
                Logger.Debug($"Failed to get control from member {member.Name}: {ex.Message}");
            }
        }

        return controls.OrderBy(c => c.Order).ToList();
    }

    /// <summary>
    /// Automatically discovers controls by common naming patterns and control types
    /// </summary>
    public static List<NavigationControlInfo> AutoDiscoverControls(UserControl view)
    {
        var controls = new List<NavigationControlInfo>();
        var discoveredControls = new List<Control>();

        // Find all interactive controls
        FindControlsRecursive(view, discoveredControls);

        // Filter to focusable controls
        var focusableControls = discoveredControls
            .Where(c => c.Focusable && c.IsEnabled && c.IsVisible)
            .Where(c => c is Button or TextBox or ComboBox or CheckBox or ListBox)
            .ToList();

        // Calculate spatial positions
        for (int i = 0; i < focusableControls.Count; i++)
        {
            var control = focusableControls[i];
            var bounds = control.Bounds;
            
            controls.Add(new NavigationControlInfo
            {
                Control = control,
                Order = i,
                GridRow = CalculateGridRowFromPosition(bounds.Y, focusableControls),
                GridColumn = CalculateGridColumnFromPosition(bounds.X, focusableControls),
                IsDefault = i == 0
            });
        }

        return controls.OrderBy(c => c.Order).ToList();
    }

    private static void FindControlsRecursive(Control parent, List<Control> result)
    {
        foreach (var child in parent.GetLogicalChildren().OfType<Control>())
        {
            result.Add(child);
            FindControlsRecursive(child, result);
        }
    }

    private static int CalculateGridRow(int index) => index / 3; // 3 columns by default
    private static int CalculateGridColumn(int index) => index % 3;

    private static int CalculateGridRowFromPosition(double y, List<Control> allControls)
    {
        var sortedByY = allControls.OrderBy(c => c.Bounds.Y).ToList();
        var yPositions = sortedByY.Select(c => c.Bounds.Y).Distinct().OrderBy(y => y).ToList();
        
        // Group controls into rows based on Y position (with tolerance)
        const double tolerance = 20;
        var row = 0;
        for (int i = 0; i < yPositions.Count; i++)
        {
            if (Math.Abs(y - yPositions[i]) <= tolerance)
            {
                return i;
            }
        }
        return row;
    }

    private static int CalculateGridColumnFromPosition(double x, List<Control> allControls)
    {
        var sortedByX = allControls.OrderBy(c => c.Bounds.X).ToList();
        var xPositions = sortedByX.Select(c => c.Bounds.X).Distinct().OrderBy(x => x).ToList();
        
        // Group controls into columns based on X position (with tolerance)
        const double tolerance = 20;
        for (int i = 0; i < xPositions.Count; i++)
        {
            if (Math.Abs(x - xPositions[i]) <= tolerance)
            {
                return i;
            }
        }
        return 0;
    }
}

/// <summary>
/// Information about a navigable control
/// </summary>
public class NavigationControlInfo
{
    public Control Control { get; set; } = null!;
    public int Order { get; set; }
    public int GridRow { get; set; }
    public int GridColumn { get; set; }
    public bool IsDefault { get; set; }
}