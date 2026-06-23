using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using FaskBar.App.Taskbar;

namespace FaskBar.App;

/// <summary>
/// Overlay panel thay the vung icon list cua taskbar thuc.
/// Trai = icon pinned (keo-tha de group thanh folder), click = launch/activate qua AppId.
/// Phai = app dang mo (running), luon co dot, sang hon khi dang foreground; click = activate qua Invoke pattern.
/// </summary>
public partial class MainWindow : Window
{
    private const string DragDataFormat = "FaskBarAppId";

    private readonly TaskbarIconWatcher _watcher = new();
    private readonly PinnedGroupStore _groupStore = new();
    private readonly Dictionary<string, BitmapSource?> _iconCache = new();
    private readonly ObservableCollection<PinnedSlotViewModel> _pinnedSlots = new();
    private readonly ObservableCollection<RunningIconViewModel> _runningIcons = new();
    private DispatcherTimer? _timer;
    private Point? _dragStartPoint;
    private string? _lastPinnedSignature;

    public MainWindow()
    {
        InitializeComponent();
        PinnedItemsControl.ItemsSource = _pinnedSlots;
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
        var emitted = new HashSet<string>();
        var slots = new List<PinnedSlotViewModel>();

        foreach (var icon in pinned)
        {
            if (emitted.Contains(icon.AppId))
            {
                continue;
            }

            var group = _groupStore.FindGroupContaining(icon.AppId);
            if (group is not null)
            {
                foreach (var id in group)
                {
                    emitted.Add(id);
                }

                var firstMemberStillPinned = group.FirstOrDefault(id => pinned.Any(p => p.AppId == id)) ?? group[0];
                slots.Add(new PinnedSlotViewModel(
                    firstMemberStillPinned, $"Folder ({group.Count} app)", GetOrLoadIcon(firstMemberStillPinned), group));
            }
            else
            {
                emitted.Add(icon.AppId);
                slots.Add(new PinnedSlotViewModel(icon.AppId, icon.DisplayName, GetOrLoadIcon(icon.AppId), new[] { icon.AppId }));
            }
        }

        var signature = string.Join("|", slots.Select(s => string.Join(",", s.AppIds)));
        if (signature == _lastPinnedSignature)
        {
            return;
        }

        _lastPinnedSignature = signature;
        _pinnedSlots.Clear();
        foreach (var slot in slots)
        {
            _pinnedSlots.Add(slot);
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

    private void PinnedButton_PreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
        _dragStartPoint = e.GetPosition(null);
    }

    private void PinnedButton_PreviewMouseMove(object sender, MouseEventArgs e)
    {
        if (_dragStartPoint is null || e.LeftButton != MouseButtonState.Pressed)
        {
            return;
        }

        var pos = e.GetPosition(null);
        if (Math.Abs(pos.X - _dragStartPoint.Value.X) < SystemParameters.MinimumHorizontalDragDistance &&
            Math.Abs(pos.Y - _dragStartPoint.Value.Y) < SystemParameters.MinimumVerticalDragDistance)
        {
            return;
        }

        if (sender is FrameworkElement { Tag: string appId } element)
        {
            _dragStartPoint = null;
            DragDrop.DoDragDrop(element, new DataObject(DragDataFormat, appId), DragDropEffects.Move);
        }
    }

    private void PinnedButton_DragOver(object sender, DragEventArgs e)
    {
        e.Effects = e.Data.GetDataPresent(DragDataFormat) ? DragDropEffects.Move : DragDropEffects.None;
        e.Handled = true;
    }

    private void PinnedButton_Drop(object sender, DragEventArgs e)
    {
        if (sender is FrameworkElement { Tag: string targetAppId } && e.Data.GetDataPresent(DragDataFormat))
        {
            var sourceAppId = (string)e.Data.GetData(DragDataFormat)!;
            _groupStore.Merge(sourceAppId, targetAppId);
            _lastPinnedSignature = null; // ep SyncPinned rebuild ngay vi group vua doi
            Refresh();
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

public sealed record PinnedSlotViewModel(string DragAppId, string DisplayName, BitmapSource? Icon, IReadOnlyList<string> AppIds)
{
    public bool IsGroup => AppIds.Count > 1;
    public Visibility GroupBadgeVisibility => IsGroup ? Visibility.Visible : Visibility.Collapsed;
    public int GroupCount => AppIds.Count;
}

public sealed record RunningIconViewModel(string AppId, string DisplayName, BitmapSource? Icon, bool IsForeground)
{
    public Visibility IsForegroundVisibility => IsForeground ? Visibility.Visible : Visibility.Collapsed;
    public Brush DotBrush => IsForeground ? Brushes.White : Brushes.Gray;
}
