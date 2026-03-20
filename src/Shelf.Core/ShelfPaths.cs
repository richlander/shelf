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
    /// Path to items markdown table.
    /// </summary>
    public static string ItemsFile => Path.Combine(DataDir, "items.md");

    /// <summary>
    /// Path to relationships markdown table.
    /// </summary>
    public static string RelationshipsFile => Path.Combine(DataDir, "relationships.md");

    /// <summary>
    /// Directory for per-domain seen-set bloom filters.
    /// </summary>
    public static string SeenDir => Path.Combine(DataDir, "seen");

    /// <summary>
    /// Ensures the data directory exists.
    /// </summary>
    public static void EnsureDirectories()
    {
        Directory.CreateDirectory(DataDir);
        Directory.CreateDirectory(SeenDir);
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
