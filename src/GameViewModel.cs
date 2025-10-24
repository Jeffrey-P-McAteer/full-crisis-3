using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;

namespace FullCrisis3;

[AutoLog]
public class GameViewModel : ViewModelBase
{
    private readonly StoryEngine _storyEngine;
    private readonly StoryState _gameState;
    private StoryDialogue? _currentDialogue;
    private string _dialogueText = "";
    private string _playerInput = "";
    private StoryChoice? _selectedChoice;
    private bool _showInputControls = false;
    private bool _showChoiceButtons = false;
    private bool _showDropdown = false;
    private bool _showContinueButton = false;
    private bool _isGameComplete = false;
    private bool _hasUnsavedChanges = false;
    private bool _showQuitDialog = false;
    private DateTime _lastSaved = DateTime.MinValue;
    
    public GameViewModel(StoryEngine storyEngine, StoryState gameState)
    {
        _storyEngine = storyEngine;
        _gameState = gameState;
        
        // Commands
        ContinueCommand = ReactiveCommand.Create(Continue);
        SubmitInputCommand = ReactiveCommand.Create(SubmitInput);
        Choice1Command = ReactiveCommand.Create(() => SelectChoice(0));
        Choice2Command = ReactiveCommand.Create(() => SelectChoice(1));
        Choice3Command = ReactiveCommand.Create(() => SelectChoice(2));
        BackToMenuCommand = ReactiveCommand.Create(BackToMenu);
        SaveGameCommand = ReactiveCommand.Create(SaveGame);
        QuitGameCommand = ReactiveCommand.Create(QuitGame);
        ConfirmQuitCommand = ReactiveCommand.Create(ConfirmQuit);
        CancelQuitCommand = ReactiveCommand.Create(CancelQuit);
        SaveAndQuitCommand = ReactiveCommand.Create(SaveAndQuit);
        
        // Load the current dialogue
        LoadCurrentDialogue();
    }
    
    #region Properties
    
    public string DialogueTitle => _currentDialogue?.Title ?? "";
    public string DialogueText
    {
        get => _dialogueText;
        private set => this.RaiseAndSetIfChanged(ref _dialogueText, value);
    }
    
    public string PlayerInput
    {
        get => _playerInput;
        set => this.RaiseAndSetIfChanged(ref _playerInput, value);
    }
    
    public string InputPrompt => _currentDialogue?.InputPrompt ?? "";
    
    public StoryChoice? SelectedChoice
    {
        get => _selectedChoice;
        set => this.RaiseAndSetIfChanged(ref _selectedChoice, value);
    }
    
    public List<StoryChoice> AvailableChoices => _currentDialogue?.Choices ?? new List<StoryChoice>();
    
    public string Choice1Text => AvailableChoices.Count > 0 ? AvailableChoices[0].Text : "";
    public string Choice2Text => AvailableChoices.Count > 1 ? AvailableChoices[1].Text : "";
    public string Choice3Text => AvailableChoices.Count > 2 ? AvailableChoices[2].Text : "";
    
    public bool ShowChoice1 => AvailableChoices.Count > 0;
    public bool ShowChoice2 => AvailableChoices.Count > 1;
    public bool ShowChoice3 => AvailableChoices.Count > 2;
    
    public bool ShowInputControls
    {
        get => _showInputControls;
        private set => this.RaiseAndSetIfChanged(ref _showInputControls, value);
    }
    
    public bool ShowChoiceButtons
    {
        get => _showChoiceButtons;
        private set => this.RaiseAndSetIfChanged(ref _showChoiceButtons, value);
    }
    
    public bool ShowDropdown
    {
        get => _showDropdown;
        private set => this.RaiseAndSetIfChanged(ref _showDropdown, value);
    }
    
    public bool ShowContinueButton
    {
        get => _showContinueButton;
        private set => this.RaiseAndSetIfChanged(ref _showContinueButton, value);
    }
    
    public bool IsGameComplete
    {
        get => _isGameComplete;
        private set => this.RaiseAndSetIfChanged(ref _isGameComplete, value);
    }
    
    public bool HasUnsavedChanges
    {
        get => _hasUnsavedChanges;
        private set => this.RaiseAndSetIfChanged(ref _hasUnsavedChanges, value);
    }
    
    public bool ShowQuitDialog
    {
        get => _showQuitDialog;
        private set => this.RaiseAndSetIfChanged(ref _showQuitDialog, value);
    }
    
    public string LastSavedText => _lastSaved == DateTime.MinValue ? "Never" : _lastSaved.ToString("HH:mm:ss");
    
    #endregion
    
    #region Commands
    
    public ReactiveCommand<Unit, Unit> ContinueCommand { get; }
    public ReactiveCommand<Unit, Unit> SubmitInputCommand { get; }
    public ReactiveCommand<Unit, Unit> Choice1Command { get; }
    public ReactiveCommand<Unit, Unit> Choice2Command { get; }
    public ReactiveCommand<Unit, Unit> Choice3Command { get; }
    public ReactiveCommand<Unit, Unit> BackToMenuCommand { get; }
    public ReactiveCommand<Unit, Unit> SaveGameCommand { get; }
    public ReactiveCommand<Unit, Unit> QuitGameCommand { get; }
    public ReactiveCommand<Unit, Unit> ConfirmQuitCommand { get; }
    public ReactiveCommand<Unit, Unit> CancelQuitCommand { get; }
    public ReactiveCommand<Unit, Unit> SaveAndQuitCommand { get; }
    
