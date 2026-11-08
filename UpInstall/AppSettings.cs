using System.Text.Json;

namespace UpInstall;

/// <summary>
/// User preferences, stored as JSON in %LOCALAPPDATA%\UpInstall\settings.json.
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
    /// <summary>Motion and transitions. Windows' own "Animation effects" setting can still turn them off.</summary>
    public bool Animations { get; set; } = true;

    /// <summary>Package ids the user chose to skip in the Upgrades list.</summary>
    public List<string> Skipped { get; set; } = new();

    /// <summary>Package id → the one version the user chose to skip. A newer version shows up again.</summary>
    public Dictionary<string, string> SkippedVersions { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Last window placement, in physical pixels. Width 0 means "never saved".</summary>
    public int WindowX { get; set; }
    public int WindowY { get; set; }
    public int WindowWidth { get; set; }
    public int WindowHeight { get; set; }
    public bool WindowMaximized { get; set; }

    public bool IsSkipped(string id) => Skipped.Contains(id, StringComparer.OrdinalIgnoreCase);

    /// <summary>True when this exact update is hidden: the whole app is skipped, or just this version.</summary>
    public bool IsSkipped(string id, string available) =>
        IsSkipped(id) || (available.Length > 0 && SkippedVersions.TryGetValue(id, out var v) && v == available);

    /// <summary>The version that is skipped for this id, or null when the skip covers every version.</summary>
    public string? SkippedVersionOf(string id) =>
        !IsSkipped(id) && SkippedVersions.TryGetValue(id, out var v) ? v : null;

    public void SetSkipped(string id, bool skipped)
    {
        Skipped.RemoveAll(x => string.Equals(x, id, StringComparison.OrdinalIgnoreCase));
        SkippedVersions.Remove(id);
        if (skipped) Skipped.Add(id);
        Save();
    }

    public void SkipVersion(string id, string version)
    {
        Skipped.RemoveAll(x => string.Equals(x, id, StringComparison.OrdinalIgnoreCase));
        SkippedVersions[id] = version;
        Save();
    }

    /// <summary>Forget version skips that no longer match what winget offers (a newer version came out).</summary>
    public void PruneSkippedVersions(IReadOnlyDictionary<string, string> offered)
    {
        var stale = SkippedVersions.Where(kv => offered.TryGetValue(kv.Key, out var avail) && avail != kv.Value)
            .Select(kv => kv.Key).ToList();
        if (stale.Count == 0) return;
        foreach (var id in stale) SkippedVersions.Remove(id);
        Save();
    }

    /// <summary>A detached copy, for editing in a dialog without touching <see cref="Current"/> until saved.</summary>
    public AppSettings Clone() => JsonSerializer.Deserialize<AppSettings>(JsonSerializer.Serialize(this)) is { } c ? Sanitize(c) : new();

    private static AppSettings? _current;
    public static AppSettings Current
    {
        get => _current ??= Load();
        private set => _current = value;
    }

    /// <summary>Where settings and history live. Demo mode keeps its own copy so it never touches real data.</summary>
    public static string DataDir => WingetService.Demo
        ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "UpInstall", "demo")
        : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "UpInstall");

    private static string FilePath => Path.Combine(DataDir, "settings.json");

    /// <summary>
    /// The app was called Updraft before v0.6 and kept its data in %LOCALAPPDATA%\Updraft. On the first run after the
    /// rename, move that folder over so settings, history and skipped versions carry across. Call before anything
    /// reads <see cref="DataDir"/>.
    /// </summary>
    public static void MigrateFromUpdraft()
    {
        if (WingetService.Demo) return; // demo mode never touches real data
        try
        {
            var root = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var legacy = Path.Combine(root, "Updraft");
            var current = Path.Combine(root, "UpInstall");
            if (Directory.Exists(legacy) && !Directory.Exists(current))
                Directory.Move(legacy, current);
        }
        catch { /* best effort: start with defaults rather than fail to launch */ }
    }

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
        s.Skipped = (s.Skipped ?? new()).Where(x => !string.IsNullOrWhiteSpace(x) && x.Length <= 200)
            .Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        s.SkippedVersions = new Dictionary<string, string>(
            (s.SkippedVersions ?? new()).Where(kv => !string.IsNullOrWhiteSpace(kv.Key) && kv.Key.Length <= 200
                                                     && !string.IsNullOrWhiteSpace(kv.Value) && kv.Value.Length <= 100)
                .DistinctBy(kv => kv.Key, StringComparer.OrdinalIgnoreCase),
            StringComparer.OrdinalIgnoreCase);
        if (s.WindowWidth is < 0 or > 20000 || s.WindowHeight is < 0 or > 20000) s.WindowWidth = s.WindowHeight = 0;
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
