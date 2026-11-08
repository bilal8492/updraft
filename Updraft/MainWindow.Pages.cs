using System.Diagnostics;
using System.Reflection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace Updraft;

/// <summary>One skipped app or version, as listed in Settings.</summary>
public sealed class SkipEntry
{
    public SkipEntry(string id, string label)
    {
        Id = id;
        Label = label;
    }

    public string Id { get; }
    public string Label { get; }
}

// Settings page, keyboard shortcuts, the version picker and Discover's start page.
public sealed partial class MainWindow
{
    // ---------- settings ----------

    private static readonly string[] ModeValues = { "default", "silent", "interactive" };
    private static readonly string[] TermValues = { "auto", "wt", "powershell" };
    private bool _loadingSettings = true;
    private string? _wingetVersion;

    private void LoadSettingsUi()
    {
        _loadingSettings = true;
        var s = AppSettings.Current;
        ModeBox.SelectedIndex = Math.Max(0, Array.IndexOf(ModeValues, s.InstallMode));
        TermBox.SelectedIndex = Math.Max(0, Array.IndexOf(TermValues, s.Terminal));
        AgreeSwitch.IsOn = s.AcceptAgreements;
        AdminSwitch.IsOn = s.DefaultAdmin;
        AnimSwitch.IsOn = s.Animations;
        AnimHint.Text = new Windows.UI.ViewManagement.UISettings().AnimationsEnabled
            ? "Motion when lists load, apps install and panels open."
            : "Windows' Animation effects setting is off, so Updraft stays still either way.";
        UpdateThemeCards();
        RefreshSkippedList();
        _loadingSettings = false;

        var version = typeof(App).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "";
        AboutVersion.Text = $"Version {version.Split('+')[0]}{(WingetService.Demo ? "  ·  demo mode" : "")}";
        _ = LoadWingetVersionAsync();
    }

    private async Task LoadWingetVersionAsync()
    {
        if (_wingetVersion is null)
        {
            AboutWinget.Text = "Checking winget…";
            try { _wingetVersion = (await Task.Run(() => WingetService.RunAsync("--version"))).Trim(); }
            catch { _wingetVersion = ""; }
        }
        AboutWinget.Text = _wingetVersion.Length > 0 ? $"Windows Package Manager {_wingetVersion}" : "winget wasn't found on this PC";
    }

    // Settings apply as soon as they change, like Windows Settings: there is no Save button.
    private void SettingChanged(object sender, RoutedEventArgs e)
    {
        if (_loadingSettings) return;
        var next = AppSettings.Current.Clone();   // keeps everything this page doesn't edit (skips, window placement)
        next.InstallMode = ModeValues[Math.Max(0, ModeBox.SelectedIndex)];
        next.Terminal = TermValues[Math.Max(0, TermBox.SelectedIndex)];
        next.AcceptAgreements = AgreeSwitch.IsOn;
        next.DefaultAdmin = AdminSwitch.IsOn;
        bool animChanged = next.Animations != AnimSwitch.IsOn;
        next.Animations = AnimSwitch.IsOn;
        next.Save();

        AdminChk.IsChecked = AppSettings.Current.DefaultAdmin;
        if (animChanged)
        {
            Motion.Refresh();
            HeroChevrons.RefreshMotion();
            DEmptyChevrons.RefreshMotion();
        }
    }

