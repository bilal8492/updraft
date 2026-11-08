using System.ComponentModel;
using Microsoft.UI;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Shapes;
using Windows.Foundation;
using Windows.UI;
using Path = Microsoft.UI.Xaml.Shapes.Path;

namespace UpInstall;

/// <summary>A small rounded badge. <see cref="Tone"/> picks one of the PillXxx styles in Styles.xaml.</summary>
public sealed class Pill : Control
{
    public static readonly DependencyProperty TextProperty =
        DependencyProperty.Register(nameof(Text), typeof(string), typeof(Pill), new PropertyMetadata(""));

    public static readonly DependencyProperty ToneProperty =
        DependencyProperty.Register(nameof(Tone), typeof(string), typeof(Pill),
            new PropertyMetadata("Neutral", (d, _) => ((Pill)d).ApplyTone()));

    public string Text { get => (string)GetValue(TextProperty); set => SetValue(TextProperty, value); }

    /// <summary>Neutral, Accent, Success, Warning, Danger or Violet.</summary>
    public string Tone { get => (string)GetValue(ToneProperty); set => SetValue(ToneProperty, value); }

    private void ApplyTone()
    {
        if (Res.Find("Pill" + (string.IsNullOrEmpty(Tone) ? "Neutral" : Tone)) is Style s && !ReferenceEquals(Style, s))
            Style = s;
    }
}

/// <summary>Resource lookups from code (searches merged dictionaries too).</summary>
public static class Res
{
    public static object? Find(string key)
    {
        var app = Application.Current.Resources;
        if (app.TryGetValue(key, out var v)) return v;
        foreach (var d in app.MergedDictionaries)
            if (d.TryGetValue(key, out v)) return v;
        return null;
    }
}

/// <summary>
/// The app's tile: a monogram on a colour derived from the package id. While a package is being processed the
/// tile dims and shows a progress ring; when it finishes a status badge pops in (and the tile shakes on failure).
/// </summary>
public sealed class PkgAvatar : Grid
{
    private readonly Border _tile = new();
    private readonly TextBlock _initials = new()
    {
        Foreground = new SolidColorBrush(Colors.White),
        FontWeight = FontWeights.SemiBold,
        HorizontalAlignment = HorizontalAlignment.Center,
        VerticalAlignment = VerticalAlignment.Center,
        TextLineBounds = TextLineBounds.Tight,
        IsTextScaleFactorEnabled = false,
    };
    private readonly Border _overlay = new() { Background = new SolidColorBrush(Color.FromArgb(150, 10, 14, 24)), Visibility = Visibility.Collapsed };
    private readonly ProgressRing _ring = new()
    {
        Foreground = new SolidColorBrush(Colors.White),
        Background = new SolidColorBrush(Color.FromArgb(60, 255, 255, 255)),
        IsActive = false,
        Maximum = 100,
    };
    private readonly FontIcon _queued = new() { Glyph = "", Foreground = new SolidColorBrush(Colors.White) };
    private readonly Grid _badge = new() { HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Bottom, Visibility = Visibility.Collapsed };
    private readonly Ellipse _badgeDot = new();
    private readonly FontIcon _badgeIcon = new() { Foreground = new SolidColorBrush(Colors.White), FontWeight = FontWeights.Bold };

    private Pkg? _pkg;
    private bool _loaded;

    public PkgAvatar()
    {
        _tile.Child = _initials;
        var overlayContent = new Grid();
        overlayContent.Children.Add(_ring);
        overlayContent.Children.Add(_queued);
        _overlay.Child = overlayContent;
        _badge.Children.Add(_badgeDot);
        _badge.Children.Add(_badgeIcon);
        Children.Add(_tile);
        Children.Add(_overlay);
        Children.Add(_badge);
        ApplySize();

        Loaded += (_, _) => { _loaded = true; Subscribe(); Render(animate: false); };
        Unloaded += (_, _) => { _loaded = false; Unsubscribe(); };
    }

