using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.ApplicationModel.DataTransfer;
using Windows.UI;

namespace Updraft;

public sealed partial class MainWindow : Window
{
    private string _mode = "upgrade";
    private int _gen;                 // bumped per load so stale async results are dropped
    private bool _jumpToDiscover;     // search submitted from another tab
    private readonly List<Pkg> _all = new();
    private readonly Dictionary<string, PkgDetail> _detailCache = new();

    public ObservableCollection<Pkg> Items { get; } = new();

    public MainWindow()
    {
        InitializeComponent();
        AppWindow.Resize(new Windows.Graphics.SizeInt32(1180, 760));
        Title = "Updraft";
        var icon = Path.Combine(AppContext.BaseDirectory, "Assets", "updraft.ico");
        if (File.Exists(icon)) AppWindow.SetIcon(icon);
        ((FrameworkElement)Content).Loaded += (_, _) => Nav.SelectedItem = NavUp;
    }

    // ---------- helpers ----------

    private IEnumerable<Pkg> Selected => _all.Where(p => p.IsSelected);

    private string VerbLabel => _mode switch { "upgrade" => "Update", "installed" => "Uninstall", _ => "Install" };

    private void SetStatus(string msg, Color color)
    {
        StatusText.Text = msg;
        Dot.Fill = new SolidColorBrush(color);
    }

    private static readonly Color Green = Color.FromArgb(255, 34, 197, 94);
    private static readonly Color Amber = Color.FromArgb(255, 245, 158, 11);
    private static readonly Color Red = Color.FromArgb(255, 239, 68, 68);

    private void SetBusy(bool busy, string msg = "")
    {
        Ring.IsActive = busy;
        if (busy) SetStatus(msg, Amber); else SetStatus("Ready", Green);
        UpdateFooter();
    }

    private void UpdateFooter()
    {
        int n = Selected.Count();
        SelCount.Text = Items.Count == _all.Count
            ? $"{n} selected  ·  {Items.Count} shown"
            : $"{n} selected  ·  {Items.Count} of {_all.Count} shown";
        RunBtn.IsEnabled = n > 0;
        CopyBtn.IsEnabled = n > 0;
        RunText.Text = n > 0 ? $"{VerbLabel} {n} in terminal" : "Run in terminal";

        EmptyMsg.Text = Items.Count > 0 || Ring.IsActive ? "" : _mode switch
        {
            "upgrade" => _all.Count > 0 ? "No matches" : "Everything is up to date 🎉",
            "installed" => "No packages",
            _ => _all.Count > 0 ? "No matches" : "Type a name in the search box and press Enter",
        };
    }

    private void ApplyFilter()
    {
        var q = _mode == "discover" ? "" : Search.Text.Trim();
        IEnumerable<Pkg> vis = _all.Where(p => q.Length == 0
            || p.Name.Contains(q, StringComparison.OrdinalIgnoreCase)
            || p.Id.Contains(q, StringComparison.OrdinalIgnoreCase));

        if (_sourceFilter.Length > 0)
            vis = vis.Where(p => string.Equals(p.Source, _sourceFilter, StringComparison.OrdinalIgnoreCase));

        vis = SortBox.SelectedIndex switch
        {
            1 => vis.OrderByDescending(p => p.Name, StringComparer.CurrentCultureIgnoreCase),
            2 => vis.OrderBy(p => p.Source, StringComparer.OrdinalIgnoreCase).ThenBy(p => p.Name, StringComparer.CurrentCultureIgnoreCase),
            3 => vis.OrderBy(p => p.Id, StringComparer.OrdinalIgnoreCase),
            _ => vis.OrderBy(p => p.Name, StringComparer.CurrentCultureIgnoreCase),
        };

        Items.Clear();
        foreach (var p in vis) Items.Add(p);
        UpdateFooter();
    }

    private string _sourceFilter = "";      // "" = all sources
    private bool _rebuildingSources;

    // Rebuild the source dropdown from what the current list actually contains.
    private void RefreshSourceChoices()
    {
        _rebuildingSources = true;
        SourceBox.Items.Clear();
        SourceBox.Items.Add(new ComboBoxItem { Content = "All sources" });
        var sources = _all.Select(p => p.Source).Where(s => s.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(s => s, StringComparer.OrdinalIgnoreCase);
        int sel = 0, i = 1;
        foreach (var s in sources)
        {
            SourceBox.Items.Add(new ComboBoxItem { Content = s });
            if (string.Equals(s, _sourceFilter, StringComparison.OrdinalIgnoreCase)) sel = i;
            i++;
        }
        if (sel == 0) _sourceFilter = "";
        SourceBox.SelectedIndex = sel;
        _rebuildingSources = false;
    }

    private void SortBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (IsLoaded()) ApplyFilter();
    }

