namespace OmniFile.Core;

/// <summary>Key-based storage implemented by each provider package.</summary>
public interface IObjectStorage
{
    Task<Stream> OpenReadAsync(string key, CancellationToken cancellationToken = default);
    Task WriteAsync(string key, Stream content, StorageWriteOptions? options = null, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default);
    Task DeleteAsync(string key, CancellationToken cancellationToken = default);
    Task<StorageObjectInfo?> GetInfoAsync(string key, CancellationToken cancellationToken = default);
}
