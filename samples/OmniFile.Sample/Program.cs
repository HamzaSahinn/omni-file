using Microsoft.Extensions.DependencyInjection;
using OmniFile.Core;
using OmniFile.DependencyInjection;
using OmniFile.Memory;
using OmniFile.Physical;
using OmniFile.Sample;


var services = new ServiceCollection();

services.AddMemoryStorage();
services.AddPhysicalStorage(opt =>
{
    opt.RootDirectory = Path.Combine(AppContext.BaseDirectory, "sample-data");
});

services.AddOmniFile(routes =>
{
    routes.Map(DomainEntityTwoStorable.StorageCategory, provider =>
    {
        return provider.GetRequiredService<MemoryStorage>();
    });

    routes.Map(DomainEntityOneStorable.StorageCategory, provider =>
    {
        return provider.GetRequiredService<PhysicalStorage>();
    });
});

using var provider = services.BuildServiceProvider();
var manager = provider.GetRequiredService<IStorageManager>();

IStorable avatar = new DomainEntityOneStorable(45);

await using (var input = new MemoryStream("Hello, OmniFile"u8.ToArray()))
    await manager.WriteAsync(avatar, input, new StorageWriteOptions { ContentType = "text/plain" });

await using var output = await manager.OpenReadAsync(avatar);
using var reader = new StreamReader(output);

Console.WriteLine(await reader.ReadToEndAsync());
