namespace RcloneClone;

public static class DropboxOopsie
{
    public static int Index;

    public static void SetIndex(int index)
    {
        Index = index;
    }

    public static int GetIndex()
    {
        return Index;
    }
}
public class DropboxFile
{
    public string Cursor { get; set; }
    public List<Entry> Entries { get; set; }
    public bool HasMore { get; set; }

    public (string name, string id, bool folder) GetName(int i)
    {
        if (Entries[i].Tag == "folder")
        {
            return (Entries[i].Name, Entries[i].Id, true);
        }
        else
        {
            return (Entries[i].Name, Entries[i].Id, false);
        }
    }
}

public class Entry
{
    public string Tag { get; set; }
    public string Id { get; set; }
    public string Name { get; set; }
    public string PathDisplay { get; set; }
    public string PathLower { get; set; }
    public List<PropertyGroup> PropertyGroups { get; set; }
    public SharingInfo SharingInfo { get; set; }
}

public class FileEntry : Entry
{
    public DateTime ClientModified { get; set; }
    public string ContentHash { get; set; }
    public FileLockInfo FileLockInfo { get; set; }
    public bool IsDownloadable { get; set; }
    public string Rev { get; set; }
    public DateTime ServerModified { get; set; }
    public long Size { get; set; }
}

public class FolderEntry : Entry
{
    public bool NoAccess { get; set; }
    public bool TraverseOnly { get; set; }
    public bool ReadOnly { get; set; }
}

public class PropertyGroup
{
    public List<PropertyField> Fields { get; set; }
    public string TemplateId { get; set; }
}

public class PropertyField
{
    public string Name { get; set; }
    public string Value { get; set; }
}

public class SharingInfo
{
    public string ParentSharedFolderId { get; set; }
    public bool ReadOnly { get; set; }
}

public class FileLockInfo
{
    public DateTime Created { get; set; }
    public bool IsLockholder { get; set; }
    public string LockholderName { get; set; }
}