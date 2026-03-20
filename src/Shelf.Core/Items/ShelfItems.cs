using Shelf.Core.Relationships;

namespace Shelf.Core.Items;

/// <summary>
/// Composite item store that shards items by source (book).
/// Each source gets its own file: items/journal.md, items/musicbrainz.md, etc.
/// </summary>
public sealed class ShelfItems
{
    private readonly string _itemsDir;
    private readonly Dictionary<string, ItemStore> _shards = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _dirty = new(StringComparer.OrdinalIgnoreCase);
    private bool _allLoaded;

    public ShelfItems(string itemsDir)
    {
        _itemsDir = itemsDir;
    }

    public int Count
    {
        get
        {
            EnsureAllLoaded();
            return _shards.Values.Sum(s => s.Count);
        }
    }

    /// <summary>
    /// Find an item by ID across all shards. Prefers journal shard.
    /// </summary>
    public Item? Get(string id)
    {
        EnsureAllLoaded();

        // Check journal first (most common for interactive use)
        if (_shards.TryGetValue(Sources.Journal, out var journal))
        {
            var item = journal.Get(id);
            if (item is not null)
                return item;
        }

        foreach (var (source, store) in _shards)
        {
            if (string.Equals(source, Sources.Journal, StringComparison.OrdinalIgnoreCase))
                continue;
            var item = store.Get(id);
            if (item is not null)
                return item;
        }

        return null;
    }

    /// <summary>
    /// Get all items with a given ID across all shards (for multi-source query).
    /// </summary>
    public IReadOnlyList<Item> GetAllById(string id)
    {
        EnsureAllLoaded();
        var results = new List<Item>();
        foreach (var store in _shards.Values)
        {
            var item = store.Get(id);
            if (item is not null)
                results.Add(item);
        }
        return results;
    }

    public IReadOnlyList<Item> GetAll()
    {
        EnsureAllLoaded();
        return _shards.Values.SelectMany(s => s.GetAll()).ToList();
    }

    public IReadOnlyList<Item> GetByDomain(string domain)
    {
        EnsureAllLoaded();
        return _shards.Values.SelectMany(s => s.GetByDomain(domain)).ToList();
    }

    /// <summary>
    /// List all source (book) names that have items.
    /// </summary>
    public IReadOnlyList<string> ListBooks()
    {
        EnsureAllLoaded();
        return [.. _shards.Keys.OrderBy(k => k, StringComparer.OrdinalIgnoreCase)];
    }

    public Item Put(string name, string type, string domain, string? keywords = null, string? url = null, string? source = null)
    {
        source ??= Sources.Journal;
        var store = GetOrCreateShard(source);
        var item = store.Put(name, type, domain, keywords, url, source);
        _dirty.Add(source);
        return item;
    }

    public (Item Item, bool Created) GetOrCreate(string name, string type, string domain, string? source = null)
    {
        // Search all shards for existing item
        var existing = Get(name) ?? Get(ItemStore.Canonicalize(name));
        if (existing is not null)
            return (existing, false);

        // Create in the specified shard (default: journal)
        var item = Put(name, type, domain, source: source);
        return (item, true);
    }

    public bool Remove(string nameOrId)
    {
        EnsureAllLoaded();
        var removed = false;
        foreach (var (source, store) in _shards)
        {
            if (store.Remove(nameOrId))
            {
                _dirty.Add(source);
                removed = true;
            }
        }
        return removed;
    }

    /// <summary>
    /// Remove all items from a source. Deletes the shard file.
    /// </summary>
    public int RemoveBySource(string source)
    {
        var path = Path.Combine(_itemsDir, $"{source}.md");
        if (!File.Exists(path))
            return 0;

        var store = GetOrLoadShard(source);
        var count = store.Count;

        _shards.Remove(source);
        _dirty.Remove(source);
        File.Delete(path);

        return count;
    }

    public void Save()
    {
        foreach (var source in _dirty)
        {
            if (_shards.TryGetValue(source, out var store))
                store.Save();
        }
        _dirty.Clear();
    }

    /// <summary>
    /// Get per-shard counts for status display.
    /// </summary>
    public IReadOnlyList<(string Source, int Count)> GetShardCounts()
    {
        EnsureAllLoaded();
        return _shards
            .Where(kv => kv.Value.Count > 0)
            .OrderBy(kv => kv.Key, StringComparer.OrdinalIgnoreCase)
            .Select(kv => (kv.Key, kv.Value.Count))
            .ToList();
    }

    private ItemStore GetOrLoadShard(string source)
    {
        if (_shards.TryGetValue(source, out var cached))
            return cached;

        var path = Path.Combine(_itemsDir, $"{source}.md");
        var store = new ItemStore(path);
        _shards[source] = store;
        return store;
    }

    private ItemStore GetOrCreateShard(string source)
    {
        return GetOrLoadShard(source);
    }

    private void EnsureAllLoaded()
    {
        if (_allLoaded) return;

        if (Directory.Exists(_itemsDir))
        {
            foreach (var file in Directory.EnumerateFiles(_itemsDir, "*.md"))
            {
                var source = Path.GetFileNameWithoutExtension(file);
                GetOrLoadShard(source);
            }
        }

        _allLoaded = true;
    }
}
