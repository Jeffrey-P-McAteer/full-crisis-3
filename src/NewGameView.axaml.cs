using Avalonia.Controls;
using Avalonia.Input;
using System;

namespace FullCrisis3;

[AutoLog]
public partial class NewGameView : UserControl, IGamepadNavigable
{
    private Control[] _controls = Array.Empty<Control>();
    private int _selectedIndex = 0;
    private readonly InputManager _inputManager = new();
    
    public NewGameView()
    {
        InitializeComponent();
        Loaded += (s, e) => SetupControls();
        KeyDown += OnKeyDown;
    }
    
    private void SetupControls()
    {
        _controls = new Control[]
        {
            this.FindControl<TextBox>("PlayerNameTextBox")!,
            this.FindControl<ComboBox>("StoryComboBox")!,
            this.FindControl<Button>("PlayButton")!,
            this.FindControl<Button>("BackButton")!
        };
        
        _inputManager.ClearSelectables();
        
        for (int i = 0; i < _controls.Length; i++)
        {
            var index = i;
            _inputManager.RegisterSelectable(
                _controls[i],
                tabIndex: i,
                onSelected: item => _selectedIndex = index
            );
        }
        
        if (_controls.Length > 0) _inputManager.SelectItem(0);
    }
    
    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        // Check if focus is on a text input element
        if (IsTextInputFocused())
        {
            // Let text input handle the key naturally, only handle navigation keys
            if (e.Key == Key.Tab || e.Key == Key.Enter || e.Key == Key.Escape)
            {
                _inputManager.HandleKeyInput(e);
            }
            // For other keys (a, b, c, etc.), let the text input handle them naturally
            return;
        }
        
        _inputManager.HandleKeyInput(e);
    }
    
    private bool IsTextInputFocused()
    {
        var focusedElement = TopLevel.GetTopLevel(this)?.FocusManager?.GetFocusedElement();
        return focusedElement is TextBox || focusedElement is ComboBox;
    }
    
    public bool HandleGamepadInput(string input)
    {
        return _inputManager.HandleGamepadInput(input);
    }
}