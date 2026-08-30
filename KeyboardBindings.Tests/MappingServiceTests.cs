using KeyboardBindings.Api.Contracts;
using KeyboardBindings.Api.Data;
using KeyboardBindings.Api.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace KeyboardBindings.Tests;

/// <summary>
/// Exercises the service against a real (in-memory) SQLite database, so the EF
/// mapping, unique index, and seeding logic are all covered end-to-end.
/// </summary>
public class MappingServiceTests : IDisposable
{
    private const string Keyboard = "Apex Pro Gen 3";
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<AppDbContext> _options;

    public MappingServiceTests()
    {
        // A shared in-memory database lives only as long as the connection is open, so keep one open for the test.
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        _options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .Options;

        using var db = new AppDbContext(_options);
        db.Database.Migrate(); // applies schema + identity-mapping seed data
    }

    private AppDbContext NewContext() => new(_options);

    private static MappingService Service(AppDbContext ctx) => TestFactory.Service(ctx);

    private async Task<MappingResult> Assign(params (string from, string to)[] maps)
    {
        await using var db = NewContext();
        var service = Service(db);
        var request = new AssignMappingsRequest(maps.Select(m => new RemapDto(m.from, m.to)).ToList());
        return await service.AssignMappingsAsync(Keyboard, request);
    }

    [Fact]
    public async Task GetMappings_ReturnsAllKeysAsIdentity_WhenNothingRemapped()
    {
        await using var db = NewContext();
        var service = Service(db);

        var (status, response) = await service.GetMappingsAsync(Keyboard);

        Assert.Equal(MappingStatus.Success, status);
        Assert.Equal(92, response!.Mappings.Count);
        Assert.All(response.Mappings, m =>
        {
            Assert.False(m.IsRemapped);
            Assert.Equal(m.PhysicalKey.Code, m.MappedKey.Code);
        });
    }

    [Fact]
    public async Task Assign_RemapsRequestedKey_AndPersists()
    {
        // A (0x04) -> Z (0x1D)
        var result = await Assign(("0x04", "0x1D"));
        Assert.True(result.IsSuccess);

        await using var db = NewContext();
        var service = Service(db);
        var (_, response) = await service.GetMappingsAsync(Keyboard);

        var a = response!.Mappings.Single(m => m.PhysicalKey.Code == 0x04);
        Assert.True(a.IsRemapped);
        Assert.Equal(0x1D, a.MappedKey.Code);
        Assert.Equal("Z", a.MappedKey.Name);

        // Everything else stays identity.
        Assert.Equal(1, response.Mappings.Count(m => m.IsRemapped));
    }

    [Fact]
    public async Task Assign_IsFullReplacement_ClearingPriorRemaps()
    {
        await Assign(("0x04", "0x1D"));           // A -> Z
        await Assign(("0x21", "0x1F"));           // 4 -> 2 (A should reset to identity)

        await using var db = NewContext();
        var (_, response) = await Service(db).GetMappingsAsync(Keyboard);

        Assert.False(response!.Mappings.Single(m => m.PhysicalKey.Code == 0x04).IsRemapped);
        Assert.True(response.Mappings.Single(m => m.PhysicalKey.Code == 0x21).IsRemapped);
    }

    [Fact]
    public async Task Assign_EmptyRequest_ResetsToAllIdentity()
    {
        await Assign(("0x04", "0x1D"));
        var reset = await Assign();  // empty
        Assert.True(reset.IsSuccess);

        await using var db = NewContext();
        var (_, response) = await Service(db).GetMappingsAsync(Keyboard);
        Assert.DoesNotContain(response!.Mappings, m => m.IsRemapped);
    }

    [Fact]
    public async Task Assign_RejectsUnknownSourceKey()
    {
        var result = await Assign(("0xFF", "0x1D"));
        Assert.Equal(MappingStatus.ValidationFailed, result.Status);
        Assert.Contains(result.Errors, e => e.Contains("from"));
    }

