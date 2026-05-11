using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using CommunicationHub.Application.Interfaces;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;

namespace CommunicationHub.Infrastructure.Services;

public class StorageService : IStorageService
{
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<StorageService> _logger;
    private readonly HttpClient _httpClient;

    public StorageService(IWebHostEnvironment environment, ILogger<StorageService> logger)
    {
        _environment = environment;
        _logger = logger;
        _httpClient = new HttpClient();
    }

    public async Task<string> DownloadAndSaveAsync(string url, string username, string password, string fileName)
    {
        try
        {
            _logger.LogInformation("Downloading media from {Url}", url);

            var authValue = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{username}:{password}"));
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", authValue);

            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var uploadsPath = Path.Combine(_environment.WebRootPath, "uploads");
            if (!Directory.Exists(uploadsPath))
            {
                Directory.CreateDirectory(uploadsPath);
            }

            // Clean filename to prevent path traversal
            var safeFileName = Path.GetFileName(fileName);
            var filePath = Path.Combine(uploadsPath, safeFileName);

            using (var fs = new FileStream(filePath, FileMode.Create))
            {
                await response.Content.CopyToAsync(fs);
            }

            _logger.LogInformation("File saved successfully to {Path}", filePath);

            // Return the relative URL for frontend access
            return $"/uploads/{safeFileName}";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downloading and saving file from {Url}", url);
            throw;
        }
    }

    public string GetFullPath(string relativePath)
    {
        // relativePath expected like "/uploads/filename.jpg"
        var cleanPath = relativePath.TrimStart('/');
        return Path.Combine(_environment.WebRootPath, cleanPath.Replace('/', Path.DirectorySeparatorChar));
    }
}