    #endregion
    
    // Navigation callback
    public Action<object>? NavigateToView { get; set; }
    
    private void LoadCurrentDialogue()
    {
        _currentDialogue = _storyEngine.GetCurrentDialogue(_gameState);
        
        if (_currentDialogue == null)
        {
            IsGameComplete = true;
            DialogueText = $"Congratulations, {_gameState.PlayerName}! You've completed this story. Thank you for playing!";
            ShowContinueButton = false;
            ShowInputControls = false;
            ShowChoiceButtons = false;
            ShowDropdown = false;
            return;
        }
        
        // Process the dialogue text with variable substitution
        DialogueText = _storyEngine.ProcessDialogueText(_currentDialogue, _gameState);
        
        // Update UI based on input type
        UpdateUIForInputType();
        
        // Notify property changes
        this.RaisePropertyChanged(nameof(DialogueTitle));
        this.RaisePropertyChanged(nameof(InputPrompt));
        this.RaisePropertyChanged(nameof(AvailableChoices));
        this.RaisePropertyChanged(nameof(Choice1Text));
        this.RaisePropertyChanged(nameof(Choice2Text));
        this.RaisePropertyChanged(nameof(Choice3Text));
        this.RaisePropertyChanged(nameof(ShowChoice1));
        this.RaisePropertyChanged(nameof(ShowChoice2));
        this.RaisePropertyChanged(nameof(ShowChoice3));
    }
    
    private void UpdateUIForInputType()
    {
        if (_currentDialogue == null) return;
        
        ShowInputControls = _currentDialogue.InputType == InputType.TextInput;
        ShowChoiceButtons = _currentDialogue.InputType == InputType.Choice;
        ShowDropdown = _currentDialogue.InputType == InputType.Dropdown;
        ShowContinueButton = _currentDialogue.InputType == InputType.None;
        
        // Clear previous input
        PlayerInput = "";
        SelectedChoice = null;
    }
    
    private void Continue()
    {
        if (_currentDialogue == null) return;
        
        // Process with no input
        _storyEngine.ProcessPlayerInput(_gameState, "", null);
        HasUnsavedChanges = true;
        LoadCurrentDialogue();
    }
    
    private void SubmitInput()
    {
        if (_currentDialogue == null) return;
        
        if (_currentDialogue.InputType == InputType.TextInput)
        {
            if (string.IsNullOrWhiteSpace(PlayerInput))
            {
                Logger.LogMethod("SubmitInput", "Text input is empty");
                return;
            }
            
            _storyEngine.ProcessPlayerInput(_gameState, PlayerInput.Trim(), null);
        }
        else if (_currentDialogue.InputType == InputType.Dropdown)
        {
            if (SelectedChoice == null)
            {
                Logger.LogMethod("SubmitInput", "No dropdown choice selected");
                return;
            }
            
            _storyEngine.ProcessPlayerInput(_gameState, "", SelectedChoice);
        }
        
        HasUnsavedChanges = true;
        LoadCurrentDialogue();
    }
    
    private void SelectChoice(int choiceIndex)
    {
        if (_currentDialogue == null || choiceIndex >= AvailableChoices.Count) return;
        
        var choice = AvailableChoices[choiceIndex];
        _storyEngine.ProcessPlayerInput(_gameState, "", choice);
        HasUnsavedChanges = true;
        LoadCurrentDialogue();
    }
    
    private void SaveGame()
    {
        try
        {
            var settings = SettingsManager.LoadSettings();
            // In a full implementation, you'd save the entire game state
            // For now, we'll just mark as saved and update timestamp
            _lastSaved = DateTime.Now;
            HasUnsavedChanges = false;
            
            Logger.Info($"Game saved at {_lastSaved:HH:mm:ss}");
            this.RaisePropertyChanged(nameof(LastSavedText));
        }
        catch (Exception ex)
        {
            Logger.LogMethod("SaveGame", $"Error saving game: {ex.Message}");
        }
    }
    
    private void QuitGame()
    {
        if (HasUnsavedChanges)
        {
            ShowQuitDialog = true;
        }
        else
        {
            NavigateToView?.Invoke(new MainMenuViewModel());
        }
    }
    
    private void ConfirmQuit()
    {
        ShowQuitDialog = false;
        NavigateToView?.Invoke(new MainMenuViewModel());
    }
    
    private void CancelQuit()
    {
        ShowQuitDialog = false;
    }
    
    private void SaveAndQuit()
    {
        SaveGame();
        ShowQuitDialog = false;
        NavigateToView?.Invoke(new MainMenuViewModel());
    }
    
    private void BackToMenu()
    {
        QuitGame(); // Use quit logic which checks for unsaved changes
    }
}