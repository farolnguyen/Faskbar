using System.Windows;
using System.Windows.Threading;
using FlaUI.Core.Definitions;
using FlaUI.UIA3;

namespace FaskBar.App;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private const string TargetIconName = "File Explorer pinned";

    private UIA3Automation? _automation;
    private DispatcherTimer? _timer;

    public MainWindow()
    {
        InitializeComponent();
        Loaded += MainWindow_Loaded;
    }

    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        _automation = new UIA3Automation();

        _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(200) };
        _timer.Tick += (_, _) => RepositionOverlay();
        _timer.Start();

        RepositionOverlay();
    }

    private void RepositionOverlay()
    {
        if (_automation is null) return;

        var desktop = _automation.GetDesktop();
        var taskbar = desktop.FindFirstChild(cf => cf.ByClassName("Shell_TrayWnd"));
        if (taskbar is null) return;

        var icon = taskbar.FindFirstDescendant(cf =>
            cf.ByControlType(ControlType.Button).And(cf.ByName(TargetIconName)));
        if (icon is null) return;

        var rectPhysical = icon.BoundingRectangle;

        var source = PresentationSource.FromVisual(this);
        if (source?.CompositionTarget is null) return;

        var transform = source.CompositionTarget.TransformFromDevice;
        var topLeft = transform.Transform(new Point(rectPhysical.Left, rectPhysical.Top));
        var bottomRight = transform.Transform(new Point(rectPhysical.Right, rectPhysical.Bottom));

        Left = topLeft.X;
        Top = topLeft.Y;
        Width = Math.Max(1, bottomRight.X - topLeft.X);
        Height = Math.Max(1, bottomRight.Y - topLeft.Y);
    }

    protected override void OnClosed(EventArgs e)
    {
        _timer?.Stop();
        _automation?.Dispose();
        base.OnClosed(e);
    }
}