    public static readonly DependencyProperty PkgProperty =
        DependencyProperty.Register(nameof(Pkg), typeof(object), typeof(PkgAvatar),
            new PropertyMetadata(null, (d, _) => ((PkgAvatar)d).OnPkgChanged()));

    public static readonly DependencyProperty TileSizeProperty =
        DependencyProperty.Register(nameof(TileSize), typeof(double), typeof(PkgAvatar),
            new PropertyMetadata(36.0, (d, _) => ((PkgAvatar)d).ApplySize()));

    public object? Pkg { get => GetValue(PkgProperty); set => SetValue(PkgProperty, value); }
    public double TileSize { get => (double)GetValue(TileSizeProperty); set => SetValue(TileSizeProperty, value); }

    private void ApplySize()
    {
        double s = TileSize;
        Width = Height = s + 4;
        var radius = new CornerRadius(Math.Round(s * 0.28));
        foreach (var b in new[] { _tile, _overlay })
        {
            b.Width = b.Height = s;
            b.CornerRadius = radius;
            b.HorizontalAlignment = HorizontalAlignment.Left;
            b.VerticalAlignment = VerticalAlignment.Top;
        }
        _initials.FontSize = Math.Round(s * 0.36);
        _ring.Width = _ring.Height = Math.Round(s * 0.62);
        _queued.FontSize = Math.Round(s * 0.38);
        double badge = Math.Max(16, Math.Round(s * 0.42));
        _badge.Width = _badge.Height = badge;
        _badgeIcon.FontSize = Math.Round(badge * 0.55);
    }

    private void OnPkgChanged()
    {
        Unsubscribe();
        _pkg = Pkg as Pkg;
        if (_loaded) Subscribe();
        Render(animate: false);
    }

    private void Subscribe() { if (_pkg is not null) { _pkg.PropertyChanged -= OnPkgPropertyChanged; _pkg.PropertyChanged += OnPkgPropertyChanged; } }
    private void Unsubscribe() { if (_pkg is not null) _pkg.PropertyChanged -= OnPkgPropertyChanged; }

    private void OnPkgPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(UpInstall.Pkg.State): Render(animate: true); break;
            case nameof(UpInstall.Pkg.Progress):
            case nameof(UpInstall.Pkg.Indeterminate): UpdateRing(); break;
        }
    }

    private void UpdateRing()
    {
        if (_pkg is null) return;
        _ring.IsIndeterminate = _pkg.Indeterminate;
        if (!_pkg.Indeterminate) _ring.Value = _pkg.Progress;
    }

    private static readonly SolidColorBrush Green = new(Color.FromArgb(255, 22, 163, 74));
    private static readonly SolidColorBrush Red = new(Color.FromArgb(255, 220, 38, 38));
    private static readonly SolidColorBrush Amber = new(Color.FromArgb(255, 217, 119, 6));

    private void Render(bool animate)
    {
        var p = _pkg;
        _tile.Background = p?.AvatarBrush;
        _initials.Text = p?.Initials ?? "";
        var state = p?.State ?? RunState.None;

        bool busy = state is RunState.Running or RunState.Queued;
        _overlay.Visibility = busy ? Visibility.Visible : Visibility.Collapsed;
        _overlay.Opacity = state == RunState.Queued ? 0.75 : 1;
        _ring.Visibility = state == RunState.Running ? Visibility.Visible : Visibility.Collapsed;
        _ring.IsActive = state == RunState.Running;
        _queued.Visibility = state == RunState.Queued ? Visibility.Visible : Visibility.Collapsed;
        UpdateRing();

        (Brush? fill, string glyph) = state switch
        {
            RunState.Succeeded => (Green, ""),
            RunState.Failed => (Red, ""),
            RunState.Cancelled => (Amber, ""),
            _ => ((Brush?)null, ""),
        };
        _badge.Visibility = fill is null ? Visibility.Collapsed : Visibility.Visible;
        _badgeDot.Fill = fill;
        _badgeIcon.Glyph = glyph;

        if (!animate) return;
        if (state == RunState.Running) Motion.FadeInUp(_overlay, distance: 0, durationMs: 220);
        if (fill is not null)
        {
            Motion.Pop(_badge, 0.2f);
            if (state == RunState.Failed) Motion.Shake(_tile);
            else if (state == RunState.Succeeded) Motion.Bump(_tile, 1.08f);
        }
    }
}

