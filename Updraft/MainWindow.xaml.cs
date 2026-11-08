using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.Windows.Storage.Pickers;
using Windows.ApplicationModel.DataTransfer;
using Windows.Graphics;
using Windows.System;
using Windows.UI;

namespace Updraft;

public sealed partial class MainWindow : Window
{
    /// <summary>"upgrade", "installed", "discover", "import" (restore list or starter pack), "history" or "settings".</summary>
    private string _mode = "upgrade";
    private int _gen;                 // bumped per load so stale async results are dropped
    private bool _jumpToDiscover;     // search submitted from another tab
    private bool _busy;
    private string _discoverQuery = "";   // empty: Discover shows its start page
    private readonly List<Pkg> _all = new();
    private readonly Dictionary<string, PkgDetail> _detailCache = new();
    private readonly Taskbar _taskbar;

    // Updates and Installed lists are kept for a few minutes so switching tabs is instant. F5 always rescans.
    private static readonly TimeSpan CacheLife = TimeSpan.FromMinutes(5);
    private readonly Dictionary<string, (List<Pkg> Items, DateTime At)> _listCache = new();
    private DateTime? _lastScan;

    public ObservableCollection<Pkg> Items { get; } = new();
    public StarterPack[] Packs => Catalog.Packs;

    public MainWindow()
    {
        InitializeComponent();
        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);
        AppWindow.TitleBar.PreferredHeightOption = TitleBarHeightOption.Tall;

        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
        _scale = Math.Max(1, GetDpiForWindow(hwnd) / 96.0);
        if (AppWindow.Presenter is OverlappedPresenter op)
        {
            op.PreferredMinimumWidth = Scaled(900);
            op.PreferredMinimumHeight = Scaled(560);
        }
        RestorePlacement();
        Title = "Updraft";
        var icon = Path.Combine(AppContext.BaseDirectory, "Assets", "updraft.ico");
        if (File.Exists(icon)) AppWindow.SetIcon(icon);
        _taskbar = new Taskbar(hwnd);

        AdminChk.IsChecked = AppSettings.Current.DefaultAdmin;
        DemoBadge.Visibility = WingetService.Demo ? Visibility.Visible : Visibility.Collapsed;
        ApplyTheme();
        SetupShortcuts();
        SetupChrome();
        SetActivity(null);

