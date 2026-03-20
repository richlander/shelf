namespace Shelf.Core;

/// <summary>
/// Resolves data directory using XDG Base Directory conventions.
/// </summary>
public static class ShelfPaths
{
    private const string AppName = "shelf";

    /// <summary>
    /// Persistent user data (items, relationships, seen-set).
    /// Resolution order: SHELF_DATA_DIR → XDG_DATA_HOME/shelf → ~/.local/share/shelf
    /// </summary>
    public static string DataDir { get; } = ResolveDataDir();

    /// <summary>
    /// Directory for per-source item shard files (items/journal.md, items/musicbrainz.md, etc.).
    /// </summary>
    public static string ItemsDir => Path.Combine(DataDir, "items");

    /// <summary>
    /// Directory for per-source relationship shard files.
    /// </summary>
    public static string RelationshipsDir => Path.Combine(DataDir, "relationships");

    /// <summary>
    /// Path to a specific source's items file.
    /// </summary>
    public static string ItemsFile(string source) => Path.Combine(ItemsDir, $"{source}.md");

    /// <summary>
    /// Path to a specific source's relationships file.
    /// </summary>
    public static string RelationshipsFile(string source) => Path.Combine(RelationshipsDir, $"{source}.md");

    /// <summary>
    /// Legacy single items file (pre-sharding). Used for migration.
    /// </summary>
    public static string LegacyItemsFile => Path.Combine(DataDir, "items.md");

    /// <summary>
    /// Legacy single relationships file (pre-sharding). Used for migration.
    /// </summary>
    public static string LegacyRelationshipsFile => Path.Combine(DataDir, "relationships.md");

    /// <summary>
    /// Directory for per-domain seen-set bloom filters.
    /// </summary>
    public static string SeenDir => Path.Combine(DataDir, "seen");

    /// <summary>
    /// Ensures the data directory exists and migrates legacy files if needed.
    /// </summary>
    public static void EnsureDirectories()
    {
        Directory.CreateDirectory(DataDir);
        Directory.CreateDirectory(ItemsDir);
        Directory.CreateDirectory(RelationshipsDir);
        Directory.CreateDirectory(SeenDir);
        Storage.ShelfMigration.MigrateIfNeeded(DataDir);
    }

    private static string ResolveDataDir()
    {
        var envOverride = Environment.GetEnvironmentVariable("SHELF_DATA_DIR");
        if (envOverride is not null)
            return envOverride;

        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var xdgDataHome = Environment.GetEnvironmentVariable("XDG_DATA_HOME")
            ?? Path.Combine(home, ".local", "share");
        return Path.Combine(xdgDataHome, AppName);
    }
}
