using System.ComponentModel;
using System.Text;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;

namespace UpInstall;

public enum RunState { None, Queued, Running, Succeeded, Failed, Cancelled }

public sealed class Pkg : INotifyPropertyChanged
{
    public string Name { get; set; } = "";
    public string Id { get; set; } = "";
    public string Version { get; set; } = "";
    public string Available { get; set; } = "";
    public string Source { get; set; } = "";

    /// <summary>The list this row belongs to: "upgrade", "installed", "discover" or "import".</summary>
    public string Mode { get; set; } = "";

    // ---------- look ----------

    private string? _initials;
    public string Initials => _initials ??= MakeInitials(Name);
    public Brush AvatarBrush => Avatars.For(Id, Name);

    // Badge: visible when winget reported a source, with a distinct tone per source.
    public Visibility SourceVisibility => string.IsNullOrEmpty(Source) ? Visibility.Collapsed : Visibility.Visible;
    public string SourceLabel => Source;
    public string SourceTone => Source.ToLowerInvariant() switch { "winget" => "Accent", "msstore" => "Violet", _ => "Neutral" };

    public string VersionText => string.IsNullOrEmpty(Available) ? Version : $"{Version}  →  {Available}";

    /// <summary>The version this row will end up on: a chosen version, else the newest one winget offers.</summary>
    public string TargetText => TargetVersion.Length > 0 ? TargetVersion : Available.Length > 0 ? Available : Version;
    public Visibility UpdateArrowVisibility => Available.Length > 0 || TargetVersion.Length > 0 ? Visibility.Visible : Visibility.Collapsed;
    public Visibility PlainVersionVisibility => UpdateArrowVisibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;

    private UpdateKind? _kind;
    /// <summary>Major, minor or patch: which part of the version number the update changes.</summary>
    public UpdateKind Kind => _kind ??= Mode == "upgrade" ? Versions.Classify(Version, Available) : UpdateKind.None;
    public string KindLabel => Kind switch { UpdateKind.Major => "Major", UpdateKind.Minor => "Minor", UpdateKind.Patch => "Patch", _ => "" };
    public string KindTone => Kind switch { UpdateKind.Major => "Warning", UpdateKind.Minor => "Accent", _ => "Neutral" };
    public Visibility KindVisibility => Kind == UpdateKind.None ? Visibility.Collapsed : Visibility.Visible;

    /// <summary>winget can only act on rows with a real package id (not "ARP\…" or "MSIX\…" entries).</summary>
    public bool IsManageable => WingetService.IsSafeId(Id);

    public string ActionLabel => Mode switch { "upgrade" => "Update", "installed" => "Uninstall", _ => "Install" };
    public string ActionGlyph => Mode switch { "upgrade" => "", "installed" => "", _ => "" };
    public Visibility QuickActionVisibility => IsManageable && !(Mode == "import" && IsInstalled) ? Visibility.Visible : Visibility.Collapsed;

    // ---------- chosen version ----------

    private string _targetVersion = "";
    /// <summary>A specific version picked by the user (empty means the latest).</summary>
    public string TargetVersion
    {
        get => _targetVersion;
        set
        {
            if (_targetVersion == value) return;
            _targetVersion = value;
            Notify(nameof(TargetVersion), nameof(TargetText), nameof(UpdateArrowVisibility), nameof(PlainVersionVisibility), nameof(VersionText));
        }
    }

