namespace OmniFile.Core;

/// <summary>Identifies content without binding it to a storage provider.</summary>
public interface IStorable
{
    string Category { get; }
    string Key { get; }
}

/// <summary>A ready-to-use descriptor for content that has no domain type.</summary>
public sealed record Storable(string Category, string Key) : IStorable;
