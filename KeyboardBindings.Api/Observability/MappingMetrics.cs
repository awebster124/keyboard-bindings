using System.Diagnostics.Metrics;

namespace KeyboardBindings.Api.Observability;

/// <summary>
/// One metric: the count of optimistic-concurrency conflicts resolved as last-write-wins. A lightweight static
/// Meter, observable with `dotnet-counters` and no DI wiring.
/// </summary>
public static class MappingMetrics
{
    public const string MeterName = "KeyboardBindings.Mappings";
    public const string ConflictsInstrument = "keyboard_bindings.assign.conflicts";

    private static readonly Meter Meter = new(MeterName);

    private static readonly Counter<long> Conflicts = Meter.CreateCounter<long>(
        ConflictsInstrument, unit: "{conflict}",
        description: "Optimistic-concurrency conflicts resolved as last-write-wins.");

    public static void RecordConflict(string keyboard) =>
        Conflicts.Add(1, new KeyValuePair<string, object?>("keyboard", keyboard));
}