    private void ThemeCard_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement { Tag: string theme }) return;
        var next = AppSettings.Current.Clone();
        next.Theme = theme;
        next.Save();
        ApplyTheme();
        UpdateThemeCards();
        if (sender is FrameworkElement card) Motion.Bump(card, 1.04f);
    }

    private void UpdateThemeCards()
    {
        var t = AppSettings.Current.Theme;
        ThemeSystemRing.Opacity = t == "system" ? 1 : 0;
        ThemeLightRing.Opacity = t == "light" ? 1 : 0;
        ThemeDarkRing.Opacity = t == "dark" ? 1 : 0;
    }

    private void RefreshSkippedList()
    {
        var s = AppSettings.Current;
        var entries = s.Skipped.Select(id => new SkipEntry(id, "All updates are skipped"))
            .Concat(s.SkippedVersions.Select(kv => new SkipEntry(kv.Key, $"Version {kv.Value} is skipped")))
            .OrderBy(x => x.Id, StringComparer.OrdinalIgnoreCase).ToList();
        SkippedList.ItemsSource = entries;
        SkippedEmpty.Visibility = entries.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        SkippedHeader.Text = entries.Count > 0 ? $"Skipped updates ({entries.Count})" : "Skipped updates";
    }

    private void Unskip_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement { Tag: string id }) return;
        AppSettings.Current.SetSkipped(id, false);
        RefreshSkippedList();
        Notify($"{id} will show up in Updates again");
    }

    private void OpenDataFolder_Click(object sender, RoutedEventArgs e)
    {
        var dir = AppSettings.DataDir;
        try
        {
            Directory.CreateDirectory(dir);
            Process.Start(new ProcessStartInfo("explorer.exe", $"\"{dir}\"") { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            Notify("Could not open the folder: " + ex.Message, "Error");
        }
    }

    // ---------- keyboard shortcuts ----------

    private void Shortcuts_Click(object sender, RoutedEventArgs e) => _ = ShowShortcutsAsync();

    private async Task ShowShortcutsAsync()
    {
        (string[] Keys, string What)[] rows =
        {
            (new[] { "Ctrl", "1 – 4" }, "Updates, Installed, Discover, History"),
            (new[] { "Ctrl", "F" }, "Search, or filter the list"),
            (new[] { "Enter" }, "Search winget for what you typed"),
            (new[] { "F5" }, "Scan again"),
            (new[] { "Ctrl", "A" }, "Select all, or none"),
            (new[] { "Ctrl", "Enter" }, "Install, update or uninstall the selected apps"),
            (new[] { "Esc" }, "Cancel a run, or dismiss a message"),
            (new[] { "Shift", "F10" }, "Actions for the highlighted app"),
            (new[] { "Ctrl", "," }, "Settings"),
            (new[] { "F1" }, "This list"),
        };

        var grid = new Grid { RowSpacing = 12, ColumnSpacing = 24, MinWidth = 420 };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(150) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        var keycap = Res.Find("KeycapStyle") as Style;
        for (int i = 0; i < rows.Length; i++)
        {
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            var keys = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 5, VerticalAlignment = VerticalAlignment.Center };
            foreach (var k in rows[i].Keys)
                keys.Children.Add(new Border
                {
                    Style = keycap,
                    Child = new TextBlock
                    {
                        Text = k,
                        FontSize = 12,
                        FontFamily = Res.Find("MonoFontFamily") as FontFamily,
                        HorizontalAlignment = HorizontalAlignment.Center,
                    },
                });
            var what = new TextBlock { Text = rows[i].What, VerticalAlignment = VerticalAlignment.Center, TextWrapping = TextWrapping.Wrap };
            Grid.SetRow(keys, i);
            Grid.SetRow(what, i);
            Grid.SetColumn(what, 1);
            grid.Children.Add(keys);
            grid.Children.Add(what);
        }

        var dlg = NewDialog();
        dlg.Title = "Keyboard shortcuts";
        dlg.Content = grid;
        dlg.CloseButtonText = "Close";
        dlg.DefaultButton = ContentDialogButton.Close;
        await ShowDialogAsync(dlg);
    }

    // ---------- version picker ----------

    /// <summary>Lets the user install or update to a specific version instead of the newest one.</summary>
    private async Task ChooseVersionAsync(Pkg p)
    {
        if (_running || !p.IsManageable) return;
        var list = new ListView { SelectionMode = ListViewSelectionMode.Single, MaxHeight = 300, MinWidth = 320 };
        var status = new TextBlock { Text = "Loading versions from winget…", Style = Res.Find("SecondaryTextStyle") as Style };
        var ring = new ProgressRing { IsActive = true, Width = 20, Height = 20, HorizontalAlignment = HorizontalAlignment.Left };
        var panel = new StackPanel { Spacing = 10 };
        panel.Children.Add(new TextBlock
        {
            Text = _mode == "upgrade"
                ? $"Installed: {p.Version}. Pick the version to update to; winget won't go back to an older one."
                : "Pick the version to install. Leave it on the latest unless you need an older release.",
            TextWrapping = TextWrapping.Wrap,
        });
        panel.Children.Add(ring);
        panel.Children.Add(status);
        panel.Children.Add(list);

        var dlg = NewDialog();
        dlg.Title = $"Choose a version of {p.Name}";
        dlg.Content = panel;
        dlg.PrimaryButtonText = "Use this version";
        dlg.SecondaryButtonText = "Use latest";
        dlg.CloseButtonText = "Cancel";
        dlg.DefaultButton = ContentDialogButton.Primary;
        dlg.IsPrimaryButtonEnabled = false;
        list.SelectionChanged += (_, _) => dlg.IsPrimaryButtonEnabled = list.SelectedItem is string;

        var load = Task.Run(() => WingetService.VersionsAsync(p.Id));
        _ = load.ContinueWith(t =>
        {
            DispatcherQueue.TryEnqueue(() =>
            {
                ring.IsActive = false;
                ring.Visibility = Visibility.Collapsed;
                var versions = t.IsCompletedSuccessfully ? t.Result : new List<string>();
                if (_mode == "upgrade")
                    versions = versions.Where(v => Versions.Compare(v, p.Version) is null or > 0).ToList();
                list.ItemsSource = versions;
                if (p.TargetVersion.Length > 0) list.SelectedItem = versions.FirstOrDefault(v => v == p.TargetVersion);
                status.Text = versions.Count == 0 ? "winget didn't list any other versions for this app."
                    : $"{versions.Count} {(versions.Count == 1 ? "version" : "versions")}, newest first";
            });
        }, TaskScheduler.Default);

        var result = await ShowDialogAsync(dlg);
        if (result == ContentDialogResult.Primary && list.SelectedItem is string chosen && Versions.IsSafe(chosen))
        {
            bool latest = list.Items.Count > 0 && Equals(list.Items[0], chosen);
            p.TargetVersion = latest ? "" : chosen;
            Notify(latest ? $"{p.Name} will use the latest version" : $"{p.Name} will be installed at version {chosen}", "Info");
        }
        else if (result == ContentDialogResult.Secondary)
        {
            p.TargetVersion = "";
            Notify($"{p.Name} will use the latest version", "Info");
        }
        else
        {
            return;
        }
        if (ReferenceEquals(_shownPkg, p)) ShowDetail(p);
    }

    // ---------- Discover start page ----------

    private bool _animatePacks;

    private void BuildPopularChips()
    {
        var style = Res.Find("ChipButtonStyle") as Style;
        foreach (var term in Catalog.PopularSearches)
        {
            var content = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 7 };
            content.Children.Add(new FontIcon { Glyph = "", FontSize = 11, Opacity = 0.7 });
            content.Children.Add(new TextBlock { Text = term });
            var chip = new Button { Style = style, Content = content };
            chip.Click += (_, _) =>
            {
                if (_running) return;
                Search.Text = term;
                _discoverQuery = term;
                ApplyModeLayout();
                PlayPageTransition();
                _ = LoadAsync();
            };
            Motion.AttachPressScale(chip, 0.94f);
            PopularPanel.Children.Add(chip);
        }
    }

    private async void ShowDiscoverHome()
    {
        _animatePacks = true;
        for (int i = 0; i < PopularPanel.Children.Count; i++)
            Motion.FadeInUp(PopularPanel.Children[i], delayMs: 40 + i * 22, distance: 10, durationMs: 380);
        await Task.Delay(900);
        _animatePacks = false;
    }

    private void PackGrid_ContainerContentChanging(ListViewBase sender, ContainerContentChangingEventArgs args)
    {
        if (!args.InRecycleQueue && _animatePacks)
            Motion.FadeInUp(args.ItemContainer, delayMs: 140 + args.ItemIndex * 55, distance: 22, durationMs: 520);
    }

    private void PackCard_Loaded(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement card || card.Tag is "motion") return;
        card.Tag = "motion";
        Motion.AttachHoverLift(card, -4);
    }

    private void PackGrid_ItemClick(object sender, ItemClickEventArgs e)
    {
        if (_running || e.ClickedItem is not StarterPack pack) return;
        var pkgs = pack.Apps.Select(a => new Pkg { Name = a.Name, Id = a.Id, Source = "winget" }).ToList();
        OpenImportList(pkgs, pack.Title, pack.Title, isPack: true);
    }

    private void AppTitleBar_BackRequested(TitleBar sender, object args) => GoDiscoverHome();

    private void GoDiscoverHome()
    {
        if (_running) return;
        Search.Text = "";
        _discoverQuery = "";
        _mode = "discover";
        if (!ReferenceEquals(Nav.SelectedItem, NavDiscover))
        {
            Nav.SelectedItem = NavDiscover;
            return;
        }
        ApplyModeLayout();
        PlayPageTransition();
        _ = LoadAsync();
    }
}
