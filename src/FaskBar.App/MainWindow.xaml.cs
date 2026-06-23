using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using FaskBar.App.Taskbar;

namespace FaskBar.App;

/// <summary>
/// Overlay panel thay the toan bo vung icon list cua taskbar thuc (M1.2).
/// Start/Search/System Tray/Clock van la native, chi vung danh sach app icon
/// (tu icon dau den icon cuoi) la do FaskBar tu ve lai.
/// </summary>
public partial class MainWindow : Window
{
    private readonly TaskbarIconWatcher _watcher = new();
    private readonly Dictionary<string, BitmapSource?> _iconCache = new();
    private readonly ObservableCollection<TaskbarIconViewModel> _icons = new();
    private DispatcherTimer? _timer;

    public MainWindow()
    {
        InitializeComponent();
        IconsItemsControl.ItemsSource = _icons;
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

        RepositionToCoverIconList(realIcons);
        SyncIconsList(realIcons);
    }

    private void RepositionToCoverIconList(IReadOnlyList<TaskbarIconInfo> realIcons)
    {
        var left = realIcons.Min(i => i.BoundingRectangle.Left);
        var right = realIcons.Max(i => i.BoundingRectangle.Right);
        var top = realIcons.Min(i => i.BoundingRectangle.Top);
        var bottom = realIcons.Max(i => i.BoundingRectangle.Bottom);

        var source = PresentationSource.FromVisual(this);
        if (source?.CompositionTarget is null)
        {
            return;
        }

        var transform = source.CompositionTarget.TransformFromDevice;
        var topLeft = transform.Transform(new Point(left, top));
        var bottomRight = transform.Transform(new Point(right, bottom));

        Left = topLeft.X;
        Top = topLeft.Y;
        Width = Math.Max(1, bottomRight.X - topLeft.X);
        Height = Math.Max(1, bottomRight.Y - topLeft.Y);
    }

    private void SyncIconsList(IReadOnlyList<TaskbarIconInfo> realIcons)
    {
        // Giu thu tu y het taskbar thuc; danh sach thuong it thay doi nen rebuild don gian la du.
        var newAppIds = realIcons.Select(i => i.AppId).ToList();
        var currentAppIds = _icons.Select(i => i.AppId).ToList();
        if (newAppIds.SequenceEqual(currentAppIds))
        {
            return;
        }

        _icons.Clear();
        foreach (var icon in realIcons)
        {
            _icons.Add(new TaskbarIconViewModel(icon.AppId, icon.DisplayName, GetOrLoadIcon(icon.AppId)));
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

    private void IconButton_Click(object sender, RoutedEventArgs e)
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

public sealed record TaskbarIconViewModel(string AppId, string DisplayName, BitmapSource? Icon);
