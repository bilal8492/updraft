using System.Numerics;
using Microsoft.UI.Composition;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using Windows.UI;

namespace Updraft;

/// <summary>
/// Small, reusable motion effects. They run on the composition layer, so they stay smooth while the UI thread is
/// busy parsing winget output. Every effect snaps straight to its end state when animations are turned off.
/// </summary>
public static class Motion
{
    private static readonly bool SystemAllows = ReadSystemSetting();

    private static bool ReadSystemSetting()
    {
        try { return new Windows.UI.ViewManagement.UISettings().AnimationsEnabled; }
        catch { return true; }
    }

    /// <summary>False when Windows "Animation effects" is off, or the user switched animations off in Updraft.</summary>
    public static bool Enabled => SystemAllows && AppSettings.Current.Animations;

    private static Visual VisualOf(UIElement e) => ElementCompositionPreview.GetElementVisual(e);

    private static CompositionEasingFunction EaseOut(Compositor c) =>
        c.CreateCubicBezierEasingFunction(new Vector2(0.16f, 1f), new Vector2(0.3f, 1f));

    private static void Center(FrameworkElement e, Visual v) =>
        v.CenterPoint = new Vector3((float)e.ActualWidth / 2, (float)e.ActualHeight / 2, 0);

    /// <summary>Runs <paramref name="action"/> now if the element has a size, otherwise right after its first layout.</summary>
    private static void WhenSized(FrameworkElement e, Action action)
    {
        if (e.ActualWidth > 0 && e.ActualHeight > 0) { action(); return; }
        void Once(object s, SizeChangedEventArgs a)
        {
            e.SizeChanged -= Once;
            action();
        }
        e.SizeChanged += Once;
    }

    private static void Delay(KeyFrameAnimation a, int delayMs)
    {
        if (delayMs <= 0) return;
        a.DelayTime = TimeSpan.FromMilliseconds(delayMs);
        a.DelayBehavior = AnimationDelayBehavior.SetInitialValueBeforeDelay;
    }

    // ---------- entrances ----------

    /// <summary>Slides an element up into place while fading it in.</summary>
    public static void FadeInUp(UIElement e, int delayMs = 0, float distance = 14, int durationMs = 420)
    {
        var v = VisualOf(e);
        ElementCompositionPreview.SetIsTranslationEnabled(e, true);
        if (!Enabled)
        {
            v.StopAnimation("Opacity");
            v.StopAnimation("Translation");
            v.Opacity = 1;
            v.Properties.InsertVector3("Translation", Vector3.Zero);
            return;
        }
        var c = v.Compositor;
        var ease = EaseOut(c);

        var fade = c.CreateScalarKeyFrameAnimation();
        fade.InsertKeyFrame(0, 0);
        fade.InsertKeyFrame(1, 1, ease);
        fade.Duration = TimeSpan.FromMilliseconds(durationMs * 0.75);
        Delay(fade, delayMs);

        var slide = c.CreateVector3KeyFrameAnimation();
        slide.InsertKeyFrame(0, new Vector3(0, distance, 0));
        slide.InsertKeyFrame(1, Vector3.Zero, ease);
        slide.Duration = TimeSpan.FromMilliseconds(durationMs);
        Delay(slide, delayMs);

        v.StartAnimation("Opacity", fade);
        v.StartAnimation("Translation", slide);
    }

    /// <summary>Same as <see cref="FadeInUp"/> but sideways: used when the detail pane switches to another app.</summary>
    public static void FadeInFromRight(UIElement e, float distance = 18, int durationMs = 380)
    {
        var v = VisualOf(e);
        ElementCompositionPreview.SetIsTranslationEnabled(e, true);
        if (!Enabled) { v.Opacity = 1; v.Properties.InsertVector3("Translation", Vector3.Zero); return; }
        var c = v.Compositor;
        var ease = EaseOut(c);
        var fade = c.CreateScalarKeyFrameAnimation();
        fade.InsertKeyFrame(0, 0);
        fade.InsertKeyFrame(1, 1, ease);
        fade.Duration = TimeSpan.FromMilliseconds(durationMs * 0.7);
        var slide = c.CreateVector3KeyFrameAnimation();
        slide.InsertKeyFrame(0, new Vector3(distance, 0, 0));
        slide.InsertKeyFrame(1, Vector3.Zero, ease);
        slide.Duration = TimeSpan.FromMilliseconds(durationMs);
        v.StartAnimation("Opacity", fade);
        v.StartAnimation("Translation", slide);
    }

