using MimeMapping;
using Newtonsoft.Json;

namespace RcloneClone;

public class Program
{
    public static async Task Main()
    {
        Manager manager = null;
        string path = @"C:\Users\aleks";
        start:
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
                //Set upload path index
                var pathSplit = path.Split(@"\");
                syncManager.UploadPathIndex(pathSplit.Length-1);
                uploadLocationId = await syncManager.SyncSelection();
                Console.Clear();
                await syncManager.UploadSelection(path,uploadLocationId);
                goto start;
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
}