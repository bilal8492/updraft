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
        AdminChk.IsChecked = AppSettings.Current.DefaultAdmin;
        ApplyTheme();
        Closed += (_, _) => _runCts?.Cancel();   // don't leave winget running after the window is gone
        ((FrameworkElement)Content).Loaded += (_, _) => Nav.SelectedItem = NavUp;
    }

    private NavigationViewItem? _prevNav;
    private bool _restoringNav;

    private void ApplyTheme() => ((FrameworkElement)Content).RequestedTheme = AppSettings.Current.Theme switch
    {
        "light" => ElementTheme.Light,
        "dark" => ElementTheme.Dark,
        _ => ElementTheme.Default,
    };

    private async Task ShowSettingsAsync()
    {
        var s = AppSettings.Current;

        static ComboBox Combo(string[] labels, int selected) =>
            new() { ItemsSource = labels, SelectedIndex = selected, MinWidth = 260 };

        string[] modes = { "default", "silent", "interactive" };
        string[] terms = { "auto", "wt", "powershell" };
        string[] themes = { "system", "light", "dark" };

        var mode = Combo(new[] { "Default (let winget decide)", "Silent (no installer windows)", "Interactive (show installer windows)" },
            Math.Max(0, Array.IndexOf(modes, s.InstallMode)));
        var term = Combo(new[] { "Automatic (Windows Terminal, else PowerShell)", "Windows Terminal", "PowerShell" },
            Math.Max(0, Array.IndexOf(terms, s.Terminal)));
        var theme = Combo(new[] { "Use system setting", "Light", "Dark" },
            Math.Max(0, Array.IndexOf(themes, s.Theme)));
        var agree = new CheckBox { Content = "Automatically accept package and source agreements", IsChecked = s.AcceptAgreements };
        var admin = new CheckBox { Content = "Run as administrator by default", IsChecked = s.DefaultAdmin };

        var panel = new StackPanel { Spacing = 14, MinWidth = 360 };
        panel.Children.Add(new StackPanel { Spacing = 4, Children = { new TextBlock { Text = "Installer mode", Opacity = 0.65 }, mode } });
        panel.Children.Add(agree);
        panel.Children.Add(admin);
        panel.Children.Add(new StackPanel { Spacing = 4, Children = { new TextBlock { Text = "Terminal", Opacity = 0.65 }, term } });
        panel.Children.Add(new StackPanel { Spacing = 4, Children = { new TextBlock { Text = "Theme", Opacity = 0.65 }, theme } });

        var dlg = new ContentDialog
        {
            Title = "Settings",
            Content = panel,
            PrimaryButtonText = "Save",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Primary,
            XamlRoot = Content.XamlRoot,
        };
        if (await dlg.ShowAsync() != ContentDialogResult.Primary) return;

        new AppSettings
        {
            InstallMode = modes[Math.Max(0, mode.SelectedIndex)],
            AcceptAgreements = agree.IsChecked == true,
            DefaultAdmin = admin.IsChecked == true,
            Terminal = terms[Math.Max(0, term.SelectedIndex)],
            Theme = themes[Math.Max(0, theme.SelectedIndex)],
            Skipped = s.Skipped,   // not edited here: carry the skip list over
        }.Save();

        AdminChk.IsChecked = AppSettings.Current.DefaultAdmin;
        ApplyTheme();
        ShowDetail(PkgList.SelectedItem as Pkg);   // refresh the command preview with the new flags
        SetStatus("Settings saved", Green);
    }

    // ---------- helpers ----------

    // Only what is on screen counts: a hidden (skipped) package must never be installed by accident.
    private IEnumerable<Pkg> Selected => Items.Where(p => p.IsSelected);

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
        int total = Listed.Count();
        SelCount.Text = Items.Count == total
            ? $"{n} selected  ·  {Items.Count} shown"
            : $"{n} selected  ·  {Items.Count} of {total} shown";
        RunBtn.IsEnabled = n > 0 && !_running;
        CopyBtn.IsEnabled = n > 0 && !_running;
        RunText.Text = n > 0 ? $"{VerbLabel} {n} in terminal" : "Run in terminal";
        InstallBtn.IsEnabled = _running || n > 0;
        InstallIcon.Glyph = _running ? "" : "";   // cancel / play
        InstallText.Text = _running ? "Cancel" : n > 0 ? $"{VerbLabel} {n}" : VerbLabel;

        EmptyMsg.Text = Items.Count > 0 || Ring.IsActive ? "" : _mode switch
        {
            "upgrade" => total > 0 ? "No matches" : "Everything is up to date 🎉",
            "installed" => "No packages",
            _ => _all.Count > 0 ? "No matches" : "Type a name in the search box and press Enter",
        };
    }

    private void ApplyFilter()
    {
        var q = _mode == "discover" ? "" : Search.Text.Trim();
        IEnumerable<Pkg> vis = Listed.Where(p => q.Length == 0
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
        HashSet<string> pins = new(StringComparer.OrdinalIgnoreCase);
        try
        {
            var listTask = Task.Run(async () => WingetService.Parse(await WingetService.RunAsync(args), mode));
            // Pins only matter for installed apps (winget leaves pinned ones out of the upgrade list).
            var pinTask = mode == "installed"
                ? Task.Run(async () => WingetService.ParsePinIds(await WingetService.RunAsync("pin", "list", "--accept-source-agreements")))
                : null;
            result = await listTask;
            if (pinTask is not null)
            {
                try { pins = await pinTask; } catch { /* no pin info: just show no pin badges */ }
            }
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
            p.IsPinned = pins.Contains(p.Id);
            p.IsSkipped = mode == "upgrade" && AppSettings.Current.IsSkipped(p.Id);
            p.PropertyChanged += OnPkgChanged;
            _all.Add(p);
        }
        RefreshSourceChoices();
        ApplyFilter();
        SetBusy(false);
        Subtitle.Text = mode switch
        {
            "installed" => pins.Count > 0 ? $"{_all.Count} packages  ·  {_all.Count(p => p.IsPinned)} pinned" : $"{_all.Count} packages",
            "discover" => $"{_all.Count} results for “{q}”",
            _ => "",
        };
        if (mode == "upgrade") RefreshUpgradeCounts();
    }

    // Subtitle and nav badge count only the updates that aren't skipped.
    private void RefreshUpgradeCounts()
    {
        if (_mode != "upgrade") return;
        int skipped = _all.Count(p => p.IsSkipped);
        int active = _all.Count - skipped;
        UpBadge.Value = active;
        UpBadge.Visibility = active > 0 ? Visibility.Visible : Visibility.Collapsed;
        var text = active > 0 ? $"{active} updates available" : "All packages are up to date";
        Subtitle.Text = skipped > 0 ? $"{text}  ·  {skipped} skipped" : text;
    }

    private bool _showSkipped;

    // What the list is allowed to show before search/source filters: skipped updates stay hidden unless asked for.
    private IEnumerable<Pkg> Listed => _mode == "upgrade" && !_showSkipped ? _all.Where(p => !p.IsSkipped) : _all;

    private void ShowSkipped_Click(object sender, RoutedEventArgs e)
    {
        _showSkipped = ShowSkippedChk.IsChecked == true;
        ApplyFilter();
    }

    private void OnPkgChanged(object? s, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(Pkg.IsSelected)) UpdateFooter();
        else if (e.PropertyName == nameof(Pkg.Log) && ReferenceEquals(s, PkgList.SelectedItem)) ShowLog(s as Pkg);
    }

    private void ShowLog(Pkg? p)
    {
        var log = p?.Log ?? "";
        DLogPanel.Visibility = log.Length > 0 ? Visibility.Visible : Visibility.Collapsed;
        DLog.Text = log;
    }

    private void DCopyLog_Click(object sender, RoutedEventArgs e)
    {
        if (PkgList.SelectedItem is not Pkg p) return;
        CopyText(p.Log);
        SetStatus("Log copied", Green);
    }

    // ---------- navigation & search ----------

    private void Nav_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        if (args.IsSettingsSelected)
        {
            // Settings is a dialog, not a page: show it and put the selection back where it was.
            _restoringNav = true;
            Nav.SelectedItem = _prevNav ?? NavUp;
            _ = ShowSettingsAsync();
            return;
        }
        if (_restoringNav) { _restoringNav = false; return; }   // selection put back after Settings: no reload
        if (args.SelectedItem is not NavigationViewItem item) return;
        _prevNav = item;
        _mode = (string)item.Tag;
        if (!_jumpToDiscover) Search.Text = "";
        _jumpToDiscover = false;
        Search.PlaceholderText = _mode == "discover" ? "Search winget" : "Filter this list";
        ShowSkippedChk.Visibility = _mode == "upgrade" ? Visibility.Visible : Visibility.Collapsed;
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
        ShowLog(p);

        DName.Text = p.Name;
        DId.Text = $"{p.Id}  ·  {p.Source}";
        DInst.Text = _mode == "discover" ? "—" : p.Version;
        DLatest.Text = p.Available.Length > 0 ? p.Available : p.Version;
        DCmd.Text = WingetService.Command(p, _mode);
        DAction.Content = $"{VerbLabel} {p.Name}";
        UpdatePinSkipButtons(p);
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
        if (_running || PkgList.SelectedItem is not Pkg p) return;
        _ = StartRun(new[] { p });
    }

    // ---------- skip & pin ----------

    private void UpdatePinSkipButtons(Pkg p)
    {
        DSkip.Visibility = _mode == "upgrade" ? Visibility.Visible : Visibility.Collapsed;
        DSkip.Content = p.IsSkipped ? "Stop skipping" : "Skip updates";
        DPin.Visibility = _mode == "discover" ? Visibility.Collapsed : Visibility.Visible;
        DPin.Content = p.IsPinned ? "Unpin" : "Pin version";
    }

    private void DSkip_Click(object sender, RoutedEventArgs e)
    {
        if (PkgList.SelectedItem is not Pkg p) return;
        bool skip = !p.IsSkipped;
        AppSettings.Current.SetSkipped(p.Id, skip);
        p.IsSkipped = skip;
        if (skip) p.IsSelected = false;
        ApplyFilter();           // a newly skipped row leaves the list (selection clears, detail pane closes)
        RefreshUpgradeCounts();
        UpdatePinSkipButtons(p);
        SetStatus(skip
            ? $"Skipping {p.Name}. Tick “Show skipped” to see it again."
            : $"{p.Name} will show up in updates again", Green);
    }

    private bool _pinBusy;

    private async void DPin_Click(object sender, RoutedEventArgs e)
    {
        if (_pinBusy || _running || PkgList.SelectedItem is not Pkg p) return;
        if (!WingetService.IsSafeId(p.Id))
        {
            SetStatus("Skipped: the package id looks unsafe or truncated", Red);
            return;
        }

        bool unpin = p.IsPinned;
        _pinBusy = true;
        DPin.IsEnabled = false;
        SetStatus($"{(unpin ? "Unpinning" : "Pinning")} {p.Name}…", Amber);
        try
        {
            var (code, output) = await Task.Run(() =>
                WingetService.RunCaptureAsync("pin", unpin ? "remove" : "add", "--id", p.Id, "-e", "--accept-source-agreements"));
            if (code != 0)
            {
                var why = WingetService.LastLine(output);
                SetStatus($"Could not {(unpin ? "unpin" : "pin")} {p.Name}" + (why.Length > 0 ? ": " + why : $" (exit code 0x{code:X8})"), Red);
                return;
            }

            p.IsPinned = !unpin;
            if (!unpin && _mode == "upgrade")
            {
                // winget no longer offers a pinned app as an update, so take it off this list too.
                p.IsSelected = false;
                _all.Remove(p);
                ApplyFilter();
                RefreshUpgradeCounts();
                SetStatus($"Pinned {p.Name}. It won't be offered as an update. Manage pins in Installed.", Green);
            }
            else
            {
                UpdatePinSkipButtons(p);
                SetStatus(unpin ? $"Unpinned {p.Name}" : $"Pinned {p.Name} at {p.Version}", Green);
            }
        }
        catch (Exception ex)
        {
            SetStatus("winget failed: " + ex.Message, Red);
        }
        finally
        {
            _pinBusy = false;
            DPin.IsEnabled = true;
        }
    }

    // ---------- in-app runner ----------

    private bool _running;
    private CancellationTokenSource? _runCts;

    private void Install_Click(object sender, RoutedEventArgs e)
    {
        if (_running) { _runCts?.Cancel(); SetStatus("Cancelling…", Amber); return; }
        _ = StartRun(Selected.ToList());
    }

    // Elevation can't be combined with captured output, so "Run as administrator" keeps using the terminal.
    private async Task StartRun(IReadOnlyList<Pkg> pkgs)
    {
        if (pkgs.Count == 0) return;
        if (!await ConfirmUninstallAsync(pkgs)) return;
        if (AdminChk.IsChecked == true)
        {
            RunInTerminal(pkgs);
            return;
        }
        await RunInAppAsync(pkgs);
    }

    // Uninstalling can't be undone, so always ask first and make Cancel the default.
    private async Task<bool> ConfirmUninstallAsync(IReadOnlyList<Pkg> pkgs)
    {
        if (_mode != "installed") return true;

        const int shown = 12;
        var list = new StackPanel { Spacing = 2 };
        foreach (var p in pkgs.Take(shown))
            list.Children.Add(new TextBlock { Text = "•  " + p.Name, TextTrimming = TextTrimming.CharacterEllipsis });
        if (pkgs.Count > shown)
            list.Children.Add(new TextBlock { Text = $"…and {pkgs.Count - shown} more", Opacity = 0.65 });

        var body = new StackPanel { Spacing = 12, MinWidth = 340 };
        body.Children.Add(new TextBlock { Text = "This removes the app and can't be undone from Updraft.", TextWrapping = TextWrapping.Wrap });
        body.Children.Add(new ScrollViewer { MaxHeight = 260, Content = list });

        var dlg = new ContentDialog
        {
            Title = pkgs.Count == 1 ? $"Uninstall {pkgs[0].Name}?" : $"Uninstall {pkgs.Count} apps?",
            Content = body,
            PrimaryButtonText = "Uninstall",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Close,
            XamlRoot = Content.XamlRoot,
        };
        return await dlg.ShowAsync() == ContentDialogResult.Primary;
    }

    private async Task RunInAppAsync(IReadOnlyList<Pkg> pkgs)
    {
        if (pkgs.Count == 0) return;
        var safe = pkgs.Where(p => WingetService.IsSafeId(p.Id)).ToList();
        if (safe.Count == 0)
        {
            SetStatus("Skipped: the package id looks unsafe or truncated", Red);
            return;
        }

        string mode = _mode;
        string verb = VerbLabel;
        _running = true;
        _runCts = new CancellationTokenSource();
        var ct = _runCts.Token;
        SetRunLock(true);

        foreach (var p in safe) { p.ResetRun(); p.SetState(RunState.Queued, "Queued"); }

        int ok = 0, failed = 0, cancelled = 0;
        var removed = new List<Pkg>();   // uninstalled apps: they leave the Installed list once the run ends
        for (int i = 0; i < safe.Count; i++)
        {
            var p = safe[i];
            if (ct.IsCancellationRequested)
            {
                p.SetState(RunState.Cancelled, "Cancelled");
                cancelled++;
                continue;
            }

            p.SetState(RunState.Running, "Starting…");
            SetStatus($"{verb} {p.Name}  ({i + 1} of {safe.Count})", Amber);
            try
            {
                int code = await WingetService.RunPackageAsync(p, mode, ct);
                if (ct.IsCancellationRequested)
                {
                    p.SetState(RunState.Cancelled, "Cancelled");
                    cancelled++;
                }
                else if (WingetService.IsSuccess(code))
                {
                    p.SetState(RunState.Succeeded, code != 0 ? "Already up to date" : mode == "installed" ? "Uninstalled" : "Done");
                    p.IsSelected = false;
                    ok++;
                    if (mode == "installed" && code == 0) removed.Add(p);
                }
                else
                {
                    p.SetState(RunState.Failed, $"Failed (exit code 0x{code:X8})");
                    failed++;
                }
            }
            catch (Exception ex)
            {
                p.AppendLog(ex.Message);
                p.SetState(RunState.Failed, "Failed: " + ex.Message);
                failed++;
            }
        }

        _running = false;
        _runCts.Dispose();
        _runCts = null;
        SetRunLock(false);

        if (removed.Count > 0 && _mode == mode)
        {
            // Dropped in one go at the end (not one by one) so rows don't jump around while the run is in progress.
            foreach (var p in removed) _all.Remove(p);
            ApplyFilter();
            int pinned = _all.Count(p => p.IsPinned);
            Subtitle.Text = pinned > 0 ? $"{_all.Count} packages  ·  {pinned} pinned" : $"{_all.Count} packages";
        }

        var parts = new List<string>();
        if (safe.Count < pkgs.Count) parts.Add($"{pkgs.Count - safe.Count} skipped (no usable package id)");
        if (ok > 0) parts.Add($"{ok} succeeded");
        if (failed > 0) parts.Add($"{failed} failed");
        if (cancelled > 0) parts.Add($"{cancelled} cancelled");
        SetStatus(string.Join(", ", parts) + (failed + cancelled > 0 ? " — unfinished items stay selected so you can retry" : " — press Refresh to rescan"),
            failed > 0 ? Red : cancelled > 0 ? Amber : Green);
    }

    // While a run is going, block anything that would reload or replace the list under it.
    private void SetRunLock(bool locked)
    {
        Nav.IsEnabled = !locked;
        RefreshBtn.IsEnabled = !locked;
        SelectAll.IsEnabled = !locked;
        DAction.IsEnabled = !locked;
        UpdateFooter();
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

    private async void Run_Click(object sender, RoutedEventArgs e)
    {
        var sel = Selected.ToList();
        if (sel.Count == 0 || !await ConfirmUninstallAsync(sel)) return;
        RunInTerminal(sel);
    }

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

        // Pass the script on the command line. No file means nothing for another process to swap before an elevated
        // run, and it also works from the MSIX build, where files written under %LOCALAPPDATA% are redirected to a
        // private per-package copy that Windows Terminal and an elevated PowerShell cannot see.
        var script = sb.ToString();
        var encoded = Convert.ToBase64String(Encoding.Unicode.GetBytes(script));
        string psArgs;
        if (encoded.Length <= 20_000)   // stay well under the ~32K command-line limit
        {
            psArgs = $"-NoExit -ExecutionPolicy Bypass -EncodedCommand {encoded}";
        }
        else
        {
            // Very large selections: fall back to a unique file in a per-user folder.
            var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Updraft", "runs");
            Directory.CreateDirectory(dir);
            foreach (var old in Directory.GetFiles(dir, "*.ps1").Where(f => File.GetLastWriteTimeUtc(f) < DateTime.UtcNow.AddDays(-1)))
            {
                try { File.Delete(old); } catch { /* best effort */ }
            }
            var file = Path.Combine(dir, Guid.NewGuid().ToString("N") + ".ps1");
            File.WriteAllText(file, script, new UTF8Encoding(true));
            psArgs = $"-NoExit -ExecutionPolicy Bypass -File \"{file}\"";
        }
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
            var term = AppSettings.Current.Terminal;
            if (term == "powershell") Launch("powershell.exe", psArgs);
            else if (term == "wt") Launch("wt.exe", "powershell.exe " + psArgs);
            else
            {
                try { Launch("wt.exe", "powershell.exe " + psArgs); }
                catch (Win32Exception ex) when (ex.NativeErrorCode != 1223) { Launch("powershell.exe", psArgs); }
            }
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
