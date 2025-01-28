namespace RcloneClone;

public class GoogleFile(string kind, string mimeType, string id, string name)
{
    public string Kind=kind;
    public string MimeType=mimeType;
    public string Id=id;
    public string Name=name;
}

public class GoogleFileResponse(string nextPageToken, string kind, bool incompleteSearch, List<GoogleFile> files)
{
    public string NextPageToken = nextPageToken;
    public string Kind = kind;
    public bool IncompleteSearch = incompleteSearch;
    public List<GoogleFile> Files = files;
    public (string name, string id, bool folder) GetName(int i)
    {
        if (Files[i].MimeType=="application/vnd.google-apps.folder")
        {
            return (Files[i].Name, Files[i].Id, true);
        }
        return (Files[i].Name, Files[i].Id, false);
    }
}