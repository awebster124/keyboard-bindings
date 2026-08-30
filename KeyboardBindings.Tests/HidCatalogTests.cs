using KeyboardBindings.Api.Hid;

namespace KeyboardBindings.Tests;

public class HidCatalogTests
{
    [Fact]
    public void Catalog_ContainsAllKnownKeys()
    {
        // 26 letters + 10 digits + 15 special + 12 function + 4 arrows
        // + 8 modifiers + 17 numpad = 92 keys.
        Assert.Equal(92, HidCatalog.All.Count);
    }

    [Theory]
    [InlineData(0x04, "A")]
    [InlineData(0x1D, "Z")]
    [InlineData(0x21, "4")]
    [InlineData(0x39, "Caps Lock")]
    [InlineData(0xE0, "Left Ctrl")]
    [InlineData(0x63, "Numpad .")]
    public void Find_ReturnsExpectedKey(byte code, string name)
    {
        var key = HidCatalog.Find(code);
        Assert.NotNull(key);
        Assert.Equal(name, key!.Name);
    }

    [Fact]
    public void Hex_IsTwoDigitUppercase()
    {
        Assert.Equal("0x04", HidCatalog.Find(0x04)!.Hex);
        Assert.Equal("0x1D", HidCatalog.Find(0x1D)!.Hex);
    }

    [Theory]
    [InlineData("0x04", 0x04)]
    [InlineData("0X1D", 0x1D)]
    [InlineData("4", 4)]     // decimal
    [InlineData("31", 31)]   // decimal 0x1F
    public void TryParseCode_AcceptsHexAndDecimal(string text, byte expected)
    {
        Assert.True(HidCatalog.TryParseCode(text, out var code));
        Assert.Equal(expected, code);
    }

    [Theory]
    [InlineData("0xFF")]  // valid byte, not a known key
    [InlineData("")]
    [InlineData(null)]
    [InlineData("banana")]
    [InlineData("256")]   // out of byte range
    public void TryParseCode_RejectsUnknownOrMalformed(string? text)
    {
        Assert.False(HidCatalog.TryParseCode(text, out _));
    }
}
