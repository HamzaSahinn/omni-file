namespace OmniFile.Core;

public sealed class StorageObjectNotFoundException(string key)
    : FileNotFoundException($"Storage object '{key}' was not found.", key);

public sealed class StorageObjectAlreadyExistsException(string key)
    : IOException($"Storage object '{key}' already exists.");

public sealed class StorageCategoryNotFoundException(string category)
    : InvalidOperationException($"No storage provider is registered for category '{category}'.");