/// <summary>
/// The logo's three chevrons, rising in a slow wave: the app's signature motion. Used while scanning and as a
/// decoration on the hero card. Static when animations are off.
/// </summary>
public sealed class RisingChevrons : Grid
{
    private readonly StackPanel _stack = new() { HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
    private readonly List<(Path Path, TranslateTransform Move, double BaseOpacity)> _chevrons = new();
    private Storyboard? _story;
    private bool _loaded;

    public RisingChevrons()
    {
        IsHitTestVisible = false;
        Children.Add(_stack);
        Build();
        Loaded += (_, _) => { _loaded = true; UpdateAnimation(); };
        Unloaded += (_, _) => { _loaded = false; _story?.Stop(); };
    }

    public static readonly DependencyProperty ChevronBrushProperty =
        DependencyProperty.Register(nameof(ChevronBrush), typeof(Brush), typeof(RisingChevrons),
            new PropertyMetadata(null, (d, _) => ((RisingChevrons)d).Build()));

    public static readonly DependencyProperty ChevronWidthProperty =
        DependencyProperty.Register(nameof(ChevronWidth), typeof(double), typeof(RisingChevrons),
            new PropertyMetadata(28.0, (d, _) => ((RisingChevrons)d).Build()));

    public static readonly DependencyProperty IsActiveProperty =
        DependencyProperty.Register(nameof(IsActive), typeof(bool), typeof(RisingChevrons),
            new PropertyMetadata(true, (d, _) => ((RisingChevrons)d).UpdateAnimation()));

    /// <summary>Seconds per wave. Lower is livelier.</summary>
    public static readonly DependencyProperty PeriodProperty =
        DependencyProperty.Register(nameof(Period), typeof(double), typeof(RisingChevrons),
            new PropertyMetadata(1.8, (d, _) => ((RisingChevrons)d).Build()));

    public Brush? ChevronBrush { get => (Brush?)GetValue(ChevronBrushProperty); set => SetValue(ChevronBrushProperty, value); }
    public double ChevronWidth { get => (double)GetValue(ChevronWidthProperty); set => SetValue(ChevronWidthProperty, value); }
    public bool IsActive { get => (bool)GetValue(IsActiveProperty); set => SetValue(IsActiveProperty, value); }
    public double Period { get => (double)GetValue(PeriodProperty); set => SetValue(PeriodProperty, value); }

    private void Build()
    {
        _story?.Stop();
        _story = null;
        _stack.Children.Clear();
        _chevrons.Clear();

        double w = ChevronWidth, h = w * 0.42, t = Math.Max(2, w * 0.16);
        _stack.Spacing = w * 0.06;
        _stack.Margin = new Thickness(0, h * 0.5, 0, 0);
        double[] opacities = { 0.45, 0.72, 1.0 };   // top → bottom, like the icon
        for (int i = 0; i < 3; i++)
        {
            var geometry = new PathGeometry();
            var figure = new PathFigure { StartPoint = new Point(t / 2, h - t / 2), IsClosed = false, IsFilled = false };
            figure.Segments.Add(new LineSegment { Point = new Point(w / 2, t / 2) });
            figure.Segments.Add(new LineSegment { Point = new Point(w - t / 2, h - t / 2) });
            geometry.Figures.Add(figure);
            var move = new TranslateTransform();
            var path = new Path
            {
                Data = geometry,
                Width = w,
                Height = h,
                Stroke = ChevronBrush ?? new SolidColorBrush(Colors.White),
                StrokeThickness = t,
                StrokeStartLineCap = PenLineCap.Round,
                StrokeEndLineCap = PenLineCap.Round,
                StrokeLineJoin = PenLineJoin.Round,
                Opacity = opacities[i],
                RenderTransform = move,
            };
            _stack.Children.Add(path);
            _chevrons.Add((path, move, opacities[i]));
        }
        UpdateAnimation();
    }

    /// <summary>Re-checks whether to animate (after the Animations setting changes).</summary>
    public void RefreshMotion() => UpdateAnimation();

    private void UpdateAnimation()
    {
        if (!_loaded) return;
        if (!IsActive || !Motion.Enabled)
        {
            _story?.Stop();
            return;
        }
        if (_story is null)
        {
            _story = new Storyboard();
            double rise = ChevronWidth * 0.2;
            for (int i = 0; i < _chevrons.Count; i++)
            {
                var (path, move, baseOpacity) = _chevrons[i];
                // Each chevron loops on its own; the bottom one starts first so the wave travels upwards.
                var delay = TimeSpan.FromSeconds((_chevrons.Count - 1 - i) * 0.16 * Period / 1.8);
                _story.Children.Add(Wave(move, "Y", 0, -rise, delay));
                _story.Children.Add(Wave(path, "Opacity", baseOpacity, 1.0, delay));
            }
        }
        _story.Begin();
    }

    // Rest → rise to the peak → fall back → rest, repeated forever.
    private DoubleAnimationUsingKeyFrames Wave(DependencyObject target, string property, double rest, double peak, TimeSpan delay)
    {
        var total = TimeSpan.FromSeconds(Period);
        var a = new DoubleAnimationUsingKeyFrames { Duration = new Duration(total), BeginTime = delay, RepeatBehavior = RepeatBehavior.Forever };
        a.KeyFrames.Add(new DiscreteDoubleKeyFrame { KeyTime = KeyTime.FromTimeSpan(TimeSpan.Zero), Value = rest });
        a.KeyFrames.Add(new EasingDoubleKeyFrame { KeyTime = KeyTime.FromTimeSpan(total * 0.32), Value = peak, EasingFunction = new SineEase { EasingMode = EasingMode.EaseOut } });
        a.KeyFrames.Add(new EasingDoubleKeyFrame { KeyTime = KeyTime.FromTimeSpan(total * 0.75), Value = rest, EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut } });
        a.KeyFrames.Add(new DiscreteDoubleKeyFrame { KeyTime = KeyTime.FromTimeSpan(total), Value = rest });
        Storyboard.SetTarget(a, target);
        Storyboard.SetTargetProperty(a, property);
        return a;
    }
}

