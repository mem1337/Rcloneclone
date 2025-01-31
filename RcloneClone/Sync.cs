namespace RcloneClone;

public class SyncClass
{
    public class Sync(Manager manager)
    {
        private Manager _manager = manager;

        public async Task<(bool status, string newFolderId)> UploadFile(string fileLocation, string folderID, string mimeType)
        {
            var result = await _manager.UploadFile(fileLocation, folderID, mimeType);
            return (result.status,result.newFolderID);
        }
        public async Task<string> GetFolderID(string location, string folderID)
        {
            var result = await _manager.GetFolderID(location, folderID);
            return result;
        }
        public async Task<(bool status, string newFolderId)> CompareMetadata(string fileLocation, string folderID)
        {
            var result = await _manager.CompareMetaData(fileLocation, folderID);
            return (result.status, result.newFolderID);
        }
        public async Task<string> GetFiles(string folderID)
        {
            var result = await _manager.GetFiles(folderID);
            return result;
        }
        public async Task GetBearerToken()
        {
            await _manager.GetBearerToken();
        }
        public async Task<string> SyncSelection()
        {
            var result = await _manager.SyncSelection();
            return result;
        }
        public async Task UploadSelection(string path, string uploadLocationId)
        {
            await _manager.UploadSelection(path, uploadLocationId);
        }

        public void UploadPathIndex(int index)
        {
            _manager.UploadPathIndex(index);
        }
    }
}
public interface Manager
{
    public Task<(bool status, string newFolderID)> UploadFile(string fileLocation, string folderID, string mimeType);
    public Task<string> GetFiles(string folderID);
    public Task<string> GetFolderID(string location, string folderID);
    public Task<(bool status, string newFolderID)> CompareMetaData(string fileLocation, string folderID);
    public Task GetBearerToken();
    public Task<string> SyncSelection();
    public Task UploadSelection(string path, string uploadLocationId);
    public void UploadPathIndex(int index);
}