using KeyboardBindings.Api.Domain;
using KeyboardBindings.Api.Hid;
using Microsoft.EntityFrameworkCore;

namespace KeyboardBindings.Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<KeyMapping> KeyMappings => Set<KeyMapping>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<KeyMapping>(entity =>
        {
            entity.HasKey(m => m.Id);
            entity.Property(m => m.KeyboardName).IsRequired();

            // A physical key appears at most once per keyboard.
            entity.HasIndex(m => new { m.KeyboardName, m.PhysicalCode }).IsUnique();

            // Concurrency token column; the write path that stamps and checks it is added later.
            entity.Property(m => m.Version).IsConcurrencyToken();

            // Seed identity mappings at migration time so the table is complete up front, with no seeding race.
            entity.HasData(BuildSeedData());
        });
    }

    /// <summary>
    /// Identity-mapping seed for every (keyboard, key) pair. Id and Version are derived (not random) so the
    /// values stay constant across model builds.
    /// </summary>
    private static IEnumerable<KeyMapping> BuildSeedData()
    {
        for (var k = 0; k < SupportedKeyboards.All.Count; k++)
        {
            foreach (var key in HidCatalog.All)
            {
                yield return new KeyMapping
                {
                    Id = (k * 256) + key.Code + 1,
                    KeyboardName = SupportedKeyboards.All[k],
                    PhysicalCode = key.Code,
                    MappedCode = key.Code,
                    Version = DeterministicVersion(k, key.Code)
                };
            }
        }
    }

    private static Guid DeterministicVersion(int keyboardIndex, byte code) =>
        new(keyboardIndex, 0, 0, [0, 0, 0, 0, 0, 0, 0, code]);
}