    /// <summary>Springy scale-in, for badges and icons that should feel like they "land".</summary>
    public static void Pop(FrameworkElement e, float from = 0.4f, int delayMs = 0) => WhenSized(e, () =>
    {
        var v = VisualOf(e);
        Center(e, v);
        if (!Enabled) { v.StopAnimation("Scale"); v.Scale = Vector3.One; return; }
        var c = v.Compositor;
        var s = c.CreateSpringVector3Animation();
        s.InitialValue = new Vector3(from, from, 1);
        s.FinalValue = Vector3.One;
        s.DampingRatio = 0.48f;
        s.Period = TimeSpan.FromMilliseconds(48);
        if (delayMs > 0)
        {
            s.DelayTime = TimeSpan.FromMilliseconds(delayMs);
            s.DelayBehavior = AnimationDelayBehavior.SetInitialValueBeforeDelay;
        }
        v.StartAnimation("Scale", s);
    });

    /// <summary>A quick swell and settle: draws the eye to a number or icon that just changed.</summary>
    public static void Bump(FrameworkElement e, float peak = 1.16f) => WhenSized(e, () =>
    {
        if (!Enabled) return;
        var v = VisualOf(e);
        Center(e, v);
        var c = v.Compositor;
        var a = c.CreateVector3KeyFrameAnimation();
        a.InsertKeyFrame(0, Vector3.One);
        a.InsertKeyFrame(0.35f, new Vector3(peak, peak, 1), EaseOut(c));
        a.InsertKeyFrame(1, Vector3.One, c.CreateCubicBezierEasingFunction(new Vector2(0.3f, 0f), new Vector2(0.2f, 1f)));
        a.Duration = TimeSpan.FromMilliseconds(320);
        v.StartAnimation("Scale", a);
    });

    /// <summary>A short horizontal shake, for something that failed.</summary>
    public static void Shake(UIElement e)
    {
        if (!Enabled) return;
        var v = VisualOf(e);
        ElementCompositionPreview.SetIsTranslationEnabled(e, true);
        var c = v.Compositor;
        var a = c.CreateVector3KeyFrameAnimation();
        float[] xs = { 0, -7, 6, -5, 4, -2, 0 };
        for (int i = 0; i < xs.Length; i++) a.InsertKeyFrame(i / (float)(xs.Length - 1), new Vector3(xs[i], 0, 0));
        a.Duration = TimeSpan.FromMilliseconds(460);
        v.StartAnimation("Translation", a);
    }

    // ---------- rotation ----------

    /// <summary>One full turn, easing out. Used when a refresh is triggered.</summary>
    public static void SpinOnce(FrameworkElement e, int durationMs = 650) => WhenSized(e, () =>
    {
        if (!Enabled) return;
        var v = VisualOf(e);
        Center(e, v);
        var c = v.Compositor;
        var a = c.CreateScalarKeyFrameAnimation();
        a.InsertKeyFrame(0, 0);
        a.InsertKeyFrame(1, 360, EaseOut(c));
        a.Duration = TimeSpan.FromMilliseconds(durationMs);
        v.StartAnimation("RotationAngleInDegrees", a);
    });

    /// <summary>Continuous rotation while something loads; stopping eases the element back upright.</summary>
    public static void SetSpinning(FrameworkElement e, bool spinning) => WhenSized(e, () =>
    {
        var v = VisualOf(e);
        Center(e, v);
        var c = v.Compositor;
        v.StopAnimation("RotationAngleInDegrees");
        if (spinning && Enabled)
        {
            var a = c.CreateScalarKeyFrameAnimation();
            a.InsertKeyFrame(0, 0);
            a.InsertKeyFrame(1, 360, c.CreateLinearEasingFunction());
            a.Duration = TimeSpan.FromMilliseconds(900);
            a.IterationBehavior = AnimationIterationBehavior.Forever;
            v.StartAnimation("RotationAngleInDegrees", a);
            return;
        }
        float current = v.RotationAngleInDegrees % 360;
        if (!Enabled || current < 1) { v.RotationAngleInDegrees = 0; return; }
        var settle = c.CreateScalarKeyFrameAnimation();
        settle.InsertKeyFrame(0, current);
        settle.InsertKeyFrame(1, 360, EaseOut(c));
        settle.Duration = TimeSpan.FromMilliseconds(Math.Max(180, (360 - current) / 360 * 600));
        v.StartAnimation("RotationAngleInDegrees", settle);
    });

