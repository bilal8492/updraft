using System.Text.Json;

namespace Updraft;

/// <summary>
/// User preferences, stored as JSON in %LOCALAPPDATA%\Updraft\settings.json.
/// Local only: nothing here is ever sent anywhere.
/// </summary>
public sealed class AppSettings
{
    /// <summary>"default" (winget decides), "silent" or "interactive".</summary>
    public string InstallMode { get; set; } = "default";
    public bool AcceptAgreements { get; set; } = true;
    public bool DefaultAdmin { get; set; }
    /// <summary>"auto" (Windows Terminal, else PowerShell), "wt" or "powershell".</summary>
    public string Terminal { get; set; } = "auto";
    /// <summary>"system", "light" or "dark".</summary>
    public string Theme { get; set; } = "system";

    public static AppSettings Current { get; private set; } = Load();

    private static string FilePath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Updraft", "settings.json");

    private static AppSettings Load()
    {
        try
        {
            if (File.Exists(FilePath))
                return Sanitize(JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(FilePath)) ?? new());
        }
        catch { /* corrupt or unreadable file: fall back to defaults */ }
        return new();
    }

    // Never trust values from disk: clamp unknown strings back to defaults.
    private static AppSettings Sanitize(AppSettings s)
    {
        if (s.InstallMode is not ("default" or "silent" or "interactive")) s.InstallMode = "default";
        if (s.Terminal is not ("auto" or "wt" or "powershell")) s.Terminal = "auto";
        if (s.Theme is not ("system" or "light" or "dark")) s.Theme = "system";
        return s;
    }

    public void Save()
    {
        Current = Sanitize(this);
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
            File.WriteAllText(FilePath, JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true }));
        }
        catch { /* best effort: settings still apply for this session */ }
    }
}
