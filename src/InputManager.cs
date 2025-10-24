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
        public bool IsEnabled => Control.IsEnabled && Control.IsVisible;
        public Action<SelectableItem>? OnSelected { get; set; }
        public Action<SelectableItem>? OnActivated { get; set; }
    }

    private List<SelectableItem> _selectableItems = new();
    private int _selectedIndex = 0;
    private InputType _lastInputType = InputType.Keyboard;
    private DateTime _lastInputTime = DateTime.MinValue;

    public event Action<SelectableItem>? SelectionChanged;
    public event Action<InputType>? InputTypeChanged;

    public SelectableItem? SelectedItem => 
        _selectableItems.Count > 0 && _selectedIndex >= 0 && _selectedIndex < _selectableItems.Count 
            ? _selectableItems[_selectedIndex] 
            : null;

    public InputType LastInputType => _lastInputType;

    public void RegisterSelectable(Control control, int tabIndex = 0, Action<SelectableItem>? onSelected = null, Action<SelectableItem>? onActivated = null)
    {
        var item = new SelectableItem
        {
            Control = control,
            TabIndex = tabIndex,
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

    public bool HandleKeyInput(KeyEventArgs e)
    {
        if (_selectableItems.Count == 0) return false;

        SetInputType(InputType.Keyboard);

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
                SelectPrevious();
                e.Handled = true;
                return true;

            case Key.Down or Key.S:
                SelectNext();
                e.Handled = true;
                return true;

            case Key.Left or Key.A:
                SelectPrevious();
                e.Handled = true;
                return true;

            case Key.Right or Key.D:
                SelectNext();
                e.Handled = true;
                return true;

            case Key.Enter or Key.Space:
                ActivateSelected();
                e.Handled = true;
                return true;
        }

        return false;
    }

    public bool HandleGamepadInput(string input)
    {
        if (_selectableItems.Count == 0) return false;

        SetInputType(InputType.Gamepad);

        switch (input)
        {
            case "Up":
            case "Left":
                SelectPrevious();
                return true;

            case "Down":
            case "Right":
                SelectNext();
                return true;

            case "Confirm":
                ActivateSelected();
                return true;
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
}