using System;
using System.IO;
using System.Threading.Tasks;

namespace CommunicationHub.Application.Interfaces;

public interface IStorageService
{
    /// <summary>
    /// Downloads a file from an authenticated URL (using Basic Auth) and saves it locally.
    /// </summary>
    /// <param name="url">The URL of the file to download</param>
    /// <param name="username">Basic auth username (e.g. AccountSid)</param>
    /// <param name="password">Basic auth password (e.g. AuthToken)</param>
    /// <param name="fileName">Desired local filename</param>
    /// <returns>The relative file path or URL to the saved file</returns>
    Task<string> DownloadAndSaveAsync(string url, string username, string password, string fileName);

    /// <summary>
    /// Returns the absolute physical path of the storage directory.
    /// </summary>
    string GetFullPath(string relativePath);
}
