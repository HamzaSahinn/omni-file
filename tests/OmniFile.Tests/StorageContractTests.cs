using OmniFile.Core;
using OmniFile.Memory;
using OmniFile.Physical;

namespace OmniFile.Tests;

public sealed class StorageContractTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Reads_writes_and_overwrites_binary_content(bool physical)
    {
        using var fixture = new ProviderFixture(physical);
        var key = "avatars/person-1.bin";
        var original = new byte[] { 0, 1, 255, 42 };
        using var first = new NonSeekableReadStream(original);

        await fixture.Storage.WriteAsync(key, first);
        Assert.True(first.CanRead); // The caller retains ownership of its input stream.
        Assert.True(await fixture.Storage.ExistsAsync(key));
        await using (var read = await fixture.Storage.OpenReadAsync(key))
        {
            using var copy = new MemoryStream();
            await read.CopyToAsync(copy);
            Assert.Equal(original, copy.ToArray());
        }

        await fixture.Storage.WriteAsync(key, new MemoryStream([9, 8, 7]));
        await using var overwritten = await fixture.Storage.OpenReadAsync(key);
        using var result = new MemoryStream();
        await overwritten.CopyToAsync(result);
        Assert.Equal(new byte[] { 9, 8, 7 }, result.ToArray());

        var info = await fixture.Storage.GetInfoAsync(key);
        Assert.Equal(3, info?.Length);
        Assert.NotNull(info?.LastModified);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Create_only_rejects_existing_key_without_changing_content(bool physical)
    {
        using var fixture = new ProviderFixture(physical);
        await fixture.Storage.WriteAsync("one.bin", new MemoryStream([1]));
        await Assert.ThrowsAsync<StorageObjectAlreadyExistsException>(() =>
            fixture.Storage.WriteAsync("one.bin", new MemoryStream([2]), new() { Overwrite = false }));
        await using var read = await fixture.Storage.OpenReadAsync("one.bin");
        Assert.Equal(1, read.ReadByte());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Missing_objects_and_delete_are_consistent(bool physical)
    {
        using var fixture = new ProviderFixture(physical);
        Assert.False(await fixture.Storage.ExistsAsync("missing.bin"));
        Assert.Null(await fixture.Storage.GetInfoAsync("missing.bin"));
        await Assert.ThrowsAsync<StorageObjectNotFoundException>(() => fixture.Storage.OpenReadAsync("missing.bin"));
        await fixture.Storage.DeleteAsync("missing.bin");
        await fixture.Storage.WriteAsync("missing.bin", new MemoryStream([1]));
        await fixture.Storage.DeleteAsync("missing.bin");
        Assert.False(await fixture.Storage.ExistsAsync("missing.bin"));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Cancellation_is_observed(bool physical)
    {
        using var fixture = new ProviderFixture(physical);
        using var canceled = new CancellationTokenSource();
        canceled.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            fixture.Storage.WriteAsync("canceled.bin", new MemoryStream([1]), cancellationToken: canceled.Token));
        Assert.False(await fixture.Storage.ExistsAsync("canceled.bin"));
    }

    [Theory]
    [InlineData("../escape.bin")]
    [InlineData("/absolute.bin")]
    [InlineData("a//b.bin")]
    [InlineData("a\\b.bin")]
    [InlineData("CON.txt")]
    public async Task Unsafe_keys_are_rejected(string key)
    {
        using var fixture = new ProviderFixture(true);
        await Assert.ThrowsAsync<ArgumentException>(() => fixture.Storage.ExistsAsync(key));
    }

    [Fact]
    public async Task Memory_preserves_content_type_and_lists_prefix()
    {
        var storage = new MemoryStorage();
        await storage.WriteAsync("photos/a.png", new MemoryStream([1]), new() { ContentType = "image/png" });
        await storage.WriteAsync("photos/b.png", new MemoryStream([2]));
        await storage.WriteAsync("other/c.png", new MemoryStream([3]));
        Assert.Equal("image/png", (await storage.GetInfoAsync("photos/a.png"))?.ContentType);
        Assert.Equal(new[] { "photos/a.png", "photos/b.png" }, storage.ListKeys("photos"));
    }

    [Fact]
    public async Task Physical_lists_local_directory_without_exposing_temporary_files()
    {
        using var fixture = new ProviderFixture(true);
        var storage = (PhysicalStorage)fixture.Storage;
        await storage.WriteAsync("docs/one.txt", new MemoryStream([1]));
        await storage.WriteAsync("docs/two.txt", new MemoryStream([2]));
        Assert.Equal(new[] { "docs/one.txt", "docs/two.txt" }, storage.ListFiles("docs"));
    }

    private sealed class ProviderFixture : IDisposable
    {
        private readonly string? _directory;
        public IObjectStorage Storage { get; }

        public ProviderFixture(bool physical)
        {
            if (physical)
            {
                _directory = Path.Combine(AppContext.BaseDirectory, "test-data", Guid.NewGuid().ToString("N"));
                Storage = new PhysicalStorage(new PhysicalStorageOptions() { RootDirectory = _directory});
            }
            else Storage = new MemoryStorage();
        }

        public void Dispose()
        {
            if (_directory is not null && Directory.Exists(_directory)) Directory.Delete(_directory, recursive: true);
        }
    }

    private sealed class NonSeekableReadStream(byte[] content) : Stream
    {
        private readonly MemoryStream _inner = new(content);
        public override bool CanRead => _inner.CanRead;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
        public override int Read(byte[] buffer, int offset, int count) => _inner.Read(buffer, offset, count);
        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default) =>
            _inner.ReadAsync(buffer, cancellationToken);
        public override void Flush() => throw new NotSupportedException();
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        protected override void Dispose(bool disposing) { if (disposing) _inner.Dispose(); base.Dispose(disposing); }
    }
}
