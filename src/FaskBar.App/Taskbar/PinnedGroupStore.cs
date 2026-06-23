using System.IO;
using System.Text.Json;

namespace FaskBar.App.Taskbar;

/// <summary>
/// Quan ly cac group (folder) icon pinned. Tu dong load luc khoi tao va save xuong dia
/// (%LocalAppData%\FaskBar\groups.json) moi khi co thay doi, de group khong mat sau khi tat may.
/// </summary>
public sealed class PinnedGroupStore
{
    private static readonly string FilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FaskBar", "groups.json");

    private readonly List<List<string>> _groups;

    public PinnedGroupStore()
    {
        _groups = Load();
    }

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

        Save();
    }

    private static List<List<string>> Load()
    {
        try
        {
            if (!File.Exists(FilePath))
            {
                return new();
            }

            var json = File.ReadAllText(FilePath);
            return JsonSerializer.Deserialize<List<List<string>>>(json) ?? new();
        }
        catch
        {
            // File loi/hong - bat dau lai voi danh sach rong, khong lam crash app.
            return new();
        }
    }

    private void Save()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
            File.WriteAllText(FilePath, JsonSerializer.Serialize(_groups));
        }
        catch
        {
            // Khong ghi duoc file (vd het quyen) - bo qua, group van dung duoc trong session hien tai.
        }
    }
}
