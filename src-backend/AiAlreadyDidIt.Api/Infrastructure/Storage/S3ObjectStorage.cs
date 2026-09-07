using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Options;

namespace AiAlreadyDidIt.Api.Infrastructure.Storage;

public sealed class S3ObjectStorage : IObjectStorage, IDisposable
{
    private readonly StorageOptions _o;
    private readonly AmazonS3Client _client;
    private readonly AmazonS3Client _publicClient;
    private readonly ILogger<S3ObjectStorage> _logger;

    public S3ObjectStorage(IOptions<StorageOptions> options, ILogger<S3ObjectStorage> logger)
    {
        _o = options.Value;
        _logger = logger;
        var credentials = new BasicAWSCredentials(_o.AccessKey, _o.SecretKey);
        _client = new AmazonS3Client(credentials, Config(_o.Endpoint));
        // Presigned URLs must be signed for the host the browser will use.
        _publicClient = new AmazonS3Client(credentials, Config(_o.PublicEndpoint));
    }

    private AmazonS3Config Config(string endpoint) => new()
    {
        ServiceURL = endpoint,
        ForcePathStyle = true,
        AuthenticationRegion = _o.Region,
        UseHttp = endpoint.StartsWith("http://", StringComparison.OrdinalIgnoreCase),
        RequestChecksumCalculation = RequestChecksumCalculation.WHEN_REQUIRED,
        ResponseChecksumValidation = ResponseChecksumValidation.WHEN_REQUIRED
    };

    public string BucketName(Bucket bucket) => $"{_o.BucketPrefix}-{bucket.ToString().ToLowerInvariant()}";

    public async Task EnsureBucketsAsync(CancellationToken ct = default)
    {
        var existing = (await _client.ListBucketsAsync(ct)).Buckets?.Select(b => b.BucketName).ToHashSet() ?? [];
        foreach (var bucket in Enum.GetValues<Bucket>())
        {
            var name = BucketName(bucket);
            if (existing.Contains(name)) continue;
            await _client.PutBucketAsync(new PutBucketRequest { BucketName = name }, ct);
            _logger.LogInformation("Created bucket {Bucket}", name);
        }
    }

    public async Task<StoredObject> PutAsync(Bucket bucket, string key, Stream content, string? contentType, long? knownLength = null, CancellationToken ct = default)
    {
        var request = new PutObjectRequest
        {
            BucketName = BucketName(bucket),
            Key = key,
            InputStream = content,
            ContentType = contentType ?? "application/octet-stream",
            AutoCloseStream = false,
            UseChunkEncoding = false
        };
        if (knownLength is { } len) request.Headers.ContentLength = len;
        await _client.PutObjectAsync(request, ct);
        var size = knownLength ?? (content.CanSeek ? content.Length : 0);
        return new StoredObject(key, size, contentType);
    }

    public async Task<Stream> OpenReadAsync(Bucket bucket, string key, CancellationToken ct = default)
    {
        var response = await _client.GetObjectAsync(BucketName(bucket), key, ct);
        return response.ResponseStream;
    }

    public async Task<StoredObject?> StatAsync(Bucket bucket, string key, CancellationToken ct = default)
    {
        try
        {
            var meta = await _client.GetObjectMetadataAsync(BucketName(bucket), key, ct);
            return new StoredObject(key, meta.ContentLength, meta.Headers.ContentType);
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public async Task DeleteAsync(Bucket bucket, string key, CancellationToken ct = default)
    {
        try { await _client.DeleteObjectAsync(BucketName(bucket), key, ct); }
        catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound) { }
    }

    public string GetPresignedDownloadUrl(Bucket bucket, string key, string? downloadFileName = null, TimeSpan? ttl = null)
    {
        var request = new GetPreSignedUrlRequest
        {
            BucketName = BucketName(bucket),
            Key = key,
            Verb = HttpVerb.GET,
            Expires = DateTime.UtcNow.Add(ttl ?? TimeSpan.FromMinutes(_o.PresignedUrlMinutes)),
            Protocol = _o.PublicEndpoint.StartsWith("https", StringComparison.OrdinalIgnoreCase) ? Protocol.HTTPS : Protocol.HTTP
        };
        if (!string.IsNullOrWhiteSpace(downloadFileName))
            request.ResponseHeaderOverrides.ContentDisposition = $"attachment; filename=\"{downloadFileName.Replace("\"", string.Empty)}\"";
        return _publicClient.GetPreSignedURL(request);
    }

    public async Task<bool> PingAsync(CancellationToken ct = default)
    {
        try { await _client.ListBucketsAsync(ct); return true; }
        catch { return false; }
    }

    public void Dispose()
    {
        _client.Dispose();
        _publicClient.Dispose();
    }
}
