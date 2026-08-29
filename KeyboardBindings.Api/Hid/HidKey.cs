namespace KeyboardBindings.Api.Hid;

/// <summary>
/// A single key defined by the USB HID Usage Tables spec (Usage Page 0x07).
/// HID usage codes are 8-bit unsigned integers (0x00–0xFF).
/// </summary>
/// <param name="Code">The HID usage code (0–255).</param>
/// <param name="Name">Human-readable name, e.g. "A" or "Caps Lock".</param>
public sealed record HidKey(byte Code, string Name)
{
    /// <summary>Hex form of the code, e.g. "0x04".</summary>
    public string Hex => $"0x{Code:X2}";
}
