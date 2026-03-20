using Microsoft.Extensions.Configuration;

namespace Tungsten.Api.Common.Services;

public class LocalFileStorageService : IFileStorageService
{
    private readonly string _basePath;

    public LocalFileStorageService(IConfiguration configuration)
    {
        _basePath = configuration["Storage:LocalPath"] ?? Path.Combine(Directory.GetCurrentDirectory(), "storage");
        Directory.CreateDirectory(_basePath);
    }

    public async Task<string> UploadAsync(string key, Stream content, string contentType, CancellationToken ct)
    {
        var filePath = GetFilePath(key);
        var dir = Path.GetDirectoryName(filePath)!;
        Directory.CreateDirectory(dir);

        await using var fileStream = File.Create(filePath);
        await content.CopyToAsync(fileStream, ct);
        return key;
    }

    public Task<Stream> DownloadAsync(string key, CancellationToken ct)
    {
        var filePath = GetFilePath(key);
        if (!File.Exists(filePath))
            throw new FileNotFoundException($"File not found: {key}");
        return Task.FromResult<Stream>(File.OpenRead(filePath));
    }

    public string GetDownloadUrl(string key) => $"/api/documents/file/{key}";

    public Task DeleteAsync(string key, CancellationToken ct)
    {
        var filePath = GetFilePath(key);
        if (File.Exists(filePath))
            File.Delete(filePath);
        return Task.CompletedTask;
    }

    private string GetFilePath(string key) => Path.Combine(_basePath, key.Replace('/', Path.DirectorySeparatorChar));
}
