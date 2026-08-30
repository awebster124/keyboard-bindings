namespace KeyboardBindings.Api.Domain;

/// <summary>
/// The keyboards this service knows about; only these may be assigned or queried (case-insensitive, canonical casing preserved). 
/// </summary>
public static class SupportedKeyboards
{
    public const string ApexProGen3 = "Apex Pro Gen 3";

    /// <summary>All supported keyboards — used to validate requests and to seed their rows via migration.</summary>
    public static IReadOnlyList<string> All { get; } = [ApexProGen3];

    /// <summary>Resolves user-supplied text to a canonical keyboard name, or null if it is not supported.</summary>
    public static string? Resolve(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return null;
        }

        return All.FirstOrDefault(k => string.Equals(k, name.Trim(), StringComparison.OrdinalIgnoreCase));
    }
}
