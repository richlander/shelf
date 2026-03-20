using System.IO.Hashing;
using System.Text;
using System.Text.RegularExpressions;
using Shelf.Core.Storage;

namespace Shelf.Core.Items;

/// <summary>
/// Manages items backed by a markdown table.
/// </summary>
public sealed class ItemStore
{
    private static readonly string[] Headers = ["id", "name", "type", "domain", "keywords", "date_added"];
    private readonly string _filePath;
    private readonly Dictionary<string, Item> _items = new(StringComparer.OrdinalIgnoreCase);

    public ItemStore(string filePath)
    {
        _filePath = filePath;
        Load();
    }

    public int Count => _items.Count;

    public Item? Get(string id) =>
        _items.TryGetValue(id, out var item) ? item
        : _items.TryGetValue(Canonicalize(id), out item) ? item
        : null;

    public Item? FindByName(string name) =>
        _items.Values.FirstOrDefault(i =>
            string.Equals(i.Name, name, StringComparison.OrdinalIgnoreCase));

    public IReadOnlyList<Item> GetAll() => [.. _items.Values];

    public IReadOnlyList<Item> GetByDomain(string domain) =>
        _items.Values.Where(i =>
            string.Equals(i.Domain, domain, StringComparison.OrdinalIgnoreCase)).ToList();

    public Item Put(string name, string type, string domain, string? keywords = null)
    {
        var id = Canonicalize(name);
        var item = new Item(id, name, type, domain, keywords ?? "", DateTime.UtcNow.ToString("yyyy-MM-dd"));

        _items[id] = item;
        return item;
    }

    public bool Remove(string nameOrId)
    {
        // Try exact match first (already an ID), then canonicalize (a display name)
        if (_items.Remove(nameOrId))
            return true;
        return _items.Remove(Canonicalize(nameOrId));
    }

    /// <summary>
    /// Gets or creates an item. Returns (item, wasCreated).
    /// </summary>
    public (Item Item, bool Created) GetOrCreate(string name, string type, string domain)
    {
        var id = Canonicalize(name);
        if (_items.TryGetValue(id, out var existing))
            return (existing, false);

        var item = Put(name, type, domain);
        return (item, true);
    }

    public void Save()
    {
        var rows = _items.Values
            .OrderBy(i => i.Domain, StringComparer.OrdinalIgnoreCase)
            .ThenBy(i => i.Name, StringComparer.OrdinalIgnoreCase)
            .Select(static i => new[] { i.Id, i.Name, i.Type, i.Domain, i.Keywords, i.DateAdded });

        MarkdownTableStore.Write(_filePath, Headers, rows);
    }

    private const int MaxSlugLength = 30;

    public static string Canonicalize(string name)
    {
        // Strip non-alphanumeric (keep spaces and hyphens), collapse whitespace, lowercase
        var slug = Regex.Replace(name.Trim(), @"[^a-zA-Z0-9\s-]", "");
        slug = Regex.Replace(slug, @"[\s-]+", "-").Trim('-').ToLowerInvariant();

        if (slug.Length <= MaxSlugLength)
            return slug;

        // Truncate at word boundary, append 4-char hash for uniqueness
        var truncated = slug[..MaxSlugLength];
        var lastHyphen = truncated.LastIndexOf('-');
        if (lastHyphen > MaxSlugLength / 2)
            truncated = truncated[..lastHyphen];

        var hash = XxHash64.HashToUInt64(Encoding.UTF8.GetBytes(slug));
        return $"{truncated}-{hash & 0xFFFF:x4}";
    }

    private void Load()
    {
        var (_, rows) = MarkdownTableStore.Read(_filePath);
        foreach (var row in rows)
        {
            if (row.Length < 4)
                continue;

            var id = row[0];
            if (string.IsNullOrWhiteSpace(id))
                continue;

            var item = new Item(
                id,
                row.Length > 1 ? row[1] : id,
                row.Length > 2 ? row[2] : "",
                row.Length > 3 ? row[3] : "",
                row.Length > 4 ? row[4] : "",
                row.Length > 5 ? row[5] : "");

            _items.TryAdd(id, item);
        }
    }
}