    // ---------- pointer feedback ----------

    /// <summary>Presses shrink the element slightly and release springs it back, like a physical button.</summary>
    public static void AttachPressScale(FrameworkElement e, float pressed = 0.96f)
    {
        e.AddHandler(UIElement.PointerPressedEvent, new PointerEventHandler((_, _) => ScaleTo(e, pressed, springy: false)), true);
        var release = new PointerEventHandler((_, _) => ScaleTo(e, 1f, springy: true));
        e.AddHandler(UIElement.PointerReleasedEvent, release, true);
        e.AddHandler(UIElement.PointerCanceledEvent, release, true);
        e.AddHandler(UIElement.PointerCaptureLostEvent, release, true);
        e.AddHandler(UIElement.PointerExitedEvent, release, true);
    }

    private static void ScaleTo(FrameworkElement e, float scale, bool springy)
    {
        var v = VisualOf(e);
        Center(e, v);
        if (!Enabled) { v.StopAnimation("Scale"); v.Scale = Vector3.One; return; }
        var s = v.Compositor.CreateSpringVector3Animation();
        s.FinalValue = new Vector3(scale, scale, 1);
        s.DampingRatio = springy ? 0.42f : 0.9f;
        s.Period = TimeSpan.FromMilliseconds(springy ? 50 : 28);
        v.StartAnimation("Scale", s);
    }

    /// <summary>Hovering lifts the element a few pixels; leaving lets it settle back.</summary>
    public static void AttachHoverLift(FrameworkElement e, float lift = -4)
    {
        ElementCompositionPreview.SetIsTranslationEnabled(e, true);
        e.PointerEntered += (_, _) => TranslateTo(e, new Vector3(0, lift, 0));
        e.PointerExited += (_, _) => TranslateTo(e, Vector3.Zero);
        e.PointerCanceled += (_, _) => TranslateTo(e, Vector3.Zero);
    }

    private static void TranslateTo(UIElement e, Vector3 to)
    {
        var v = VisualOf(e);
        if (!Enabled) { v.StopAnimation("Translation"); v.Properties.InsertVector3("Translation", Vector3.Zero); return; }
        var s = v.Compositor.CreateSpringVector3Animation();
        s.FinalValue = to;
        s.DampingRatio = 0.62f;
        s.Period = TimeSpan.FromMilliseconds(42);
        v.StartAnimation("Translation", s);
    }

    // ---------- show / hide ----------

    private static readonly List<WeakReference<UIElement>> ShowHideElements = new();

    /// <summary>
    /// Makes Visibility changes animate: showing fades and slides the element in, collapsing fades it out
    /// (XAML keeps it on screen until the hide animation has finished).
    /// </summary>
    public static void UseShowHide(UIElement e, float distance = 10)
    {
        ShowHideElements.Add(new WeakReference<UIElement>(e));
        ApplyShowHide(e, distance);
    }

