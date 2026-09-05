namespace AiAlreadyDidIt.Api.Infrastructure.Storage;

public enum Bucket { Screenshots, Installers, Sources, Icons, Banners }

public sealed record StoredObject(string Key, long Size, string? ContentType);

/// <summary>S3-compatible object storage (MinIO locally). Keys are relative to the logical bucket.</summary>
public interface IObjectStorage
{
    Task EnsureBucketsAsync(CancellationToken ct = default);
    Task<StoredObject> PutAsync(Bucket bucket, string key, Stream content, string? contentType, long? knownLength = null, CancellationToken ct = default);
    Task<Stream> OpenReadAsync(Bucket bucket, string key, CancellationToken ct = default);
    Task<StoredObject?> StatAsync(Bucket bucket, string key, CancellationToken ct = default);
    Task DeleteAsync(Bucket bucket, string key, CancellationToken ct = default);
    /// <summary>Time-limited download URL reachable from the browser (uses <c>Storage:PublicEndpoint</c>).</summary>
    string GetPresignedDownloadUrl(Bucket bucket, string key, string? downloadFileName = null, TimeSpan? ttl = null);
    string BucketName(Bucket bucket);
    Task<bool> PingAsync(CancellationToken ct = default);
}
