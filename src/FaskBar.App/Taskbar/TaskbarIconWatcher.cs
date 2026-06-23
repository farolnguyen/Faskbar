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
            result.Add(new TaskbarIconInfo(appId, button.Name, button.BoundingRectangle));
        }

        return result;
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
