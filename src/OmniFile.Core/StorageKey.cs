namespace OmniFile.Core;

/// <summary>Validates a portable, relative, slash-separated storage key.</summary>
public static class StorageKey
{
    private static readonly char[] InvalidCharacters = ['\\', ':', '<', '>', '"', '|', '?', '*'];

    public static void Validate(string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        foreach (var segment in key.Split('/'))
        {
            if (segment.Length == 0 || segment is "." or ".." ||
                segment.EndsWith(' ') || segment.EndsWith('.') ||
                segment.IndexOfAny(InvalidCharacters) >= 0 ||
                segment.Any(char.IsControl) || IsReservedDeviceName(segment))
            {
                throw new ArgumentException($"'{key}' is not a portable relative storage key.", nameof(key));
            }
        }
    }

    private static bool IsReservedDeviceName(string segment)
    {
        var name = segment.Split('.')[0].ToUpperInvariant();
        return name is "CON" or "PRN" or "AUX" or "NUL" or
            "COM1" or "COM2" or "COM3" or "COM4" or "COM5" or "COM6" or "COM7" or "COM8" or "COM9" or
            "LPT1" or "LPT2" or "LPT3" or "LPT4" or "LPT5" or "LPT6" or "LPT7" or "LPT8" or "LPT9";
    }
}
