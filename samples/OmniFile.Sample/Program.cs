using Microsoft.Extensions.DependencyInjection;
using OmniFile.Core;
using OmniFile.DependencyInjection;
using OmniFile.Memory;
using OmniFile.Physical;

// An application can load this mapping from JSON, a database, or another source at startup.
var settings = new Dictionary<string, string>
{
    ["avatars"] = "memory",
    ["documents"] = "physical"
};

var services = new ServiceCollection();
services.AddSingleton<MemoryStorage>();
services.AddSingleton(new PhysicalStorage(Path.Combine(AppContext.BaseDirectory, "sample-data")));
services.AddOmniFile(routes =>
{
    foreach (var (category, providerName) in settings)
    {
        routes.Map(category, serviceProvider => providerName switch
        {
            "memory" => serviceProvider.GetRequiredService<MemoryStorage>(),
            "physical" => serviceProvider.GetRequiredService<PhysicalStorage>(),
            _ => throw new InvalidOperationException($"Unknown provider '{providerName}'.")
        });
    }
});

using var provider = services.BuildServiceProvider();
var manager = provider.GetRequiredService<IStorageManager>();
IStorable avatar = new Storable("avatars", "people/alice.txt");
await using (var input = new MemoryStream("Hello, OmniFile"u8.ToArray()))
    await manager.WriteAsync(avatar, input, new StorageWriteOptions { ContentType = "text/plain" });

await using var output = await manager.OpenReadAsync(avatar);
using var reader = new StreamReader(output);
Console.WriteLine(await reader.ReadToEndAsync());
