using Amazon.S3;
using Amazon.S3.Model;

namespace Tungsten.Api.Common.Services;

public class R2FileStorageService : IFileStorageService
{
    private readonly IAmazonS3 _s3;
    private readonly string _bucketName;
    private readonly string _publicUrl;

    public R2FileStorageService(IConfiguration configuration)
    {
        var accountId = configuration["R2:AccountId"] ?? "";
        var accessKey = configuration["R2:AccessKeyId"] ?? "";
        var secretKey = configuration["R2:SecretAccessKey"] ?? "";
        _bucketName = configuration["R2:BucketName"] ?? "tungsten-documents";
        _publicUrl = configuration["R2:PublicUrl"] ?? "";

        var config = new AmazonS3Config
        {
            ServiceURL = $"https://{accountId}.r2.cloudflarestorage.com",
            ForcePathStyle = true,
        };

        _s3 = new AmazonS3Client(accessKey, secretKey, config);
    }

    public async Task<string> UploadAsync(string key, Stream content, string contentType, CancellationToken ct)
    {
        var request = new PutObjectRequest
        {
            BucketName = _bucketName,
            Key = key,
            InputStream = content,
            ContentType = contentType,
        };

        await _s3.PutObjectAsync(request, ct);
        return key;
    }

    public async Task<Stream> DownloadAsync(string key, CancellationToken ct)
    {
        var response = await _s3.GetObjectAsync(_bucketName, key, ct);
        return response.ResponseStream;
    }

    public string GetDownloadUrl(string key)
    {
        // Generate a pre-signed URL valid for 1 hour
        var request = new GetPreSignedUrlRequest
        {
            BucketName = _bucketName,
            Key = key,
            Expires = DateTime.UtcNow.AddHours(1),
            Verb = HttpVerb.GET,
        };

        return _s3.GetPreSignedURL(request);
    }

    public async Task DeleteAsync(string key, CancellationToken ct)
    {
        await _s3.DeleteObjectAsync(_bucketName, key, ct);
    }
}
