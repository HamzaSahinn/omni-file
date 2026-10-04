using OmniFile.Core;
using System.Collections.Concurrent;

namespace OmniFile.Memory;

/// <summary>Thread-safe, process-local storage. Content is discarded with this instance.</summary>
public sealed class MemoryStorage : IObjectStorage
{
    private sealed record Entry(byte[] Bytes, string? ContentType, DateTimeOffset LastModified);
    private readonly ConcurrentDictionary<string, Entry> _items = new(StringComparer.Ordinal);

    public Task<Stream> OpenReadAsync(string key, CancellationToken cancellationToken = default)
    {
        StorageKey.Validate(key);
        cancellationToken.ThrowIfCancellationRequested();
        if (!_items.TryGetValue(key, out var entry))
            throw new StorageObjectNotFoundException(key);
        return Task.FromResult<Stream>(new MemoryStream(entry.Bytes, writable: false));
    }

    public async Task WriteAsync(string key, Stream content, StorageWriteOptions? options = null, CancellationToken cancellationToken = default)
    {
        StorageKey.Validate(key);
        ArgumentNullException.ThrowIfNull(content);
        if (!content.CanRead)
            throw new ArgumentException("Content stream must be readable.", nameof(content));
        options ??= new StorageWriteOptions();
        await using var buffer = new MemoryStream();
        await content.CopyToAsync(buffer, cancellationToken).ConfigureAwait(false);
        var entry = new Entry(buffer.ToArray(), options.ContentType, DateTimeOffset.UtcNow);
        if (options.Overwrite)
            _items[key] = entry;
        else if (!_items.TryAdd(key, entry))
            throw new StorageObjectAlreadyExistsException(key);
    }

    public Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
    {
        StorageKey.Validate(key);
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(_items.ContainsKey(key));
    }

    public Task DeleteAsync(string key, CancellationToken cancellationToken = default)
    {
        StorageKey.Validate(key);
        cancellationToken.ThrowIfCancellationRequested();
        _items.TryRemove(key, out _);
        return Task.CompletedTask;
    }

    public Task<StorageObjectInfo?> GetInfoAsync(string key, CancellationToken cancellationToken = default)
    {
        StorageKey.Validate(key);
        cancellationToken.ThrowIfCancellationRequested();
        StorageObjectInfo? info = _items.TryGetValue(key, out var entry)
            ? new(entry.Bytes.LongLength, entry.ContentType, entry.LastModified)
            : null;
        return Task.FromResult(info);
    }

    /// <summary>Lists keys beneath a prefix. This is a provider-specific convenience API.</summary>
    public IReadOnlyList<string> ListKeys(string? prefix = null)
    {
        if (prefix is not null)
            StorageKey.Validate(prefix);
        var pathPrefix = prefix is null ? string.Empty : prefix + "/";
        return _items.Keys.Where(key => key.StartsWith(pathPrefix, StringComparison.Ordinal))
            .OrderBy(key => key, StringComparer.Ordinal).ToArray();
    }
}
