using KeyboardBindings.Api.Domain;
using KeyboardBindings.Api.Hid;
using Microsoft.EntityFrameworkCore;

namespace KeyboardBindings.Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    /// <summary>Maximum stored length of <see cref="KeyMapping.KeyboardName"/>; bounded at the database by a CHECK constraint.</summary>
    public const int KeyboardNameMaxLength = 100;

    public DbSet<KeyMapping> KeyMappings => Set<KeyMapping>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<KeyMapping>(entity =>
        {
            entity.HasKey(m => m.Id);

            // Keyboard names come from the SupportedKeyboards whitelist (longest today is 14 chars), so 100 is a
            // generous ceiling for any real product name while keeping the column bounded rather than unlimited TEXT.
            entity.Property(m => m.KeyboardName).IsRequired().HasMaxLength(KeyboardNameMaxLength);
            entity.ToTable(t => t.HasCheckConstraint(
                "CK_KeyMappings_KeyboardName_MaxLength",
                $"length(\"KeyboardName\") <= {KeyboardNameMaxLength}"));

            // A physical key appears at most once per keyboard.
            entity.HasIndex(m => new { m.KeyboardName, m.PhysicalCode }).IsUnique();

            // Optimistic concurrency: EF checks the original Version in the UPDATE's WHERE clause, so a row changed
            // by another writer since our read updates 0 rows.
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

    // Override the widest overloads: the parameterless forms delegate to these, so a direct SaveChanges(false)
    // can't slip through unstamped.
    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        StampVersions();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        StampVersions();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    /// <summary>
    /// Stamps a fresh concurrency token on each inserted/updated row (SQLite has no server-generated rowversion).
    /// The tracker keeps the original for the WHERE clause, so this only sets the new value.
    /// </summary>
    private void StampVersions()
    {
        foreach (var entry in ChangeTracker.Entries<KeyMapping>())
        {
            if (entry.State is EntityState.Added or EntityState.Modified)
            {
                entry.Entity.Version = Guid.NewGuid();
            }
        }
    }
}
