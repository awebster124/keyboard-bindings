using System.Diagnostics.Metrics;
using KeyboardBindings.Api.Observability;
using KeyboardBindings.Api.Services;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;

namespace KeyboardBindings.Tests;

/// <summary>
/// Boots the real API against a throwaway SQLite file, so no test touches the app's real database; the temp
/// file is deleted on dispose.
/// </summary>
public class ApiTestFactory : WebApplicationFactory<Program>
{
    private readonly string _dbPath =
        Path.Combine(Path.GetTempPath(), $"kbtests-{Guid.NewGuid():N}.db");

    protected override IHost CreateHost(IHostBuilder builder)
    {
        builder.ConfigureHostConfiguration(config =>
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Default"] = $"Data Source={_dbPath}"
            }));
        return base.CreateHost(builder);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (!disposing)
        {
            return;
        }

        // SQLite pools connections (keeping the file handle open); release the pool before deleting, best-effort.
        SqliteConnection.ClearAllPools();
        try
        {
            if (File.Exists(_dbPath))
            {
                File.Delete(_dbPath);
            }
        }
        catch (IOException)
        {
            // Temp file will be reclaimed by the OS.
        }
    }
}

internal static class TestFactory
{
    public static MappingService Service(Api.Data.AppDbContext db) =>
        new(db, NullLogger<MappingService>.Instance);
}

/// <summary>Listens for the conflict counter so tests can assert it was recorded.</summary>
internal sealed class ConflictMeterCollector : IDisposable
{
    private readonly MeterListener _listener = new();
    private long _conflicts;

    public long Conflicts => Interlocked.Read(ref _conflicts);

    public ConflictMeterCollector()
    {
        _listener.InstrumentPublished = (instrument, listener) =>
        {
            if (instrument.Meter.Name == MappingMetrics.MeterName
                && instrument.Name == MappingMetrics.ConflictsInstrument)
            {
                listener.EnableMeasurementEvents(instrument);
            }
        };
        _listener.SetMeasurementEventCallback<long>(
            (_, measurement, _, _) => Interlocked.Add(ref _conflicts, measurement));
        _listener.Start();
    }

    public void Dispose() => _listener.Dispose();
}
