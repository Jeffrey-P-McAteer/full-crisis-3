using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;

namespace FullCrisis3;

[AutoLog]
public class LoadGameViewModel : ViewModelBase
{
    private List<SaveGameData> _savedGames = new();
    private SaveGameData? _selectedSave;
    private bool _isLoading = true;
    
    public LoadGameViewModel()
    {
        // Commands
        PlaySaveCommand = ReactiveCommand.Create<SaveGameData?>(PlaySave);
        DeleteSelectedCommand = ReactiveCommand.Create(DeleteSelected, this.WhenAnyValue(x => x.SelectedSave).Select(save => save != null));
        BackCommand = ReactiveCommand.Create(Back);
        RefreshCommand = ReactiveCommand.Create(RefreshSaveGames);
        
        // Load saved games
        RefreshSaveGames();
    }
    
    #region Properties
    
    public List<SaveGameData> SavedGames
    {
        get => _savedGames;
        private set => this.RaiseAndSetIfChanged(ref _savedGames, value);
    }
    
    public SaveGameData? SelectedSave
    {
        get => _selectedSave;
        set => this.RaiseAndSetIfChanged(ref _selectedSave, value);
    }
    
    public bool IsLoading
    {
        get => _isLoading;
        private set => this.RaiseAndSetIfChanged(ref _isLoading, value);
    }
    
    public bool HasSaveGames => SavedGames.Count > 0;
    public bool NoSaveGames => SavedGames.Count == 0 && !IsLoading;
    
    #endregion
    
    #region Commands
    
    public ReactiveCommand<SaveGameData?, Unit> PlaySaveCommand { get; }
    public ReactiveCommand<Unit, Unit> DeleteSelectedCommand { get; }
    public ReactiveCommand<Unit, Unit> BackCommand { get; }
    public ReactiveCommand<Unit, Unit> RefreshCommand { get; }
    
    #endregion
    
    #region Navigation
    
    public Action<object>? NavigateToView { get; set; }
    
    #endregion
    
    #region Methods
    
    private void RefreshSaveGames()
    {
        IsLoading = true;
        
        try
        {
            SavedGames = SaveGameManager.GetAllSaveGames();
            SelectedSave = SavedGames.FirstOrDefault();
        }
        finally
        {
            IsLoading = false;
            this.RaisePropertyChanged(nameof(HasSaveGames));
            this.RaisePropertyChanged(nameof(NoSaveGames));
        }
    }
    
    private void PlaySave(SaveGameData? saveData)
    {
        if (saveData == null)
        {
            Logger.Info("PlaySave: No save data provided");
            return;
        }
        
        Logger.Info($"PlaySave: Attempting to load save game '{saveData.GameName}' (ID: {saveData.SaveId})");
        Logger.Debug($"PlaySave: Save game details - Player: {saveData.PlayerName}, Saved: {saveData.SavedAt}");
        
        try
        {
            // Load the save game data from disk to get the latest state
            Logger.Debug($"PlaySave: Loading save game from disk...");
            var loadedSaveData = SaveGameManager.LoadSaveGame(saveData.SaveId);
            if (loadedSaveData == null)
            {
                Logger.Info($"PlaySave: Failed to load save game from disk - SaveGameManager.LoadSaveGame returned null for ID: {saveData.SaveId}");
                Logger.Debug($"PlaySave: This could indicate the save file is missing, corrupted, or inaccessible");
                return;
            }
            
            Logger.Debug($"PlaySave: Save game loaded successfully from disk");
            Logger.Trace($"PlaySave: Loaded game state has {(loadedSaveData.GameState?.Variables?.Count ?? 0)} variable entries");
            
            // Create story engine and register stories
            Logger.Debug($"PlaySave: Creating story engine and registering stories...");
            var storyEngine = new StoryEngine();
            var stories = ExampleStories.GetAllStories();
            Logger.Trace($"PlaySave: Found {stories.Count} stories to register");
            
            foreach (var story in stories)
            {
                Logger.Trace($"PlaySave: Registering story: {story.Id}");
                storyEngine.RegisterStory(story);
            }
            
            Logger.Debug($"PlaySave: Stories registered successfully");
            
            // Create game view model with loaded state
            Logger.Debug($"PlaySave: Creating GameViewModel with loaded state...");
            var gameViewModel = new GameViewModel(storyEngine, loadedSaveData.GameState);
            Logger.Info($"PlaySave: GameViewModel created successfully, navigating to game view");
            
            NavigateToView?.Invoke(gameViewModel);
            Logger.Info($"PlaySave: Successfully loaded and started save game '{saveData.GameName}'");
        }
        catch (Exception ex)
        {
            Logger.Info($"PlaySave: Error loading game '{saveData.GameName}' (ID: {saveData.SaveId}): {ex.Message}");
            Logger.Debug($"PlaySave: Exception details: {ex}");
            Logger.Trace($"PlaySave: Full exception stack trace: {ex.StackTrace}");
        }
    }
    
    private void DeleteSelected()
    {
        if (SelectedSave == null) return;
        
        try
        {
            if (SaveGameManager.DeleteSaveGame(SelectedSave.SaveId))
            {
                RefreshSaveGames();
                Logger.Info($"Deleted save game: {SelectedSave.SaveId}");
            }
        }
        catch (Exception ex)
        {
            Logger.LogMethod("DeleteSelected", $"Error deleting save game: {ex.Message}");
        }
    }
    
    private void Back()
    {
        NavigateToView?.Invoke(new MainMenuViewModel());
    }
    
    #endregion
}