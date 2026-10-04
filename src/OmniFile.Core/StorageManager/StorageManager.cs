using System.Collections.ObjectModel;

namespace OmniFile.Core;

public sealed record StorageRoute(string Category, IObjectStorage Storage);

/// <summary>Routes each operation to the provider mapped to the item's category.</summary>
public sealed class StorageManager : IStorageManager
{
    private readonly IReadOnlyDictionary<string, IObjectStorage> _routes;

    public StorageManager(IEnumerable<StorageRoute> routes)
    {
        ArgumentNullException.ThrowIfNull(routes);
        var map = new Dictionary<string, IObjectStorage>(StringComparer.Ordinal);
        foreach (var route in routes)
        {
            ArgumentNullException.ThrowIfNull(route);
            ArgumentException.ThrowIfNullOrWhiteSpace(route.Category);
            ArgumentNullException.ThrowIfNull(route.Storage);
            if (!map.TryAdd(route.Category, route.Storage))
                throw new ArgumentException($"Category '{route.Category}' is registered more than once.", nameof(routes));
        }
        _routes = new ReadOnlyDictionary<string, IObjectStorage>(map);
    }

    public Task<Stream> OpenReadAsync(IStorable item, CancellationToken cancellationToken = default)
    {
        var storage = Resolve(item);
        return storage.OpenReadAsync(item.Key, cancellationToken);
    }

    public Task WriteAsync(IStorable item, Stream content, StorageWriteOptions? options = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(content);
        if (!content.CanRead)
        {
            throw new ArgumentException("Content stream must be readable.", nameof(content));
        }

        var storage = Resolve(item);
        
        if(options != null && string.IsNullOrEmpty(options.ContentType))
        {
            options.ContentType = item.ContentType;
        }
        else if (options == null)
        {
            options = new StorageWriteOptions()
            {
                ContentType = item.ContentType
            };
        }

        return storage.WriteAsync(item.Key, content, options, cancellationToken);
    }

    public Task<bool> ExistsAsync(IStorable item, CancellationToken cancellationToken = default)
    {
        var storage = Resolve(item);
        return storage.ExistsAsync(item.Key, cancellationToken);
    }

    public Task DeleteAsync(IStorable item, CancellationToken cancellationToken = default)
    {
        var storage = Resolve(item);
        return storage.DeleteAsync(item.Key, cancellationToken);
    }

    public Task<StorageObjectInfo?> GetInfoAsync(IStorable item, CancellationToken cancellationToken = default)
    {
        var storage = Resolve(item);
        return storage.GetInfoAsync(item.Key, cancellationToken);
    }

    private IObjectStorage Resolve(IStorable item)
    {
        ArgumentNullException.ThrowIfNull(item);
        ArgumentException.ThrowIfNullOrWhiteSpace(item.Category);
        StorageKey.Validate(item.Key);
        return _routes.TryGetValue(item.Category, out var storage)
            ? storage
            : throw new StorageCategoryNotFoundException(item.Category);
    }
}
