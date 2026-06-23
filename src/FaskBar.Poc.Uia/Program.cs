using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;
using FlaUI.UIA3;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        using var automation = new UIA3Automation();
        var desktop = automation.GetDesktop();

        var taskbar = desktop.FindFirstChild(cf => cf.ByClassName("Shell_TrayWnd"));
        if (taskbar == null)
        {
            Console.WriteLine("Khong tim thay taskbar (Shell_TrayWnd).");
            return;
        }

        Console.WriteLine($"Taskbar found: {taskbar.BoundingRectangle}");

        var buttons = taskbar.FindAllDescendants(cf => cf.ByControlType(ControlType.Button));
        Console.WriteLine($"So luong button tim thay: {buttons.Length}");

        foreach (AutomationElement button in buttons)
        {
            Console.WriteLine($"- Name='{button.Name}' Rect={button.BoundingRectangle}");
        }
    }
}
