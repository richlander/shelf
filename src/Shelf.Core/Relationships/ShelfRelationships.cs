namespace Shelf.Core.Relationships;

/// <summary>
/// Composite relationship store that shards relationships by source (book).
/// Each source gets its own file: relationships/journal.md, relationships/musicbrainz.md, etc.
/// </summary>
public sealed class ShelfRelationships
{
    private readonly string _relationshipsDir;
    private readonly Dictionary<string, RelationshipStore> _shards = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _dirty = new(StringComparer.OrdinalIgnoreCase);
    private bool _allLoaded;

    public ShelfRelationships(string relationshipsDir)
    {
        _relationshipsDir = relationshipsDir;
    }

    public int Count
    {
        get
        {
            EnsureAllLoaded();
            return _shards.Values.Sum(s => s.Count);
        }
    }

    public IReadOnlyList<Relationship> GetAll()
    {
        EnsureAllLoaded();
        return _shards.Values.SelectMany(s => s.GetAll()).ToList();
    }

    public IReadOnlyList<Relationship> GetBySubject(string subjectId)
    {
        EnsureAllLoaded();
        return _shards.Values.SelectMany(s => s.GetBySubject(subjectId)).ToList();
    }

    public IReadOnlyList<Relationship> GetByVerb(string verb)
    {
        EnsureAllLoaded();
        return _shards.Values.SelectMany(s => s.GetByVerb(verb)).ToList();
    }

    public IReadOnlyList<Relationship> GetBySubjectAndVerb(string subjectId, string verb)
    {
        EnsureAllLoaded();
        return _shards.Values.SelectMany(s => s.GetBySubjectAndVerb(subjectId, verb)).ToList();
    }

    public Relationship Add(string subjectId, string verb, string? targetId = null, string? reason = null, string? source = null)
    {
        source ??= Sources.Journal;
        var store = GetOrCreateShard(source);
        var rel = store.Add(subjectId, verb, targetId, reason, source);
        _dirty.Add(source);
        return rel;
    }

    public int RemoveBySubject(string subjectId)
    {
        EnsureAllLoaded();
        var total = 0;
        foreach (var (source, store) in _shards)
        {
            var removed = store.RemoveBySubject(subjectId);
            if (removed > 0)
            {
                total += removed;
                _dirty.Add(source);
            }
        }
        return total;
    }

    public int RemoveBySubjectAndVerb(string subjectId, string verb)
    {
        EnsureAllLoaded();
        var total = 0;
        foreach (var (source, store) in _shards)
        {
            var removed = store.RemoveBySubjectAndVerb(subjectId, verb);
            if (removed > 0)
            {
                total += removed;
                _dirty.Add(source);
            }
        }
        return total;
    }

    /// <summary>
    /// Remove all relationships from a source. Deletes the shard file.
    /// </summary>
    public int RemoveBySource(string source)
    {
        var path = Path.Combine(_relationshipsDir, $"{source}.md");
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

    private RelationshipStore GetOrLoadShard(string source)
    {
        if (_shards.TryGetValue(source, out var cached))
            return cached;

        var path = Path.Combine(_relationshipsDir, $"{source}.md");
        var store = new RelationshipStore(path);
        _shards[source] = store;
        return store;
    }

    private RelationshipStore GetOrCreateShard(string source)
    {
        return GetOrLoadShard(source);
    }

    private void EnsureAllLoaded()
    {
        if (_allLoaded) return;

        if (Directory.Exists(_relationshipsDir))
        {
            foreach (var file in Directory.EnumerateFiles(_relationshipsDir, "*.md"))
            {
                var source = Path.GetFileNameWithoutExtension(file);
                GetOrLoadShard(source);
            }
        }

        _allLoaded = true;
    }
}
