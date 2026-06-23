namespace FaskBar.App.Taskbar;

/// <summary>
/// Quan ly cac group (folder) icon pinned, trong nho tam (M1.6 se them persist xuong dia).
/// </summary>
public sealed class PinnedGroupStore
{
    private readonly List<List<string>> _groups = new();

    public List<string>? FindGroupContaining(string appId) =>
        _groups.FirstOrDefault(g => g.Contains(appId));

    /// <summary>
    /// Keo sourceAppId tha vao targetAppId. Tu dong gop nhom theo 4 truong hop:
    /// ca 2 da co nhom rieng -> gop 2 nhom; 1 trong 2 da co nhom -> them ben con lai vao nhom do;
    /// ca 2 chua co nhom -> tao nhom moi.
    /// </summary>
    public void Merge(string sourceAppId, string targetAppId)
    {
        if (sourceAppId == targetAppId)
        {
            return;
        }

        var sourceGroup = FindGroupContaining(sourceAppId);
        var targetGroup = FindGroupContaining(targetAppId);

        if (sourceGroup is not null && targetGroup is not null)
        {
            if (ReferenceEquals(sourceGroup, targetGroup))
            {
                return;
            }

            foreach (var id in sourceGroup)
            {
                if (!targetGroup.Contains(id))
                {
                    targetGroup.Add(id);
                }
            }

            _groups.Remove(sourceGroup);
        }
        else if (targetGroup is not null)
        {
            if (!targetGroup.Contains(sourceAppId))
            {
                targetGroup.Add(sourceAppId);
            }
        }
        else if (sourceGroup is not null)
        {
            if (!sourceGroup.Contains(targetAppId))
            {
                sourceGroup.Add(targetAppId);
            }
        }
        else
        {
            _groups.Add(new List<string> { targetAppId, sourceAppId });
        }
    }
}
