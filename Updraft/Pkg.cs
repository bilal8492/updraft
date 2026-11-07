using System.ComponentModel;
using System.Text;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;

namespace Updraft;

public enum RunState { None, Queued, Running, Succeeded, Failed, Cancelled }

public sealed class Pkg : INotifyPropertyChanged
{
    public string Name { get; set; } = "";
    public string Id { get; set; } = "";
    public string Version { get; set; } = "";
    public string Available { get; set; } = "";
    public string Source { get; set; } = "";

    // Badge: always visible when winget reported a source, with a distinct colour per source.
    public Visibility SourceVisibility => string.IsNullOrEmpty(Source) ? Visibility.Collapsed : Visibility.Visible;
    public string SourceLabel => Source;
    public Brush SourceBrush => Source.ToLowerInvariant() switch
    {
        "winget" => new SolidColorBrush(ColorHelper.FromArgb(255, 76, 194, 255)),   // blue
        "msstore" => new SolidColorBrush(ColorHelper.FromArgb(255, 108, 203, 95)),  // green
        _ => new SolidColorBrush(ColorHelper.FromArgb(255, 157, 157, 157)),         // grey
    };

    public string VersionText => string.IsNullOrEmpty(Available) ? Version : $"{Version}  →  {Available}";

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
    public Brush StatusBrush => State switch
    {
        RunState.Succeeded => new SolidColorBrush(ColorHelper.FromArgb(255, 34, 197, 94)),
        RunState.Failed => new SolidColorBrush(ColorHelper.FromArgb(255, 239, 68, 68)),
        RunState.Cancelled => new SolidColorBrush(ColorHelper.FromArgb(255, 245, 158, 11)),
        _ => new SolidColorBrush(ColorHelper.FromArgb(255, 157, 157, 157)),
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
        Notify(nameof(State), nameof(RunStatus), nameof(StatusVisibility), nameof(ProgressVisibility), nameof(StatusBrush));
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
}

public sealed record PkgDetail(string Description, string Publisher, string License, string Homepage);
