using KeyboardBindings.Api.Contracts;
using KeyboardBindings.Api.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace KeyboardBindings.Tests;

public class ConcurrencyTests
{
    private const string Keyboard = "Apex Pro Gen 3";

    [Fact]
    public async Task ConcurrencyToken_DetectsInterleavedWrite()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options;

        await using (var db = new AppDbContext(options))
        {
            await db.Database.MigrateAsync(); // schema + seeded identity rows
        }

        // Two readers load the same (already-seeded) row, then both try to write it.
        await using var ctxA = new AppDbContext(options);
        await using var ctxB = new AppDbContext(options);
        var a = await ctxA.KeyMappings.FirstAsync(m => m.PhysicalCode == 0x04);
        var b = await ctxB.KeyMappings.FirstAsync(m => m.PhysicalCode == 0x04);

        a.MappedCode = 0x1D;
        await ctxA.SaveChangesAsync(); // first writer wins and bumps the token

        b.MappedCode = 0x1F;
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => ctxB.SaveChangesAsync());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task AllSavePaths_StampConcurrencyToken(bool async)
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options;

        await using var db = new AppDbContext(options);
        await db.Database.MigrateAsync();

        // acceptAllChangesOnSuccess: false is the overload that previously bypassed stamping, silently disabling
        // conflict detection.
        var row = await db.KeyMappings.FirstAsync(m => m.PhysicalCode == 0x04);
        var before = row.Version;
        row.MappedCode = 0x1D;

        if (async)
        {
            await db.SaveChangesAsync(acceptAllChangesOnSuccess: false);
        }
        else
        {
            db.SaveChanges(acceptAllChangesOnSuccess: false);
        }

        Assert.NotEqual(before, row.Version);
    }

    [Fact]
    public async Task Assign_ResolvesConcurrentWrite_AsLastWriteWins_AndRecordsConflict()
    {
        var path = Path.Combine(Path.GetTempPath(), $"kb-conc-{Guid.NewGuid():N}.db");
        try
        {
            var plain = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite($"Data Source={path}").Options;

            await using (var db = new AppDbContext(plain))
            {
                await db.Database.MigrateAsync(); // schema + seeded identity rows
            }

            using var conflicts = new ConflictMeterCollector();

            // The service's context sabotages its own first save: an out-of-band writer changes key 0x04 after the
            // service reads but before it saves, guaranteeing a DbUpdateConcurrencyException on attempt 1.
            var withConflict = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite($"Data Source={path}")
                .AddInterceptors(new ConflictOnceInterceptor(plain, Keyboard, 0x04))
                .Options;

            await using (var svc = new AppDbContext(withConflict))
            {
                var result = await TestFactory.Service(svc).AssignMappingsAsync(
                    Keyboard, new AssignMappingsRequest([new RemapDto("0x04", "0x1D")]));

                Assert.True(result.IsSuccess); // last write still wins
            }

            // The assign's intended state (A -> Z) overwrites the interloper.
            await using (var check = new AppDbContext(plain))
            {
                var (_, response) = await TestFactory.Service(check).GetMappingsAsync(Keyboard);
                var a = response!.Mappings.Single(m => m.PhysicalKey.Code == 0x04);
                Assert.Equal(0x1D, a.MappedKey.Code);
            }

            // The conflict was surfaced as a metric, not silently swallowed.
            Assert.True(conflicts.Conflicts >= 1);
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }

    /// <summary>
    /// Injects exactly one out-of-band write, before the intercepted save runs, to deterministically force an
    /// optimistic-concurrency conflict.
    /// </summary>
    private sealed class ConflictOnceInterceptor(
        DbContextOptions<AppDbContext> sideOptions, string keyboard, byte physicalCode)
        : SaveChangesInterceptor
    {
        private bool _fired;

        public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData, InterceptionResult<int> result, CancellationToken ct = default)
        {
            if (!_fired)
            {
                _fired = true;
                await using var side = new AppDbContext(sideOptions);
                var row = await side.KeyMappings.FirstAsync(
                    m => m.KeyboardName == keyboard && m.PhysicalCode == physicalCode, ct);
                // Any real value change makes SaveChanges stamp a new token, invalidating the in-flight writer's.
                row.MappedCode = row.MappedCode == 0x1E ? (byte)0x1F : (byte)0x1E;
                await side.SaveChangesAsync(ct);
            }

            return await base.SavingChangesAsync(eventData, result, ct);
        }
    }
}
