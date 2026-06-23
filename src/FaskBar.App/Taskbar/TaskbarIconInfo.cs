using System.Drawing;

namespace FaskBar.App.Taskbar;

public sealed record TaskbarIconInfo(string AppId, string DisplayName, Rectangle BoundingRectangle);
