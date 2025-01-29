using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using Dropbox.Api.FileRequests;
using MimeMapping;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace RcloneClone;

public class Dropbox : Manager
{
    private string ClientId = Environment.GetEnvironmentVariable("DClientId");
    private string ClientSecret = Environment.GetEnvironmentVariable("DClientSecret");
    private string? AuthorizationCode;
    private string? OAuth2;
    
    public async Task GetBearerToken()
    {
        string redirectUri = "http://localhost:3000";

        using (HttpListener listener = new HttpListener())
        {
            listener.Prefixes.Add(redirectUri + "/");
            Process.Start(new ProcessStartInfo
            {
                FileName = $"https://www.dropbox.com/oauth2/authorize?client_id={ClientId}&response_type=code&redirect_uri={redirectUri}", UseShellExecute = true
            });
            listener.Start();
            HttpListenerContext context = await listener.GetContextAsync();
            string query = context.Request.Url.Query;
            var queryParams = System.Web.HttpUtility.ParseQueryString(query);
            AuthorizationCode = queryParams["code"];
            var response = context.Response;
            var responseString = "<html><body><center>Authorization successful. You can close this window.</center></body></html>";
            var buffer = Encoding.UTF8.GetBytes(responseString);
            response.ContentLength64 = buffer.Length;
            response.OutputStream.Write(buffer, 0, buffer.Length);
            response.OutputStream.Close();
            listener.Stop();
        }
        using (var httpClient = new HttpClient())
        {
            using (var request = new HttpRequestMessage(new HttpMethod("POST"), "https://api.dropbox.com/oauth2/token"))
            {
                var contentList = new List<string>();
                contentList.Add($"code={AuthorizationCode}");
                contentList.Add("grant_type=authorization_code");
                contentList.Add($"redirect_uri={redirectUri}");
                contentList.Add($"client_id={ClientId}");
                contentList.Add($"client_secret={ClientSecret}");
                request.Content = new StringContent(string.Join("&", contentList));
                request.Content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/x-www-form-urlencoded"); 

                var response = await httpClient.SendAsync(request);
                var contents = response.Content.ReadAsStringAsync().Result;
                OAuth2 = JObject.Parse(contents).SelectToken("$.access_token").ToString();
            }
        }
    }
    public async Task<(bool status, string newFolderID)> UploadFile(string fileLocation, string folderID, string mimeType)
    {
        var uploadFolderIndex = DropboxOopsie.GetIndex();
        var fileLocationArray = fileLocation.Split(@"\");
        var uploadDropboxLocationArray = fileLocationArray[uploadFolderIndex..];
        string? uploadDropboxLocation = "/";
        for (int i = 0; i < uploadDropboxLocationArray.Length; i++)
        {
            uploadDropboxLocation += uploadDropboxLocationArray[i]+"/";
        }

        uploadDropboxLocation = uploadDropboxLocation.Remove(uploadDropboxLocation.Length-1);
        using (var httpClient = new HttpClient())
        {
            using (var request =
                   new HttpRequestMessage(new HttpMethod("POST"), "https://content.dropboxapi.com/2/files/upload"))
            {
                request.Headers.TryAddWithoutValidation("Authorization", $"Bearer {OAuth2}");
                request.Headers.TryAddWithoutValidation("Dropbox-API-Arg",
                    $"{{\"autorename\":false,\"mode\":\"add\",\"mute\":false,\"path\":\"{uploadDropboxLocation}\",\"strict_conflict\":false}}");
                byte[] fileBytes = await File.ReadAllBytesAsync(fileLocation);
                request.Content = new ByteArrayContent(fileBytes);
                request.Content.Headers.ContentType = MediaTypeHeaderValue.Parse(mimeType);
                try
                {
                    var response = await httpClient.SendAsync(request);
                    Console.WriteLine($"Uploading: {fileLocation}");
                    var contents = response.ToString();
                    
                    if (contents.Contains("StatusCode: 200"))
                    {
                        Console.WriteLine($"Successfully uploaded: {fileLocation}");
                        return (response.IsSuccessStatusCode, "");
                    }
                    Console.WriteLine($"Failed to upload: {fileLocation}!");
                    return (false, "");
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    throw;
                }

            }
        }
    }
    public async Task<string> GetFolderID(string location, string folderID)
    {
        return "";
    }
    public async Task<string> GetFiles(string folder)
    {
        using (var httpClient = new HttpClient())
        {
            using (var request = new HttpRequestMessage(new HttpMethod("POST"), "https://api.dropboxapi.com/2/files/list_folder"))
            {
                request.Headers.TryAddWithoutValidation("Authorization", $"Bearer {OAuth2}"); 

                request.Content = new StringContent($"{{\"include_deleted\":false,\"include_has_explicit_shared_members\":false,\"include_media_info\":false,\"include_mounted_folders\":true,\"include_non_downloadable_files\":true,\"path\":\"{folder}\",\"recursive\":false}}");
                request.Content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/json"); 

                var response = await httpClient.SendAsync(request);
                var contents = await response.Content.ReadAsStringAsync();
                return contents;
            }
        }
    }
    public async Task<(bool status, string newFolderID)> CompareMetaData(string fileLocation, string folderID)
    {
        string localFile = await LocalMetaData(fileLocation);
        string dropBoxFile = await CheckMetaData(fileLocation);
        if (localFile == dropBoxFile)
        {
            Console.WriteLine($"Metadata match on file {fileLocation}");
            return (true,"");
        }
        return (false,"");
    }
    private async Task<string> CheckMetaData(string fileLocation)
    {
        using (var httpClient = new HttpClient())
        {
            using (var request = new HttpRequestMessage(new HttpMethod("POST"),
                       "https://api.dropboxapi.com/2/files/get_metadata"))
            {
                request.Headers.TryAddWithoutValidation("Authorization", $"Bearer {OAuth2}");

                request.Content =
                    new StringContent(
                        $"{{\"include_deleted\":false,\"include_has_explicit_shared_members\":false,\"include_media_info\":false,\"path\":\"{fileLocation}\"}}");
                request.Content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/json");
                try
                {
                    var response = await httpClient.SendAsync(request);
                    var contents = response.Content.ReadAsStringAsync().Result;
                    var contentHash = JObject.Parse(contents).SelectToken("$.content_hash").ToString();
                    return contentHash;
                }
                catch (Exception e)
                {
                    Console.WriteLine($"No metadata found on file {fileLocation}!");
                    return "";
                }
            }
        }
    }
    private async Task<string> LocalMetaData(string fileLocation)
    {
        byte[] fileContent;
        using (FileStream fs = new FileStream(fileLocation, FileMode.Open, FileAccess.Read))
        {
            fileContent = new byte[fs.Length];
            await fs.ReadAsync(fileContent, 0, fileContent.Length);
        }
        byte[][] chunks = DivideArray(fileContent, 4 * 1024 * 1024);
        return ArrayToDropboxMetaData(chunks);
    }
    private static byte[][] DivideArray(byte[] source, int chunkSize)
    {
        int numberOfChunks = (int)Math.Ceiling(source.Length / (double)chunkSize);
        byte[][] ret = new byte[numberOfChunks][];
        int start = 0;
        for (int i = 0; i < numberOfChunks; i++)
        {
            int currentChunkSize = Math.Min(chunkSize, source.Length - start);
            ret[i] = new byte[currentChunkSize];
            Array.Copy(source, start, ret[i], 0, currentChunkSize);
            start += chunkSize;
        }
        return ret;
    }
    private static string ArrayToDropboxMetaData(byte[][] byteArray)
    {
        using (SHA256 sha256 = SHA256.Create())
        {
            List<byte> concatenatedHashes = new List<byte>();
            foreach (byte[] chunk in byteArray)
            {
                byte[] hashValue = sha256.ComputeHash(chunk);
                concatenatedHashes.AddRange(hashValue);
            }
            byte[] finalHash = sha256.ComputeHash(concatenatedHashes.ToArray());
            return BitConverter.ToString(finalHash).Replace("-", "").ToLower();
        }
    }
    public async Task<string> SyncSelection()
    {
        string locationDecision = "";
        Stack<string> currentDirectory = new Stack<string>();
        currentDirectory.Push("");
        while (true)
        {
            string dropboxFilesRequest = await GetFiles(currentDirectory.Peek());
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
    public async Task UploadSelection(string path, string uploadLocationId)
    {
        Console.Clear();
        int successfulyUploaded = 0;
        int failedToUpload = 0;
        string mimeType = "";
        foreach (string file in Directory.EnumerateFiles(Path.Combine(path), "*.*",
                     SearchOption.AllDirectories))
        {
            mimeType = MimeUtility.GetMimeMapping(file);
            var metaData = await CompareMetaData(file,uploadLocationId);
            if (metaData.status==false)
            {
                var status = await UploadFile(file,uploadLocationId,mimeType);
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

