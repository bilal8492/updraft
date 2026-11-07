using System.Text.Json;
using Microsoft.UI;
using Microsoft.UI.Xaml.Media;

namespace Updraft;

/// <summary>One finished in-app install, update or uninstall.</summary>
public sealed class HistoryEntry
{
    public DateTimeOffset When { get; set; }
    /// <summary>"Update", "Install" or "Uninstall".</summary>
    public string Action { get; set; } = "";
    public string Name { get; set; } = "";
    public string Id { get; set; } = "";
    public string FromVersion { get; set; } = "";
    public string ToVersion { get; set; } = "";
    /// <summary>"Succeeded", "Failed" or "Cancelled".</summary>
    public string Result { get; set; } = "";
    public int ExitCode { get; set; }

    // ---------- display helpers (not stored) ----------

    public string WhenText => When.ToLocalTime().ToString("g");

    public string Summary
    {
        get
        {
            var v = (FromVersion, ToVersion) switch
            {
                ({ Length: > 0 } f, { Length: > 0 } t) when f != t => $"{f}  →  {t}",
                ({ Length: > 0 } f, _) => f,
                (_, { Length: > 0 } t) => t,
                _ => "",
            };
            return v.Length > 0 ? $"{Action}  ·  {Id}  ·  {v}" : $"{Action}  ·  {Id}";
        }
    }

    public string ResultText => Result == "Failed" && ExitCode != 0 ? $"Failed (0x{ExitCode:X8})" : Result;

    public Brush ResultBrush => Result switch
    {
        "Succeeded" => new SolidColorBrush(ColorHelper.FromArgb(255, 34, 197, 94)),
        "Failed" => new SolidColorBrush(ColorHelper.FromArgb(255, 239, 68, 68)),
        _ => new SolidColorBrush(ColorHelper.FromArgb(255, 245, 158, 11)),
    };
}

/// <summary>
/// Local log of what Updraft installed, updated or removed, in %LOCALAPPDATA%\Updraft\history.json.
/// Never leaves the machine. Capped so the file stays small.
/// </summary>
public static class HistoryStore
{
    private const int MaxEntries = 500;

    private static string FilePath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Updraft", "history.json");

    private static List<HistoryEntry>? _cache;

    /// <summary>Newest first.</summary>
    public static IReadOnlyList<HistoryEntry> All => _cache ??= Load();

    private static List<HistoryEntry> Load()
    {
        try
        {
            if (File.Exists(FilePath))
            {
                var list = JsonSerializer.Deserialize<List<HistoryEntry>>(File.ReadAllText(FilePath)) ?? new();
                return list.Where(e => e is not null && e.Id.Length is > 0 and <= 200)
                    .OrderByDescending(e => e.When).Take(MaxEntries).ToList();
            }
        }
        catch { /* corrupt file: start a fresh history */ }
        return new();
    }

    public static void Add(HistoryEntry entry)
    {
        var list = (List<HistoryEntry>)All;
        list.Insert(0, entry);
        if (list.Count > MaxEntries) list.RemoveRange(MaxEntries, list.Count - MaxEntries);
        Save(list);
    }

    public static void Clear()
    {
        _cache = new();
        Save(_cache);
    }

    private static void Save(List<HistoryEntry> list)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
            File.WriteAllText(FilePath, JsonSerializer.Serialize(list, new JsonSerializerOptions { WriteIndented = true }));
        }
        catch { /* best effort: history is a convenience, never block a run on it */ }
    }
}

/// <summary>Reads the JSON written by <c>winget export</c>, so a saved app list can be reinstalled on any PC.</summary>
public static class BackupFile
{
    /// <summary>Packages listed in an export file. Throws <see cref="FormatException"/> if it isn't one.</summary>
    public static List<Pkg> Parse(string json)
    {
        JsonDocument doc;
        try { doc = JsonDocument.Parse(json); }
        catch (JsonException ex) { throw new FormatException("The file is not valid JSON.", ex); }

        using (doc)
        {
            if (doc.RootElement.ValueKind != JsonValueKind.Object
                || !doc.RootElement.TryGetProperty("Sources", out var sources)
                || sources.ValueKind != JsonValueKind.Array)
                throw new FormatException("This doesn't look like a winget export file (no \"Sources\" list).");

            var result = new List<Pkg>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var src in sources.EnumerateArray())
            {
                string source = src.TryGetProperty("SourceDetails", out var d) && d.TryGetProperty("Name", out var n)
                                && n.ValueKind == JsonValueKind.String ? n.GetString() ?? "" : "";
                if (!src.TryGetProperty("Packages", out var pkgs) || pkgs.ValueKind != JsonValueKind.Array) continue;

                foreach (var p in pkgs.EnumerateArray())
                {
                    if (!p.TryGetProperty("PackageIdentifier", out var idEl) || idEl.ValueKind != JsonValueKind.String) continue;
                    var id = idEl.GetString() ?? "";
                    if (!WingetService.IsSafeId(id) || !seen.Add(id)) continue;   // the id ends up on a command line
                    var ver = p.TryGetProperty("Version", out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() ?? "" : "";
                    result.Add(new Pkg { Name = id, Id = id, Version = ver, Source = source });
                }
            }
            return result;
        }
    }
}
