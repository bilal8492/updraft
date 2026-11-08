using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Windows.System;
using Windows.UI;
using DispatcherQueueTimer = Microsoft.UI.Dispatching.DispatcherQueueTimer;

namespace Updraft;

// Everything that makes the window feel alive: status, toasts, the hero card, empty states, loading placeholders,
// the floating selection bar, row hover actions and context menus.
public sealed partial class MainWindow
{
    private Storyboard? _skeletonStory;

    private void SetupChrome()
    {
        Motion.UseShowHide(ActionBar, 28);
        Motion.UseShowHide(Toast, 16);
        Motion.UseShowHide(DNotesSection, 8);
        Motion.ClipRounded(Hero, 14);
        Motion.AttachPressScale(UpdateAllBtn, 0.95f);
        Motion.AttachPressScale(InstallBtn, 0.95f);
        Motion.AttachPressScale(DAction, 0.97f);
        Motion.AttachPressScale(RefreshBtn, 0.95f);

        Skeleton.ItemsSource = new double[] { 190, 250, 160, 228, 140, 262, 176, 214, 168, 236 };
        var pulse = new DoubleAnimation
        {
            From = 1,
            To = 0.45,
            Duration = new Duration(TimeSpan.FromMilliseconds(850)),
            AutoReverse = true,
            RepeatBehavior = RepeatBehavior.Forever,
            EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut },
        };
        Storyboard.SetTarget(pulse, Skeleton);
        Storyboard.SetTargetProperty(pulse, "Opacity");
        _skeletonStory = new Storyboard();
        _skeletonStory.Children.Add(pulse);

        BuildPopularChips();
        ContentRoot.SizeChanged += (_, _) => AdaptLayout();
        Hero.SizeChanged += (_, e) => HeroChevrons.Visibility = e.NewSize.Width > 760 ? Visibility.Visible : Visibility.Collapsed;
        Nav.DisplayModeChanged += (_, _) => UpdatePaneFooter();
        Nav.PaneOpened += (_, _) => UpdatePaneFooter();
        Nav.PaneClosed += (_, _) => UpdatePaneFooter();

