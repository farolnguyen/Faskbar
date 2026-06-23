using System.Runtime.InteropServices;
using System.Text;

namespace FaskBar.App.Taskbar;

/// <summary>
/// Ep mot window cua app (xac dinh qua AppId) len foreground thuc su.
/// Can thiet vi UIA Invoke pattern tren nut taskbar thuc khong luon thang duoc co che
/// chong "focus-stealing" cua Windows khi nguoi goi (explorer.exe) khong phai noi nhan input that.
/// </summary>
public static class ForegroundActivator
{
    public static bool TryActivateByAppId(string appId)
    {
        var targetHwnd = FindWindowByAppId(appId);
        if (targetHwnd == IntPtr.Zero)
        {
            return false;
        }

        ForceForeground(targetHwnd);
        return true;
    }

    private static IntPtr FindWindowByAppId(string appId)
    {
        var found = IntPtr.Zero;

        NativeMethods.EnumWindows((hwnd, _) =>
        {
            if (!NativeMethods.IsWindowVisible(hwnd))
            {
                return true;
            }

            NativeMethods.GetWindowThreadProcessId(hwnd, out var pid);
            if (pid == 0)
            {
                return true;
            }

            var hProcess = NativeMethods.OpenProcess(NativeMethods.PROCESS_QUERY_LIMITED_INFORMATION, false, pid);
            if (hProcess == IntPtr.Zero)
            {
                return true;
            }

            try
            {
                uint length = 256;
                var sb = new StringBuilder((int)length);
                var result = NativeMethods.GetApplicationUserModelId(hProcess, ref length, sb);
                if (result == 0 && string.Equals(sb.ToString(), appId, StringComparison.Ordinal))
                {
                    found = hwnd;
                    return false;
                }
            }
            finally
            {
                NativeMethods.CloseHandle(hProcess);
            }

            return true;
        }, IntPtr.Zero);

        return found;
    }

    private static void ForceForeground(IntPtr hwnd)
    {
        var foregroundHwnd = NativeMethods.GetForegroundWindow();
        var foregroundThreadId = NativeMethods.GetWindowThreadProcessId(foregroundHwnd, out _);
        var targetThreadId = NativeMethods.GetWindowThreadProcessId(hwnd, out _);
        var currentThreadId = NativeMethods.GetCurrentThreadId();

        var attachedToForeground = foregroundThreadId != currentThreadId &&
            NativeMethods.AttachThreadInput(currentThreadId, foregroundThreadId, true);
        var attachedToTarget = targetThreadId != currentThreadId &&
            NativeMethods.AttachThreadInput(currentThreadId, targetThreadId, true);

        try
        {
            if (NativeMethods.IsIconic(hwnd))
            {
                NativeMethods.ShowWindow(hwnd, NativeMethods.SW_RESTORE);
            }

            NativeMethods.BringWindowToTop(hwnd);
            NativeMethods.SetForegroundWindow(hwnd);
        }
        finally
        {
            if (attachedToTarget)
            {
                NativeMethods.AttachThreadInput(currentThreadId, targetThreadId, false);
            }

            if (attachedToForeground)
            {
                NativeMethods.AttachThreadInput(currentThreadId, foregroundThreadId, false);
            }
        }
    }

    private static class NativeMethods
    {
        public const uint PROCESS_QUERY_LIMITED_INFORMATION = 0x1000;
        public const int SW_RESTORE = 9;

        public delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        [DllImport("user32.dll")]
        public static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

        [DllImport("user32.dll")]
        public static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern IntPtr OpenProcess(uint processAccess, bool bInheritHandle, uint processId);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool CloseHandle(IntPtr hObject);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        public static extern int GetApplicationUserModelId(IntPtr hProcess, ref uint applicationUserModelIdLength, StringBuilder applicationUserModelId);

        [DllImport("user32.dll")]
        public static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        public static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern bool BringWindowToTop(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        public static extern bool IsIconic(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern bool AttachThreadInput(uint idAttach, uint idAttachTo, bool fAttach);

        [DllImport("kernel32.dll")]
        public static extern uint GetCurrentThreadId();
    }
}
