using System.Collections;
using System.ComponentModel.DataAnnotations;

namespace KeyboardBindings.Api.Contracts;

/// <summary>
/// A single remapping request entry. Codes are accepted as hex ("0x04") or decimal ("4") strings.
/// </summary>
public record RemapDto(string From, string To);

/// <summary>
/// The complete set of remappings for a keyboard. Any key not listed is identity, so an empty list resets it.
/// The null-rejection rules are DataAnnotations so minimal-API validation enforces them at the boundary (400)
/// before the handler runs; the service can then trust the shape of its input.
/// </summary>
public record AssignMappingsRequest(
    [property: Required(ErrorMessage = "A 'mappings' array is required.")]
    [property: NoNullElements(ErrorMessage = "A mapping entry is required.")]
    List<RemapDto> Mappings);

/// <summary>How a single key is represented in responses.</summary>
public record KeyDto(byte Code, string Hex, string Name);

/// <summary>One physical key and the key it currently emits.</summary>
public record KeyMappingDto(KeyDto PhysicalKey, KeyDto MappedKey, bool IsRemapped);

/// <summary>Full mapping state for a keyboard.</summary>
public record KeyboardMappingsResponse(string Keyboard, IReadOnlyList<KeyMappingDto> Mappings);

/// <summary>
/// Fails if any element of the annotated collection is null. DataAnnotations has no built-in way to say
/// "no null items", and JSON can yield a null element for a non-nullable element type. A null collection itself
/// is treated as valid here so <see cref="RequiredAttribute"/> owns the "is the collection present?" rule.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter, AllowMultiple = false)]
public sealed class NoNullElementsAttribute : ValidationAttribute
{
    public override bool IsValid(object? value)
    {
        if (value is not IEnumerable items)
        {
            return true;
        }

        foreach (var item in items)
        {
            if (item is null)
            {
                return false;
            }
        }

        return true;
    }
}
