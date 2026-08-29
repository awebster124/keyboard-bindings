namespace KeyboardBindings.Api.Data;

/// <summary>
/// A single physical key on a keyboard and the HID code it emits. One row per physical key per keyboard; an
/// "identity" mapping has <see cref="MappedCode"/> == <see cref="PhysicalCode"/> (the key is not remapped).
/// </summary>
public class KeyMapping
{
    public int Id { get; set; }

    /// <summary>The keyboard this mapping belongs to, e.g. "Apex Pro Gen 3".</summary>
    public string KeyboardName { get; set; } = string.Empty;

    /// <summary>The HID code of the physical key that is pressed.</summary>
    public byte PhysicalCode { get; set; }

    /// <summary>The HID code that is emitted when the physical key is pressed.</summary>
    public byte MappedCode { get; set; }

    /// <summary>
    /// Optimistic-concurrency token, stamped by <see cref="AppDbContext"/> on every save (SQLite has no native
    /// rowversion). A mismatch means another writer won.
    /// </summary>
    public Guid Version { get; set; }

    public bool IsRemapped => PhysicalCode != MappedCode;
}
