using System.ComponentModel;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;

namespace Updraft;

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
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsSelected)));
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
}

public sealed record PkgDetail(string Description, string Publisher, string License, string Homepage);