    /// <summary>Two letters for the app's tile: "Visual Studio Code" → "VS", "PowerToys" → "PT", "7-Zip" → "7Z".</summary>
    public static string MakeInitials(string name)
    {
        var words = System.Text.RegularExpressions.Regex.Split(name, @"[\s\-_.()\[\]/,:+]+")
            .Where(w => w.Length > 0 && char.IsLetterOrDigit(w[0])).ToList();
        if (words.Count == 0) return "?";
        // A leading vendor name says little about the app itself ("Microsoft Teams" → "T").
        string[] vendors = { "microsoft", "mozilla", "google", "adobe", "the", "apple", "amazon" };
        if (words.Count > 1 && vendors.Contains(words[0].ToLowerInvariant())) words.RemoveAt(0);

        var first = words[0];
        if (words.Count > 1 && (char.IsUpper(words[1][0]) || char.IsDigit(words[1][0])))
            return $"{char.ToUpperInvariant(first[0])}{char.ToUpperInvariant(words[1][0])}";
        var inner = first.Skip(1).FirstOrDefault(char.IsUpper);   // camel case: PowerToys, GitHub
        if (inner != default && !first.All(c => !char.IsLetter(c) || char.IsUpper(c)))
            return $"{char.ToUpperInvariant(first[0])}{inner}";
        return char.ToUpperInvariant(first[0]).ToString();
    }

    private bool _selected;
    public bool IsSelected
    {
        get => _selected;
        set
        {
            if (_selected == value) return;
            _selected = value;
            Notify(nameof(IsSelected));
        }
    }

    // ---------- pin / skip ----------

    private bool _isPinned, _isSkipped, _isInstalled;
    private string _skipLabel = "skipped";

    /// <summary>Already on this PC (used when restoring an app list, so those rows aren't reinstalled).</summary>
    public bool IsInstalled
    {
        get => _isInstalled;
        set { if (_isInstalled == value) return; _isInstalled = value; Notify(nameof(IsInstalled), nameof(InstalledVisibility), nameof(QuickActionVisibility)); }
    }

    public Visibility InstalledVisibility => _isInstalled ? Visibility.Visible : Visibility.Collapsed;

    /// <summary>Badge text: "skipped", or "skipped 1.2.3" when only that version is skipped.</summary>
    public string SkipLabel
    {
        get => _skipLabel;
        set { if (_skipLabel == value) return; _skipLabel = value; Notify(nameof(SkipLabel)); }
    }

    /// <summary>Held at its current version via <c>winget pin</c>.</summary>
    public bool IsPinned
    {
        get => _isPinned;
        set { if (_isPinned == value) return; _isPinned = value; Notify(nameof(IsPinned), nameof(PinnedVisibility)); }
    }

    /// <summary>Hidden from the update list by the user (stored locally by UpInstall, not by winget).</summary>
    public bool IsSkipped
    {
        get => _isSkipped;
        set { if (_isSkipped == value) return; _isSkipped = value; Notify(nameof(IsSkipped), nameof(SkippedVisibility)); }
    }

    public Visibility PinnedVisibility => _isPinned ? Visibility.Visible : Visibility.Collapsed;
    public Visibility SkippedVisibility => _isSkipped ? Visibility.Visible : Visibility.Collapsed;

    // ---------- in-app run state ----------

    private const int MaxLog = 24_000;
    private readonly StringBuilder _log = new();

    public RunState State { get; private set; }
    public string RunStatus { get; private set; } = "";
    public double Progress { get; private set; }
    public bool Indeterminate { get; private set; } = true;
    public string Log => _log.ToString();

    public Visibility StatusVisibility => State == RunState.None ? Visibility.Collapsed : Visibility.Visible;
    public Visibility ProgressVisibility => State == RunState.Running ? Visibility.Visible : Visibility.Collapsed;
    /// <summary>The row's status line uses the pill tones so it matches the badges in both themes.</summary>
    public string StatusTone => State switch
    {
        RunState.Succeeded => "Success",
        RunState.Failed => "Danger",
        RunState.Cancelled => "Warning",
        RunState.Running => "Accent",
        _ => "Neutral",
    };

    public void ResetRun()
    {
        _log.Clear();
        Progress = 0;
        Indeterminate = true;
        SetState(RunState.None, "");
        Notify(nameof(Log), nameof(Progress), nameof(Indeterminate));
    }

