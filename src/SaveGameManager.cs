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
        try
        {
            var saveFilePath = Path.Combine(SaveDirectory, $"{saveId}.json");
            if (!File.Exists(saveFilePath))
            {
                Logger.LogMethod("LoadSaveGame", $"Save file not found: {saveFilePath}");
                return null;
            }
            
            var json = File.ReadAllText(saveFilePath);
            var saveData = JsonSerializer.Deserialize<SaveGameData>(json);
            
            Logger.Info($"Game loaded: {saveFilePath}");
            return saveData;
        }
        catch (Exception ex)
        {
            Logger.LogMethod("LoadSaveGame", $"Error loading save game {saveId}: {ex.Message}");
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