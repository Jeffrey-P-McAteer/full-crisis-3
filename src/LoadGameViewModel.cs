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
        PlaySelectedCommand = ReactiveCommand.Create(PlaySelected, this.WhenAnyValue(x => x.SelectedSave).Select(save => save != null));
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
    
    public ReactiveCommand<Unit, Unit> PlaySelectedCommand { get; }
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
    
    private void PlaySelected()
    {
        if (SelectedSave == null) return;
        
        try
        {
            // Load the save game data
            var saveData = SaveGameManager.LoadSaveGame(SelectedSave.SaveId);
            if (saveData == null)
            {
                Logger.LogMethod("PlaySelected", "Failed to load save game");
                return;
            }
            
            // Create story engine and register stories
            var storyEngine = new StoryEngine();
            var stories = ExampleStories.GetAllStories();
            foreach (var story in stories)
            {
                storyEngine.RegisterStory(story);
            }
            
            // Create game view model with loaded state
            var gameViewModel = new GameViewModel(storyEngine, saveData.GameState);
            NavigateToView?.Invoke(gameViewModel);
        }
        catch (Exception ex)
        {
            Logger.LogMethod("PlaySelected", $"Error loading game: {ex.Message}");
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