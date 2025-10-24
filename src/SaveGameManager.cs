using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace FullCrisis3;

/// <summary>
/// Represents a saved game with metadata
/// </summary>
public class SaveGameData
{
    public string SaveId { get; set; } = "";
    public string GameName { get; set; } = "";
    public string PlayerName { get; set; } = "";
    public DateTime SavedAt { get; set; } = DateTime.UtcNow;
    public StoryState GameState { get; set; } = new();
    public string CurrentDialogueTitle { get; set; } = "";
}

/// <summary>
/// Manages saving and loading game states
/// </summary>
[AutoLog]
public static class SaveGameManager
{
    private static readonly string SaveDirectory = Path.Combine(AppSettings.AppDataLocation, "Saves");
    
    static SaveGameManager()
    {
        // Ensure save directory exists
        Directory.CreateDirectory(SaveDirectory);
    }
    
    /// <summary>
    /// Save current game state to disk
    /// </summary>
    public static string SaveGame(StoryEngine storyEngine, StoryState gameState, string currentDialogueTitle)
    {
        try
        {
            var story = storyEngine.GetStory(gameState.StoryId);
            if (story == null)
            {
                throw new InvalidOperationException($"Story '{gameState.StoryId}' not found");
            }
            
            var saveId = $"{gameState.StoryId}_{gameState.PlayerName}_{DateTime.UtcNow:yyyyMMdd_HHmmss}";
            var saveData = new SaveGameData
            {
                SaveId = saveId,
                GameName = story.Title,
                PlayerName = gameState.PlayerName,
                SavedAt = DateTime.UtcNow,
                GameState = CloneGameState(gameState),
                CurrentDialogueTitle = currentDialogueTitle
            };
            
            var json = JsonSerializer.Serialize(saveData, new JsonSerializerOptions
            {
                WriteIndented = true
            });
            
            var saveFilePath = Path.Combine(SaveDirectory, $"{saveId}.json");
            File.WriteAllText(saveFilePath, json);
            
            Logger.Info($"Game saved: {saveFilePath}");
            return saveId;
        }
        catch (Exception ex)
        {
            Logger.LogMethod("SaveGame", $"Error saving game: {ex.Message}");
            throw;
        }
    }
    
    /// <summary>
    /// Load all saved games
    /// </summary>
    public static List<SaveGameData> GetAllSaveGames()
    {
        try
        {
            var saveFiles = Directory.GetFiles(SaveDirectory, "*.json");
            var saveGames = new List<SaveGameData>();
            
            foreach (var file in saveFiles)
            {
                try
                {
                    var json = File.ReadAllText(file);
                    var saveData = JsonSerializer.Deserialize<SaveGameData>(json);
                    if (saveData != null)
                    {
                        saveGames.Add(saveData);
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogMethod("GetAllSaveGames", $"Error loading save file {file}: {ex.Message}");
                    // Continue loading other files
                }
            }
            
            // Sort by save date, newest first
            return saveGames.OrderByDescending(s => s.SavedAt).ToList();
        }
        catch (Exception ex)
        {
            Logger.LogMethod("GetAllSaveGames", $"Error getting save games: {ex.Message}");
            return new List<SaveGameData>();
        }
    }
    
    /// <summary>
    /// Load a specific saved game
    /// </summary>
    public static SaveGameData? LoadSaveGame(string saveId)
    {
        Logger.Debug($"LoadSaveGame: Attempting to load save game with ID: {saveId}");
        
        try
        {
            var saveFilePath = Path.Combine(SaveDirectory, $"{saveId}.json");
            Logger.Trace($"LoadSaveGame: Looking for save file at: {saveFilePath}");
            
            if (!File.Exists(saveFilePath))
            {
                Logger.Info($"LoadSaveGame: Save file not found: {saveFilePath}");
                Logger.Debug($"LoadSaveGame: Available files in save directory: {string.Join(", ", Directory.GetFiles(SaveDirectory))}");
                return null;
            }
            
            Logger.Debug($"LoadSaveGame: Reading save file contents...");
            var json = File.ReadAllText(saveFilePath);
            Logger.Trace($"LoadSaveGame: Save file content length: {json.Length} characters");
            
            if (string.IsNullOrWhiteSpace(json))
            {
                Logger.Info($"LoadSaveGame: Save file is empty: {saveFilePath}");
                return null;
            }
            
            Logger.Debug($"LoadSaveGame: Deserializing save data from JSON...");
            var saveData = JsonSerializer.Deserialize<SaveGameData>(json);
            
            if (saveData == null)
            {
                Logger.Info($"LoadSaveGame: Failed to deserialize save data - result was null");
                return null;
            }
            
            Logger.Info($"LoadSaveGame: Successfully loaded save game '{saveData.GameName}' from {saveFilePath}");
            Logger.Debug($"LoadSaveGame: Loaded save data - Player: {saveData.PlayerName}, Game: {saveData.GameName}, Saved: {saveData.SavedAt}");
            Logger.Trace($"LoadSaveGame: Game state has {(saveData.GameState?.Variables?.Count ?? 0)} variable entries");
            
            return saveData;
        }
        catch (JsonException ex)
        {
            Logger.Info($"LoadSaveGame: JSON parsing error for save game {saveId}: {ex.Message}");
            Logger.Debug($"LoadSaveGame: JSON exception details: {ex}");
            return null;
        }
        catch (Exception ex)
        {
            Logger.Info($"LoadSaveGame: Error loading save game {saveId}: {ex.Message}");
            Logger.Debug($"LoadSaveGame: Exception details: {ex}");
            return null;
        }
    }
    
    /// <summary>
    /// Delete a saved game
    /// </summary>
    public static bool DeleteSaveGame(string saveId)
    {
        try
        {
            var saveFilePath = Path.Combine(SaveDirectory, $"{saveId}.json");
            if (File.Exists(saveFilePath))
            {
                File.Delete(saveFilePath);
                Logger.Info($"Save game deleted: {saveFilePath}");
                return true;
            }
            return false;
        }
        catch (Exception ex)
        {
            Logger.LogMethod("DeleteSaveGame", $"Error deleting save game {saveId}: {ex.Message}");
            return false;
        }
    }
    
    /// <summary>
    /// Create a deep copy of game state for saving
    /// </summary>
    private static StoryState CloneGameState(StoryState original)
    {
        return new StoryState
        {
            StoryId = original.StoryId,
            PlayerName = original.PlayerName,
            CurrentDialogueId = original.CurrentDialogueId,
            Variables = new Dictionary<string, string>(original.Variables),
            LastPlayed = DateTime.UtcNow
        };
    }
}