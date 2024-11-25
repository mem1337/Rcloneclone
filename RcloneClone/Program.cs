using System.Runtime.CompilerServices;
using MimeMapping;
namespace RcloneClone;

class Program
{
    static async Task Main()
    {
        SyncManager syncManager = new SyncManager();
        string homePath = "/home/aleks/";
        string mimeType = "";

        while (true)
        {
            string fileList = "";
            Console.WriteLine(
                $"Would you like to sync current path - {homePath}? Type 'sync' to sync or type '/' to go back!");

            foreach (string file in Directory.GetDirectories(Path.Combine(homePath), "*.*",
                         SearchOption.TopDirectoryOnly))
            {
                fileList += $"{file}\n";
            }
            foreach (string file in Directory.GetFiles(Path.Combine(homePath), "*.*", SearchOption.TopDirectoryOnly))
            {
                fileList += $"{file}\n";
            }

            Console.WriteLine(fileList);
            string userInput = Console.ReadLine();

            if (Directory.Exists(Path.Combine(homePath, userInput)))
            {
                Console.Clear();
                homePath = Path.Combine(homePath, userInput);
            }
            else if (userInput == "sync")
            {
                int successfulyUploaded = 0;
                int failedToUpload = 0;
                foreach (string file in Directory.EnumerateFiles(Path.Combine(homePath), "*.*",
                             SearchOption.AllDirectories))
                {
                    mimeType = MimeUtility.GetMimeMapping(file);
                    var metaData = await syncManager.CompareMetaData(file);
                    if (metaData==false)
                    {
                        var status = await syncManager.UploadFile(file, mimeType);
                        if (status)
                        {
                            successfulyUploaded++;
                        }
                        else if(status==false)
                        {
                            failedToUpload++;
                        }
                    }
                }
                if (failedToUpload==0)
                {
                    Console.WriteLine($"Successfully synced all {successfulyUploaded} files in {homePath}!");
                }
                Console.WriteLine($"Failed to sync all files in the directory, {failedToUpload} files failed to upload!");
            }
        }
    }
}