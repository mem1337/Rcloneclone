using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using MimeMapping;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace RcloneClone;

public class Google : Manager
{
    private string ClientId = Environment.GetEnvironmentVariable("GClientId");
    private string ClientSecret = Environment.GetEnvironmentVariable("GClientSecret");
    private string? AuthorizationCode;
    private string? OAuth2;
    private int UploadIndex;

    public async Task<(bool status, string newFolderID)> CompareMetaData(string a, string b)
    {
        return (false, null);
    }
    public async Task GetBearerToken()
    {
        string redirectUri = "http://localhost:3000/";
        using (HttpListener listener = new HttpListener())
        {
            listener.Prefixes.Add(redirectUri);
            Process.Start(new ProcessStartInfo
            {
                FileName =
                    $"https://accounts.google.com/o/oauth2/v2/auth?client_id={ClientId}&redirect_uri={redirectUri}&response_type=code&access_type=offline&scope=https://www.googleapis.com/auth/drive https://www.googleapis.com/auth/drive.readonly https://www.googleapis.com/auth/drive.metadata https://www.googleapis.com/auth/drive.metadata.readonly",
                UseShellExecute = true
            });
            listener.Start();
            HttpListenerContext context = await listener.GetContextAsync();
            string query = context.Request.Url.Query;
            var queryParams = System.Web.HttpUtility.ParseQueryString(query);
            AuthorizationCode = queryParams["code"];
            var response = context.Response;
            var responseString =
                "<html><body><center>Authorization successful. You can close this window.</center></body></html>";
            var buffer = System.Text.Encoding.UTF8.GetBytes(responseString);
            response.ContentLength64 = buffer.Length;
            response.OutputStream.Write(buffer, 0, buffer.Length);
            response.OutputStream.Close();
            listener.Stop();
        }

        using (var httpClient = new HttpClient())
        {
            using (var request =
                   new HttpRequestMessage(new HttpMethod("POST"), "https://accounts.google.com/o/oauth2/token"))
            {
                request.Content =
                    new StringContent(
                        $"code={AuthorizationCode}&client_id={ClientId}&client_secret={ClientSecret}&redirect_uri={redirectUri}&grant_type=authorization_code");
                request.Content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/x-www-form-urlencoded");
                var response = await httpClient.SendAsync(request);
                var contents = response.Content.ReadAsStringAsync().Result;
                OAuth2 = JObject.Parse(contents).SelectToken("$.access_token").ToString();
            }
        }
    }
    public async Task<(bool status, string newFolderID)> UploadFile(string fileLocation, string folderID, string mimeType)
    {
        var fileLocationArray = fileLocation.Split(@"\");
        var fileName = fileLocationArray[^1];
        var fileDestination = fileLocationArray[^2];
        var uploadTo = await GetFolderID(fileDestination, folderID);
        string newFolderID = "";

        if (uploadTo!=folderID)
        {
            using (var client = new HttpClient())
            {
                var url = "https://www.googleapis.com/upload/drive/v3/files?uploadType=multipart";
                var boundary = "my-boundary";
                var content = new MultipartFormDataContent(boundary);
                var metadata = new StringContent($"{{\"name\":\"{fileName}\",\"parents\":[\"{uploadTo}\"]}}", Encoding.UTF8,
                    "application/json");
                content.Add(metadata, "metadata");
                var fileContent = new ByteArrayContent(File.ReadAllBytes(fileLocation));
                fileContent.Headers.ContentType = new MediaTypeHeaderValue("text/plain");
                content.Add(fileContent, "file");
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", OAuth2);
                var response = await client.PostAsync(url, content);
                var contents = response.Content.ReadAsStringAsync().Result;
                if (response.IsSuccessStatusCode)
                {
                    return (true, folderID);
                }
                return (false, folderID);
            }
        }
        using (var httpClient = new HttpClient())
        {
            using (var request = new HttpRequestMessage(new HttpMethod("POST"), "https://www.googleapis.com/drive/v3/files"))
            {
                request.Headers.TryAddWithoutValidation("Authorization", $"Bearer {OAuth2}"); 
                request.Content = new StringContent($"{{\n        \"name\": \"{fileDestination}\",\n        \"mimeType\": \"application/vnd.google-apps.folder\",\n        \"parents\": [\"{folderID}\"]\n      }}");
                request.Content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/json"); 
                var response = await httpClient.SendAsync(request);
                var folderResponse = response.Content.ReadAsStringAsync().Result;
                newFolderID = JObject.Parse(folderResponse).SelectToken("$.id").ToString();
            }
        }
        
        using (var client = new HttpClient())
        {
            Console.WriteLine($"Uploading {fileLocation}...");
            var url = "https://www.googleapis.com/upload/drive/v3/files?uploadType=multipart";
            var boundary = "my-boundary";
            var content = new MultipartFormDataContent(boundary);
            var metadata = new StringContent($"{{\"name\":\"{fileName}\",\"parents\":[\"{newFolderID}\"]}}", Encoding.UTF8,
                "application/json");
            content.Add(metadata, "metadata");
            var fileContent = new ByteArrayContent(File.ReadAllBytes(fileLocation));
            fileContent.Headers.ContentType = new MediaTypeHeaderValue("text/plain");
            content.Add(fileContent, "file");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", OAuth2);
            var response = await client.PostAsync(url, content);
            if (response.IsSuccessStatusCode)
            {
                return (true, newFolderID);
            }
            return (false, newFolderID);
        }
    }
    public async Task<string> GetFiles(string folderId)
    {
        using (var httpClient = new HttpClient())
        {
            string requestUri = $"https://www.googleapis.com/drive/v3/files?q=%27{folderId}%27%20in%20parents";
            using (var request = new HttpRequestMessage(new HttpMethod("GET"),
                       requestUri))
            {
                request.Headers.TryAddWithoutValidation("Authorization", $"Bearer {OAuth2}");
                var response = await httpClient.SendAsync(request);
                var contents = await response.Content.ReadAsStringAsync();
                return contents;
            }
        }
    }
    public async Task<string> GetFolderID(string location,string folderID)
    {
        string requestUri = $"https://www.googleapis.com/drive/v3/files?q=%27{folderID}%27%20in%20parents";
        using (var httpClient = new HttpClient())
        {
            using (var request = new HttpRequestMessage(new HttpMethod("GET"),
                       requestUri))
            {
                request.Headers.TryAddWithoutValidation("Authorization", $"Bearer {OAuth2}");
                var response = await httpClient.SendAsync(request);
                var contents = await response.Content.ReadAsStringAsync();
                GoogleFileResponse googleFileResponse = JsonConvert.DeserializeObject<GoogleFileResponse>(contents);   
                var foldersList = new List<string>();
                var filesList = new List<string>();
                for (int i = 0; i < googleFileResponse.Files.Count; i++)
                {
                    var nameStatus = googleFileResponse.GetName(i);
                    if (nameStatus.folder)
                    {
                        foldersList.Add(nameStatus.name);
                    }
                    filesList.Add(nameStatus.name);
                }
                foldersList.AddRange(filesList);
                for (int i = 0; i < googleFileResponse.Files.Count; i++)
                {
                    var result = googleFileResponse.GetName(i);
                    if (location==result.name)
                    {
                        folderID = result.id;
                        return folderID;
                    }
                }
            }
        }
        return folderID;
    }
    public async Task<string> SyncSelection()
    {
        string uploadLocationId = "";
        string locationDecision = "";
        Stack<string> currentDirectory = new Stack<string>();
        currentDirectory.Push("root");
        string googleFilesRequest = await GetFiles(currentDirectory.Peek());
        GoogleFileResponse? googleFileResponse = JsonConvert.DeserializeObject<GoogleFileResponse>(googleFilesRequest);
        while (true)
        {
            Console.Clear();
            googleFilesRequest = await GetFiles(currentDirectory.Peek());
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
                currentDirectory.Push(await GetFolderID(locationDecision, currentDirectory.Peek()));
            }
        }
    }
    public async Task UploadSelection(string path, string uploadLocationId)
    {
        int successfulyUploaded = 0;
        int failedToUpload = 0;
        var mimeType = "";
                
        foreach (string file in Directory.EnumerateFiles(Path.Combine(path), "*.*",
                     SearchOption.AllDirectories))
        {
            mimeType = MimeUtility.GetMimeMapping(file);
            var status = await UploadFile(file, uploadLocationId, mimeType); 
            if (status.status) 
            { 
                successfulyUploaded++;
            }
            else if(status.status==false) 
            { 
                failedToUpload++;
            }
            uploadLocationId = status.newFolderID;
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
    public void UploadPathIndex(int index)
    {
        UploadIndex = index;
    }
    public async Task AlternativeUpload(string fileLocation, string folderID, string mimeType)
    {
        
    }
}