/// <summary>Lays children out left to right and wraps onto new lines (used for tags and quick-search chips).</summary>
public sealed class WrapPanel : Panel
{
    public double HorizontalSpacing { get; set; } = 6;
    public double VerticalSpacing { get; set; } = 6;

    protected override Size MeasureOverride(Size availableSize)
    {
        double x = 0, y = 0, lineHeight = 0, width = 0;
        foreach (var child in Children)
        {
            child.Measure(new Size(availableSize.Width, double.PositiveInfinity));
            var d = child.DesiredSize;
            if (x > 0 && x + d.Width > availableSize.Width)
            {
                y += lineHeight + VerticalSpacing;
                x = 0;
                lineHeight = 0;
            }
            x += d.Width + HorizontalSpacing;
            lineHeight = Math.Max(lineHeight, d.Height);
            width = Math.Max(width, x - HorizontalSpacing);
        }
        return new Size(double.IsInfinity(availableSize.Width) ? width : Math.Min(width, availableSize.Width), y + lineHeight);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        double x = 0, y = 0, lineHeight = 0;
        foreach (var child in Children)
        {
            var d = child.DesiredSize;
            if (x > 0 && x + d.Width > finalSize.Width)
            {
                y += lineHeight + VerticalSpacing;
                x = 0;
                lineHeight = 0;
            }
            child.Arrange(new Rect(x, y, d.Width, d.Height));
            x += d.Width + HorizontalSpacing;
            lineHeight = Math.Max(lineHeight, d.Height);
        }
        return finalSize;
    }
}
