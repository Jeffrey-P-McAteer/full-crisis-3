namespace FullCrisis3;

/// <summary>
/// Centralized settings management
/// </summary>
public static class SettingsManager
{
    public static AppSettings LoadSettings()
    {
        return AppSettings.Load();
    }
    
    public static void SaveSettings(AppSettings settings)
    {
        settings.Save();
    }
}