    private static void ApplyShowHide(UIElement e, float distance = 10)
    {
        if (!Enabled)
        {
            ElementCompositionPreview.SetImplicitShowAnimation(e, null);
            ElementCompositionPreview.SetImplicitHideAnimation(e, null);
            return;
        }
        ElementCompositionPreview.SetIsTranslationEnabled(e, true);
        var c = VisualOf(e).Compositor;
        var ease = EaseOut(c);

        var showFade = c.CreateScalarKeyFrameAnimation();
        showFade.Target = "Opacity";
        showFade.InsertKeyFrame(0, 0);
        showFade.InsertKeyFrame(1, 1, ease);
        showFade.Duration = TimeSpan.FromMilliseconds(260);
        var showSlide = c.CreateVector3KeyFrameAnimation();
        showSlide.Target = "Translation";
        showSlide.InsertKeyFrame(0, new Vector3(0, distance, 0));
        showSlide.InsertKeyFrame(1, Vector3.Zero, ease);
        showSlide.Duration = TimeSpan.FromMilliseconds(340);
        var show = c.CreateAnimationGroup();
        show.Add(showFade);
        show.Add(showSlide);

        var hideFade = c.CreateScalarKeyFrameAnimation();
        hideFade.Target = "Opacity";
        hideFade.InsertKeyFrame(1, 0);
        hideFade.Duration = TimeSpan.FromMilliseconds(150);
        var hideSlide = c.CreateVector3KeyFrameAnimation();
        hideSlide.Target = "Translation";
        hideSlide.InsertKeyFrame(1, new Vector3(0, distance / 2, 0));
        hideSlide.Duration = TimeSpan.FromMilliseconds(150);
        var hide = c.CreateAnimationGroup();
        hide.Add(hideFade);
        hide.Add(hideSlide);

        ElementCompositionPreview.SetImplicitShowAnimation(e, show);
        ElementCompositionPreview.SetImplicitHideAnimation(e, hide);
    }

    /// <summary>Re-applies show/hide animations after the user changes the Animations setting.</summary>
    public static void Refresh()
    {
        foreach (var w in ShowHideElements.ToList())
        {
            if (w.TryGetTarget(out var e)) ApplyShowHide(e);
            else ShowHideElements.Remove(w);
        }
    }

    // ---------- ambient ----------

    /// <summary>A soft ring that keeps expanding and fading behind a status dot while something is in progress.</summary>
    public static void SetPulsing(FrameworkElement e, bool on)
    {
        e.Visibility = on && Enabled ? Visibility.Visible : Visibility.Collapsed;
        if (e.Visibility == Visibility.Collapsed) return;
        WhenSized(e, () =>
        {
            var v = VisualOf(e);
            Center(e, v);
            var c = v.Compositor;
            var scale = c.CreateVector3KeyFrameAnimation();
            scale.InsertKeyFrame(0, Vector3.One);
            scale.InsertKeyFrame(1, new Vector3(2.8f, 2.8f, 1), EaseOut(c));
            scale.Duration = TimeSpan.FromMilliseconds(1400);
            scale.IterationBehavior = AnimationIterationBehavior.Forever;
            var fade = c.CreateScalarKeyFrameAnimation();
            fade.InsertKeyFrame(0, 0.6f);
            fade.InsertKeyFrame(1, 0f);
            fade.Duration = TimeSpan.FromMilliseconds(1400);
            fade.IterationBehavior = AnimationIterationBehavior.Forever;
            v.StartAnimation("Scale", scale);
            v.StartAnimation("Opacity", fade);
        });
    }

    /// <summary>Shrinks a thin bar from full width to nothing over <paramref name="durationMs"/>: a visible timer.</summary>
    public static void Countdown(FrameworkElement e, int durationMs)
    {
        var v = VisualOf(e);
        v.StopAnimation("Scale");
        v.Scale = Vector3.One;
        e.Visibility = Enabled ? Visibility.Visible : Visibility.Collapsed;
        if (!Enabled) return;
        WhenSized(e, () =>
        {
            v.CenterPoint = new Vector3(0, (float)e.ActualHeight / 2, 0);
            var a = v.Compositor.CreateVector3KeyFrameAnimation();
            a.InsertKeyFrame(0, Vector3.One);
            a.InsertKeyFrame(1, new Vector3(0, 1, 1), v.Compositor.CreateLinearEasingFunction());
            a.Duration = TimeSpan.FromMilliseconds(durationMs);
            v.StartAnimation("Scale", a);
        });
    }

    /// <summary>Clips an element (and anything that overflows it) to a rounded rectangle that follows its size.</summary>
    public static void ClipRounded(FrameworkElement e, float radius)
    {
        var v = VisualOf(e);
        var c = v.Compositor;
        var geometry = c.CreateRoundedRectangleGeometry();
        geometry.CornerRadius = new Vector2(radius, radius);
        v.Clip = c.CreateGeometricClip(geometry);
        void Resize() => geometry.Size = new Vector2((float)e.ActualWidth, (float)e.ActualHeight);
        e.SizeChanged += (_, _) => Resize();
        Resize();
    }

