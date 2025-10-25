using Avalonia.Controls;
using Avalonia.Input;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FullCrisis3;

/// <summary>
/// General-purpose input and selection tracking mechanism for keyboard, gamepad, and mouse navigation
/// </summary>
[AutoLog]
public class InputManager
{
    public enum InputType
    {
        Keyboard,
        Gamepad,
        Mouse
    }

    public class SelectableItem
    {
        public Control Control { get; set; } = null!;
        public int TabIndex { get; set; }
        public int GridRow { get; set; } = 0;
        public int GridColumn { get; set; } = 0;
        public bool IsEnabled => Control.IsEnabled && Control.IsVisible;
        public Action<SelectableItem>? OnSelected { get; set; }
        public Action<SelectableItem>? OnActivated { get; set; }
    }

    private List<SelectableItem> _selectableItems = new();
    private int _selectedIndex = 0;
    private InputType _lastInputType = InputType.Keyboard;
    private DateTime _lastInputTime = DateTime.MinValue;
    private bool _useGridNavigation = false;

    public event Action<SelectableItem>? SelectionChanged;
    public event Action<InputType>? InputTypeChanged;

    public SelectableItem? SelectedItem => 
        _selectableItems.Count > 0 && _selectedIndex >= 0 && _selectedIndex < _selectableItems.Count 
            ? _selectableItems[_selectedIndex] 
            : null;

    public InputType LastInputType => _lastInputType;

    public void RegisterSelectable(Control control, int tabIndex = 0, int gridRow = 0, int gridColumn = 0, Action<SelectableItem>? onSelected = null, Action<SelectableItem>? onActivated = null)
    {
        var item = new SelectableItem
        {
            Control = control,
            TabIndex = tabIndex,
            GridRow = gridRow,
            GridColumn = gridColumn,
            OnSelected = onSelected,
            OnActivated = onActivated
        };

        _selectableItems.Add(item);
        _selectableItems = _selectableItems.OrderBy(x => x.TabIndex).ToList();

        // Set up mouse hover detection
        control.PointerEntered += (s, e) =>
        {
            SetInputType(InputType.Mouse);
            var index = _selectableItems.IndexOf(item);
            if (index >= 0 && item.IsEnabled)
            {
                SelectItem(index);
            }
        };

        // If this is the first item, select it
        if (_selectableItems.Count == 1)
        {
            SelectItem(0);
        }
    }

    public void UnregisterSelectable(Control control)
    {
        var item = _selectableItems.FirstOrDefault(x => x.Control == control);
        if (item != null)
        {
            _selectableItems.Remove(item);
            if (_selectedIndex >= _selectableItems.Count)
            {
                _selectedIndex = Math.Max(0, _selectableItems.Count - 1);
            }
        }
    }

    public void ClearSelectables()
    {
        _selectableItems.Clear();
        _selectedIndex = 0;
    }
    
    public void SetGridNavigation(bool useGridNavigation)
    {
        _useGridNavigation = useGridNavigation;
    }

    public bool HandleKeyInput(KeyEventArgs e)
    {
        if (_selectableItems.Count == 0) return false;

        SetInputType(InputType.Keyboard);
        
        var selectedItem = SelectedItem;
        if (selectedItem?.Control is ComboBox comboBox)
        {
            var handled = HandleComboBoxKeyInput(comboBox, e);
            if (handled) return true;
        }

        switch (e.Key)
        {
            case Key.Tab:
                if (e.KeyModifiers.HasFlag(KeyModifiers.Shift))
                {
                    SelectPrevious();
                }
                else
                {
                    SelectNext();
                }
                e.Handled = true;
                return true;

            case Key.Up or Key.W:
                if (_useGridNavigation)
                {
                    SelectUp();
                }
                else
                {
                    SelectPrevious();
                }
                e.Handled = true;
                return true;

            case Key.Down or Key.S:
                if (_useGridNavigation)
                {
                    SelectDown();
                }
                else
                {
                    SelectNext();
                }
                e.Handled = true;
                return true;

            case Key.Left or Key.A:
                if (_useGridNavigation)
                {
                    SelectLeft();
                }
                else
                {
                    SelectPrevious();
                }
                e.Handled = true;
                return true;

            case Key.Right or Key.D:
                if (_useGridNavigation)
                {
                    SelectRight();
                }
                else
                {
                    SelectNext();
                }
                e.Handled = true;
                return true;

            case Key.Enter or Key.Space:
                ActivateSelected();
                e.Handled = true;
                return true;
        }

        return false;
    }
    