    private void SourceBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_rebuildingSources || !IsLoaded()) return;
        _sourceFilter = SourceBox.SelectedIndex > 0 && SourceBox.SelectedItem is ComboBoxItem it
            ? (string)it.Content : "";
        ApplyFilter();
    }

    // The ComboBoxes fire SelectionChanged during InitializeComponent, before the rest of the UI exists.
    private bool IsLoaded() => Search is not null && SelCount is not null;

    // ---------- loading ----------

    private async Task LoadAsync()
    {
        int gen = ++_gen;
        string mode = _mode;
        string q = Search.Text.Trim();

        _all.Clear();
        Items.Clear();
        SelectAll.IsChecked = false;
        ShowDetail(null);

        PageTitle.Text = mode switch { "upgrade" => "Upgrades", "installed" => "Installed", _ => "Discover" };
        Subtitle.Text = "";

        string[] args;
        string busyMsg;
        switch (mode)
        {
            case "upgrade":
                args = new[] { "upgrade", "--accept-source-agreements" };
                busyMsg = "Scanning for updates…";
                break;
            case "installed":
                args = new[] { "list", "--accept-source-agreements" };
                busyMsg = "Reading installed packages…";
                break;
            default:
                if (q.Length == 0)
                {
                    Subtitle.Text = "Search the winget catalogue";
                    SetBusy(false);
                    return;
                }
                args = new[] { "search", q, "--accept-source-agreements" };
                busyMsg = $"Searching for “{q}”…";
                break;
        }

        SetBusy(true, busyMsg);
        List<Pkg> result;
        try
        {
            result = await Task.Run(async () => WingetService.Parse(await WingetService.RunAsync(args), mode));
        }
        catch (Exception ex)
        {
            if (gen != _gen) return;
            Ring.IsActive = false;
            SetStatus("winget failed: " + ex.Message, Red);
            UpdateFooter();
            return;
        }
        if (gen != _gen) return;

        foreach (var p in result)
        {
            p.PropertyChanged += OnPkgChanged;
            _all.Add(p);
        }
        RefreshSourceChoices();
        ApplyFilter();
        SetBusy(false);
        if (mode == "upgrade")
        {
            UpBadge.Value = _all.Count;
            UpBadge.Visibility = _all.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
        }
        Subtitle.Text = mode switch
        {
            "upgrade" => _all.Count > 0 ? $"{_all.Count} updates available" : "All packages are up to date",
            "installed" => $"{_all.Count} packages",
            _ => $"{_all.Count} results for “{q}”",
        };
    }

    private void OnPkgChanged(object? s, PropertyChangedEventArgs e) => UpdateFooter();

    // ---------- navigation & search ----------

    private void Nav_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        if (args.SelectedItem is not NavigationViewItem item) return;
        _mode = (string)item.Tag;
        if (!_jumpToDiscover) Search.Text = "";
        _jumpToDiscover = false;
        Search.PlaceholderText = _mode == "discover" ? "Search winget" : "Filter this list";
        _ = LoadAsync();
    }

    private void Search_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
    {
        if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput && _mode != "discover")
            ApplyFilter();
    }

    private void Search_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
    {
        if (_mode != "discover")
        {
            _jumpToDiscover = true;
            Nav.SelectedItem = NavDiscover;   // triggers the search
        }
        else
        {
            _ = LoadAsync();
        }
    }

    private void Refresh_Click(object sender, RoutedEventArgs e) => _ = LoadAsync();

    // ---------- detail pane ----------

    private void PkgList_SelectionChanged(object sender, SelectionChangedEventArgs e) =>
        ShowDetail(PkgList.SelectedItem as Pkg);

    private async void ShowDetail(Pkg? p)
    {
        if (p is null)
        {
            DPanel.Visibility = Visibility.Collapsed;
            DEmpty.Visibility = Visibility.Visible;
            return;
        }
        DEmpty.Visibility = Visibility.Collapsed;
        DPanel.Visibility = Visibility.Visible;

        DName.Text = p.Name;
        DId.Text = $"{p.Id}  ·  {p.Source}";
        DInst.Text = _mode == "discover" ? "—" : p.Version;
        DLatest.Text = p.Available.Length > 0 ? p.Available : p.Version;
        DCmd.Text = WingetService.Command(p, _mode);
        DAction.Content = $"{VerbLabel} {p.Name}";
        DDesc.Text = "Loading details…";
        DPub.Text = DLic.Text = "";
        DHome.Content = "";
        DHome.NavigateUri = null;

        if (!_detailCache.TryGetValue(p.Id, out var d))
        {
            try { d = await Task.Run(() => WingetService.ShowAsync(p.Id)); }
            catch { d = new PkgDetail("", "", "", ""); }
            _detailCache[p.Id] = d;
        }
        if (!ReferenceEquals(PkgList.SelectedItem, p)) return;   // selection moved on

        DDesc.Text = d.Description.Length > 0 ? d.Description : "No description available.";
        DPub.Text = d.Publisher;
        DLic.Text = d.License;
        DHome.Content = d.Homepage;
        DHome.NavigateUri = Uri.TryCreate(d.Homepage, UriKind.Absolute, out var u) ? u : null;
    }

    private void DCopy_Click(object sender, RoutedEventArgs e)
    {
        CopyText(DCmd.Text);
        SetStatus("Command copied", Green);
    }

    private void DAction_Click(object sender, RoutedEventArgs e)
    {
        if (PkgList.SelectedItem is Pkg p) RunInTerminal(new[] { p });
    }

    // ---------- selection / script ----------

    private void SelectAll_Click(object sender, RoutedEventArgs e)
    {
        bool v = SelectAll.IsChecked == true;
        foreach (var p in Items) p.IsSelected = v;
    }

    private void CopyScript_Click(object sender, RoutedEventArgs e)
    {
        CopyText(string.Join("\r\n", Selected.Where(p => WingetService.IsSafeId(p.Id)).Select(p => WingetService.Command(p, _mode))));
        SetStatus("Script copied to clipboard — paste it into any terminal", Green);
    }

    private static void CopyText(string text)
    {
        var dp = new DataPackage();
        dp.SetText(text);
        Clipboard.SetContent(dp);
        Clipboard.Flush();
    }

    private void Run_Click(object sender, RoutedEventArgs e) => RunInTerminal(Selected.ToList());

    private void RunInTerminal(IReadOnlyList<Pkg> pkgs)
    {
        if (pkgs.Count == 0) return;

        pkgs = pkgs.Where(p => WingetService.IsSafeId(p.Id)).ToList();
        if (pkgs.Count == 0)
        {
            SetStatus("Skipped: the package id looks unsafe or truncated", Red);
            return;
        }

        var sb = new StringBuilder();
        sb.AppendLine("$Host.UI.RawUI.WindowTitle = 'Updraft'");
        sb.AppendLine($"Write-Host 'Processing {pkgs.Count} package(s)...' -ForegroundColor Cyan");
        foreach (var p in pkgs)
        {
            var safeName = new string(p.Name.Where(c => !char.IsControl(c)).ToArray()).Replace("'", "''");
            sb.AppendLine($"Write-Host ''; Write-Host '>> {safeName}' -ForegroundColor Yellow");
            sb.AppendLine(WingetService.Command(p, _mode));
        }
        sb.AppendLine("Write-Host ''; Write-Host 'All done. You can close this window.' -ForegroundColor Green");

        // Unique file in a per-user folder (not a predictable path in %TEMP%), because it may be run elevated.
        var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Updraft", "runs");
        Directory.CreateDirectory(dir);
        foreach (var old in Directory.GetFiles(dir, "*.ps1").Where(f => File.GetLastWriteTimeUtc(f) < DateTime.UtcNow.AddDays(-1)))
        {
            try { File.Delete(old); } catch { /* best effort */ }
        }
        var file = Path.Combine(dir, Guid.NewGuid().ToString("N") + ".ps1");
        File.WriteAllText(file, sb.ToString(), new UTF8Encoding(true));

        var psArgs = $"-NoExit -ExecutionPolicy Bypass -File \"{file}\"";
        bool admin = AdminChk.IsChecked == true;

        bool Launch(string exe, string args)
        {
            var psi = new ProcessStartInfo(exe, args) { UseShellExecute = true };
            if (admin) psi.Verb = "runas";
            Process.Start(psi);
            return true;
        }

        try
        {
            try { Launch("wt.exe", "powershell.exe " + psArgs); }
            catch (Win32Exception ex) when (ex.NativeErrorCode != 1223) { Launch("powershell.exe", psArgs); }
            SetStatus($"Terminal opened for {pkgs.Count} package(s)", Green);
        }
        catch (Win32Exception ex) when (ex.NativeErrorCode == 1223)
        {
            SetStatus("Cancelled (administrator prompt declined)", Amber);
        }
        catch (Exception ex)
        {
            SetStatus("Could not open terminal: " + ex.Message, Red);
        }
    }
}
