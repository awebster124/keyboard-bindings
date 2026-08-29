using System.Globalization;

namespace KeyboardBindings.Api.Hid;

/// <summary>
/// The set of keys that exist on the keyboard, keyed by HID usage code — the source of truth for which codes are
/// valid for remapping. Codes follow the USB HID Usage Tables specification (Usage Page 0x07).
/// </summary>
public static class HidCatalog
{
    private static readonly IReadOnlyDictionary<byte, HidKey> KeysByCode = BuildCatalog();

    /// <summary>All keys, ordered by HID code.</summary>
    public static IReadOnlyList<HidKey> All { get; } =
        KeysByCode.Values.OrderBy(k => k.Code).ToArray();

    /// <summary>True if the given code corresponds to a key on the keyboard.</summary>
    public static bool IsValid(byte code) => KeysByCode.ContainsKey(code);

    /// <summary>Looks up a key by code, or null if the code is unknown.</summary>
    public static HidKey? Find(byte code) =>
        KeysByCode.TryGetValue(code, out var key) ? key : null;

    /// <summary>
    /// Parses a HID code from a string: hex when prefixed with 0x/0X ("0x04"), otherwise decimal ("4").
    /// Returns false if the value is not a valid byte or not a known key.
    /// </summary>
    public static bool TryParseCode(string? text, out byte code)
    {
        code = 0;
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        text = text.Trim();

        var isHex = text.StartsWith("0x", StringComparison.OrdinalIgnoreCase);
        var body = isHex ? text[2..] : text;
        var style = isHex ? NumberStyles.HexNumber : NumberStyles.Integer;

        return byte.TryParse(body, style, CultureInfo.InvariantCulture, out code)
               && IsValid(code);
    }

    private static Dictionary<byte, HidKey> BuildCatalog()
    {
        var keys = new List<HidKey>();

        void Add(byte code, string name) => keys.Add(new HidKey(code, name));

        // Letters A–Z: 0x04–0x1D
        for (byte c = 0x04, letter = (byte)'A'; c <= 0x1D; c++, letter++)
        {
            Add(c, ((char)letter).ToString());
        }

        // Numbers (top row) 1–9 then 0: 0x1E–0x27
        var digits = new[] { "1", "2", "3", "4", "5", "6", "7", "8", "9", "0" };
        for (byte c = 0x1E; c <= 0x27; c++)
        {
            Add(c, digits[c - 0x1E]);
        }

        // Special / editing keys
        Add(0x28, "Enter");
        Add(0x29, "Escape");
        Add(0x2A, "Backspace");
        Add(0x2B, "Tab");
        Add(0x2C, "Space");
        Add(0x39, "Caps Lock");
        Add(0x46, "Print Screen");
        Add(0x47, "Scroll Lock");
        Add(0x48, "Pause");
        Add(0x49, "Insert");
        Add(0x4A, "Home");
        Add(0x4B, "Page Up");
        Add(0x4C, "Delete");
        Add(0x4D, "End");
        Add(0x4E, "Page Down");

        // Function keys F1–F12: 0x3A–0x45
        for (byte c = 0x3A, f = 1; c <= 0x45; c++, f++)
        {
            Add(c, $"F{f}");
        }

        // Arrow keys
        Add(0x4F, "Right Arrow");
        Add(0x50, "Left Arrow");
        Add(0x51, "Down Arrow");
        Add(0x52, "Up Arrow");

        // Modifier keys
        Add(0xE0, "Left Ctrl");
        Add(0xE1, "Left Shift");
        Add(0xE2, "Left Alt");
        Add(0xE3, "Left GUI (Win/Cmd)");
        Add(0xE4, "Right Ctrl");
        Add(0xE5, "Right Shift");
        Add(0xE6, "Right Alt");
        Add(0xE7, "Right GUI");

        // Numpad
        Add(0x53, "Num Lock");
        Add(0x54, "Numpad /");
        Add(0x55, "Numpad *");
        Add(0x56, "Numpad -");
        Add(0x57, "Numpad +");
        Add(0x58, "Numpad Enter");
        for (byte c = 0x59, n = 1; c <= 0x61; c++, n++)
        {
            Add(c, $"Numpad {n}");
        }
        Add(0x62, "Numpad 0");
        Add(0x63, "Numpad .");

        return keys.ToDictionary(k => k.Code);
    }
}
