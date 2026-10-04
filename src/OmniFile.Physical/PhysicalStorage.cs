using OmniFile.Core;

namespace OmniFile.Physical;

/// <summary>Stores keys as files beneath a configured directory.</summary>
public sealed class PhysicalStorage : IObjectStorage
{
    private readonly string _root;
    private readonly string _rootPrefix;
    private readonly StringComparison _pathComparison = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;

    public PhysicalStorage(PhysicalStorageOptions options)
    {
        ArgumentNullException.ThrowIfNull(options, nameof(options));
        ArgumentException.ThrowIfNullOrWhiteSpace(options.RootDirectory, nameof(options.RootDirectory));

        _root = Path.GetFullPath(options.RootDirectory);

        _rootPrefix = Path.EndsInDirectorySeparator(_root) ? _root : _root + Path.DirectorySeparatorChar;        
    }

    public Task<Stream> OpenReadAsync(string key, CancellationToken cancellationToken = default)
    {
        var path = GetPath(key);
        cancellationToken.ThrowIfCancellationRequested();
        try
        {
            return Task.FromResult<Stream>(new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read | FileShare.Delete,
                4096, FileOptions.Asynchronous | FileOptions.SequentialScan));
        }
        catch (FileNotFoundException)
        {
            throw new StorageObjectNotFoundException(key);
        }
        catch (DirectoryNotFoundException)
        {
            throw new StorageObjectNotFoundException(key);
        }
    }

    public async Task WriteAsync(string key, Stream content, StorageWriteOptions? options = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(content);
        if (!content.CanRead) throw new ArgumentException("Content stream must be readable.", nameof(content));
        var path = GetPath(key);
        cancellationToken.ThrowIfCancellationRequested();
        options ??= new StorageWriteOptions();
        var directory = Path.GetDirectoryName(path)!;
        Directory.CreateDirectory(directory);
        EnsureNoLinks(path);
        var temp = Path.Combine(directory, $".omnifile-{Guid.NewGuid():N}.tmp");
        try
        {
            await using (var output = new FileStream(temp, FileMode.CreateNew, FileAccess.Write, FileShare.None,
                81920, FileOptions.Asynchronous))
            {
                await content.CopyToAsync(output, cancellationToken).ConfigureAwait(false);
                await output.FlushAsync(cancellationToken).ConfigureAwait(false);
            }
            cancellationToken.ThrowIfCancellationRequested();
            try { File.Move(temp, path, options.Overwrite); }
            catch (IOException) when (!options.Overwrite && File.Exists(path))
            {
                throw new StorageObjectAlreadyExistsException(key);
            }
        }
        finally
        {
            if (File.Exists(temp)) File.Delete(temp);
        }
    }

    public Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
    {
        var path = GetPath(key);
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(File.Exists(path));
    }

    public Task DeleteAsync(string key, CancellationToken cancellationToken = default)
    {
        var path = GetPath(key);
        cancellationToken.ThrowIfCancellationRequested();
        try { File.Delete(path); }
        catch (DirectoryNotFoundException) { /* Deleting a missing key is idempotent. */ }
        return Task.CompletedTask;
    }

    public Task<StorageObjectInfo?> GetInfoAsync(string key, CancellationToken cancellationToken = default)
    {
        var path = GetPath(key);
        cancellationToken.ThrowIfCancellationRequested();
        var file = new FileInfo(path);
        StorageObjectInfo? info = file.Exists
            ? new(file.Length, null, new DateTimeOffset(file.LastWriteTimeUtc, TimeSpan.Zero))
            : null;
        return Task.FromResult(info);
    }

    /// <summary>Lists files beneath a directory using local file-system semantics.</summary>
    public IReadOnlyList<string> ListFiles(string? directoryKey = null)
    {
        var directory = directoryKey is null ? _root : GetPath(directoryKey);
        EnsureNoLinks(directory);
        if (!Directory.Exists(directory)) return [];
        return Directory.EnumerateFiles(directory, "*", SearchOption.TopDirectoryOnly)
            .Where(path => !Path.GetFileName(path).StartsWith(".omnifile-", StringComparison.Ordinal))
            .Select(path => Path.GetRelativePath(_root, path).Replace(Path.DirectorySeparatorChar, '/'))
            .OrderBy(key => key, StringComparer.Ordinal).ToArray();
    }

    private string GetPath(string key)
    {
        StorageKey.Validate(key);
        var path = Path.GetFullPath(Path.Combine(_root, key.Replace('/', Path.DirectorySeparatorChar)));
        if (!path.StartsWith(_rootPrefix, _pathComparison))
            throw new ArgumentException("Storage key escapes the configured root.", nameof(key));
        EnsureNoLinks(path);
        return path;
    }

    private void EnsureNoLinks(string path)
    {
        var current = _root;
        CheckLink(current);
        var relative = Path.GetRelativePath(_root, path);
        foreach (var part in relative.Split(Path.DirectorySeparatorChar))
        {
            current = Path.Combine(current, part);
            CheckLink(current);
        }
    }

    private static void CheckLink(string path)
    {
        if ((File.Exists(path) || Directory.Exists(path)) &&
            (File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0)
            throw new IOException($"Symbolic links are not supported within a physical storage root: '{path}'.");
    }
}
