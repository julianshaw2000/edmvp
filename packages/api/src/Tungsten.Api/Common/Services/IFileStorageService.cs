namespace Tungsten.Api.Common.Services;

public interface IFileStorageService
{
    Task<string> UploadAsync(string key, Stream content, string contentType, CancellationToken ct);
    Task<Stream> DownloadAsync(string key, CancellationToken ct);
    string GetDownloadUrl(string key);
    Task DeleteAsync(string key, CancellationToken ct);
}
