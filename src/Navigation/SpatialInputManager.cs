using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Input;

namespace FullCrisis3.Navigation;

/// <summary>
/// Represents a selectable control in the navigation system
/// </summary>
public class SelectableItem
{
    public Control Control { get; set; } = null!;
    public int TabIndex { get; set; }
    public int GridRow { get; set; } = 0;
    public int GridColumn { get; set; } = 0;
    public bool IsEnabled { get; set; } = true;
    public Action<SelectableItem>? OnSelected { get; set; }
    public Action<SelectableItem>? OnActivated { get; set; }
}

/// <summary>
/// Enhanced InputManager with spatial navigation and reflection-based discovery
/// </summary>
[AutoLog]
public class SpatialInputManager
{
    private readonly List<SelectableItem> _selectableItems = new();
    private int _selectedIndex = -1;
    private bool _useGridNavigation = true;
    private FullCrisis3.InputManager.InputType _lastInputType = FullCrisis3.InputManager.InputType.Keyboard;

    #region Properties

    public SelectableItem? SelectedItem => _selectedIndex >= 0 && _selectedIndex < _selectableItems.Count
        ? _selectableItems[_selectedIndex]
        : null;

    public int SelectedIndex => _selectedIndex;
    public int Count => _selectableItems.Count;

    #endregion

    #region Setup

    public void ClearSelectables()
    {
        _selectableItems.Clear();
        _selectedIndex = -1;
    }

    public void SetGridNavigation(bool useGrid)
    {
        _useGridNavigation = useGrid;
    }

    public void RegisterSelectables(List<NavigationControlInfo> controls)
    {
        ClearSelectables();
        
        foreach (var controlInfo in controls)
        {
            RegisterSelectable(
                controlInfo.Control,
                controlInfo.Order,
                controlInfo.GridRow,
                controlInfo.GridColumn,
                null
            );
        }

        // Select the default control
        var defaultControl = controls.FirstOrDefault(c => c.IsDefault);
        if (defaultControl != null)
        {
            SelectItem(defaultControl.Order);
        }
        else if (controls.Count > 0)
        {
            SelectItem(0);
        }
    }

    public void RegisterSelectable(Control control, int tabIndex, int gridRow, int gridColumn, Action<SelectableItem>? onSelected)
    {
        if (control == null) return;

        var item = new SelectableItem
        {
            Control = control,
            TabIndex = tabIndex,
            GridRow = gridRow,
            GridColumn = gridColumn,
            OnSelected = onSelected,
            IsEnabled = control.IsEnabled
        };

        _selectableItems.Add(item);
        UpdateControlAppearance(item, false);
    }

    #endregion

    #region Navigation

    public bool SelectItem(int index)
    {
        if (index < 0 || index >= _selectableItems.Count) return false;

        var previousIndex = _selectedIndex;
        _selectedIndex = index;

        // Update visual state
        if (previousIndex >= 0 && previousIndex < _selectableItems.Count)
        {
            UpdateControlAppearance(_selectableItems[previousIndex], false);
        }

        var selectedItem = _selectableItems[_selectedIndex];
        UpdateControlAppearance(selectedItem, true);

        // Focus the control
        if (selectedItem.Control.Focusable)
        {
            selectedItem.Control.Focus();
        }

        // Notify callback
        selectedItem.OnSelected?.Invoke(selectedItem);

        Logger.Debug($"Selected item {index}: {selectedItem.Control.GetType().Name}");
        return true;
    }

    public bool SelectNext()
    {
        if (_selectableItems.Count == 0) return false;

        var enabledItems = _selectableItems.Where(item => item.IsEnabled).ToList();
        if (enabledItems.Count == 0) return false;

        var currentItem = SelectedItem;
        if (currentItem == null)
        {
            return SelectItem(enabledItems[0].TabIndex);
        }

        var currentIndexInEnabled = enabledItems.FindIndex(item => item == currentItem);
        var nextIndexInEnabled = (currentIndexInEnabled + 1) % enabledItems.Count;
        
        return SelectItem(enabledItems[nextIndexInEnabled].TabIndex);
    }

