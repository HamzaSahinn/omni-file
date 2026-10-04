# OmniFile

OmniFile is a small, category-routed storage library for .NET 10. An application describes content with an `IStorable` category and key, then passes a stream to `IStorageManager`. The manager routes the operation to the provider configured for that category. Domain classes may implement `IStorable`, or applications may use the built-in `Storable` record for arbitrary files.

This repository contains a first implementation, not a published NuGet release.

## Packages

| Project | Purpose |
| --- | --- |
| `OmniFile.Core` | Contracts, key validation, and category routing |
| `OmniFile.Memory` | Thread-safe, process-local provider |
| `OmniFile.Physical` | Rooted local-file provider |
| `OmniFile.DependencyInjection` | `IServiceCollection` setup |

Each provider depends only on `OmniFile.Core`. A future S3, Azure Blob, or Dropbox provider can implement `IObjectStorage` in its own package. Folder operations stay provider-specific: `MemoryStorage.ListKeys` lists by prefix, while `PhysicalStorage.ListFiles` lists the files directly inside a local directory.

## Quick start

```csharp
using Microsoft.Extensions.DependencyInjection;
using OmniFile.Core;
using OmniFile.DependencyInjection;
using OmniFile.Memory;

var services = new ServiceCollection();
services.AddSingleton<MemoryStorage>();
services.AddOmniFile(routes => routes.Map("avatars", sp => sp.GetRequiredService<MemoryStorage>()));

using var provider = services.BuildServiceProvider();
var manager = provider.GetRequiredService<IStorageManager>();
var avatar = new Storable("avatars", "users/42/photo.png");

await using var source = File.OpenRead("photo.png");
await manager.WriteAsync(avatar, source, new StorageWriteOptions { ContentType = "image/png" });

await using var stored = await manager.OpenReadAsync(avatar);
// Read or copy the returned stream; the caller disposes it.
```

An application can load category mappings from a database or configuration file **at startup** and call `Map` for each entry. Routing remains fixed for the lifetime of the manager. A category maps to one provider instance; several categories may share one instance. Moving a deployed category to a new provider requires copying its existing data before changing the mapping.

## Contract

- `IStorable` exposes `Category` and `Key`; it does not own content or serialization. `WriteAsync` accepts any readable stream, including a non-seekable stream, and leaves it open. `OpenReadAsync` returns a stream the caller must dispose.
- Keys are relative, slash-separated, and portable. Empty segments, `.` and `..`, backslashes, control characters, Windows-reserved characters, and device names are rejected. A key is unique within its category's provider, although two categories mapped to the same provider can address the same key.
- Writing overwrites by default. Set `Overwrite = false` for create-only behavior. A missing read throws `StorageObjectNotFoundException`; deleting a missing key succeeds. `GetInfoAsync` returns `null` for a missing key. Metadata fields may be `null` when unknown; the physical provider does not persist content type.
- The physical provider stores a completed write through a temporary file and rename in the target directory. It rejects known symbolic links beneath its configured root. As with ordinary local file APIs, an untrusted process that can concurrently replace paths inside that root is outside this guarantee.
- The memory provider holds the full content in process memory. It is suitable for tests and bounded workloads, not large persistent files.

## Build and test

```text
dotnet restore OmniFile.slnx
dotnet build OmniFile.slnx --no-restore
dotnet test tests/OmniFile.Tests/OmniFile.Tests.csproj --no-restore
dotnet run --project samples/OmniFile.Sample
dotnet pack src/OmniFile.Core/OmniFile.Core.csproj -c Release
```

The test project applies the same contract cases to both first-party providers. Before public NuGet publication, choose a license and repository URL, then add package ownership and publishing credentials in the release pipeline.
