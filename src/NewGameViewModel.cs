using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;

namespace FullCrisis3;

[AutoLog]
public class NewGameViewModel : ViewModelBase
{
    private string _playerName = "";
    private Story? _selectedStory;
    private readonly StoryEngine _storyEngine;
    
    public NewGameViewModel()
    {
        _storyEngine = new StoryEngine();
        ExampleStories.RegisterAllStories(_storyEngine);
        
        // Load settings and set default player name
        LoadPlayerSettings();
        
        // Commands
        PlayGameCommand = ReactiveCommand.Create(PlayGame);
        BackCommand = ReactiveCommand.Create(GoBack);
        
        // Available stories
        AvailableStories = _storyEngine.GetAvailableStories().ToList();
        if (AvailableStories.Count > 0)
        {
            SelectedStory = AvailableStories[0];
        }
    }
    
    public string PlayerName
    {
        get => _playerName;
        set => this.RaiseAndSetIfChanged(ref _playerName, value);
    }
    
    public Story? SelectedStory
    {
        get => _selectedStory;
        set => this.RaiseAndSetIfChanged(ref _selectedStory, value);
    }
    
    public List<Story> AvailableStories { get; }
    
    public ReactiveCommand<Unit, Unit> PlayGameCommand { get; }
    public ReactiveCommand<Unit, Unit> BackCommand { get; }
    
    // Navigation callback
    public Action<object>? NavigateToView { get; set; }
    
    private void LoadPlayerSettings()
    {
        try
        {
            var settings = SettingsManager.LoadSettings();
            
            // Try to get last used player name or fall back to system username
            PlayerName = settings.LastPlayerName ?? Environment.UserName ?? "Player";
        }
        catch (Exception ex)
        {
            Logger.LogMethod("LoadPlayerSettings", $"Error loading settings: {ex.Message}");
            PlayerName = Environment.UserName ?? "Player";
        }
    }
    
    private void SavePlayerSettings()
    {
        try
        {
            var settings = SettingsManager.LoadSettings();
            settings.LastPlayerName = PlayerName;
            SettingsManager.SaveSettings(settings);
        }
        catch (Exception ex)
        {
            Logger.LogMethod("SavePlayerSettings", $"Error saving settings: {ex.Message}");
        }
    }
    
    private void PlayGame()
    {
        if (SelectedStory == null)
        {
            Logger.LogMethod("PlayGame", "No story selected");
            return;
        }
        
        if (string.IsNullOrWhiteSpace(PlayerName))
        {
            Logger.LogMethod("PlayGame", "Player name is empty");
            return;
        }
        
        // Save player name for future use
        SavePlayerSettings();
        
        // Create new game state
        var gameState = _storyEngine.StartNewGame(SelectedStory.Id, PlayerName.Trim());
        
        // Navigate to game view
        var gameViewModel = new GameViewModel(_storyEngine, gameState);
        NavigateToView?.Invoke(gameViewModel);
    }
    
    private void GoBack()
    {
        NavigateToView?.Invoke(new MainMenuViewModel());
    }
}