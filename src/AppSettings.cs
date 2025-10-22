using System;
using System.IO;
using System.Text.Json;

namespace FullCrisis3;

[AutoLog]
public class AppSettings
{
    public bool AudioEnabled { get; set; } = true;
    public bool BackgroundMusicEnabled { get; set; } = true;

    private static readonly string _appDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "FullCrisis3");
    private static readonly string _settingsFilePath = Path.Combine(_appDataPath, "settings.json");

    public static string AppDataLocation => _appDataPath;

    public static AppSettings Load()
    {
        try
        {
            if (File.Exists(_settingsFilePath))
            {
                var json = File.ReadAllText(_settingsFilePath);
                return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
            }
        }
        catch (Exception ex)
        {
            Logger.Info($"Failed to load settings: {ex.Message}");
        }
        
        return new AppSettings();
    }

    public void Save()
    {
        try
        {
            Directory.CreateDirectory(_appDataPath);
            var json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_settingsFilePath, json);
        }
        catch (Exception ex)
        {
            Logger.Info($"Failed to save settings: {ex.Message}");
        }
    }
}