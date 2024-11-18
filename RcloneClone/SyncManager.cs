using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Dropbox.Api.Team;

namespace RcloneClone;

internal class SyncManager()
{
    private const string ApiKey = "hvi5da1228lcdqs";
    private const string ApiSecret = "m5tngo1b0l54yy4";

    private const string OAuth2 =
        "sl.CA_j5KMB_Q0WN60JD-H8kuI98bB1aQ9Pr7AfqFt_7CsR_dO6TTcckJ4VYQBrPsrKc5MKpHoLuFx2Fl8GsA6Gy_STIktqCgEln1wWw_naEAI_KJn7u1VVrEH8mhMcXAo2rMOd5Mfz4STZesKCrgkynEM";

    public void UploadFolder(string folderLocation)
    {

    }

    public void UploadFile(string fileLocation)
    {

    }

    public async Task<bool> CheckConnection()
    {
        using (var httpClient = new HttpClient())
        {
            using (var request =
                   new HttpRequestMessage(new HttpMethod("POST"), "https://api.dropboxapi.com/2/check/user"))
            {
                request.Headers.TryAddWithoutValidation($"Authorization", $"Bearer {OAuth2}");

                request.Content = new StringContent("{\"query\":\"foo\"}");
                request.Content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/json");

                try
                {
                    var response = await httpClient.SendAsync(request);
                    return response.IsSuccessStatusCode;
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    return false;
                }
            }
        }
    }
    public async Task<string> GetFolderInfo(string folderName)
    {
        bool status = await CheckConnection();
        if (status)
        {
            using (var httpClient = new HttpClient())
            {
                using (var request =
                       new HttpRequestMessage(new HttpMethod("POST"), "https://api.dropboxapi.com/2/files/list_folder"))
                {
                    request.Headers.TryAddWithoutValidation("Authorization",
                        "Basic aHZpNWRhMTIyOGxjZHFzOm01dG5nbzFiMGw1NHl5NA==");

                    request.Content = new StringContent(
                        $"{{\"include_deleted\":false,\"include_has_explicit_shared_members\":false,\"include_media_info\":false,\"include_mounted_folders\":true,\"include_non_downloadable_files\":true,\"path\":\"/{folderName}\",\"recursive\":true}}");
                    request.Content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/json");
                    try
                    {
                        var response = await httpClient.SendAsync(request);
                        Console.WriteLine(response.ToString());
                        return response.ToString();
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                        throw;
                    }

                }
            }
        }
        return "failed!";
    }

    public async Task CreateFolder(string folderName)
    {
        bool status = await CheckConnection();
        if (status)
        {
            using (var httpClient = new HttpClient())
            {
                using (var request = new HttpRequestMessage(new HttpMethod("POST"), "https://api.dropboxapi.com/2/files/create_folder_v2"))
                {
                    request.Headers.TryAddWithoutValidation("Authorization", "Bearer sl.CA_j5KMB_Q0WN60JD-H8kuI98bB1aQ9Pr7AfqFt_7CsR_dO6TTcckJ4VYQBrPsrKc5MKpHoLuFx2Fl8GsA6Gy_STIktqCgEln1wWw_naEAI_KJn7u1VVrEH8mhMcXAo2rMOd5Mfz4STZesKCrgkynEM"); 

                    request.Content = new StringContent($"{{\"autorename\":false,\"path\":\"/{folderName}\"}}");
                    request.Content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/json");
                    try
                    {
                        var response = await httpClient.SendAsync(request);
                        Console.WriteLine(response.ToString());
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                        throw;
                    }
                }
            }
        }
    }
}