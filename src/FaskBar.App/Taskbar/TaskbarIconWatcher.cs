using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;
using FlaUI.UIA3;

namespace FaskBar.App.Taskbar;

/// <summary>
/// Đọc danh sách icon app trên taskbar thật qua UI Automation.
/// Định danh icon bằng AppId (rút từ AutomationId dạng "Appid: <AppUserModelID>"),
/// vì RuntimeId/AutomationElement có thể bị tạo lại nhưng AppId ổn định qua các lần quét.
/// </summary>
public sealed class TaskbarIconWatcher : IDisposable
{
    private const string TaskListButtonClassName = "Taskbar.TaskListButtonAutomationPeer";
    private const string AppIdPrefix = "Appid: ";

    private readonly UIA3Automation _automation = new();

    // STATE_SYSTEM_PRESSED - Windows danh dau nut taskbar cua app dang foreground bang co nay (MSAA/LegacyIAccessible).
    private const int StateSystemPressed = 0x00800000;

    public IReadOnlyList<TaskbarIconInfo> Refresh()
    {
        var desktop = _automation.GetDesktop();
        var taskbar = desktop.FindFirstChild(cf => cf.ByClassName("Shell_TrayWnd"));
        if (taskbar is null)
        {
            return Array.Empty<TaskbarIconInfo>();
        }

        var buttons = taskbar.FindAllDescendants(cf => cf.ByControlType(ControlType.Button));

        var result = new List<TaskbarIconInfo>();
        foreach (AutomationElement button in buttons)
        {
            if (button.ClassName != TaskListButtonClassName)
            {
                continue;
            }

            var automationId = button.AutomationId;
            if (string.IsNullOrEmpty(automationId) || !automationId.StartsWith(AppIdPrefix, StringComparison.Ordinal))
            {
                continue;
            }

            var appId = automationId[AppIdPrefix.Length..];
            var name = button.Name ?? string.Empty;
            var isPinned = name.Contains("pinned", StringComparison.OrdinalIgnoreCase);
            var isRunning = name.Contains("running window", StringComparison.OrdinalIgnoreCase);
            var isForeground = IsPressed(button);

            result.Add(new TaskbarIconInfo(appId, name, button.BoundingRectangle, isPinned, isRunning, isForeground));
        }

        return result;
    }

    private static bool IsPressed(AutomationElement button)
    {
        try
        {
            if (!button.Patterns.LegacyIAccessible.IsSupported)
            {
                return false;
            }

            var state = (int)button.Patterns.LegacyIAccessible.Pattern.State.Value;
            return (state & StateSystemPressed) != 0;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// "Click" icon thuc qua UIA Invoke pattern - khong dung mouse click thuc vi
    /// overlay cua FaskBar dang phu len tren, click chuot thuc se trung overlay chu khong trung icon thuc.
    /// </summary>
    public bool Activate(string appId)
    {
        var desktop = _automation.GetDesktop();
        var taskbar = desktop.FindFirstChild(cf => cf.ByClassName("Shell_TrayWnd"));
        if (taskbar is null)
        {
            return false;
        }

        var target = taskbar.FindFirstDescendant(cf =>
            cf.ByControlType(ControlType.Button).And(cf.ByAutomationId(AppIdPrefix + appId)));

        if (target is null || !target.Patterns.Invoke.IsSupported)
        {
            return false;
        }

        target.Patterns.Invoke.Pattern.Invoke();
        return true;
    }

    public void Dispose() => _automation.Dispose();
}
