using Avalonia.Controls;
using Avalonia.Input;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FullCrisis3;

[AutoLog]
public partial class GameView : UserControl, IGamepadNavigable
{
    private readonly InputManager _inputManager = new();
    private readonly InputManager _quitDialogInputManager = new();
    private GameViewModel? _viewModel;
    
    public GameView()
    {
        InitializeComponent();
        Loaded += (s, e) => SetupControls();
        KeyDown += OnKeyDown;
        DataContextChanged += OnDataContextChanged;
    }
    
    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        _viewModel = DataContext as GameViewModel;
        if (_viewModel != null)
        {
            _viewModel.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(GameViewModel.ShowQuitDialog))
                {
                    if (_viewModel.ShowQuitDialog)
                    {
                        SetupQuitDialogControls();
                    }
                    else
                    {
                        SetupControls();
                    }
                }
                else if (e.PropertyName == nameof(GameViewModel.ShowInputControls) ||
                        e.PropertyName == nameof(GameViewModel.ShowChoiceButtons) ||
                        e.PropertyName == nameof(GameViewModel.ShowDropdown) ||
                        e.PropertyName == nameof(GameViewModel.ShowContinueButton))
                {
                    SetupControls();
                }
            };
        }
    }
    
    private void SetupControls()
    {
        if (_viewModel?.ShowQuitDialog == true) return;
        
        var controls = new List<Control>();
        
        // Always include save/quit buttons
        var saveButton = this.FindControl<Button>("SaveButton");
        var quitButton = this.FindControl<Button>("QuitButton");
        if (saveButton != null) controls.Add(saveButton);
        if (quitButton != null) controls.Add(quitButton);
        
        // Add dynamic controls based on current state
        if (_viewModel?.ShowInputControls == true)
        {
            var textBox = this.FindControl<TextBox>("PlayerInputTextBox");
            var submitButton = this.FindControl<Button>("SubmitInputButton");
            if (textBox != null) controls.Add(textBox);
            if (submitButton != null) controls.Add(submitButton);
        }
        else if (_viewModel?.ShowChoiceButtons == true)
        {
            var choice1 = this.FindControl<Button>("Choice1Button");
            var choice2 = this.FindControl<Button>("Choice2Button");
            var choice3 = this.FindControl<Button>("Choice3Button");
            if (choice1?.IsVisible == true) controls.Add(choice1);
            if (choice2?.IsVisible == true) controls.Add(choice2);
            if (choice3?.IsVisible == true) controls.Add(choice3);
        }
        else if (_viewModel?.ShowDropdown == true)
        {
            var dropdown = this.FindControl<ComboBox>("DropdownComboBox");
            var submitButton = this.FindControl<Button>("SubmitDropdownButton");
            if (dropdown != null) controls.Add(dropdown);
            if (submitButton != null) controls.Add(submitButton);
        }
        else if (_viewModel?.ShowContinueButton == true)
        {
            var continueButton = this.FindControl<Button>("ContinueButton");
            if (continueButton != null) controls.Add(continueButton);
        }
        else if (_viewModel?.IsGameComplete == true)
        {
            var backButton = this.FindControl<Button>("BackToMenuButton");
            if (backButton != null) controls.Add(backButton);
        }
        
        RegisterControls(controls.ToArray(), _inputManager);
    }
    
    private void SetupQuitDialogControls()
    {
        var controls = new Control[]
        {
            this.FindControl<Button>("SaveAndQuitDialogButton")!,
            this.FindControl<Button>("QuitWithoutSavingButton")!,
            this.FindControl<Button>("CancelQuitButton")!
        };
        
        RegisterControls(controls, _quitDialogInputManager);
    }
    
    private void RegisterControls(Control[] controls, InputManager inputManager)
    {
        inputManager.ClearSelectables();
        inputManager.SetGridNavigation(true);
        
        // Arrange controls in a grid layout for better navigation
        for (int i = 0; i < controls.Length; i++)
        {
            if (controls[i] != null)
            {
                var gridRow = i < 2 ? 0 : (i - 2) / 3 + 1; // Save/Quit in row 0, others flow into rows
                var gridColumn = i < 2 ? i : (i - 2) % 3; // Save/Quit spread in row 0, choices in columns
                
                inputManager.RegisterSelectable(
                    controls[i], 
                    tabIndex: i, 
                    gridRow: gridRow, 
                    gridColumn: gridColumn
                );
            }
        }
        
        if (controls.Length > 0) inputManager.SelectItem(0);
    }
    
    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        // Check if focus is on a text input element
        if (IsTextInputFocused())
        {
            // Let text input handle the key naturally, only handle navigation keys
            if (e.Key == Key.Tab || e.Key == Key.Enter || e.Key == Key.Escape)
            {
                if (_viewModel?.ShowQuitDialog == true)
                {
                    _quitDialogInputManager.HandleKeyInput(e);
                }
                else
                {
                    _inputManager.HandleKeyInput(e);
                }
            }
            // For other keys (a, b, c, etc.), let the text input handle them naturally
            return;
        }
        
        // Normal navigation when not in text input
        if (_viewModel?.ShowQuitDialog == true)
        {
            _quitDialogInputManager.HandleKeyInput(e);
        }
        else
        {
            _inputManager.HandleKeyInput(e);
        }
    }
    
    private bool IsTextInputFocused()
    {
        var focusedElement = TopLevel.GetTopLevel(this)?.FocusManager?.GetFocusedElement();
        return focusedElement is TextBox || focusedElement is ComboBox;
    }
    
    public bool HandleGamepadInput(string input)
    {
        if (_viewModel?.ShowQuitDialog == true)
        {
            return _quitDialogInputManager.HandleGamepadInput(input);
        }
        else
        {
            return _inputManager.HandleGamepadInput(input);
        }
    }
}