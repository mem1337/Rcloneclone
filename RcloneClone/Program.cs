namespace RcloneClone;

class Program
{
    static void Main()
    {
        while (true)
        {
            SyncManager syncManager = new SyncManager();
            string absolutePath = "/var/home/aleks/RiderProjects/RcloneClone/RcloneClone/";
            string homePath = "/home/aleks/";
            
            Console.WriteLine("Please provide the directory you would like to sync."); 
            string? location = Console.ReadLine();
            //if (Directory.Exists(Path.Combine(homePath, location)) || File.Exists(Path.Combine(homePath, location))) syncManager.CreateFolder(location);
            syncManager.GetFolderInfo(location);
        }
    }
}