using Shelf.Core.Relationships;

namespace Shelf.Core.Storage;

/// <summary>
/// One-time migration from single-file storage to per-source sharded files.
/// </summary>
public static class ShelfMigration
{
    public static void MigrateIfNeeded(string dataDir)
    {
        MigrateFile(
            Path.Combine(dataDir, "items.md"),
            Path.Combine(dataDir, "items"),
            sourceColumnIndex: 6); // id, name, type, domain, keywords, url, source, date_added

        MigrateFile(
            Path.Combine(dataDir, "relationships.md"),
            Path.Combine(dataDir, "relationships"),
            sourceColumnIndex: 4); // subject, verb, target, reason, source, date_added
    }

    private static void MigrateFile(string legacyPath, string shardDir, int sourceColumnIndex)
    {
        if (!File.Exists(legacyPath))
            return;

        // Already migrated
        if (File.Exists(legacyPath + ".bak"))
            return;

        // If shard dir already has content, don't migrate
        if (Directory.Exists(shardDir) && Directory.EnumerateFiles(shardDir, "*.md").Any())
            return;

        var (headers, rows) = MarkdownTableStore.Read(legacyPath);
        if (headers.Length == 0)
            return;

        Directory.CreateDirectory(shardDir);

        // Partition rows by source column
        var partitions = new Dictionary<string, List<string[]>>(StringComparer.OrdinalIgnoreCase);
        foreach (var row in rows)
        {
            var source = row.Length > sourceColumnIndex && !string.IsNullOrWhiteSpace(row[sourceColumnIndex])
                ? row[sourceColumnIndex]
                : Sources.Journal;

            if (!partitions.TryGetValue(source, out var list))
            {
                list = [];
                partitions[source] = list;
            }

            list.Add(row);
        }

        // Write each partition to its shard file
        foreach (var (source, partitionRows) in partitions)
        {
            var shardPath = Path.Combine(shardDir, $"{source}.md");
            MarkdownTableStore.Write(shardPath, headers, partitionRows);
        }

        // Rename legacy file to .bak
        File.Move(legacyPath, legacyPath + ".bak");
    }
}
