using System.ComponentModel;

namespace Updraft;

public sealed class Pkg : INotifyPropertyChanged
{
    public string Name { get; set; } = "";
    public string Id { get; set; } = "";
    public string Version { get; set; } = "";
    public string Available { get; set; } = "";
    public string Source { get; set; } = "";

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
