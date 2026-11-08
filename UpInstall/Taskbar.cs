using System.Runtime.InteropServices;

namespace UpInstall;

/// <summary>
/// Shows install progress on the app's taskbar button and flashes it when a run ends in the background.
/// Pure best-effort: any failure here is ignored, the in-app UI already shows everything.
/// </summary>
internal sealed class Taskbar
{
    public enum State { None = 0, Indeterminate = 0x1, Normal = 0x2, Error = 0x4, Paused = 0x8 }

    private readonly IntPtr _hwnd;
    private readonly ITaskbarList3? _list;

    public Taskbar(IntPtr hwnd)
    {
        _hwnd = hwnd;
        try
        {
            _list = (ITaskbarList3)new TaskbarListInstance();
            if (_list.HrInit() != 0) _list = null;
        }
        catch
        {
            _list = null;
        }
    }

    public void SetState(State state)
    {
        try { _list?.SetProgressState(_hwnd, state); } catch { /* best effort */ }
    }

    public void SetProgress(double fraction)
    {
        try
        {
            _list?.SetProgressState(_hwnd, State.Normal);
            _list?.SetProgressValue(_hwnd, (ulong)Math.Clamp(fraction * 1000, 0, 1000), 1000);
        }
        catch { /* best effort */ }
    }

    /// <summary>Flashes the taskbar button until the window is brought to the front, if it isn't already.</summary>
    public void FlashIfInBackground()
    {
        try
        {
            if (GetForegroundWindow() == _hwnd) return;
            var info = new FLASHWINFO
            {
                cbSize = (uint)Marshal.SizeOf<FLASHWINFO>(),
                hwnd = _hwnd,
                dwFlags = FLASHW_TRAY | FLASHW_TIMERNOFG,
                uCount = 3,
            };
            FlashWindowEx(ref info);
        }
        catch { /* best effort */ }
    }

    // ---------- interop ----------

    [ComImport, Guid("ea1afb91-9e28-4b86-90e9-9e9f8a5eefaf"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface ITaskbarList3
    {
        // ITaskbarList
        [PreserveSig] int HrInit();
        [PreserveSig] int AddTab(IntPtr hwnd);
        [PreserveSig] int DeleteTab(IntPtr hwnd);
        [PreserveSig] int ActivateTab(IntPtr hwnd);
        [PreserveSig] int SetActiveAlt(IntPtr hwnd);
        // ITaskbarList2
        [PreserveSig] int MarkFullscreenWindow(IntPtr hwnd, [MarshalAs(UnmanagedType.Bool)] bool fullscreen);
        // ITaskbarList3 (only the members used here; later vtable slots are never called)
        [PreserveSig] int SetProgressValue(IntPtr hwnd, ulong completed, ulong total);
        [PreserveSig] int SetProgressState(IntPtr hwnd, State state);
    }

    [ComImport, Guid("56FDF344-FD6D-11d0-958A-006097C9A090"), ClassInterface(ClassInterfaceType.None)]
    private class TaskbarListInstance { }

    [StructLayout(LayoutKind.Sequential)]
    private struct FLASHWINFO
    {
        public uint cbSize;
        public IntPtr hwnd;
        public uint dwFlags;
        public uint uCount;
        public uint dwTimeout;
    }

    private const uint FLASHW_TRAY = 0x2;
    private const uint FLASHW_TIMERNOFG = 0xC;

    [DllImport("user32.dll")]
    private static extern bool FlashWindowEx(ref FLASHWINFO info);

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();
}
