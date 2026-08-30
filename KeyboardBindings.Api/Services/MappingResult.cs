using KeyboardBindings.Api.Contracts;

namespace KeyboardBindings.Api.Services;

/// <summary>
/// Outcome of an operation that can fail for known, user-facing reasons.
/// </summary>
public enum MappingStatus
{
    Success,
    KeyboardNotFound,
    ValidationFailed,

    /// <summary>
    /// Retry budget exhausted under sustained contention. Transient — surfaced as 503 + Retry-After (not 409:
    /// a full-replacement request has nothing to reconcile).
    /// </summary>
    WriteConflict,

    /// <summary>
    /// An unexpected error prevented the operation from completing — surfaced as a 500. Distinct from the known,
    /// user-facing failures above; the cause is logged server-side rather than returned to the caller.
    /// </summary>
    UnexpectedError
}

public record MappingResult(MappingStatus Status, IReadOnlyList<string> Errors)
{
    public bool IsSuccess => Status == MappingStatus.Success;

    public static MappingResult Ok() => new(MappingStatus.Success, []);

    // No message: the HTTP layer formats the 404 detail; the status is enough here.
    public static MappingResult KeyboardNotFound() =>
        new(MappingStatus.KeyboardNotFound, []);

    public static MappingResult Invalid(IReadOnlyList<string> errors) =>
        new(MappingStatus.ValidationFailed, errors);

    public static MappingResult WriteConflict() =>
        new(MappingStatus.WriteConflict,
            ["The keyboard mappings are being updated concurrently. Please retry."]);

    // Generic message only: the actual exception is logged server-side, never surfaced to the caller.
    public static MappingResult UnexpectedError() =>
        new(MappingStatus.UnexpectedError,
            ["An unexpected error occurred while saving the mappings. Please try again."]);
}

/// <summary>
/// Outcome of a read: the full mapping state for a keyboard, or a not-found status. Mirrors
/// <see cref="MappingResult"/> so both service methods report outcomes the same way rather than one
/// returning a bare tuple. <see cref="Response"/> is non-null exactly when <see cref="IsSuccess"/>.
/// </summary>
public record MappingsResult(MappingStatus Status, KeyboardMappingsResponse? Response)
{
    public bool IsSuccess => Status == MappingStatus.Success;

    public static MappingsResult Ok(KeyboardMappingsResponse response) =>
        new(MappingStatus.Success, response);

    // No message: the HTTP layer formats the 404 detail; the status is enough here.
    public static MappingsResult KeyboardNotFound() =>
        new(MappingStatus.KeyboardNotFound, null);
}