    // ---------- numbers & celebrations ----------

    /// <summary>Counts a number up from where it was to <paramref name="to"/>, easing out.</summary>
    public static void CountTo(TextBlock tb, int to, int durationMs = 650)
    {
        int.TryParse(tb.Text, out int from);
        if (!Enabled || from == to || tb.DispatcherQueue is null) { tb.Text = to.ToString(); return; }
        var started = DateTime.UtcNow;
        var timer = tb.DispatcherQueue.CreateTimer();
        timer.Interval = TimeSpan.FromMilliseconds(16);
        if (tb.Tag is DispatcherQueueTimer old) old.Stop();
        tb.Tag = timer;
        timer.Tick += (t, _) =>
        {
            double p = Math.Min(1, (DateTime.UtcNow - started).TotalMilliseconds / durationMs);
            double eased = 1 - Math.Pow(1 - p, 3);
            tb.Text = ((int)Math.Round(from + (to - from) * eased)).ToString();
            if (p >= 1) t.Stop();
        };
        timer.Start();
    }

    private static readonly Color[] BurstColors =
    {
        Color.FromArgb(255, 255, 255, 255), Color.FromArgb(255, 184, 212, 251), Color.FromArgb(255, 253, 230, 138),
        Color.FromArgb(255, 167, 243, 208), Color.FromArgb(255, 249, 168, 212), Color.FromArgb(255, 196, 181, 253),
    };

    /// <summary>A small burst of confetti dots from a point inside <paramref name="host"/>.</summary>
    public static async void Burst(Panel host, double x, double y, int count = 18)
    {
        if (!Enabled) return;
        var canvas = new Canvas { IsHitTestVisible = false };
        if (host is Grid g)
        {
            Grid.SetRowSpan(canvas, Math.Max(1, g.RowDefinitions.Count));
            Grid.SetColumnSpan(canvas, Math.Max(1, g.ColumnDefinitions.Count));
        }
        host.Children.Add(canvas);
        var rnd = new Random();
        Compositor? c = null;
        for (int i = 0; i < count; i++)
        {
            double size = 5 + rnd.NextDouble() * 5;
            UIElement dot = i % 3 == 0
                ? new Rectangle { Width = size, Height = size * 0.6, RadiusX = 1, RadiusY = 1, Fill = new SolidColorBrush(BurstColors[i % BurstColors.Length]) }
                : new Ellipse { Width = size, Height = size, Fill = new SolidColorBrush(BurstColors[i % BurstColors.Length]) };
            Canvas.SetLeft(dot, x - size / 2);
            Canvas.SetTop(dot, y - size / 2);
            canvas.Children.Add(dot);

            var v = VisualOf(dot);
            ElementCompositionPreview.SetIsTranslationEnabled(dot, true);
            c ??= v.Compositor;
            double angle = Math.PI * 2 * i / count + rnd.NextDouble() * 0.5;
            float dist = 46 + rnd.Next(0, 54);
            var end = new Vector3((float)(Math.Cos(angle) * dist), (float)(Math.Sin(angle) * dist) + 18, 0);

            var move = c.CreateVector3KeyFrameAnimation();
            move.InsertKeyFrame(0, Vector3.Zero);
            move.InsertKeyFrame(1, end, EaseOut(c));
            move.Duration = TimeSpan.FromMilliseconds(820);
            var fade = c.CreateScalarKeyFrameAnimation();
            fade.InsertKeyFrame(0, 1);
            fade.InsertKeyFrame(0.55f, 1);
            fade.InsertKeyFrame(1, 0);
            fade.Duration = TimeSpan.FromMilliseconds(820);
            var spin = c.CreateScalarKeyFrameAnimation();
            spin.InsertKeyFrame(0, 0);
            spin.InsertKeyFrame(1, rnd.Next(-200, 200));
            spin.Duration = TimeSpan.FromMilliseconds(820);
            v.CenterPoint = new Vector3((float)size / 2, (float)size / 2, 0);
            v.StartAnimation("Translation", move);
            v.StartAnimation("Opacity", fade);
            v.StartAnimation("RotationAngleInDegrees", spin);
        }
        await Task.Delay(950);
        host.Children.Remove(canvas);
    }
}
