namespace Shelf.Core.SeenSet;

/// <summary>
/// Per-domain seen-set backed by bloom filters.
/// Each domain gets its own bloom filter stored as binary + metadata JSON.
/// </summary>
public sealed class SeenStore
{
    private const int DefaultExpectedItems = 10_000;
    private const double DefaultFalsePositiveRate = 0.01;

    private readonly string _seenDir;
    private readonly Dictionary<string, BloomFilter> _filters = new(StringComparer.OrdinalIgnoreCase);

    public SeenStore(string seenDir)
    {
        _seenDir = seenDir;
    }

    /// <summary>
    /// Check if an item has been seen in a domain. Returns true if probably seen.
    /// </summary>
    public bool Check(string domain, string item)
    {
        var filter = GetOrLoadFilter(domain);
        return filter?.Contains(item) ?? false;
    }

    /// <summary>
    /// Mark an item as seen in a domain. Returns true if it was already seen.
    /// </summary>
    public bool Add(string domain, string item)
    {
        var filter = GetOrCreateFilter(domain);
        var alreadySeen = filter.Contains(item);
        if (!alreadySeen)
        {
            filter.Add(item);
        }
        return alreadySeen;
    }

    /// <summary>
    /// Save all modified filters to disk.
    /// </summary>
    public void Save()
    {
        foreach (var (domain, filter) in _filters)
        {
            var (bloomPath, metaPath) = GetPaths(domain);
            filter.Save(bloomPath, metaPath);
        }
    }

    /// <summary>
    /// Get stats for a domain's bloom filter.
    /// </summary>
    public (int Count, double FillRatio, double EstimatedFPR)? GetStats(string domain)
    {
        var filter = GetOrLoadFilter(domain);
        if (filter is null)
            return null;

        return (filter.Count, filter.FillRatio(), filter.EstimatedFalsePositiveRate());
    }

    /// <summary>
    /// List all domains that have seen-sets.
    /// </summary>
    public IReadOnlyList<string> ListDomains()
    {
        if (!Directory.Exists(_seenDir))
            return [];

        return Directory.EnumerateFiles(_seenDir, "*.bloom")
            .Select(Path.GetFileNameWithoutExtension)
            .Where(n => n is not null)
            .Select(n => n!)
            .ToList();
    }

    private BloomFilter? GetOrLoadFilter(string domain)
    {
        if (_filters.TryGetValue(domain, out var cached))
            return cached;

        var (bloomPath, metaPath) = GetPaths(domain);
        var loaded = BloomFilter.Load(bloomPath, metaPath);
        if (loaded is not null)
            _filters[domain] = loaded;

        return loaded;
    }

    private BloomFilter GetOrCreateFilter(string domain)
    {
        var existing = GetOrLoadFilter(domain);
        if (existing is not null)
            return existing;

        var filter = new BloomFilter(DefaultExpectedItems, DefaultFalsePositiveRate);
        _filters[domain] = filter;
        return filter;
    }

    private (string BloomPath, string MetaPath) GetPaths(string domain)
    {
        return (
            Path.Combine(_seenDir, $"{domain}.bloom"),
            Path.Combine(_seenDir, $"{domain}.json")
        );
    }
}
