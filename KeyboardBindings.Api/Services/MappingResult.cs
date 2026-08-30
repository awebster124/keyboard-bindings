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
    WriteConflict
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
}
