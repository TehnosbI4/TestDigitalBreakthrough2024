using System.ComponentModel.DataAnnotations;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Text.Json;

namespace RazorClient.Pages;

public class IndexModel : PageModel
{
    public List<AnalyzeResult> Results { get; set; }
    
    private readonly ILogger<IndexModel> _logger;
    private IWebHostEnvironment _environment;
    private static HttpClient _httpClient = new();
    private string _filesPath;

    public IndexModel(ILogger<IndexModel> logger, IWebHostEnvironment hostingEnvironment)
    {
        _logger = logger;
        _environment = hostingEnvironment;
        _filesPath = Path.Combine(_environment.ContentRootPath, "AudioFiles");
    }

    [BindProperty, Display(Name = "File")] public IList<IFormFile> UploadedFiles { get; set; }

    public async Task OnPost()
    {
        var audioFiles = await GetAudioFilesAsync();

        var response = SendAudioFilesAsync(audioFiles);


        var table = new List<AnalyzeResult>();
        for (var i = 0; i < audioFiles.Count; i++)
        {
            var file = audioFiles[i];
            var filePath = Path.Combine(_filesPath, file.Name);
            var row = new AnalyzeResult(filePath, true, "shiiiiish");
            table.Add(row);
        }

        Results = table;
    }

    private async Task<List<AudioFile>> GetAudioFilesAsync()
    {
        var exists = Directory.Exists(_filesPath);

        if (!exists)
        {
            Directory.CreateDirectory(_filesPath);
        }

        var audioFiles = new List<AudioFile>();
        foreach (var file in UploadedFiles)
        {
            var filePath = Path.Combine(_filesPath, file.FileName);
            await using (var fileStream = new FileStream(filePath, FileMode.Create)) 
            {
                await file.CopyToAsync(fileStream);
            }

            // byte[] bytes = await System.IO.File.ReadAllBytesAsync(filePath);
            // string fileInBase64 = Convert.ToBase64String(bytes);
            // audioFiles.Add(new AudioFile(file.FileName, fileInBase64));
            audioFiles.Add(new AudioFile(file.FileName, filePath));
        }

        return audioFiles;
    }

    private async Task<string> SendAudioFilesAsync(List<AudioFile> audioFiles)
    {
        using StringContent jsonContent = new(
            JsonSerializer.Serialize(new
            {
                files = audioFiles
            }),
            Encoding.UTF8,
            "application/json");

        using var response = await _httpClient.PostAsync(
            "http://127.0.0.1:5000/submit_input",
            jsonContent);

        //response.EnsureSuccessStatusCode().WriteRequestToConsole();

        var jsonResponse = await response.Content.ReadAsStringAsync();
        Console.WriteLine($"{jsonResponse}\n");

        return jsonResponse;
    }
}

public record AudioFile(string Name, string Content);
public record AnalyzeResult(string Name, bool Status, string Text);