    public bool SelectPrevious()
    {
        if (_selectableItems.Count == 0) return false;

        var enabledItems = _selectableItems.Where(item => item.IsEnabled).ToList();
        if (enabledItems.Count == 0) return false;

        var currentItem = SelectedItem;
        if (currentItem == null)
        {
            return SelectItem(enabledItems[^1].TabIndex);
        }

        var currentIndexInEnabled = enabledItems.FindIndex(item => item == currentItem);
        var prevIndexInEnabled = currentIndexInEnabled == 0 ? enabledItems.Count - 1 : currentIndexInEnabled - 1;
        
        return SelectItem(enabledItems[prevIndexInEnabled].TabIndex);
    }

    public bool SelectUp()
    {
        if (!_useGridNavigation) return SelectPrevious();

        var currentItem = SelectedItem;
        if (currentItem == null) return false;

        // Find the nearest control above in the same or nearby column
        var candidates = _selectableItems
            .Where(item => item.IsEnabled && item.GridRow < currentItem.GridRow)
            .OrderByDescending(item => item.GridRow)
            .ThenBy(item => Math.Abs(item.GridColumn - currentItem.GridColumn))
            .ToList();

        if (candidates.Count > 0)
        {
            return SelectItem(candidates[0].TabIndex);
        }

        // Wrap around to bottom
        var bottomRowCandidates = _selectableItems
            .Where(item => item.IsEnabled)
            .GroupBy(item => item.GridRow)
            .OrderByDescending(g => g.Key)
            .FirstOrDefault()?
            .OrderBy(item => Math.Abs(item.GridColumn - currentItem.GridColumn))
            .ToList();

        if (bottomRowCandidates?.Count > 0)
        {
            return SelectItem(bottomRowCandidates[0].TabIndex);
        }

        return false;
    }

    public bool SelectDown()
    {
        if (!_useGridNavigation) return SelectNext();

        var currentItem = SelectedItem;
        if (currentItem == null) return false;

        // Find the nearest control below in the same or nearby column
        var candidates = _selectableItems
            .Where(item => item.IsEnabled && item.GridRow > currentItem.GridRow)
            .OrderBy(item => item.GridRow)
            .ThenBy(item => Math.Abs(item.GridColumn - currentItem.GridColumn))
            .ToList();

        if (candidates.Count > 0)
        {
            return SelectItem(candidates[0].TabIndex);
        }

        // Wrap around to top
        var topRowCandidates = _selectableItems
            .Where(item => item.IsEnabled)
            .GroupBy(item => item.GridRow)
            .OrderBy(g => g.Key)
            .FirstOrDefault()?
            .OrderBy(item => Math.Abs(item.GridColumn - currentItem.GridColumn))
            .ToList();

        if (topRowCandidates?.Count > 0)
        {
            return SelectItem(topRowCandidates[0].TabIndex);
        }

        return false;
    }

    public bool SelectLeft()
    {
        if (!_useGridNavigation) return SelectPrevious();

        var currentItem = SelectedItem;
        if (currentItem == null) return false;

        // Find controls to the left in the same row
        var candidates = _selectableItems
            .Where(item => item.IsEnabled && 
                          item.GridRow == currentItem.GridRow && 
                          item.GridColumn < currentItem.GridColumn)
            .OrderByDescending(item => item.GridColumn)
            .ToList();

        if (candidates.Count > 0)
        {
            return SelectItem(candidates[0].TabIndex);
        }

        // Wrap around to rightmost in same row
        var rightmostCandidates = _selectableItems
            .Where(item => item.IsEnabled && item.GridRow == currentItem.GridRow)
            .OrderByDescending(item => item.GridColumn)
            .ToList();

        if (rightmostCandidates.Count > 0 && rightmostCandidates[0] != currentItem)
        {
            return SelectItem(rightmostCandidates[0].TabIndex);
        }

        return false;
    }

