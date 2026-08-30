using KeyboardBindings.Api.Contracts;
using KeyboardBindings.Api.Data;
using KeyboardBindings.Api.Domain;
using KeyboardBindings.Api.Hid;
using KeyboardBindings.Api.Observability;
using Microsoft.EntityFrameworkCore;

namespace KeyboardBindings.Api.Services;

public class MappingService(
    AppDbContext db,
    ILogger<MappingService> logger)
{
    // Limits the last-write-wins retry loop
    private const int MaxAssignAttempts = 5;

    /// <summary>
    /// Returns every key on the keyboard along with the key it currently emits. Keys are seeded as identity
    /// mappings at migration time, so the full keyboard is always present (remapped or not).
    /// </summary>
    public async Task<(MappingStatus Status, KeyboardMappingsResponse? Response)> GetMappingsAsync(
        string keyboardName, CancellationToken ct = default)
    {
        var canonical = SupportedKeyboards.Resolve(keyboardName);
        if (canonical is null)
        {
            return (MappingStatus.KeyboardNotFound, null);
        }

        var rows = await db.KeyMappings
            .Where(m => m.KeyboardName == canonical)
            .ToListAsync(ct);

        var byCode = rows.ToDictionary(r => r.PhysicalCode);

        var mappings = HidCatalog.All
            .Select(key =>
            {
                var mappedCode = byCode.TryGetValue(key.Code, out var row) ? row.MappedCode : key.Code;
                var mapped = HidCatalog.Find(mappedCode)!;
                return new KeyMappingDto(ToDto(key), ToDto(mapped), key.Code != mappedCode);
            })
            .ToList();

        return (MappingStatus.Success, new KeyboardMappingsResponse(canonical, mappings));
    }

    /// <summary>
    /// Validates the requested remappings and persists them. The request is the complete non-identity remap set:
    /// any key not listed is reset to identity.
    /// </summary>
    public async Task<MappingResult> AssignMappingsAsync(
        string keyboardName, AssignMappingsRequest request, CancellationToken ct = default)
    {
        var canonical = SupportedKeyboards.Resolve(keyboardName);
        if (canonical is null)
        {
            return MappingResult.KeyboardNotFound();
        }

        var (errors, parsed) = Validate(request);
        if (errors.Count > 0)
        {
            return MappingResult.Invalid(errors);
        }

        // Last-write-wins: on a concurrency conflict, reload and reapply the request 
        // so the latest write wins on fresh data rather than clobbering from a stale read.
        DbUpdateConcurrencyException? lastConflict = null;
        for (var attempt = 1; attempt <= MaxAssignAttempts; attempt++)
        {
            // Drop stale tracked entities so the retry reloads fresh rows/tokens.
            if (attempt > 1)
            {
                db.ChangeTracker.Clear();
            }

            var rows = await db.KeyMappings
                .Where(m => m.KeyboardName == canonical)
                .ToListAsync(ct);
            var byCode = rows.ToDictionary(r => r.PhysicalCode);

            foreach (var row in rows)
            {
                row.MappedCode = row.PhysicalCode;
            }

            foreach (var (from, to) in parsed)
            {
                if (byCode.TryGetValue(from, out var row))
                {
                    row.MappedCode = to;
                    continue;
                }

                // Validated key with no persisted row — a data anomaly (e.g. a keyboard added without its seed
                // migration). Recreate it rather than fail; the read path already tolerates it via identity.
                logger.LogWarning(
                    "Missing key mapping row for {Keyboard} key {PhysicalCode}; recreating it. "
                    + "This usually means the keyboard was not seeded by a migration.",
                    canonical, from);

                var created = new KeyMapping
                {
                    KeyboardName = canonical,
                    PhysicalCode = from,
                    MappedCode = to
                };
                db.KeyMappings.Add(created);
                byCode[from] = created;
            }

            try
            {
                await db.SaveChangesAsync(ct);
                if (attempt > 1)
                {
                    logger.LogInformation(
                        "Assign for {Keyboard} succeeded after {Attempts} attempt(s) following a write conflict (last-write-wins).",
                        canonical, attempt);
                }

                return MappingResult.Ok();
            }
            catch (DbUpdateConcurrencyException ex)
            {
                // Record every conflict so silent retries don't hide contention.
                MappingMetrics.RecordConflict(canonical);
                lastConflict = ex;

                if (attempt < MaxAssignAttempts)
                {
                    logger.LogWarning(
                        "Write conflict assigning mappings for {Keyboard} on attempt {Attempt}; reloading and retrying (last-write-wins).",
                        canonical, attempt);
                }
            }
        }

        // Every attempt hit a write conflict — give up and let the client retry.
        logger.LogError(
            lastConflict,
            "Abandoning assign for {Keyboard} after {Attempts} attempts due to sustained write contention.",
            canonical, MaxAssignAttempts);
        return MappingResult.WriteConflict();
    }

    private static (List<string> Errors, List<(byte From, byte To)> Parsed) Validate(
        AssignMappingsRequest request)
    {
        var errors = new List<string>();
        var parsed = new List<(byte From, byte To)>();

        // Minimal-API validation (see AssignMappingsRequest) already rejects a null/missing Mappings array and any
        // null element at the boundary, so this method can assume a non-null list of non-null entries.

        // A keyboard has a fixed number of keys, so more mappings than keys is necessarily invalid; reject up
        // front rather than iterating an unbounded array (DoS guard).
        if (request.Mappings.Count > HidCatalog.All.Count)
        {
            errors.Add($"Too many mappings: at most {HidCatalog.All.Count} are allowed.");
            return (errors, parsed);
        }

        var seenSources = new HashSet<byte>();

        for (var i = 0; i < request.Mappings.Count; i++)
        {
            var entry = request.Mappings[i];

            var fromOk = HidCatalog.TryParseCode(entry.From, out var from);
            if (!fromOk)
            {
                errors.Add($"mappings[{i}].from: '{entry.From}' is not a valid HID key on this keyboard.");
            }

            var toOk = HidCatalog.TryParseCode(entry.To, out var to);
            if (!toOk)
            {
                errors.Add($"mappings[{i}].to: '{entry.To}' is not a valid HID key on this keyboard.");
            }

            if (!fromOk || !toOk)
            {
                continue;
            }

            if (!seenSources.Add(from))
            {
                errors.Add($"mappings[{i}].from: key {HidCatalog.Find(from)!.Hex} is remapped more than once.");
                continue;
            }

            parsed.Add((from, to));
        }

        return (errors, parsed);
    }

    private static KeyDto ToDto(HidKey key) => new(key.Code, key.Hex, key.Name);
}
