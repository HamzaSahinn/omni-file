using Microsoft.Extensions.DependencyInjection;
using OmniFile.Core;

namespace OmniFile.DependencyInjection;

public sealed class OmniFileBuilder
{
    private readonly List<(string Category, Func<IServiceProvider, IObjectStorage> Factory)> _routes = [];
    private bool _sealed;

    public OmniFileBuilder Map(string category, IObjectStorage storage)
    {
        ArgumentNullException.ThrowIfNull(storage);
        return Map(category, _ => storage);
    }

    public OmniFileBuilder Map(string category, Func<IServiceProvider, IObjectStorage> storageFactory)
    {
        if (_sealed)
        {
            throw new InvalidOperationException("Storage routes cannot be changed after registration.");
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(category);
        ArgumentNullException.ThrowIfNull(storageFactory);

        if (_routes.Any(route => string.Equals(route.Category, category, StringComparison.Ordinal)))
        {
            throw new ArgumentException($"Category '{category}' is registered more than once.", nameof(category));

        }
        _routes.Add((category, storageFactory));
        
        return this;
    }

    internal IReadOnlyList<StorageRoute> Build(IServiceProvider services) =>
        _routes.Select(route => new StorageRoute(route.Category,
            route.Factory(services) ?? throw new InvalidOperationException($"No storage provider was created for '{route.Category}'.")))
            .ToArray();

    internal void Seal() => _sealed = true;
}

public static class OmniFileServiceCollectionExtensions
{
    /// <summary>Registers one singleton manager with an immutable category map.</summary>
    public static IServiceCollection AddOmniFile(this IServiceCollection services, Action<OmniFileBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        var builder = new OmniFileBuilder();
        configure(builder);
        builder.Seal();
        
        services.AddSingleton<IStorageManager>(provider => new StorageManager(builder.Build(provider)));
        
        return services;
    }
}
