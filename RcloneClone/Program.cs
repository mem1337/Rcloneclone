using MimeMapping;
using Newtonsoft.Json;

namespace RcloneClone;

public class Program
{
    public static async Task Main()
    {
        Manager manager = null;
        string path = @"C:\Users\aleks";

        Console.WriteLine("1. Dropbox 2. GDrive");
        string? decision = Console.ReadLine();
        switch (decision)
        {
            case("1"):
                manager = new Dropbox();
                break;
            case("2"):
                manager = new Google();
                break;
        }
        var syncManager = new SyncClass.Sync(manager);
        await syncManager.GetBearerToken();
        ListFiles(path);
        while (true)
        {
            Console.WriteLine(
                $"Would you like to sync current path - {path}? Type '-sync' to sync!");
            string? userInput = Console.ReadLine();
            if (userInput != null && Directory.Exists(Path.Combine(path, userInput)))
            {
                Console.Clear();
                path = Path.Combine(path, userInput);
                ListFiles(path);
            }
            else if (userInput == "-sync")
            {
                string uploadLocationId = "";
                
                //Fixing my dropbox upload path mistake
                var pathSplit = path.Split(@"\");
                DropboxOopsie.SetIndex(pathSplit.Length-1);
                
                if (manager is Dropbox)
                {
                    uploadLocationId = await SyncDropBox(syncManager);
                    Console.Clear();
                    await UploadDropBox(path,uploadLocationId,syncManager);
                    break;
                }
                else
                {
                    uploadLocationId = await SyncGoogle(syncManager);
                    Console.Clear();
                    await UploadGoogle(path, uploadLocationId, syncManager);
                    break;
                }
            }
        }
    }

    public static void ListFiles(string path)
    {
        string fileList = "";
        foreach (string file in Directory.GetDirectories(Path.Combine(path), "*.*",
                     SearchOption.TopDirectoryOnly))
        {
            fileList += $"{file}\n";
        }
        foreach (string file in Directory.GetFiles(Path.Combine(path), "*.*", SearchOption.TopDirectoryOnly))
        {
            fileList += $"{file}\n";
        }
        Console.WriteLine(fileList);
    }

    public async static Task<string> SyncGoogle(SyncClass.Sync syncManager)
    {
        string uploadLocationId = "";
        string locationDecision = "";
        Stack<string> currentDirectory = new Stack<string>();
        currentDirectory.Push("root");
        string googleFilesRequest = await syncManager.GetFiles(currentDirectory.Peek());
        GoogleFileResponse? googleFileResponse = JsonConvert.DeserializeObject<GoogleFileResponse>(googleFilesRequest);
        while (true)
        {
            Console.Clear();
            googleFilesRequest = await syncManager.GetFiles(currentDirectory.Peek());
            googleFileResponse = JsonConvert.DeserializeObject<GoogleFileResponse>(googleFilesRequest);
                    
            for (int i = 0; i < googleFileResponse.Files.Count; i++)
            {
                Console.WriteLine(googleFileResponse.GetName(i).name);
            }
            locationDecision = Console.ReadLine();
            if (locationDecision == "-sync")
            {
                uploadLocationId = currentDirectory.Peek();
                return uploadLocationId;
            }
            else if (locationDecision == "/")
            {
                currentDirectory.Pop();
            }
            else
            {
                currentDirectory.Push(await syncManager.GetFolderID(locationDecision, currentDirectory.Peek()));
            }
        }
    }

    public static async Task<string> SyncDropBox(SyncClass.Sync syncManager)
    {
        string locationDecision = "";
        Stack<string> currentDirectory = new Stack<string>();
        currentDirectory.Push("");
        while (true)
        {
            string dropboxFilesRequest = await syncManager.GetFiles(currentDirectory.Peek());
            DropboxFile dropboxFileResponse = JsonConvert.DeserializeObject<DropboxFile>(dropboxFilesRequest);
            Console.Clear();
            for (int i = 0; i < dropboxFileResponse.Entries.Count; i++)
            {
                Console.WriteLine(dropboxFileResponse.GetName(i).name);
            }
            locationDecision = Console.ReadLine();
            if (locationDecision == "-sync")
            {
                var directoryString = String.Join("/", currentDirectory);
                return directoryString;
            }
            else if (locationDecision == "/")
            {
                currentDirectory.Pop();
            }
            else
            {
                currentDirectory.Push("/"+locationDecision);
            }
        }
    }
    public static async Task UploadGoogle(string path, string uploadLocationId, SyncClass.Sync syncManager)
    {
        int successfulyUploaded = 0;
        int failedToUpload = 0;
        var mimeType = "";
                
        foreach (string file in Directory.EnumerateFiles(Path.Combine(path), "*.*",
                     SearchOption.AllDirectories))
        {
            mimeType = MimeUtility.GetMimeMapping(file);
            var status = await syncManager.UploadFile(file, uploadLocationId, mimeType); 
            if (status.status) 
            { 
                successfulyUploaded++;
            }
            else if(status.status==false) 
            { 
                failedToUpload++;
            }
            uploadLocationId = status.newFolderId;
        }
        if (failedToUpload==0)
        {
            Console.WriteLine($"Successfully synced all files in {path}! Uploaded {successfulyUploaded} files.");
            Console.ReadKey();
        }
        else if (failedToUpload>0)
        {
            Console.WriteLine($"Failed to sync all files in the directory, {failedToUpload} files failed to upload!");
            Console.ReadKey();
        }
    }

    public static async Task UploadDropBox(string path, string uploadLocationId, SyncClass.Sync syncManager)
    {
        //Console.Clear();
        int successfulyUploaded = 0;
        int failedToUpload = 0;
        string mimeType = "";
        foreach (string file in Directory.EnumerateFiles(Path.Combine(path), "*.*",
                     SearchOption.AllDirectories))
        {
            mimeType = MimeUtility.GetMimeMapping(file);
            var metaData = await syncManager.CompareMetadata(file,uploadLocationId);
            if (metaData.status==false)
            {
                var status = await syncManager.UploadFile(path,uploadLocationId,mimeType);
                if (status.status)
                {
                    successfulyUploaded++;
                }
                else if(status.status==false)
                {
                    failedToUpload++;
                }
            }
        }
        if (failedToUpload==0)
        {
            Console.WriteLine($"Successfully synced all files in {path}! Uploaded {successfulyUploaded} files.");
            Console.ReadKey();
        }
        else if (failedToUpload>0)
        {
            Console.WriteLine($"Failed to sync all files in the directory, {failedToUpload} files failed to upload!");
            Console.ReadKey();
        }
    }
}