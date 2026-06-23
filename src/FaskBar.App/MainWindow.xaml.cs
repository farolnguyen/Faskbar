using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using FaskBar.App.Taskbar;

namespace FaskBar.App;

/// <summary>
/// Overlay panel thay the vung icon list cua taskbar thuc (M1.2 + layout 2 vung trai/phai).
/// Trai = icon pinned (co the group thanh folder sau nay), click = launch/activate qua AppId (giong mo moi/mo lai cua so).
/// Phai = app dang mo (running), luon co dot, sang hon khi dang la foreground; click = activate dung cua so dang chay qua Invoke pattern.
/// </summary>
public partial class MainWindow : Window
{
    private readonly TaskbarIconWatcher _watcher = new();
    private readonly Dictionary<string, BitmapSource?> _iconCache = new();
    private readonly ObservableCollection<PinnedIconViewModel> _pinnedIcons = new();
    private readonly ObservableCollection<RunningIconViewModel> _runningIcons = new();
    private DispatcherTimer? _timer;

    public MainWindow()
    {
        InitializeComponent();
        PinnedItemsControl.ItemsSource = _pinnedIcons;
        RunningItemsControl.ItemsSource = _runningIcons;
        Loaded += MainWindow_Loaded;
    }

    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
        _timer.Tick += (_, _) => Refresh();
        _timer.Start();

        Refresh();
    }

    private void Refresh()
    {
        var realIcons = _watcher.Refresh();
        if (realIcons.Count == 0)
        {
            return;
        }

        var leftmost = realIcons.Min(i => i.BoundingRectangle.Left);
        var top = realIcons.Min(i => i.BoundingRectangle.Top);
        var bottom = realIcons.Max(i => i.BoundingRectangle.Bottom);
        RepositionTopLeft(leftmost, top, bottom);

        SyncPinned(realIcons.Where(i => i.IsPinned).ToList());
        SyncRunning(realIcons.Where(i => i.IsRunning).ToList());
    }

    private void RepositionTopLeft(int left, int top, int bottom)
    {
        var source = PresentationSource.FromVisual(this);
        if (source?.CompositionTarget is null)
        {
            return;
        }

        var transform = source.CompositionTarget.TransformFromDevice;
        var topLeft = transform.Transform(new Point(left, top));
        var bottomRight = transform.Transform(new Point(left, bottom));

        Left = topLeft.X;
        Top = topLeft.Y;
        Height = Math.Max(1, bottomRight.Y - topLeft.Y);
    }

    private void SyncPinned(IReadOnlyList<TaskbarIconInfo> pinned)
    {
        var newIds = pinned.Select(i => i.AppId).ToList();
        var currentIds = _pinnedIcons.Select(i => i.AppId).ToList();
        if (newIds.SequenceEqual(currentIds))
        {
            return;
        }

        _pinnedIcons.Clear();
        foreach (var icon in pinned)
        {
            _pinnedIcons.Add(new PinnedIconViewModel(icon.AppId, icon.DisplayName, GetOrLoadIcon(icon.AppId)));
        }
    }

    private void SyncRunning(IReadOnlyList<TaskbarIconInfo> running)
    {
        // Foreground co the doi moi lan refresh nen luon rebuild (danh sach running thuong khong dai).
        _runningIcons.Clear();
        foreach (var icon in running)
        {
            _runningIcons.Add(new RunningIconViewModel(
                icon.AppId, icon.DisplayName, GetOrLoadIcon(icon.AppId), icon.IsForeground));
        }
    }

    private BitmapSource? GetOrLoadIcon(string appId)
    {
        if (_iconCache.TryGetValue(appId, out var cached))
        {
            return cached;
        }

        var icon = AppIconExtractor.TryGetIcon(appId);
        _iconCache[appId] = icon;
        return icon;
    }

    private void PinnedIconButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { Tag: string appId })
        {
            try
            {
                Process.Start(new ProcessStartInfo("explorer.exe", $@"shell:AppsFolder\{appId}")
                {
                    UseShellExecute = true,
                });
            }
            catch
            {
                // App khong the launch qua AppsFolder (vd AppId dang la duong dan exe cu) - bo qua cho M1.2/M1.3, se xu ly sau.
            }
        }
    }

    private void RunningIconButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { Tag: string appId })
        {
            _watcher.Activate(appId);
        }
    }

    protected override void OnClosed(EventArgs e)
    {
        _timer?.Stop();
        _watcher.Dispose();
        base.OnClosed(e);
    }
}

public sealed record PinnedIconViewModel(string AppId, string DisplayName, BitmapSource? Icon);

public sealed record RunningIconViewModel(string AppId, string DisplayName, BitmapSource? Icon, bool IsForeground)
{
    public Visibility IsForegroundVisibility => IsForeground ? Visibility.Visible : Visibility.Collapsed;
    public Brush DotBrush => IsForeground ? Brushes.White : Brushes.Gray;
}