        Toast.PointerEntered += (_, _) => _toastTimer?.Stop();
        Toast.PointerExited += (_, _) => RestartToastTimer(2500);
    }

    private void AppTitleBar_PaneToggleRequested(TitleBar sender, object args) => Nav.IsPaneOpen = !Nav.IsPaneOpen;

    private void UpdatePaneFooter() =>
        StatusText.Visibility = Nav.IsPaneOpen || Nav.DisplayMode == NavigationViewDisplayMode.Expanded ? Visibility.Visible : Visibility.Collapsed;

    // ---------- status (pane footer) ----------

    private static readonly SolidColorBrush ReadyBrush = new(Color.FromArgb(255, 34, 197, 94));
    private static readonly SolidColorBrush BusyBrush = new(Color.FromArgb(255, 245, 158, 11));
    private static readonly SolidColorBrush ErrorBrush = new(Color.FromArgb(255, 239, 68, 68));

    /// <summary>What winget is doing right now. null means idle.</summary>
    private void SetActivity(string? text, bool error = false)
    {
        bool busy = text is not null && !error;
        StatusText.Text = text ?? (WingetService.Demo ? "Demo mode · nothing is installed" : WingetBar.IsOpen ? "winget not found" : "winget ready");
        Dot.Fill = error || (text is null && WingetBar.IsOpen) ? ErrorBrush : busy ? BusyBrush : ReadyBrush;
        Motion.SetPulsing(DotPulse, busy);
    }

    // ---------- toasts ----------

    private DispatcherQueueTimer? _toastTimer;
    private Action? _toastAction;
    private int _toastMs;

    private static readonly SolidColorBrush ToastGreen = new(Color.FromArgb(255, 22, 163, 74));
    private static readonly SolidColorBrush ToastRed = new(Color.FromArgb(255, 220, 38, 38));
    private static readonly SolidColorBrush ToastAmber = new(Color.FromArgb(255, 217, 119, 6));
    private static readonly SolidColorBrush ToastBlue = new(Color.FromArgb(255, 47, 123, 230));

    /// <summary>A short message that slides in above the selection bar and dismisses itself (errors stay longer).</summary>
    private void Notify(string message, string tone = "Success", string? actionText = null, Action? action = null)
    {
        (ToastDot.Fill, ToastIcon.Glyph) = tone switch
        {
            "Error" => (ToastRed, ""),
            "Warning" => (ToastAmber, ""),
            "Info" => (ToastBlue, ""),
            _ => (ToastGreen, ""),
        };
        ToastText.Text = message;
        ToastAction.Content = actionText;
        ToastAction.Visibility = actionText is null ? Visibility.Collapsed : Visibility.Visible;
        _toastAction = action;

        bool wasVisible = Toast.Visibility == Visibility.Visible;
        Toast.Visibility = Visibility.Visible;
        if (wasVisible) Motion.Bump(Toast, 1.025f);
        Motion.Pop(ToastBadge, 0.3f, delayMs: wasVisible ? 0 : 90);

        _toastMs = tone == "Error" ? 10000 : message.Length > 90 ? 7000 : 4500;
        RestartToastTimer(_toastMs);

        // Screen readers hear the message too.
        FrameworkElementAutomationPeer.CreatePeerForElement(ToastText)?.RaiseNotificationEvent(
            AutomationNotificationKind.Other, AutomationNotificationProcessing.ImportantMostRecent, message, "UpdraftToast");
    }

    private void RestartToastTimer(int ms)
    {
        if (Toast.Visibility != Visibility.Visible) return;
        _toastTimer?.Stop();
        _toastTimer = DispatcherQueue.CreateTimer();
        _toastTimer.Interval = TimeSpan.FromMilliseconds(ms);
        _toastTimer.IsRepeating = false;
        _toastTimer.Tick += (_, _) => HideToast();
        _toastTimer.Start();
        Motion.Countdown(ToastTimer, ms);
    }

    private void HideToast()
    {
        _toastTimer?.Stop();
        _toastAction = null;
        Toast.Visibility = Visibility.Collapsed;
    }

    private void ToastAction_Click(object sender, RoutedEventArgs e)
    {
        var action = _toastAction;
        HideToast();
        action?.Invoke();
    }

    private void ToastClose_Click(object sender, RoutedEventArgs e) => HideToast();

    // ---------- hero card ----------

    private void UpdateHero()
    {
        if (_mode != "upgrade" || Hero.Visibility != Visibility.Visible) return;
        if (_running)
        {
            ShowRunProgress();
            return;
        }
        HeroProgress.Visibility = Visibility.Collapsed;
        UpdateAllBtn.IsEnabled = true;

        if (_busy)
        {
            SetHeroText(null, "Checking for updates…", "winget is comparing your apps with the latest versions", "");
            HeroChevrons.Period = 1.3;
            UpdateAllBtn.Visibility = Visibility.Collapsed;
            return;
        }
        HeroChevrons.Period = 2.6;

        if (WingetBar.IsOpen)
        {
            SetHeroText(null, "Can't check for updates", "winget isn't available on this PC. Install App Installer, then press Refresh.", "");
            UpdateAllBtn.Visibility = Visibility.Collapsed;
            return;
        }

        var pending = _all.Where(p => !p.IsSkipped && p.State != RunState.Succeeded).ToList();
        int skipped = _all.Count(p => p.IsSkipped);
        string when = _lastScan is { } t ? $"Checked {FormatWhen(t)}" : "";
        if (pending.Count == 0)
        {
            bool justUpdated = _all.Any(p => p.State == RunState.Succeeded);
            var meta = new List<string>();
            if (when.Length > 0) meta.Add(when);
            if (skipped > 0) meta.Add($"{skipped} skipped");
            SetHeroText(null, justUpdated ? "All updates installed" : "You're up to date",
                meta.Count > 0 ? string.Join("  ·  ", meta) : "Every app winget can update is on its latest version", "");
            UpdateAllBtn.Visibility = Visibility.Collapsed;
            return;
        }

        var parts = new List<string>();
        int major = pending.Count(p => p.Kind == UpdateKind.Major), minor = pending.Count(p => p.Kind == UpdateKind.Minor),
            patch = pending.Count(p => p.Kind == UpdateKind.Patch);
        if (major > 0) parts.Add($"{major} major");
        if (minor > 0) parts.Add($"{minor} minor");
        if (patch > 0) parts.Add($"{patch} patch");
        if (skipped > 0) parts.Add($"{skipped} skipped");
        if (when.Length > 0) parts.Add(when);
        SetHeroText(pending.Count, pending.Count == 1 ? "update available" : "updates available", string.Join("  ·  ", parts), null);
        UpdateAllText.Text = pending.Count == 1 ? "Update" : $"Update all {pending.Count}";
        UpdateAllBtn.Visibility = Visibility.Visible;
    }

    private string _heroState = "";

    private void SetHeroText(int? count, string title, string meta, string? glyph)
    {
        string state = $"{count.HasValue}|{title}|{glyph}";
        bool changed = state != _heroState;
        _heroState = state;

        HeroCount.Visibility = count.HasValue ? Visibility.Visible : Visibility.Collapsed;
        if (count is { } n) Motion.CountTo(HeroCount, n);
        HeroTitle.Text = title;
        HeroTitle.FontSize = count.HasValue ? 20 : 26;
        HeroTitle.Margin = count.HasValue ? new Thickness(0, 0, 0, 9) : new Thickness(0);
        HeroTitle.VerticalAlignment = count.HasValue ? VerticalAlignment.Bottom : VerticalAlignment.Center;
        HeroMeta.Text = meta;
        HeroBadge.Visibility = glyph is null ? Visibility.Collapsed : Visibility.Visible;
        if (glyph is not null) HeroGlyph.Glyph = glyph;

        if (!changed) return;
        if (glyph is not null) Motion.Pop(HeroBadge, 0.5f);
        Motion.FadeInUp(HeroTitle, distance: 8, durationMs: 360);
        Motion.FadeInUp(HeroMeta, delayMs: 60, distance: 6, durationMs: 360);
    }

    private static string FormatWhen(DateTime t)
    {
        var ago = DateTime.Now - t;
        return ago.TotalMinutes < 1 ? "just now"
            : ago.TotalMinutes < 60 ? $"{(int)ago.TotalMinutes} min ago"
            : $"at {t:t}";
    }

    // ---------- run progress ----------

    private Pkg? _runPkg;
    private int _runIndex, _runTotal;
    private string _runVerb = "";

    private void ShowRunProgress()
    {
        if (_runPkg is not { } p || _runTotal == 0) return;
        double frac = (_runIndex + (p.Indeterminate ? 0 : p.Progress / 100.0)) / _runTotal;
        string verbing = _runVerb switch { "Update" => "Updating", "Uninstall" => "Removing", _ => "Installing" };
        RunLabel.Text = $"{verbing} {p.Name}  ·  {_runIndex + 1} of {_runTotal}";
        RunProgress.Value = frac * 100;
        SetActivity($"{verbing} {p.Name}");
        if (frac > 0) _taskbar.SetProgress(frac);

        if (_mode == "upgrade" && Hero.Visibility == Visibility.Visible)
        {
            SetHeroText(null, $"{verbing} {_runIndex + 1} of {_runTotal}", $"{p.Name}  ·  {p.RunStatus}", "");
            HeroProgress.Visibility = Visibility.Visible;
            HeroProgress.Value = frac * 100;
            UpdateAllBtn.Visibility = Visibility.Collapsed;
        }
    }

    private void Celebrate()
    {
        if (_mode == "upgrade" && Hero.Visibility == Visibility.Visible && Hero.ActualWidth > 0)
            Motion.Burst(Hero, 52, Hero.ActualHeight / 2, 22);
        else if (Body.ActualWidth > 0)
            Motion.Burst(Body, Body.ActualWidth / 2, Math.Max(80, Body.ActualHeight - 70), 22);
    }

    // ---------- footer, empty and loading states ----------

    private int _lastSelCount;

    private void UpdateFooter()
    {
        UpdateEmptyState();
        UpdateLoadingState();
        if (!IsListMode)
        {
            SetActionBar(false);
            return;
        }

        int n = Selected.Count();
        SelCount.Text = n.ToString();
        if (n != _lastSelCount && n > 0 && _lastSelCount > 0) Motion.Bump(SelBadge, 1.22f);
        _lastSelCount = n;
        SelLabel.Text = n == 1 ? "app selected" : "apps selected";

        var runVis = _running ? Visibility.Visible : Visibility.Collapsed;
        var idleVis = _running ? Visibility.Collapsed : Visibility.Visible;
        BarSelection.Visibility = idleVis;
        BarRun.Visibility = runVis;
        CopyBtn.Visibility = RunBtn.Visibility = AdminChk.Visibility = idleVis;
        RunBtn.IsEnabled = CopyBtn.IsEnabled = n > 0 && !_running;
        InstallBtn.IsEnabled = _running || n > 0;
        InstallIcon.Glyph = _running ? "" : _mode switch { "installed" => "", "upgrade" => "", _ => "" };
        InstallText.Text = _running ? "Cancel" : $"{VerbLabel} {n}";
        SetActionBar(n > 0 || _running);

        int selectable = Items.Count;
        SelectAll.IsChecked = n == 0 ? false : n >= selectable ? true : null;
    }

    private void SetActionBar(bool show)
    {
        var v = show ? Visibility.Visible : Visibility.Collapsed;
        if (ActionBar.Visibility != v) ActionBar.Visibility = v;
    }

    private void UpdateLoadingState()
    {
        bool skeleton = _busy && Items.Count == 0 && IsListMode;
        if (skeleton == (Skeleton.Visibility == Visibility.Visible)) return;
        Skeleton.Visibility = skeleton ? Visibility.Visible : Visibility.Collapsed;
        if (skeleton)
        {
            Motion.FadeInUp(Skeleton, distance: 8, durationMs: 300);
            if (Motion.Enabled) _skeletonStory?.Begin();
        }
        else
        {
            _skeletonStory?.Stop();
        }
    }

    private string _emptyKind = "";
    private Action? _emptyAction;

    private void UpdateEmptyState()
    {
        string kind = "", glyph = "", title = "", body = "", action = "";
        Action? onAction = null;
        string q = Search.Text.Trim();

        if (_mode == "history")
        {
            if (HistoryCount == 0)
            {
                if (HistoryStore.All.Count > 0)
                {
                    (kind, glyph, title, body) = ("nomatch", "", "No matches", $"No history entry matches “{q}”.");
                    (action, onAction) = ("Clear filter", () => { Search.Text = ""; ApplyFilter(); });
                }
                else
                {
                    (kind, glyph, title, body) = ("nohistory", "", "No history yet",
                        "Installs, updates and uninstalls you run inside Updraft show up here, grouped by day.");
                    (action, onAction) = ("Check for updates", () => Nav.SelectedItem = NavUp);
                }
            }
        }
        else if (IsListMode && !_busy && Items.Count == 0)
        {
            int total = Listed.Count();
            if (WingetBar.IsOpen)
            {
                (kind, glyph, title, body) = ("error", "", "Couldn't reach winget",
                    "Updraft needs the Windows Package Manager. Get App Installer from the Microsoft Store, then try again.");
                (action, onAction) = ("Try again", () => _ = LoadAsync(force: true));
            }
            else if (total > 0)
            {
                (kind, glyph, title) = ("nomatch", "", "No matches");
                body = q.Length > 0 ? $"Nothing in this list matches “{q}”." : "Nothing matches the selected source.";
                (action, onAction) = ("Show everything", () =>
                {
                    Search.Text = "";
                    _sourceFilter = null;
                    RefreshSourceChoices();
                    ApplyFilter();
                });
            }
            else
            {
                switch (_mode)
                {
                    case "upgrade":
                        (kind, title, body) = ("uptodate", "All caught up",
                            "Every app winget can update is already on its latest version.");
                        (action, onAction) = ("Discover apps", () => Nav.SelectedItem = NavDiscover);
                        break;
                    case "installed":
                        (kind, glyph, title, body) = ("noinstalled", "", "No packages", "winget didn't report any installed apps.");
                        break;
                    case "import":
                        (kind, glyph, title, body) = ("norestore", "", "Nothing to install", "That list doesn't contain any apps.");
                        (action, onAction) = ("Back to Discover", GoDiscoverHome);
                        break;
                    default:
                        (kind, glyph, title, body) = ("noresults", "", $"No results for “{_discoverQuery}”",
                            "Check the spelling, or try a shorter name or the publisher.");
                        (action, onAction) = ("Back to Discover", GoDiscoverHome);
                        break;
                }
            }
        }

        _emptyAction = onAction;
        if (kind.Length == 0)
        {
            _emptyKind = "";
            EmptyState.Visibility = Visibility.Collapsed;
            return;
        }

        EmptyTitle.Text = title;
        EmptyBody.Text = body;
        EmptyAction.Content = action;
        EmptyAction.Visibility = action.Length > 0 ? Visibility.Visible : Visibility.Collapsed;
        if (kind == _emptyKind) return;   // same state, new text: no need to replay the entrance
        _emptyKind = kind;

        bool success = kind == "uptodate";
        EmptyGlyph.Glyph = glyph;
        EmptyGlyph.Visibility = success ? Visibility.Collapsed : Visibility.Visible;
        EmptyDisc.Visibility = EmptyCheck.Visibility = success ? Visibility.Visible : Visibility.Collapsed;
        EmptyState.Visibility = Visibility.Visible;

        Motion.FadeInUp(EmptyState, distance: 18, durationMs: 460);
        Motion.Pop(EmptyHalo, 0.7f);
        if (success)
        {
            Motion.Pop(EmptyDisc, 0.3f, delayMs: 120);
            DrawCheck();
        }
        else
        {
            Motion.Pop(EmptyGlyph, 0.5f, delayMs: 80);
        }
    }

    private void DrawCheck()
    {
        EmptyCheck.StrokeDashOffset = Motion.Enabled ? 9 : 0;
        if (!Motion.Enabled) return;
        var draw = new DoubleAnimation
        {
            From = 9,
            To = 0,
            BeginTime = TimeSpan.FromMilliseconds(320),
            Duration = new Duration(TimeSpan.FromMilliseconds(480)),
            EnableDependentAnimation = true,
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut },
        };
        Storyboard.SetTarget(draw, EmptyCheck);
        Storyboard.SetTargetProperty(draw, "StrokeDashOffset");
        var sb = new Storyboard();
        sb.Children.Add(draw);
        sb.Begin();
    }

    private void EmptyAction_Click(object sender, RoutedEventArgs e) => _emptyAction?.Invoke();

    // ---------- history ----------

    private int HistoryCount;

    private void RefreshHistory()
    {
        var hq = Search.Text.Trim();
        var entries = HistoryStore.All.Where(h => hq.Length == 0
            || h.Name.Contains(hq, StringComparison.OrdinalIgnoreCase)
            || h.Id.Contains(hq, StringComparison.OrdinalIgnoreCase)
            || h.Action.Contains(hq, StringComparison.OrdinalIgnoreCase)
            || h.Result.Contains(hq, StringComparison.OrdinalIgnoreCase)).ToList();
        HistoryCount = entries.Count;
        var groups = entries.GroupBy(h => h.When.ToLocalTime().Date)
            .Select(g => new HistoryGroup(g.Key, g)).ToList();
        HistList.ItemsSource = new CollectionViewSource { IsSourceGrouped = true, Source = groups }.View;

        int n = HistoryStore.All.Count;
        var month = HistoryStore.All.Where(h => h.When.ToLocalTime() > DateTime.Now.AddDays(-30)).ToList();
        Subtitle.Text = n == 0 ? "Stored on this PC only"
            : $"{n} {(n == 1 ? "entry" : "entries")}  ·  {month.Count(h => h.Result == "Succeeded")} succeeded in the last 30 days  ·  stored on this PC only";
        UpdateFooter();
    }

    // ---------- list rows ----------

    private bool _staggerArmed;

    /// <summary>The next rows to appear cascade in (used when a list loads, not while filtering).</summary>
    private async void ArmStagger()
    {
        _staggerArmed = true;
        await Task.Delay(700);
        _staggerArmed = false;
    }

    private void PkgList_ContainerContentChanging(ListViewBase sender, ContainerContentChangingEventArgs args)
    {
        if (args.InRecycleQueue) return;
        if (args.ItemContainer.ContentTemplateRoot is Panel row && row.Children.OfType<Button>().LastOrDefault() is { } quick)
            quick.Opacity = 0;   // a recycled row may still have its hover button showing
        if (_staggerArmed && args.ItemIndex < 18)
            Motion.FadeInUp(args.ItemContainer, delayMs: args.ItemIndex * 26, distance: 18, durationMs: 460);
    }

    private void Row_PointerEntered(object sender, PointerRoutedEventArgs e) => SetQuickButton(sender, 1);
    private void Row_PointerExited(object sender, PointerRoutedEventArgs e) => SetQuickButton(sender, 0);

    private static void SetQuickButton(object sender, double opacity)
    {
        if (sender is Panel row && row.Children.OfType<Button>().LastOrDefault() is { } quick) quick.Opacity = opacity;
    }

    private void QuickAction_Click(object sender, RoutedEventArgs e)
    {
        if (_running || (sender as FrameworkElement)?.DataContext is not Pkg p) return;
        PkgList.SelectedItem = p;
        _ = StartRun(new[] { p });
    }

    private void PkgList_ContextRequested(UIElement sender, ContextRequestedEventArgs args)
    {
        var source = args.OriginalSource as FrameworkElement;
        var p = source?.DataContext as Pkg
                ?? (source is ListViewItem lvi ? PkgList.ItemFromContainer(lvi) as Pkg : null)
                ?? PkgList.SelectedItem as Pkg;
        if (p is null || source is null) return;
        args.Handled = true;
        PkgList.SelectedItem = p;

        var menu = new MenuFlyout();
        BuildPkgMenu(menu.Items, p, includePrimary: true);
        if (args.TryGetPosition(source, out var point)) menu.ShowAt(source, new FlyoutShowOptions { Position = point });
        else menu.ShowAt(source);
    }

    /// <summary>Actions for one app, shared by the right-click menu and the detail pane's "…" button.</summary>
    private void BuildPkgMenu(IList<MenuFlyoutItemBase> items, Pkg p, bool includePrimary)
    {
        void Item(string text, string glyph, Action click, bool enabled = true)
        {
            var it = new MenuFlyoutItem { Text = text, Icon = new FontIcon { Glyph = glyph }, IsEnabled = enabled };
            it.Click += (_, _) => click();
            items.Add(it);
        }
        void Separator()
        {
            if (items.Count > 0 && items[^1] is not MenuFlyoutSeparator) items.Add(new MenuFlyoutSeparator());
        }

        bool canRun = !_running && p.IsManageable;
        if (includePrimary)
        {
            if (!(_mode == "import" && p.IsInstalled))
                Item($"{p.ActionLabel} now", p.ActionGlyph, () => _ = StartRun(new[] { p }), canRun);
            Item(p.IsSelected ? "Remove from selection" : "Add to selection", p.IsSelected ? "" : "",
                () => p.IsSelected = !p.IsSelected, !_running);
            Separator();
        }

        if (_mode == "upgrade")
        {
            if (p.IsSkipped)
            {
                Item("Stop skipping", "", () => Unskip(p));
            }
            else
            {
                Item(p.Available.Length > 0 ? $"Skip version {p.Available}" : "Skip this version", "", () => SkipVersion(p), p.Available.Length > 0);
                Item("Always skip this app", "", () => SkipAlways(p));
            }
        }
        if (_mode is "upgrade" or "installed" && p.IsManageable)
            Item(p.IsPinned ? "Unpin" : "Pin current version", p.IsPinned ? "" : "", () => _ = TogglePinAsync(p), !_running && !_pinBusy);
        if (_mode != "installed" && p.IsManageable && !(_mode == "import" && p.IsInstalled))
            Item(p.TargetVersion.Length > 0 ? $"Version {p.TargetVersion} (change…)" : "Choose version…", "",
                () => _ = ChooseVersionAsync(p), !_running);
        Separator();

        Item("Copy package id", "", () =>
        {
            CopyText(p.Id);
            Notify($"Copied {p.Id}", "Info");
        });
        if (p.IsManageable)
            Item("Copy winget command", "", () =>
            {
                CopyText(WingetService.Command(p, _mode));
                Notify("Command copied. Paste it into any terminal.", "Info");
            });
        if (_detailCache.TryGetValue(p.Id, out var d)
            && Uri.TryCreate(d.Homepage, UriKind.Absolute, out var home) && home.Scheme is "https" or "http")
            Item("Open homepage", "", () => _ = Launcher.LaunchUriAsync(home));
        if (items.Count > 0 && items[^1] is MenuFlyoutSeparator) items.RemoveAt(items.Count - 1);
    }

    // ---------- dialogs ----------

    private bool _dialogOpen;

    /// <summary>A dialog that follows the in-app theme (dialogs otherwise use the system one).</summary>
    private ContentDialog NewDialog() => new()
    {
        XamlRoot = Content.XamlRoot,
        RequestedTheme = ((FrameworkElement)Content).ActualTheme,
        Style = Res.Find("DefaultContentDialogStyle") as Style,
    };

    private async Task<ContentDialogResult> ShowDialogAsync(ContentDialog dlg)
    {
        if (_dialogOpen) return ContentDialogResult.None;   // only one dialog can be open at a time
        _dialogOpen = true;
        try { return await dlg.ShowAsync(); }
        finally { _dialogOpen = false; }
    }
}
