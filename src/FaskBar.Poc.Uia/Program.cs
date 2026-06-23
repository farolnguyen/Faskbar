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
            var runtimeId = string.Join(",", button.Properties.RuntimeId.ValueOrDefault ?? Array.Empty<int>());
            var patterns = string.Join(",", button.GetSupportedPatterns().Select(p => p.Name));
            string selectedInfo = "n/a";
            try
            {
                if (button.Patterns.SelectionItem.IsSupported)
                {
                    selectedInfo = button.Patterns.SelectionItem.Pattern.IsSelected.Value.ToString();
                }
            }
            catch { }
            Console.WriteLine($"- Name='{button.Name}' AutomationId='{button.AutomationId}' ClassName='{button.ClassName}' RuntimeId=[{runtimeId}] Patterns=[{patterns}] IsSelected={selectedInfo}");
        }

        Console.WriteLine();
        Console.WriteLine("Cho 3s roi quet lai lan 2 de so sanh RuntimeId/AutomationId co on dinh khong...");
        Thread.Sleep(3000);

        var buttons2 = taskbar.FindAllDescendants(cf => cf.ByControlType(ControlType.Button));
        foreach (AutomationElement button in buttons2)
        {
            var runtimeId = string.Join(",", button.Properties.RuntimeId.ValueOrDefault ?? Array.Empty<int>());
            Console.WriteLine($"- Name='{button.Name}' AutomationId='{button.AutomationId}' RuntimeId=[{runtimeId}]");
        }
    }
}
