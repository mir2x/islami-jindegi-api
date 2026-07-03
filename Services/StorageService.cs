using Amazon.S3;
using Amazon.S3.Model;

namespace IslamiJindegiApi.Services;

public class StorageService
{
    private readonly AmazonS3Client _client;
    private readonly string _bucket;

    const string PublicBase = "https://static.islamijindegi.com/uploads/store/";

    public StorageService(IConfiguration config)
    {
        var accessKey = Environment.GetEnvironmentVariable("STORAGE_ACCESS_KEY_ID")
            ?? config["Storage:AccessKeyId"]
            ?? throw new InvalidOperationException("STORAGE_ACCESS_KEY_ID not configured.");

        var secretKey = Environment.GetEnvironmentVariable("STORAGE_SECRET_ACCESS_KEY")
            ?? config["Storage:SecretAccessKey"]
            ?? throw new InvalidOperationException("STORAGE_SECRET_ACCESS_KEY not configured.");

        var endpoint = Environment.GetEnvironmentVariable("STORAGE_ENDPOINT")
            ?? config["Storage:Endpoint"]
            ?? "https://fly.storage.tigris.dev";

        _bucket = Environment.GetEnvironmentVariable("STORAGE_BUCKET_NAME")
            ?? config["Storage:BucketName"]
            ?? "static.islamijindegi.com";

        _client = new AmazonS3Client(accessKey, secretKey, new AmazonS3Config
        {
            ServiceURL = endpoint,
            AuthenticationRegion = "auto",
            ForcePathStyle = true,
        });
    }

    public async Task<string> UploadAsync(Stream stream, string fileName, string contentType)
    {
        var ext = Path.GetExtension(fileName).ToLowerInvariant();
        var key = $"uploads/store/admin/{Guid.NewGuid()}{ext}";
        await PutAsync(stream, key, contentType);
        return PublicBase + key.Substring("uploads/store/".Length);
    }

    public async Task<(string Key, string Url)> UploadWithKeyAsync(Stream stream, string fileName, string contentType)
    {
        var ext = Path.GetExtension(fileName).ToLowerInvariant();
        var key = $"uploads/store/admin/{Guid.NewGuid()}{ext}";
        await PutAsync(stream, key, contentType);
        return (key, PublicBase + key.Substring("uploads/store/".Length));
    }

    public async Task DeleteAsync(string key)
    {
        await _client.DeleteObjectAsync(new DeleteObjectRequest
        {
            BucketName = _bucket,
            Key = key,
        });
    }

    public string GetPresignedUrl(string key, int expiresInSeconds = 3600)
    {
        var request = new GetPreSignedUrlRequest
        {
            BucketName = _bucket,
            Key = key,
            Verb = HttpVerb.GET,
            Expires = DateTime.UtcNow.AddSeconds(expiresInSeconds),
        };
        return _client.GetPreSignedURL(request);
    }

    public async Task<long> GetFileSizeAsync(string key)
    {
        var meta = await _client.GetObjectMetadataAsync(new GetObjectMetadataRequest
        {
            BucketName = _bucket,
            Key = key,
        });
        return meta.ContentLength;
    }

    async Task PutAsync(Stream stream, string key, string contentType)
    {
        var response = await _client.PutObjectAsync(new PutObjectRequest
        {
            BucketName = _bucket,
            Key = key,
            InputStream = stream,
            ContentType = contentType,
            CannedACL = S3CannedACL.PublicRead,
        });

        if ((int)response.HttpStatusCode >= 300)
            throw new InvalidOperationException($"Storage upload failed with status {response.HttpStatusCode} for key '{key}'.");
    }
}
