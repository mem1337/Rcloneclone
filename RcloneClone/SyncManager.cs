using System.Net.Http.Headers;
using System.Security.Cryptography;
using Newtonsoft.Json.Linq;
namespace RcloneClone;

internal class SyncManager()
{
    private const string OAuth2 =
        "";
    public async Task<bool> UploadFile(string fileLocation, string mimeType)
    {
        using (var httpClient = new HttpClient())
        {
            using (var request =
                   new HttpRequestMessage(new HttpMethod("POST"), "https://content.dropboxapi.com/2/files/upload"))
            {
                request.Headers.TryAddWithoutValidation("Authorization", $"Bearer {OAuth2}");
                request.Headers.TryAddWithoutValidation("Dropbox-API-Arg",
                    $"{{\"autorename\":false,\"mode\":\"add\",\"mute\":false,\"path\":\"{fileLocation}\",\"strict_conflict\":false}}");

                request.Content = new StringContent(File.ReadAllText(fileLocation));
                request.Content.Headers.ContentType = MediaTypeHeaderValue.Parse(mimeType);
                try
                {
                    var response = await httpClient.SendAsync(request);
                    Console.WriteLine($"Uploading: {fileLocation}");
                    var contents = response.ToString();
                    
                    if (contents.Contains("StatusCode: 200"))
                    {
                        Console.WriteLine($"Successfully uploaded: {fileLocation}");
                        return response.IsSuccessStatusCode;
                    }
                    Console.WriteLine($"Failed to upload: {fileLocation}!");
                    return false;
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    throw;
                }

            }
        }
    }

    public async Task<string> CheckMetaData(string fileLocation)
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
    public async Task<string> LocalMetaData(string fileLocation)
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
    public static byte[][] DivideArray(byte[] source, int chunkSize)
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
    public static string ArrayToDropboxMetaData(byte[][] byteArray)
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
    public async Task<bool> CompareMetaData(string fileLocation)
    {
        string localFile = await LocalMetaData(fileLocation);
        string dropBoxFile = await CheckMetaData(fileLocation);
        if (localFile == dropBoxFile)
        {
            Console.WriteLine($"Metadata match on file {fileLocation}");
            return true;
        }
        return false;
    }
}