    public void SetState(RunState state, string text)
    {
        State = state;
        RunStatus = text;
        Notify(nameof(State), nameof(RunStatus), nameof(StatusVisibility), nameof(ProgressVisibility), nameof(StatusTone));
    }

    public void SetStatusText(string text)
    {
        RunStatus = text;
        Notify(nameof(RunStatus));
    }

    public void SetProgress(double percent)
    {
        Progress = Math.Clamp(percent, 0, 100);
        Indeterminate = false;
        Notify(nameof(Progress), nameof(Indeterminate));
    }

    public void AppendLog(string line)
    {
        _log.AppendLine(line);
        if (_log.Length > MaxLog) _log.Remove(0, _log.Length - MaxLog);   // keep the tail only
        Notify(nameof(Log));
    }

    private void Notify(params string[] names)
    {
        foreach (var n in names) PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>What screen readers announce for the row.</summary>
    public override string ToString() => Name;
}

/// <summary>What <c>winget show</c> tells us about a package. Any field can be empty.</summary>
public sealed class PkgDetail
{
    public string Version { get; init; } = "";
    public string Description { get; init; } = "";
    public string Publisher { get; init; } = "";
    public string PublisherUrl { get; init; } = "";
    public string Author { get; init; } = "";
    public string License { get; init; } = "";
    public string LicenseUrl { get; init; } = "";
    public string Homepage { get; init; } = "";
    public string ReleaseNotes { get; init; } = "";
    public string ReleaseNotesUrl { get; init; } = "";
    public string ReleaseDate { get; init; } = "";
    public string InstallerType { get; init; } = "";
    public IReadOnlyList<string> Tags { get; init; } = Array.Empty<string>();

    public static readonly PkgDetail Empty = new();
}

/// <summary>Deterministic gradient tiles: the same app always gets the same colour (by publisher, from the id).</summary>
public static class Avatars
{
    private static readonly (string From, string To)[] Colors =
    {
        ("#4F46E5", "#2F7BE6"), // indigo → blue
        ("#0284C7", "#0891B2"), // sky → cyan
        ("#0D9488", "#059669"), // teal → emerald
        ("#7C3AED", "#C026D3"), // violet → fuchsia
        ("#E11D48", "#EA580C"), // rose → orange
        ("#D97706", "#DC2626"), // amber → red
        ("#4D7C0F", "#15803D"), // lime → green
        ("#475569", "#1E293B"), // slate
        ("#DB2777", "#9333EA"), // pink → purple
        ("#2563EB", "#7C3AED"), // blue → violet
    };

    private static readonly Brush?[] Cache = new Brush?[Colors.Length];

    public static int Count => Colors.Length;

    public static Brush Palette(int index)
    {
        index = ((index % Colors.Length) + Colors.Length) % Colors.Length;
        return Cache[index] ??= Make(Colors[index].From, Colors[index].To);
    }

    public static Brush For(string id, string name)
    {
        // "Publisher.App" ids: colour by publisher so an app family shares a hue; otherwise by name.
        var key = WingetService.IsSafeId(id) && id.Contains('.') ? id[..id.IndexOf('.')] : name;
        uint h = 2166136261;
        foreach (var c in key.ToLowerInvariant()) { h ^= c; h *= 16777619; }
        return Palette((int)(h % (uint)Colors.Length));
    }

    private static Brush Make(string from, string to)
    {
        var b = new LinearGradientBrush { StartPoint = new Windows.Foundation.Point(0, 0), EndPoint = new Windows.Foundation.Point(1, 1) };
        b.GradientStops.Add(new GradientStop { Color = Parse(from), Offset = 0 });
        b.GradientStops.Add(new GradientStop { Color = Parse(to), Offset = 1 });
        return b;
    }

    private static Windows.UI.Color Parse(string hex) => ColorHelper.FromArgb(255,
        Convert.ToByte(hex.Substring(1, 2), 16), Convert.ToByte(hex.Substring(3, 2), 16), Convert.ToByte(hex.Substring(5, 2), 16));
}
