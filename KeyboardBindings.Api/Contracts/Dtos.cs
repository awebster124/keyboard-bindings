namespace KeyboardBindings.Api.Contracts;

/// <summary>
/// A single remapping request entry. Codes are accepted as hex ("0x04") or decimal ("4") strings.
/// </summary>
public record RemapDto(string From, string To);

/// <summary>
/// The complete set of remappings for a keyboard. Any key not listed is identity, so an empty list resets it.
/// </summary>
public record AssignMappingsRequest(List<RemapDto> Mappings);

/// <summary>How a single key is represented in responses.</summary>
public record KeyDto(byte Code, string Hex, string Name);

/// <summary>One physical key and the key it currently emits.</summary>
public record KeyMappingDto(KeyDto PhysicalKey, KeyDto MappedKey, bool IsRemapped);

/// <summary>Full mapping state for a keyboard.</summary>
public record KeyboardMappingsResponse(string Keyboard, IReadOnlyList<KeyMappingDto> Mappings);
