namespace OmniFile.Core;

public sealed record StorageWriteOptions
{
    public bool Overwrite { get; set; } = true;
    public string? ContentType { get; set; }
}
