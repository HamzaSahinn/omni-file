using Microsoft.Extensions.DependencyInjection;
using OmniFile.Core;
using OmniFile.DependencyInjection;
using OmniFile.Memory;

namespace OmniFile.Tests;

public sealed class StorageManagerTests
{
    [Fact]
    public async Task Routes_by_category_and_accepts_developer_defined_storable()
    {
        var avatars = new MemoryStorage();
        var invoices = new MemoryStorage();
        IStorageManager manager = new StorageManager([
            new("avatars", avatars), new("invoices", invoices)]);
        IStorable item = new Avatar("alice");

        await manager.WriteAsync(item, new MemoryStream([4, 5]));
        Assert.True(await avatars.ExistsAsync("alice.png"));
        Assert.False(await invoices.ExistsAsync("alice.png"));
        Assert.True(await manager.ExistsAsync(item));
        await manager.DeleteAsync(item);
        Assert.False(await manager.ExistsAsync(item));
    }

    [Fact]
    public async Task Registration_can_use_external_startup_settings()
    {
        var settings = new Dictionary<string, string> { ["avatars"] = "memory" };
        var memory = new MemoryStorage();
        var services = new ServiceCollection();
        services.AddOmniFile(builder =>
        {
            foreach (var (category, provider) in settings)
                builder.Map(category, provider == "memory" ? memory : throw new InvalidOperationException());
        });
        using var serviceProvider = services.BuildServiceProvider();
        var manager = serviceProvider.GetRequiredService<IStorageManager>();
        await manager.WriteAsync(new Storable("avatars", "x.bin"), new MemoryStream([1]));
        Assert.True(await memory.ExistsAsync("x.bin"));
    }

    [Fact]
    public async Task Duplicate_and_unknown_categories_fail_clearly()
    {
        var storage = new MemoryStorage();
        Assert.Throws<ArgumentException>(() => new StorageManager([
            new("avatars", storage), new("avatars", storage)]));
        var manager = new StorageManager([new("avatars", storage)]);
        await Assert.ThrowsAsync<StorageCategoryNotFoundException>(() => manager.ExistsAsync(new Storable("other", "a.bin")));
        var builder = new OmniFileBuilder().Map("avatars", storage);
        Assert.Throws<ArgumentException>(() => builder.Map("avatars", storage));
    }

    private sealed record Avatar(string Id) : IStorable
    {
        public string Category => "avatars";
        public string Key => $"{Id}.png";
    }
}