    public bool SelectRight()
    {
        if (!_useGridNavigation) return SelectNext();

        var currentItem = SelectedItem;
        if (currentItem == null) return false;

        // Find controls to the right in the same row
        var candidates = _selectableItems
            .Where(item => item.IsEnabled && 
                          item.GridRow == currentItem.GridRow && 
                          item.GridColumn > currentItem.GridColumn)
            .OrderBy(item => item.GridColumn)
            .ToList();

        if (candidates.Count > 0)
        {
            return SelectItem(candidates[0].TabIndex);
        }

        // Wrap around to leftmost in same row
        var leftmostCandidates = _selectableItems
            .Where(item => item.IsEnabled && item.GridRow == currentItem.GridRow)
            .OrderBy(item => item.GridColumn)
            .ToList();

        if (leftmostCandidates.Count > 0 && leftmostCandidates[0] != currentItem)
        {
            return SelectItem(leftmostCandidates[0].TabIndex);
        }

        return false;
    }

    #endregion

    #region Input Handling

    public bool HandleKeyInput(KeyEventArgs e)
    {
        SetInputType(FullCrisis3.InputManager.InputType.Keyboard);

        var direction = InputHelpers.GetNavigationDirection(e.Key);
        if (direction.HasValue)
        {
            var handled = HandleDirection(direction.Value, e.KeyModifiers);
            if (handled)
            {
                e.Handled = true;
                return true;
            }
        }

        switch (e.Key)
        {
            case Key.Enter:
            case Key.Space:
                ActivateSelected();
                e.Handled = true;
                return true;
        }

        return false;
    }

    public bool HandleGamepadInput(string input)
    {
        SetInputType(FullCrisis3.InputManager.InputType.Gamepad);

        switch (input)
        {
            case "Confirm":
                ActivateSelected();
                return true;
        }

        return false;
    }

    private bool HandleDirection(NavigationDirection direction, KeyModifiers modifiers)
    {
        return direction switch
        {
            NavigationDirection.Up => SelectUp(),
            NavigationDirection.Down => SelectDown(),
            NavigationDirection.Left => SelectLeft(),
            NavigationDirection.Right => SelectRight(),
            NavigationDirection.Next => SelectNext(),
            NavigationDirection.Previous => SelectPrevious(),
            _ => false
        };
    }

    #endregion

    #region Activation

    public void ActivateSelected()
    {
        var selectedItem = SelectedItem;
        if (selectedItem?.Control == null) return;

        Logger.Debug($"Activating control: {selectedItem.Control.GetType().Name}");

        // Handle different control types
        switch (selectedItem.Control)
        {
            case Button button:
                if (button.IsEnabled && button.Command?.CanExecute(button.CommandParameter) == true)
                {
                    button.Command.Execute(button.CommandParameter);
                }
                break;

            case ComboBox comboBox:
                if (comboBox.IsEnabled)
                {
                    comboBox.IsDropDownOpen = !comboBox.IsDropDownOpen;
                }
                break;

            case ListBox listBox:
                // Focus and potentially select first item
                if (listBox.IsEnabled && listBox.Items.Count > 0)
                {
                    listBox.Focus();
                    if (listBox.SelectedIndex < 0)
                    {
                        listBox.SelectedIndex = 0;
                    }
                }
                break;
        }
    }

    #endregion

    #region Helper Methods

    private void SetInputType(FullCrisis3.InputManager.InputType inputType)
    {
        _lastInputType = inputType;
    }

    private void UpdateControlAppearance(SelectableItem item, bool isSelected)
    {
        // This could be enhanced to apply visual selection styles
        // For now, we rely on focus and existing styles
    }

    public void RefreshEnabledStates()
    {
        foreach (var item in _selectableItems)
        {
            item.IsEnabled = item.Control.IsEnabled;
        }
    }

    #endregion
}