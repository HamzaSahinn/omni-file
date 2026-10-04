namespace OmniFile.Core;

public sealed record StorageWriteOptions
{
    public bool Overwrite { get; init; } = true;
    public string? ContentType { get; init; }
}
