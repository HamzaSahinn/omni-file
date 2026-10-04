namespace OmniFile.Core;

/// <summary>Portable metadata. Null means the provider cannot supply the field.</summary>
public sealed record StorageObjectInfo(long? Length, string? ContentType, DateTimeOffset? LastModified);
