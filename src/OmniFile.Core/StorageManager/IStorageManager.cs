namespace OmniFile.Core;

public interface IStorageManager
{
    Task<Stream> OpenReadAsync(IStorable item, CancellationToken cancellationToken = default);
    Task WriteAsync(IStorable item, Stream content, StorageWriteOptions? options = null, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(IStorable item, CancellationToken cancellationToken = default);
    Task DeleteAsync(IStorable item, CancellationToken cancellationToken = default);
    Task<StorageObjectInfo?> GetInfoAsync(IStorable item, CancellationToken cancellationToken = default);
}