        AppWindow.Changed += AppWindow_Changed;
        AppWindow.Closing += (_, _) => SavePlacement();
        Closed += (_, _) =>
        {
            _runCts?.Cancel();   // don't leave winget running after the window is gone
            _taskbar.SetState(Taskbar.State.None);
        };
        Activated += (_, e) =>
        {
            // A red taskbar button after a failed background run is cleared once the user comes back.
            if (e.WindowActivationState != WindowActivationState.Deactivated && !_running) _taskbar.SetState(Taskbar.State.None);
        };
        var root = (FrameworkElement)Content;
        root.ActualThemeChanged += (_, _) => ApplyCaptionColors();
        root.Loaded += (_, _) =>
        {
            Nav.SelectedItem = NavUp;
            PkgList.Focus(FocusState.Programmatic);   // so keyboard shortcuts work straight away
        };
    }

    // ---------- window placement ----------

    private readonly double _scale;
    private int Scaled(double effectivePixels) => (int)Math.Round(effectivePixels * _scale);

    [DllImport("user32.dll")]
    private static extern uint GetDpiForWindow(IntPtr hwnd);

    private RectInt32 _normalBounds;   // last size/position while neither minimized nor maximized

    private OverlappedPresenterState PresenterState =>
        AppWindow.Presenter is OverlappedPresenter op ? op.State : OverlappedPresenterState.Restored;

    private void RestorePlacement()
    {
        var s = AppSettings.Current;
        bool restored = false;
        if (s.WindowWidth >= 600 && s.WindowHeight >= 400)
        {
            // Only reuse the saved spot if its title bar is still on a connected monitor.
            var titleBar = new PointInt32(s.WindowX + s.WindowWidth / 2, s.WindowY + 16);
            if (DisplayArea.GetFromPoint(titleBar, DisplayAreaFallback.None) is not null)
            {
                AppWindow.MoveAndResize(new RectInt32(s.WindowX, s.WindowY, s.WindowWidth, s.WindowHeight));
                restored = true;
            }
            else
            {
                AppWindow.Resize(new SizeInt32(s.WindowWidth, s.WindowHeight));
            }
        }
        if (!restored && s.WindowWidth == 0) AppWindow.Resize(new SizeInt32(Scaled(1320), Scaled(840)));
        _normalBounds = new RectInt32(AppWindow.Position.X, AppWindow.Position.Y, AppWindow.Size.Width, AppWindow.Size.Height);
        if (s.WindowMaximized && AppWindow.Presenter is OverlappedPresenter op) op.Maximize();
    }

    private void AppWindow_Changed(AppWindow sender, AppWindowChangedEventArgs args)
    {
        if ((args.DidPositionChange || args.DidSizeChange) && PresenterState == OverlappedPresenterState.Restored)
            _normalBounds = new RectInt32(sender.Position.X, sender.Position.Y, sender.Size.Width, sender.Size.Height);
    }

    private void SavePlacement()
    {
        if (_normalBounds.Width <= 0 || _normalBounds.Height <= 0) return;
        var s = AppSettings.Current;
        s.WindowX = _normalBounds.X;
        s.WindowY = _normalBounds.Y;
        s.WindowWidth = _normalBounds.Width;
        s.WindowHeight = _normalBounds.Height;
        s.WindowMaximized = PresenterState == OverlappedPresenterState.Maximized;
        s.Save();
    }

    // ---------- theme ----------

    private void ApplyTheme()
    {
        ((FrameworkElement)Content).RequestedTheme = AppSettings.Current.Theme switch
        {
            "light" => ElementTheme.Light,
            "dark" => ElementTheme.Dark,
            _ => ElementTheme.Default,
        };
        ApplyCaptionColors();
    }

    /// <summary>The caption buttons are drawn by Windows: give them colours that match the in-app theme.</summary>
    private void ApplyCaptionColors()
    {
        var tb = AppWindow.TitleBar;
        bool dark = ((FrameworkElement)Content).ActualTheme == ElementTheme.Dark;
        var fg = dark ? Colors.White : Colors.Black;
        tb.ButtonBackgroundColor = Colors.Transparent;
        tb.ButtonInactiveBackgroundColor = Colors.Transparent;
        tb.ButtonForegroundColor = fg;
        tb.ButtonHoverForegroundColor = fg;
        tb.ButtonPressedForegroundColor = fg;
        tb.ButtonHoverBackgroundColor = dark ? Color.FromArgb(0x18, 0xFF, 0xFF, 0xFF) : Color.FromArgb(0x12, 0, 0, 0);
        tb.ButtonPressedBackgroundColor = dark ? Color.FromArgb(0x10, 0xFF, 0xFF, 0xFF) : Color.FromArgb(0x0C, 0, 0, 0);
        tb.ButtonInactiveForegroundColor = Color.FromArgb(0xFF, 0x8A, 0x8A, 0x8A);
    }

    // ---------- keyboard shortcuts ----------

    private void SetupShortcuts()
    {
        Nav.KeyboardAcceleratorPlacementMode = KeyboardAcceleratorPlacementMode.Hidden;

        void Add(VirtualKey key, VirtualKeyModifiers mods, Func<bool> action)
        {
            var acc = new KeyboardAccelerator { Key = key, Modifiers = mods };
            acc.Invoked += (_, e) => e.Handled = action();   // not handled → the key keeps its normal meaning
            RootGrid.KeyboardAccelerators.Add(acc);
        }

        bool Refresh()
        {
            if (_running || !IsListMode || !RefreshBtn.IsEnabled) return false;
            Motion.SpinOnce(RefreshIcon);
            _ = LoadAsync(force: true);
            return true;
        }
        Add(VirtualKey.F5, VirtualKeyModifiers.None, Refresh);
        Add(VirtualKey.R, VirtualKeyModifiers.Control, Refresh);

        Add(VirtualKey.F, VirtualKeyModifiers.Control, () => Search.Focus(FocusState.Keyboard));
        Add(VirtualKey.E, VirtualKeyModifiers.Control, () => Search.Focus(FocusState.Keyboard));

        var pages = new[] { NavUp, NavInstalled, NavDiscover, NavHistory };
        for (int i = 0; i < pages.Length; i++)
        {
            var page = pages[i];
            Add(VirtualKey.Number1 + i, VirtualKeyModifiers.Control, () =>
            {
                if (!Nav.IsEnabled) return false;
                Nav.SelectedItem = page;
                return true;
            });
        }
        Add((VirtualKey)188 /* comma */, VirtualKeyModifiers.Control, () =>
        {
            if (!Nav.IsEnabled) return false;
            Nav.SelectedItem = Nav.SettingsItem;
            return true;
        });
        Add(VirtualKey.F1, VirtualKeyModifiers.None, () => { _ = ShowShortcutsAsync(); return true; });

        Add(VirtualKey.A, VirtualKeyModifiers.Control, () =>
        {
            // Inside a text box Ctrl+A selects the text, as usual.
            if (FocusManager.GetFocusedElement(Content.XamlRoot) is TextBox or AutoSuggestBox) return false;
            if (_running || !IsListMode || Items.Count == 0) return false;
            bool all = Items.Any(p => !p.IsSelected);
            foreach (var p in Items) p.IsSelected = all;
            return true;
        });

        Add(VirtualKey.Enter, VirtualKeyModifiers.Control, () =>
        {
            if (_running || !IsListMode || !Selected.Any()) return false;
            _ = StartRun(Selected.ToList());
            return true;
        });

        Add(VirtualKey.Escape, VirtualKeyModifiers.None, () =>
        {
            if (_running)
            {
                _runCts?.Cancel();
                SetActivity("Cancelling…");
                return true;
            }
            if (Toast.Visibility == Visibility.Visible)
            {
                HideToast();
                return true;
            }
            return false;
        });
    }

    // ---------- helpers ----------

    // Only what is on screen counts: a hidden (skipped) package must never be installed by accident.
    private IEnumerable<Pkg> Selected => Items.Where(p => p.IsSelected);

    private string VerbLabel => _mode switch { "upgrade" => "Update", "installed" => "Uninstall", _ => "Install" };

    /// <summary>True when the page shows the package list (not History, Settings or the Discover start page).</summary>
    private bool IsListMode => _mode is "upgrade" or "installed" or "import" || (_mode == "discover" && _discoverQuery.Length > 0);

    private void SetBusy(bool busy, string msg = "")
    {
        _busy = busy;
        SetActivity(busy ? msg : null);
        Motion.SetSpinning(RefreshIcon, busy);
        UpdateFooter();
        UpdateHero();
    }

    private void ApplyFilter()
    {
        if (_mode == "history")
        {
            RefreshHistory();
            return;
        }
        if (!IsListMode)
        {
            UpdateFooter();
            return;
        }

        var q = _mode == "discover" ? "" : Search.Text.Trim();
        IEnumerable<Pkg> vis = Listed.Where(p => q.Length == 0
            || p.Name.Contains(q, StringComparison.OrdinalIgnoreCase)
            || p.Id.Contains(q, StringComparison.OrdinalIgnoreCase));

        if (_sourceFilter is not null)
            vis = vis.Where(p => string.Equals(p.Source, _sourceFilter, StringComparison.OrdinalIgnoreCase));

        vis = _sort switch
        {
            1 => vis.OrderByDescending(p => p.Name, StringComparer.CurrentCultureIgnoreCase),
            2 => vis.OrderBy(p => p.Source, StringComparer.OrdinalIgnoreCase).ThenBy(p => p.Name, StringComparer.CurrentCultureIgnoreCase),
            3 => vis.OrderBy(p => p.Id, StringComparer.OrdinalIgnoreCase),
            4 => vis.OrderByDescending(p => p.Kind).ThenBy(p => p.Name, StringComparer.CurrentCultureIgnoreCase),
            _ => vis.OrderBy(p => p.Name, StringComparer.CurrentCultureIgnoreCase),
        };

        SyncItems(vis.ToList());
        UpdateFooter();
    }

    /// <summary>
    /// Brings <see cref="Items"/> in line with <paramref name="target"/> with the fewest changes, so filtering keeps the
    /// selection and scroll position, and rows that stay slide into their new places instead of being rebuilt.
    /// </summary>
    private void SyncItems(IList<Pkg> target)
    {
        var keep = new HashSet<Pkg>(target);
        for (int i = Items.Count - 1; i >= 0; i--)
            if (!keep.Contains(Items[i])) Items.RemoveAt(i);
        for (int i = 0; i < target.Count; i++)
        {
            if (i < Items.Count && ReferenceEquals(Items[i], target[i])) continue;
            int j = Items.IndexOf(target[i]);
            if (j >= 0) Items.Move(j, i);
            else Items.Insert(i, target[i]);
        }
    }

    private int _sort;
    private string? _sourceFilter;      // null = all sources, "" = no source reported
    private bool _rebuildingSources;

    // Rebuild the source filter from what the current list actually contains.
    private void RefreshSourceChoices()
    {
        _rebuildingSources = true;
        SourceBar.Items.Clear();
        var listed = Listed.ToList();
        var groups = listed.GroupBy(p => p.Source, StringComparer.OrdinalIgnoreCase)
            .OrderByDescending(g => g.Count()).ThenBy(g => g.Key, StringComparer.OrdinalIgnoreCase).ToList();
        var all = new SelectorBarItem { Text = $"All  {listed.Count}", Tag = "*" };
        SourceBar.Items.Add(all);
        var selected = all;
        foreach (var g in groups)
        {
            var item = new SelectorBarItem { Text = $"{(g.Key.Length == 0 ? "Other" : g.Key)}  {g.Count()}", Tag = g.Key };
            SourceBar.Items.Add(item);
            if (_sourceFilter is not null && string.Equals(g.Key, _sourceFilter, StringComparison.OrdinalIgnoreCase)) selected = item;
        }
        if (ReferenceEquals(selected, all)) _sourceFilter = null;
        SourceBar.SelectedItem = selected;
        SourceBar.Visibility = groups.Count > 1 ? Visibility.Visible : Visibility.Collapsed;
        _rebuildingSources = false;
    }

    private void SourceBar_SelectionChanged(SelectorBar sender, SelectorBarSelectionChangedEventArgs args)
    {
        if (_rebuildingSources) return;
        var tag = sender.SelectedItem?.Tag as string;
        _sourceFilter = tag is null or "*" ? null : tag;
        ApplyFilter();
    }

    private void Sort_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not RadioMenuFlyoutItem item) return;
        _sort = int.Parse((string)item.Tag);
        SortText.Text = _sort switch { 1 => "Name Z–A", 2 => "Source", 3 => "Id", 4 => "Update type", _ => "Name" };
        ApplyFilter();
    }

    private void ResetSort()
    {
        _sort = 0;
        SortName.IsChecked = true;
        SortText.Text = "Name";
    }

    // ---------- loading ----------

    private void SetHeader()
    {
        PageTitle.Text = _mode switch
        {
            "upgrade" => "Updates",
            "installed" => "Installed",
            "history" => "History",
            "import" => _importTitle,
            "settings" => "Settings",
            _ => "Discover",
        };
        Subtitle.Text = _mode == "discover" && _discoverQuery.Length == 0
            ? "Find apps in the winget catalogue, or set up a new PC with a starter pack"
            : "";
    }

    private async Task LoadAsync(bool force = false)
    {
        int gen = ++_gen;
        string mode = _mode;
        string q = Search.Text.Trim();

        _all.Clear();
        Items.Clear();
        SelectAll.IsChecked = false;
        ShowDetail(null);
        SetHeader();

        if (mode == "settings") return;
        if (mode == "history")
        {
            SetBusy(false);
            RefreshHistory();
            return;
        }

        if (!force && mode is "upgrade" or "installed" && _listCache.TryGetValue(mode, out var hit) && DateTime.Now - hit.At < CacheLife)
        {
            Fill(hit.Items, mode);
            AfterLoad(mode, q);
            return;
        }

        string[] args;
        string busyMsg;
        switch (mode)
        {
            case "import":
                // Restoring needs to know what's already here, so those apps aren't reinstalled.
                args = new[] { "list", "--accept-source-agreements" };
                busyMsg = "Checking which apps are already installed…";
                break;
            case "upgrade":
                args = new[] { "upgrade", "--accept-source-agreements" };
                busyMsg = "Scanning for updates…";
                break;
            case "installed":
                args = new[] { "list", "--accept-source-agreements" };
                busyMsg = "Reading installed packages…";
                break;
            default:
                _discoverQuery = q;
                ApplyModeLayout();
                if (q.Length == 0)
                {
                    SetBusy(false);
                    ShowDiscoverHome();
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
            var parseMode = mode == "import" ? "installed" : mode;
            var listTask = Task.Run(async () => WingetService.Parse(await WingetService.RunAsync(args), parseMode));
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
            ReportWingetError(ex);
            SetBusy(false);
            return;
        }
        if (gen != _gen) return;
        WingetBar.IsOpen = false;

        if (mode == "import") result = MatchImport(result);
        if (mode == "upgrade")
        {
            // A version skip ends once winget offers something newer.
            AppSettings.Current.PruneSkippedVersions(result.GroupBy(p => p.Id, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First().Available, StringComparer.OrdinalIgnoreCase));
            _lastScan = DateTime.Now;
        }
        foreach (var p in result) p.IsPinned = pins.Contains(p.Id);
        if (mode is "upgrade" or "installed") _listCache[mode] = (result, DateTime.Now);

        Fill(result, mode);
        AfterLoad(mode, q);
    }

    private void Fill(List<Pkg> pkgs, string mode)
    {
        foreach (var p in pkgs)
        {
            p.Mode = mode;
            if (mode == "upgrade") RefreshSkip(p);
            p.PropertyChanged -= OnPkgChanged;
            p.PropertyChanged += OnPkgChanged;
            _all.Add(p);
        }
        ArmStagger();
        RefreshSourceChoices();
        ApplyFilter();
        SetBusy(false);
    }

    private void AfterLoad(string mode, string q)
    {
        int pinned = _all.Count(p => p.IsPinned);
        Subtitle.Text = mode switch
        {
            "installed" => pinned > 0 ? $"{_all.Count} packages  ·  {pinned} pinned" : $"{_all.Count} packages",
            "discover" => $"{_all.Count} {(_all.Count == 1 ? "result" : "results")} for “{q}”",
            "import" => _importIsPack
                ? $"{_all.Count} apps  ·  {_all.Count(p => p.IsInstalled)} already installed"
                : $"{_all.Count} apps in {_importName}  ·  {_all.Count(p => p.IsInstalled)} already installed",
            _ => "",
        };
        if (mode == "upgrade") RefreshUpgradeCounts();
        UpdateHero();
    }

    // Subtitle, hero and nav badge count only the updates that aren't skipped (or already done).
    private void RefreshUpgradeCounts()
    {
        if (_mode != "upgrade") return;
        int skipped = _all.Count(p => p.IsSkipped);
        int active = _all.Count(p => !p.IsSkipped && p.State != RunState.Succeeded);
        bool changed = !Equals(UpBadge.Value, active);
        UpBadge.Value = active;
        UpBadge.Visibility = active > 0 ? Visibility.Visible : Visibility.Collapsed;
        if (changed && active > 0) Motion.Pop(UpBadge, 0.5f);
        var text = active > 0 ? $"{active} {(active == 1 ? "update" : "updates")} available" : "All apps are up to date";
        Subtitle.Text = skipped > 0 ? $"{text}  ·  {skipped} skipped" : text;
        UpdateHero();
    }

    private bool _showSkipped;

    // What the list is allowed to show before search/source filters: skipped updates stay hidden unless asked for.
    private IEnumerable<Pkg> Listed => _mode == "upgrade" && !_showSkipped ? _all.Where(p => !p.IsSkipped) : _all;

    private void ShowSkipped_Click(object sender, RoutedEventArgs e)
    {
        _showSkipped = ShowSkippedItem.IsChecked;
        RefreshSourceChoices();
        ApplyFilter();
    }

    private void OnPkgChanged(object? s, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(Pkg.IsSelected)) UpdateFooter();
        else if (e.PropertyName == nameof(Pkg.Log) && ReferenceEquals(s, _shownPkg)) ShowLog(s as Pkg);
    }

    private void ShowLog(Pkg? p)
    {
        var log = p?.Log ?? "";
        DLogPanel.Visibility = log.Length > 0 ? Visibility.Visible : Visibility.Collapsed;
        DLog.Text = log;
        if (log.Length > 0) DispatcherQueue.TryEnqueue(() => DLogScroll.ChangeView(null, DLogScroll.ScrollableHeight, null, true));
    }

    private void DCopyLog_Click(object sender, RoutedEventArgs e)
    {
        if (_shownPkg is not { } p) return;
        CopyText(p.Log);
        Notify("Log copied", "Info");
    }

    // ---------- navigation & search ----------

    private void Nav_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        string next;
        if (args.IsSettingsSelected) next = "settings";
        else if (args.SelectedItem is NavigationViewItem item) next = _pendingImport ? "import" : (string)item.Tag;
        else return;

        _pendingImport = false;
        _mode = next;
        if (!_jumpToDiscover) Search.Text = "";
        _jumpToDiscover = false;
        if (_mode == "discover") _discoverQuery = Search.Text.Trim();
        if (_mode != "upgrade" && _sort == 4) ResetSort();
        _sourceFilter = null;
        ApplyModeLayout();
        PlayPageTransition();
        if (_mode == "settings")
        {
            ShowDetail(null);
            LoadSettingsUi();
            return;
        }
        _ = LoadAsync();
    }

    /// <summary>Shows the parts of the window the current page uses.</summary>
    private void ApplyModeLayout()
    {
        bool hist = _mode == "history", settings = _mode == "settings";
        bool home = _mode == "discover" && _discoverQuery.Length == 0;
        bool list = IsListMode;
        static Visibility V(bool b) => b ? Visibility.Visible : Visibility.Collapsed;

        MainCol.Visibility = V(!settings);
        SettingsView.Visibility = V(settings);
        Hero.Visibility = V(_mode == "upgrade");
        ListTools.Visibility = V(list);
        PkgList.Visibility = V(list);
        HistList.Visibility = V(hist);
        DiscoverHome.Visibility = V(home);
        RefreshBtn.Visibility = V(list);
        BackupBtn.Visibility = V(!hist && !settings);
        ClearHistoryBtn.Visibility = V(hist);
        DetailPane.Visibility = V(list);
        AdaptLayout();
        ShowSkippedItem.Visibility = ShowSkippedSep.Visibility = V(_mode == "upgrade");
        SortKind.Visibility = V(_mode == "upgrade");
        AppTitleBar.IsBackButtonVisible = _mode == "import" || (_mode == "discover" && !home);
        Search.PlaceholderText = _mode switch
        {
            "discover" => "Search the winget catalogue",
            "history" => "Filter history",
            "upgrade" => "Filter updates  (Enter searches winget)",
            "installed" => "Filter installed apps  (Enter searches winget)",
            _ => "Search the winget catalogue",
        };
        if (!list) SetActionBar(false);
        UpdateFooter();
    }

    /// <summary>The detail pane narrows on small windows so the list keeps enough room.</summary>
    private void AdaptLayout()
    {
        bool detail = DetailPane.Visibility == Visibility.Visible;
        double w = ContentRoot.ActualWidth;
        DetailCol.Width = new GridLength(!detail ? 0 : w > 0 && w < 1000 ? 336 : 400);
    }

    private void PlayPageTransition()
    {
        if (_mode == "settings")
        {
            Motion.FadeInUp(SettingsContent, distance: 16, durationMs: 420);
            return;
        }
        Motion.FadeInUp(MainCol, distance: 14, durationMs: 400);
        if (DetailPane.Visibility == Visibility.Visible) Motion.FadeInUp(DetailPane, delayMs: 70, distance: 14, durationMs: 420);
    }

    private async void ClearHistory_Click(object sender, RoutedEventArgs e)
    {
        if (HistoryStore.All.Count == 0) return;
        var dlg = NewDialog();
        dlg.Title = "Clear history?";
        dlg.Content = "This deletes the list of past installs, updates and uninstalls. Your apps aren't affected.";
        dlg.PrimaryButtonText = "Clear";
        dlg.CloseButtonText = "Cancel";
        dlg.DefaultButton = ContentDialogButton.Close;
        if (await ShowDialogAsync(dlg) != ContentDialogResult.Primary) return;
        HistoryStore.Clear();
        RefreshHistory();
        Notify("History cleared");
    }

    /// <summary>winget not being installed gets a clear explanation and a fix, not a raw exception message.</summary>
    private void ReportWingetError(Exception ex)
    {
        if (WingetService.IsMissing(ex))
        {
            WingetBar.IsOpen = true;
            SetActivity("winget was not found", error: true);
        }
        else
        {
            SetActivity("winget failed", error: true);
            Notify("winget failed: " + ex.Message, "Error");
        }
        UpdateFooter();
        UpdateHero();
    }

    private void Search_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
    {
        if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput && _mode is not ("discover" or "settings"))
            ApplyFilter();
    }

    private void Search_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
    {
        if (_running) return;
        if (Search.Text.Trim().Length == 0 && _mode != "discover") return;
        if (_mode == "import")
        {
            // Discover is already the selected nav item while restoring, so switch the mode directly.
            _mode = "discover";
            _discoverQuery = Search.Text.Trim();
            ApplyModeLayout();
            _ = LoadAsync();
        }
        else if (_mode != "discover")
        {
            _jumpToDiscover = true;
            Nav.SelectedItem = NavDiscover;   // triggers the search
        }
        else
        {
            _discoverQuery = Search.Text.Trim();
            ApplyModeLayout();
            _ = LoadAsync();
        }
    }

    private void Refresh_Click(object sender, RoutedEventArgs e)
    {
        Motion.SpinOnce(RefreshIcon);
        _ = LoadAsync(force: true);
    }

    // ---------- detail pane ----------

    private Pkg? _shownPkg;

    private void PkgList_SelectionChanged(object sender, SelectionChangedEventArgs e) =>
        ShowDetail(PkgList.SelectedItem as Pkg);

    private async void ShowDetail(Pkg? p)
    {
        if (p is null)
        {
            _shownPkg = null;
            DPanel.Visibility = Visibility.Collapsed;
            DEmpty.Visibility = Visibility.Visible;
            DAvatar.Pkg = null;
            return;
        }
        bool changed = !ReferenceEquals(_shownPkg, p);
        _shownPkg = p;
        DEmpty.Visibility = Visibility.Collapsed;
        DPanel.Visibility = Visibility.Visible;
        if (changed)
        {
            DPanel.ChangeView(null, 0, null, true);
            Motion.FadeInFromRight(DContent);
        }
        ShowLog(p);

        DAvatar.Pkg = p;
        DName.Text = p.Name;
        DId.Text = p.Id;
        DSourcePill.Text = p.Source;
        DSourcePill.Tone = p.SourceTone;
        DSourcePill.Visibility = p.SourceVisibility;
        DKindPill.Text = p.KindLabel.Length > 0 ? $"{p.KindLabel} update" : "";
        DKindPill.Tone = p.KindTone;
        DKindPill.Visibility = p.KindVisibility;
        DPinnedPill.Visibility = p.PinnedVisibility;
        DSkippedPill.Text = p.SkipLabel;
        DSkippedPill.Visibility = p.SkippedVisibility;
        DInstalledPill.Visibility = p.InstalledVisibility;
        UpdateVersionCard(p);
        UpdateDetailAction(p);
        DCmd.Text = p.IsManageable ? WingetService.Command(p, _mode) : "winget can't manage this app by id (it was installed outside winget).";

        DDesc.Text = "Loading details…";
        DPub.Text = DLic.Text = DInstaller.Text = "";
        DSource.Text = p.Source.Length > 0 ? p.Source : "Not from a winget source";
        DHome.Content = "";
        DHome.NavigateUri = null;
        DTags.Children.Clear();
        DNotesSection.Visibility = Visibility.Collapsed;

        if (!_detailCache.TryGetValue(p.Id, out var d))
        {
            if (!p.IsManageable) d = PkgDetail.Empty;
            else
            {
                try { d = await Task.Run(() => WingetService.ShowAsync(p.Id)); }
                catch { d = PkgDetail.Empty; }
            }
            _detailCache[p.Id] = d;
        }
        if (!ReferenceEquals(_shownPkg, p)) return;   // selection moved on

        DDesc.Text = d.Description.Length > 0 ? d.Description : "No description available.";
        DPub.Text = d.Publisher.Length > 0 ? d.Publisher : d.Author.Length > 0 ? d.Author : "—";
        DLic.Text = d.License.Length > 0 ? d.License : "—";
        bool hasHome = Uri.TryCreate(d.Homepage, UriKind.Absolute, out var home) && home.Scheme is "https" or "http";
        DHome.NavigateUri = hasHome ? home : null;
        DHome.Content = hasHome ? (home!.Host + home.AbsolutePath).TrimEnd('/') : "";
        DHome.Visibility = DHomeLabel.Visibility = hasHome ? Visibility.Visible : Visibility.Collapsed;
        DInstaller.Text = d.InstallerType;
        DInstaller.Visibility = DInstallerLabel.Visibility = d.InstallerType.Length > 0 ? Visibility.Visible : Visibility.Collapsed;
        foreach (var tag in d.Tags) DTags.Children.Add(new Pill { Text = tag });
        DTags.Visibility = DTagsLabel.Visibility = d.Tags.Count > 0 ? Visibility.Visible : Visibility.Collapsed;

        bool hasNotes = d.ReleaseNotes.Length > 0;
        bool hasNotesLink = Uri.TryCreate(d.ReleaseNotesUrl, UriKind.Absolute, out var notesUri) && notesUri.Scheme is "https" or "http";
        if (hasNotes || hasNotesLink)
        {
            DNotes.Text = hasNotes ? d.ReleaseNotes : "This release doesn't include notes in winget.";
            DNotes.MaxLines = 7;
            DNotesMore.Content = "Show more";
            DNotesMore.Visibility = d.ReleaseNotes.Length > 360 || d.ReleaseNotes.Count(c => c == '\n') > 6 ? Visibility.Visible : Visibility.Collapsed;
            DNotesLink.NavigateUri = hasNotesLink ? notesUri : null;
            DNotesLink.Visibility = hasNotesLink ? Visibility.Visible : Visibility.Collapsed;
            DNotesDate.Text = DateTime.TryParse(d.ReleaseDate, System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None, out var released)
                ? $"{(d.Version.Length > 0 ? d.Version + "  ·  " : "")}{released:d MMM yyyy}"
                : d.Version;
            DNotesSection.Visibility = Visibility.Visible;
        }
    }

    private void UpdateVersionCard(Pkg p)
    {
        bool twoSided = _mode == "upgrade" || (p.TargetVersion.Length > 0 && _mode != "installed") || (_mode == "installed" && p.Available.Length > 0);
        DArrow.Visibility = DTo.Visibility = DToLabel.Visibility = twoSided ? Visibility.Visible : Visibility.Collapsed;
        switch (_mode)
        {
            case "upgrade":
                DFromLabel.Text = "INSTALLED";
                DFrom.Text = p.Version;
                DToLabel.Text = p.TargetVersion.Length > 0 ? "CHOSEN" : "LATEST";
                DTo.Text = p.TargetText;
                break;
            case "installed":
                DFromLabel.Text = "INSTALLED";
                DFrom.Text = p.Version;
                DToLabel.Text = "AVAILABLE";
                DTo.Text = p.Available;
                break;
            case "import":
                DFromLabel.Text = p.IsInstalled ? "INSTALLED" : "WILL INSTALL";
                DFrom.Text = p.IsInstalled ? p.Version : p.TargetVersion.Length > 0 ? p.TargetVersion : "Latest version";
                DToLabel.Text = "CHOSEN";
                DTo.Text = p.TargetVersion;
                if (!p.IsInstalled) DArrow.Visibility = DTo.Visibility = DToLabel.Visibility = Visibility.Collapsed;
                break;
            default:
                DFromLabel.Text = "LATEST";
                DFrom.Text = p.Version;
                DToLabel.Text = "CHOSEN";
                DTo.Text = p.TargetVersion;
                break;
        }
    }

    private void UpdateDetailAction(Pkg p)
    {
        bool done = _mode == "import" && p.IsInstalled;
        DActionIcon.Glyph = done ? "" : p.ActionGlyph;
        DActionText.Text = !p.IsManageable ? "Can't be managed by winget"
            : done ? "Already installed"
            : _mode switch
            {
                "upgrade" => $"Update to {p.TargetText}",
                "installed" => $"Uninstall {p.Name}",
                _ => p.TargetVersion.Length > 0 ? $"Install {p.TargetVersion}" : $"Install {p.Name}",
            };
        DAction.IsEnabled = !_running && p.IsManageable && !done;
    }

    private void DNotesMore_Click(object sender, RoutedEventArgs e)
    {
        bool expand = DNotes.MaxLines != 0;
        DNotes.MaxLines = expand ? 0 : 7;
        DNotesMore.Content = expand ? "Show less" : "Show more";
    }

    private async void DCopy_Click(object sender, RoutedEventArgs e)
    {
        CopyText(DCmd.Text);
        DCopyIcon.Glyph = "";
        Motion.Pop(DCopyIcon, 0.3f);
        ToolTipService.SetToolTip(DCopy, "Copied!");
        await Task.Delay(1600);
        DCopyIcon.Glyph = "";
        Motion.Pop(DCopyIcon, 0.6f);
        ToolTipService.SetToolTip(DCopy, "Copy command");
    }

    private void DAction_Click(object sender, RoutedEventArgs e)
    {
        if (_running || _shownPkg is not { } p) return;
        _ = StartRun(new[] { p });
    }

    private void DMoreMenu_Opening(object sender, object e)
    {
        DMoreMenu.Items.Clear();
        if (_shownPkg is { } p) BuildPkgMenu(DMoreMenu.Items, p, includePrimary: false);
    }

    // ---------- skip & pin ----------

    private static void RefreshSkip(Pkg p)
    {
        var s = AppSettings.Current;
        p.IsSkipped = s.IsSkipped(p.Id, p.Available);
        p.SkipLabel = s.SkippedVersionOf(p.Id) is { } v ? $"Skipped {v}" : "Skipped";
    }

    private void ApplySkip(Pkg p, string message, Action? undo)
    {
        RefreshSkip(p);
        if (p.IsSkipped) p.IsSelected = false;
        RefreshSourceChoices();
        ApplyFilter();           // a newly skipped row leaves the list (and the detail pane closes)
        RefreshUpgradeCounts();
        if (ReferenceEquals(_shownPkg, p)) ShowDetail(p);
        Notify(message, "Success", undo is null ? null : "Undo", undo);
    }

    private void SkipVersion(Pkg p)
    {
        if (p.Available.Length == 0) return;
        AppSettings.Current.SkipVersion(p.Id, p.Available);
        ApplySkip(p, $"Skipping {p.Name} {p.Available}. It comes back when a newer version is out.", () => Unskip(p, quiet: true));
    }

    private void SkipAlways(Pkg p)
    {
        AppSettings.Current.SetSkipped(p.Id, true);
        ApplySkip(p, $"Skipping all updates for {p.Name}. Manage skips in Settings.", () => Unskip(p, quiet: true));
    }

    private void Unskip(Pkg p, bool quiet = false)
    {
        AppSettings.Current.SetSkipped(p.Id, false);
        if (quiet)
        {
            RefreshSkip(p);
            RefreshSourceChoices();
            ApplyFilter();
            RefreshUpgradeCounts();
            Notify($"{p.Name} is back in Updates", "Info");
            return;
        }
        ApplySkip(p, $"{p.Name} will show up in Updates again", null);
    }

    private bool _pinBusy;

    private async Task TogglePinAsync(Pkg p)
    {
        if (_pinBusy || _running) return;
        if (!p.IsManageable)
        {
            Notify("Skipped: the package id looks unsafe or truncated", "Error");
            return;
        }

        bool unpin = p.IsPinned;
        _pinBusy = true;
        SetActivity($"{(unpin ? "Unpinning" : "Pinning")} {p.Name}…");
        try
        {
            var (code, output) = await Task.Run(() =>
                WingetService.RunCaptureAsync("pin", unpin ? "remove" : "add", "--id", p.Id, "-e", "--accept-source-agreements"));
            if (code != 0)
            {
                var why = WingetService.LastLine(output);
                Notify($"Could not {(unpin ? "unpin" : "pin")} {p.Name}" + (why.Length > 0 ? ": " + why : $" (exit code 0x{code:X8})"), "Error");
                return;
            }

            _listCache.Clear();   // pins change what winget offers as updates
            p.IsPinned = !unpin;
            if (!unpin && _mode == "upgrade")
            {
                // winget no longer offers a pinned app as an update, so take it off this list too.
                p.IsSelected = false;
                _all.Remove(p);
                RefreshSourceChoices();
                ApplyFilter();
                RefreshUpgradeCounts();
                Notify($"Pinned {p.Name}. It won't be offered as an update. Manage pins in Installed.");
            }
            else
            {
                if (ReferenceEquals(_shownPkg, p)) ShowDetail(p);
                Notify(unpin ? $"Unpinned {p.Name}" : $"Pinned {p.Name} at {p.Version}");
            }
        }
        catch (Exception ex)
        {
            Notify("winget failed: " + ex.Message, "Error");
        }
        finally
        {
            _pinBusy = false;
            SetActivity(null);
        }
    }

    // ---------- in-app runner ----------

    private bool _running;
    private CancellationTokenSource? _runCts;

    private void Install_Click(object sender, RoutedEventArgs e)
    {
        if (_running)
        {
            _runCts?.Cancel();
            SetActivity("Cancelling…");
            return;
        }
        _ = StartRun(Selected.ToList());
    }

    private void UpdateAll_Click(object sender, RoutedEventArgs e)
    {
        if (_running || _mode != "upgrade") return;
        // What runs is exactly what is on screen and ticked: clear the filters so every pending update is listed.
        Search.Text = "";
        _sourceFilter = null;
        RefreshSourceChoices();
        ApplyFilter();
        var targets = Items.Where(p => !p.IsSkipped && p.IsManageable && p.State != RunState.Succeeded).ToList();
        foreach (var p in Items) p.IsSelected = targets.Contains(p);
        _ = StartRun(targets);
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

        var dlg = NewDialog();
        dlg.Title = pkgs.Count == 1 ? $"Uninstall {pkgs[0].Name}?" : $"Uninstall {pkgs.Count} apps?";
        dlg.Content = body;
        dlg.PrimaryButtonText = "Uninstall";
        dlg.CloseButtonText = "Cancel";
        dlg.DefaultButton = ContentDialogButton.Close;
        return await ShowDialogAsync(dlg) == ContentDialogResult.Primary;
    }

    private async Task RunInAppAsync(IReadOnlyList<Pkg> pkgs)
    {
        if (pkgs.Count == 0) return;
        var safe = pkgs.Where(p => WingetService.IsSafeId(p.Id)).ToList();
        if (safe.Count == 0)
        {
            Notify("Skipped: the package id looks unsafe or truncated", "Error");
            return;
        }

        string mode = _mode;
        string verb = VerbLabel;
        _running = true;
        _runCts = new CancellationTokenSource();
        var ct = _runCts.Token;
        SetRunLock(true);
        _taskbar.SetState(Taskbar.State.Indeterminate);

        foreach (var p in safe) { p.ResetRun(); p.SetState(RunState.Queued, "Queued"); }

        void Record(Pkg p, string result, int code) => HistoryStore.Add(new HistoryEntry
        {
            When = DateTimeOffset.Now,
            Action = verb,
            Name = p.Name,
            Id = p.Id,
            FromVersion = mode is "upgrade" or "installed" ? p.Version : "",
            ToVersion = mode switch { "upgrade" => p.TargetText, "discover" or "import" => p.TargetVersion.Length > 0 ? p.TargetVersion : p.Version, _ => "" },
            Result = result,
            ExitCode = code,
        });

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
            _runPkg = p;
            _runIndex = i;
            _runTotal = safe.Count;
            _runVerb = verb;
            ShowRunProgress();
            PropertyChangedEventHandler onProgress = (_, e) =>
            {
                if (e.PropertyName is nameof(Pkg.Progress) or nameof(Pkg.RunStatus)) ShowRunProgress();
            };
            p.PropertyChanged += onProgress;
            try
            {
                int code = await WingetService.RunPackageAsync(p, mode, ct);
                if (ct.IsCancellationRequested)
                {
                    p.SetState(RunState.Cancelled, "Cancelled");
                    cancelled++;
                    Record(p, "Cancelled", code);
                }
                else if (WingetService.IsSuccess(code))
                {
                    p.SetState(RunState.Succeeded, code != 0 ? "Already up to date" : mode == "installed" ? "Uninstalled" : "Done");
                    p.IsSelected = false;
                    if (mode == "import") p.IsInstalled = true;
                    ok++;
                    if (mode == "installed" && code == 0) removed.Add(p);
                    Record(p, "Succeeded", code);
                }
                else
                {
                    p.SetState(RunState.Failed, $"Failed (exit code 0x{code:X8})");
                    failed++;
                    Record(p, "Failed", code);
                }
            }
            catch (Exception ex)
            {
                p.AppendLog(ex.Message);
                p.SetState(RunState.Failed, "Failed: " + ex.Message);
                failed++;
                Record(p, "Failed", 0);
                if (WingetService.IsMissing(ex)) WingetBar.IsOpen = true;
            }
            finally
            {
                p.PropertyChanged -= onProgress;
            }
        }

        _running = false;
        _runPkg = null;
        _runCts.Dispose();
        _runCts = null;
        _listCache.Clear();   // installed apps and available updates have changed
        SetRunLock(false);

        if (removed.Count > 0 && _mode == mode)
        {
            // Dropped in one go at the end (not one by one) so rows don't jump around while the run is in progress.
            foreach (var p in removed) _all.Remove(p);
            RefreshSourceChoices();
            ApplyFilter();
            int pinned = _all.Count(p => p.IsPinned);
            Subtitle.Text = pinned > 0 ? $"{_all.Count} packages  ·  {pinned} pinned" : $"{_all.Count} packages";
        }
        if (_mode == mode && mode == "import")
            Subtitle.Text = _importIsPack
                ? $"{_all.Count} apps  ·  {_all.Count(p => p.IsInstalled)} already installed"
                : $"{_all.Count} apps in {_importName}  ·  {_all.Count(p => p.IsInstalled)} already installed";
        if (_shownPkg is { } shown) ShowDetail(shown);
        RefreshUpgradeCounts();
        UpdateHero();

        // Taskbar: red if anything failed (cleared when the window is next activated), and a flash if we're in the background.
        if (failed > 0)
        {
            _taskbar.SetProgress(1);
            _taskbar.SetState(Taskbar.State.Error);
        }
        else
        {
            _taskbar.SetState(Taskbar.State.None);
        }
        _taskbar.FlashIfInBackground();

        var parts = new List<string>();
        if (safe.Count < pkgs.Count) parts.Add($"{pkgs.Count - safe.Count} skipped (no usable package id)");
        if (ok > 0) parts.Add($"{ok} {(mode == "installed" ? "removed" : mode == "upgrade" ? "updated" : "installed")}");
        if (failed > 0) parts.Add($"{failed} failed");
        if (cancelled > 0) parts.Add($"{cancelled} cancelled");
        var summary = string.Join(", ", parts);
        if (failed + cancelled > 0)
        {
            Notify(summary + ". Unfinished items stay selected so you can retry.", failed > 0 ? "Error" : "Warning", "Retry", () =>
            {
                if (!_running && _mode == mode) _ = StartRun(Selected.ToList());
            });
        }
        else
        {
            Celebrate();
            if (mode == "upgrade") Notify(summary + ". Nice and fresh.", "Success", "Rescan", () => _ = LoadAsync(force: true));
            else Notify(summary, "Success", "History", () => Nav.SelectedItem = NavHistory);
        }
        SetActivity(null);
    }

    // While a run is going, block anything that would reload or replace the list under it.
    private void SetRunLock(bool locked)
    {
        Nav.IsEnabled = !locked;
        RefreshBtn.IsEnabled = !locked;
        BackupBtn.IsEnabled = !locked;
        SelectAll.IsEnabled = !locked;
        DAction.IsEnabled = !locked && _shownPkg is { IsManageable: true };
        UpdateAllBtn.IsEnabled = !locked;
        AppTitleBar.IsBackButtonEnabled = !locked;
        UpdateFooter();
        UpdateHero();
    }

    // ---------- backup & restore ----------

    private List<Pkg>? _importPkgs;     // packages read from the chosen export file, or a starter pack
    private string _importName = "";
    private string _importTitle = "Restore";
    private bool _importIsPack;
    private bool _pendingImport;        // the next nav selection opens the restore list

    private async void Export_Click(object sender, RoutedEventArgs e)
    {
        if (_running) return;
        string path;
        try
        {
            var picker = new FileSavePicker(AppWindow.Id)
            {
                SuggestedFileName = $"winget-apps-{DateTime.Now:yyyy-MM-dd}",
                SuggestedStartLocation = PickerLocationId.DocumentsLibrary,
            };
            picker.FileTypeChoices.Add("winget app list", new List<string> { ".json" });
            var res = await picker.PickSaveFileAsync();
            if (res is null) return;
            path = res.Path;
        }
        catch (Exception ex)
        {
            Notify("Could not open the save dialog: " + ex.Message, "Error");
            return;
        }

        BackupBtn.IsEnabled = false;
        SetActivity("Exporting installed apps…");
        try
        {
            var (code, output) = await Task.Run(() =>
                WingetService.RunCaptureAsync("export", "-o", path, "--accept-source-agreements"));
            int n = 0;
            try { n = BackupFile.Parse(await File.ReadAllTextAsync(path)).Count; } catch { /* reported below */ }
            if (n == 0)
            {
                var why = WingetService.LastLine(output);
                Notify("Export failed" + (why.Length > 0 ? ": " + why : $" (exit code 0x{code:X8})"), "Error");
            }
            else
            {
                Notify($"Exported {n} apps to {Path.GetFileName(path)}. Use Backup → Restore on any PC to reinstall them.", "Success",
                    "Show file", () => Process.Start(new ProcessStartInfo("explorer.exe", $"/select,\"{path}\"") { UseShellExecute = true }));
            }
        }
        catch (Exception ex)
        {
            ReportWingetError(ex);
        }
        finally
        {
            BackupBtn.IsEnabled = !_running;
            SetActivity(null);
        }
    }

    private async void Import_Click(object sender, RoutedEventArgs e)
    {
        if (_running) return;
        string path;
        try
        {
            var picker = new FileOpenPicker(AppWindow.Id) { SuggestedStartLocation = PickerLocationId.DocumentsLibrary };
            picker.FileTypeFilter.Add(".json");
            var res = await picker.PickSingleFileAsync();
            if (res is null) return;
            path = res.Path;
        }
        catch (Exception ex)
        {
            Notify("Could not open the file dialog: " + ex.Message, "Error");
            return;
        }

        List<Pkg> pkgs;
        try
        {
            if (new FileInfo(path).Length > 5 * 1024 * 1024) throw new FormatException("The file is too large to be an app list.");
            pkgs = BackupFile.Parse(await File.ReadAllTextAsync(path));
        }
        catch (Exception ex) when (ex is FormatException or IOException or UnauthorizedAccessException)
        {
            Notify("Can't restore from that file: " + ex.Message, "Error");
            return;
        }
        if (pkgs.Count == 0)
        {
            Notify("That file doesn't list any apps", "Warning");
            return;
        }

        OpenImportList(pkgs, "Restore", Path.GetFileName(path), isPack: false);
    }

    /// <summary>Shows a list of apps to install (a restore file or a starter pack) under Discover.</summary>
    private void OpenImportList(List<Pkg> pkgs, string title, string name, bool isPack)
    {
        _importPkgs = pkgs;
        _importTitle = title;
        _importName = name;
        _importIsPack = isPack;
        Search.Text = "";
        if (ReferenceEquals(Nav.SelectedItem, NavDiscover))
        {
            _mode = "import";
            ApplyModeLayout();
            PlayPageTransition();
            _ = LoadAsync();
        }
        else
        {
            _pendingImport = true;
            Nav.SelectedItem = NavDiscover;   // restoring is installing, so it lives under Discover
        }
    }

    /// <summary>Turns the saved list into rows, marking what's already installed and ticking the rest.</summary>
    private List<Pkg> MatchImport(List<Pkg> installed)
    {
        var have = installed.GroupBy(p => p.Id, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);
        var rows = new List<Pkg>();
        foreach (var t in _importPkgs ?? new())
        {
            var p = new Pkg { Name = t.Name, Id = t.Id, Version = t.Version, Source = t.Source };
            if (have.TryGetValue(t.Id, out var i))
            {
                p.IsInstalled = true;
                if (i.Name.Length > 0) p.Name = i.Name;
                if (p.Version.Length == 0) p.Version = i.Version;
            }
            else
            {
                p.IsSelected = true;
            }
            if (p.Version.Length == 0) p.Version = "latest";   // winget installs the newest version
            rows.Add(p);
        }
        return rows;
    }

    // ---------- selection / script ----------

    private void SelectAll_Click(object sender, RoutedEventArgs e)
    {
        bool v = SelectAll.IsChecked == true;
        foreach (var p in Items) p.IsSelected = v && (p.IsManageable || _mode == "installed");
    }

    private void ClearSel_Click(object sender, RoutedEventArgs e)
    {
        foreach (var p in Items) p.IsSelected = false;
    }

    private async void CopyScript_Click(object sender, RoutedEventArgs e)
    {
        CopyText(string.Join("\r\n", Selected.Where(p => WingetService.IsSafeId(p.Id)).Select(p => WingetService.Command(p, _mode))));
        Notify("Script copied to the clipboard. Paste it into any terminal.", "Info");
        CopyBtnIcon.Glyph = "";
        Motion.Pop(CopyBtnIcon, 0.3f);
        await Task.Delay(1400);
        CopyBtnIcon.Glyph = "";
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
            Notify("Skipped: the package id looks unsafe or truncated", "Error");
            return;
        }
        if (WingetService.Demo)
        {
            Notify($"Demo mode: a terminal would open for {pkgs.Count} package(s) here", "Info");
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
            _listCache.Clear();   // the terminal is about to change what's installed
            Notify($"Terminal opened for {pkgs.Count} package(s). Press Refresh when it's done.", "Info", "Refresh", () => _ = LoadAsync(force: true));
        }
        catch (Win32Exception ex) when (ex.NativeErrorCode == 1223)
        {
            Notify("Cancelled (administrator prompt declined)", "Warning");
        }
        catch (Exception ex)
        {
            Notify("Could not open terminal: " + ex.Message, "Error");
        }
    }
}