    private bool HandleComboBoxKeyInput(ComboBox comboBox, KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.Enter or Key.Space:
                // Toggle dropdown open/close
                comboBox.IsDropDownOpen = !comboBox.IsDropDownOpen;
                e.Handled = true;
                return true;
                
            case Key.Escape:
                // Close dropdown without changing selection
                if (comboBox.IsDropDownOpen)
                {
                    comboBox.IsDropDownOpen = false;
                    e.Handled = true;
                    return true;
                }
                break;
                
            case Key.Up or Key.W:
                if (comboBox.IsDropDownOpen)
                {
                    // Navigate up in dropdown
                    var currentIndex = comboBox.SelectedIndex;
                    if (currentIndex > 0)
                    {
                        comboBox.SelectedIndex = currentIndex - 1;
                    }
                    e.Handled = true;
                    return true;
                }
                break;
                
            case Key.Down or Key.S:
                if (comboBox.IsDropDownOpen)
                {
                    // Navigate down in dropdown
                    var currentIndex = comboBox.SelectedIndex;
                    if (currentIndex < comboBox.ItemCount - 1)
                    {
                        comboBox.SelectedIndex = currentIndex + 1;
                    }
                    e.Handled = true;
                    return true;
                }
                break;
        }
        
        return false;
    }

    public bool HandleGamepadInput(string input)
    {
        if (_selectableItems.Count == 0) return false;

        SetInputType(InputType.Gamepad);
        
        var selectedItem = SelectedItem;
        if (selectedItem?.Control is ComboBox comboBox)
        {
            return HandleComboBoxGamepadInput(comboBox, input);
        }

        switch (input)
        {
            case "Confirm":
                ActivateSelected();
                return true;
        }

        return false;
    }
    
    private bool HandleComboBoxGamepadInput(ComboBox comboBox, string input)
    {
        switch (input)
        {
            case "Confirm":
                // Toggle dropdown open/close
                comboBox.IsDropDownOpen = !comboBox.IsDropDownOpen;
                return true;
                
            case "Cancel":
                // Close dropdown without changing selection
                if (comboBox.IsDropDownOpen)
                {
                    comboBox.IsDropDownOpen = false;
                    return true;
                }
                break;
        }
        
        return false;
    }

    public void SelectNext()
    {
        if (_selectableItems.Count == 0) return;

        var originalIndex = _selectedIndex;
        do
        {
            _selectedIndex = (_selectedIndex + 1) % _selectableItems.Count;
        } while (!_selectableItems[_selectedIndex].IsEnabled && _selectedIndex != originalIndex);

        if (_selectableItems[_selectedIndex].IsEnabled)
        {
            SelectItem(_selectedIndex);
        }
    }

    public void SelectPrevious()
    {
        if (_selectableItems.Count == 0) return;

        var originalIndex = _selectedIndex;
        do
        {
            _selectedIndex = (_selectedIndex - 1 + _selectableItems.Count) % _selectableItems.Count;
        } while (!_selectableItems[_selectedIndex].IsEnabled && _selectedIndex != originalIndex);

        if (_selectableItems[_selectedIndex].IsEnabled)
        {
            SelectItem(_selectedIndex);
        }
    }

    public void SelectItem(int index)
    {
        if (index < 0 || index >= _selectableItems.Count) return;

        _selectedIndex = index;
        var item = _selectableItems[_selectedIndex];
        
        if (item.IsEnabled)
        {
            item.Control.Focus();
            item.OnSelected?.Invoke(item);
            SelectionChanged?.Invoke(item);
        }
    }

    public void SelectItem(Control control)
    {
        var index = _selectableItems.FindIndex(x => x.Control == control);
        if (index >= 0)
        {
            SelectItem(index);
        }
    }

    public void ActivateSelected()
    {
        var item = SelectedItem;
        if (item?.IsEnabled == true)
        {
            item.OnActivated?.Invoke(item);
            
            // Handle different control types
            switch (item.Control)
            {
                case Button button when button.Command != null:
                    button.Command.Execute(button.CommandParameter);
                    break;
                case CheckBox checkBox:
                    checkBox.IsChecked = !checkBox.IsChecked;
                    break;
                case ComboBox comboBox:
                    comboBox.IsDropDownOpen = !comboBox.IsDropDownOpen;
                    break;
            }
        }
    }

    private void SetInputType(InputType inputType)
    {
        var now = DateTime.Now;
        // Only change input type if enough time has passed (prevents rapid switching)
        if (now - _lastInputTime > TimeSpan.FromMilliseconds(100) && _lastInputType != inputType)
        {
            _lastInputType = inputType;
            InputTypeChanged?.Invoke(inputType);
        }
        _lastInputTime = now;
    }

    public void RefreshSelection()
    {
        if (_selectableItems.Count > 0 && _selectedIndex < _selectableItems.Count)
        {
            SelectItem(_selectedIndex);
        }
    }
    
    private void SelectUp()
    {
        if (_selectableItems.Count == 0) return;
        
        var currentItem = SelectedItem;
        if (currentItem == null) return;
        
        var currentRow = currentItem.GridRow;
        var currentColumn = currentItem.GridColumn;
        
        Logger.Debug($"SelectUp: currentRow={currentRow}, currentColumn={currentColumn}, totalItems={_selectableItems.Count}");
        
        // Find item in row above with same or closest column
        var candidateItems = _selectableItems
            .Where(item => item.IsEnabled && item.GridRow < currentRow)
            .OrderByDescending(item => item.GridRow)
            .ThenBy(item => Math.Abs(item.GridColumn - currentColumn))
            .ToList();
        
        Logger.Debug($"SelectUp: found {candidateItems.Count} candidates");
        foreach (var candidate in candidateItems)
        {
            Logger.Debug($"SelectUp: candidate row={candidate.GridRow}, col={candidate.GridColumn}, enabled={candidate.IsEnabled}, control={candidate.Control.GetType().Name}");
        }
        
        var targetItem = candidateItems.FirstOrDefault();
        if (targetItem != null)
        {
            Logger.Debug($"SelectUp: selecting target at row={targetItem.GridRow}, col={targetItem.GridColumn}");
            var targetIndex = _selectableItems.IndexOf(targetItem);
            SelectItem(targetIndex);
        }
        else
        {
            Logger.Debug("SelectUp: no target item found, trying wraparound to bottom row");
            // If no item found above, wrap around to the bottom row
            var maxRow = _selectableItems.Where(item => item.IsEnabled).Max(item => item.GridRow);
            var bottomRowCandidates = _selectableItems
                .Where(item => item.IsEnabled && item.GridRow == maxRow)
                .OrderBy(item => Math.Abs(item.GridColumn - currentColumn))
                .ToList();
            
            var wraparoundTarget = bottomRowCandidates.FirstOrDefault();
            if (wraparoundTarget != null)
            {
                Logger.Debug($"SelectUp: wrapping around to row={wraparoundTarget.GridRow}, col={wraparoundTarget.GridColumn}");
                var targetIndex = _selectableItems.IndexOf(wraparoundTarget);
                SelectItem(targetIndex);
            }
            else
            {
                Logger.Debug("SelectUp: no wraparound target found either");
            }
        }
    }
    
    private void SelectDown()
    {
        if (_selectableItems.Count == 0) return;
        
        var currentItem = SelectedItem;
        if (currentItem == null) return;
        
        var currentRow = currentItem.GridRow;
        var currentColumn = currentItem.GridColumn;
        
        // Find item in row below with same or closest column
        var candidateItems = _selectableItems
            .Where(item => item.IsEnabled && item.GridRow > currentRow)
            .OrderBy(item => item.GridRow)
            .ThenBy(item => Math.Abs(item.GridColumn - currentColumn))
            .ToList();
        
        var targetItem = candidateItems.FirstOrDefault();
        if (targetItem != null)
        {
            var targetIndex = _selectableItems.IndexOf(targetItem);
            SelectItem(targetIndex);
        }
        else
        {
            // If no item found below, wrap around to the top row
            var minRow = _selectableItems.Where(item => item.IsEnabled).Min(item => item.GridRow);
            var topRowCandidates = _selectableItems
                .Where(item => item.IsEnabled && item.GridRow == minRow)
                .OrderBy(item => Math.Abs(item.GridColumn - currentColumn))
                .ToList();
            
            var wraparoundTarget = topRowCandidates.FirstOrDefault();
            if (wraparoundTarget != null)
            {
                var targetIndex = _selectableItems.IndexOf(wraparoundTarget);
                SelectItem(targetIndex);
            }
        }
    }
    
    private void SelectLeft()
    {
        if (_selectableItems.Count == 0) return;
        
        var currentItem = SelectedItem;
        if (currentItem == null) return;
        
        var currentRow = currentItem.GridRow;
        var currentColumn = currentItem.GridColumn;
        
        // Find item in same row to the left
        var candidateItems = _selectableItems
            .Where(item => item.IsEnabled && item.GridRow == currentRow && item.GridColumn < currentColumn)
            .OrderByDescending(item => item.GridColumn)
            .ToList();
        
        var targetItem = candidateItems.FirstOrDefault();
        if (targetItem != null)
        {
            var targetIndex = _selectableItems.IndexOf(targetItem);
            SelectItem(targetIndex);
        }
        else
        {
            // If no item to the left in same row, try previous row's rightmost item
            var prevRowItems = _selectableItems
                .Where(item => item.IsEnabled && item.GridRow < currentRow)
                .OrderByDescending(item => item.GridRow)
                .ThenByDescending(item => item.GridColumn)
                .ToList();
            
            targetItem = prevRowItems.FirstOrDefault();
            if (targetItem != null)
            {
                var targetIndex = _selectableItems.IndexOf(targetItem);
                SelectItem(targetIndex);
            }
        }
    }
    
    private void SelectRight()
    {
        if (_selectableItems.Count == 0) return;
        
        var currentItem = SelectedItem;
        if (currentItem == null) return;
        
        var currentRow = currentItem.GridRow;
        var currentColumn = currentItem.GridColumn;
        
        // Find item in same row to the right
        var candidateItems = _selectableItems
            .Where(item => item.IsEnabled && item.GridRow == currentRow && item.GridColumn > currentColumn)
            .OrderBy(item => item.GridColumn)
            .ToList();
        
        var targetItem = candidateItems.FirstOrDefault();
        if (targetItem != null)
        {
            var targetIndex = _selectableItems.IndexOf(targetItem);
            SelectItem(targetIndex);
        }
        else
        {
            // If no item to the right in same row, try next row's leftmost item
            var nextRowItems = _selectableItems
                .Where(item => item.IsEnabled && item.GridRow > currentRow)
                .OrderBy(item => item.GridRow)
                .ThenBy(item => item.GridColumn)
                .ToList();
            
            targetItem = nextRowItems.FirstOrDefault();
            if (targetItem != null)
            {
                var targetIndex = _selectableItems.IndexOf(targetItem);
                SelectItem(targetIndex);
            }
        }
    }
}