    [Fact]
    public async Task Assign_RejectsDuplicateSourceKey()
    {
        var result = await Assign(("0x04", "0x1D"), ("0x04", "0x1F"));
        Assert.Equal(MappingStatus.ValidationFailed, result.Status);
        Assert.Contains(result.Errors, e => e.Contains("more than once"));
    }

    [Fact]
    public async Task Assign_RejectsTooManyMappings()
    {
        // More mappings than the keyboard has keys is rejected up front.
        var tooMany = Enumerable.Range(0, 500).Select(_ => ("0x04", "0x1D")).ToArray();
        var result = await Assign(tooMany);

        Assert.Equal(MappingStatus.ValidationFailed, result.Status);
        Assert.Contains(result.Errors, e => e.Contains("Too many mappings"));
    }

    [Fact]
    public async Task Assign_RejectsNullMappingEntry()
    {
        // Regression: a null element used to throw NullReferenceException.
        await using var db = NewContext();
        var request = new AssignMappingsRequest([null!, new RemapDto("0x04", "0x1D")]);

        var result = await Service(db).AssignMappingsAsync(Keyboard, request);

        Assert.Equal(MappingStatus.ValidationFailed, result.Status);
        Assert.Contains(result.Errors, e => e.Contains("mappings[0]"));
    }

    [Fact]
    public async Task Assign_RecreatesMissingRow_InsteadOfThrowing()
    {
        // Regression: the write path used a raw dictionary indexer, so a missing row (e.g. a keyboard never seeded
        // by a migration) threw KeyNotFoundException while the read path tolerated it.
        await using (var seed = NewContext())
        {
            var row = await seed.KeyMappings.FirstAsync(m => m.PhysicalCode == 0x04);
            seed.KeyMappings.Remove(row);
            await seed.SaveChangesAsync();
        }

        await using (var db = NewContext())
        {
            var result = await Service(db).AssignMappingsAsync(
                Keyboard, new AssignMappingsRequest([new RemapDto("0x04", "0x1D")]));
            Assert.True(result.IsSuccess);
        }

        // The row is restored and carries the requested mapping.
        await using var check = NewContext();
        var (_, response) = await Service(check).GetMappingsAsync(Keyboard);
        var a = response!.Mappings.Single(m => m.PhysicalKey.Code == 0x04);
        Assert.Equal(0x1D, a.MappedKey.Code);
        Assert.True(a.IsRemapped);
    }

    [Fact]
    public async Task Assign_RejectsWholeRequest_WhenAnyEntryInvalid()
    {
        // First entry is valid, second is not: nothing should be persisted.
        var result = await Assign(("0x04", "0x1D"), ("0x04", "banana"));
        Assert.Equal(MappingStatus.ValidationFailed, result.Status);

        await using var db = NewContext();
        var (_, response) = await Service(db).GetMappingsAsync(Keyboard);
        Assert.DoesNotContain(response!.Mappings, m => m.IsRemapped);
    }

    [Fact]
    public async Task UnknownKeyboard_IsReported()
    {
        await using var db = NewContext();
        var service = Service(db);

        var (status, _) = await service.GetMappingsAsync("Logitech G915");
        Assert.Equal(MappingStatus.KeyboardNotFound, status);

        var assign = await service.AssignMappingsAsync(
            "Logitech G915", new AssignMappingsRequest([]));
        Assert.Equal(MappingStatus.KeyboardNotFound, assign.Status);
    }

    [Fact]
    public async Task KeyboardName_IsCaseInsensitive()
    {
        var (status, response) = await Service(NewContext())
            .GetMappingsAsync("apex pro gen 3");
        Assert.Equal(MappingStatus.Success, status);
        Assert.Equal("Apex Pro Gen 3", response!.Keyboard);
    }

    public void Dispose() => _connection.Dispose();
}
