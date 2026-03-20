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
        _items.TryGetValue(Canonicalize(id), out var item) ? item : null;

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

    public bool Remove(string id)
    {
        return _items.Remove(Canonicalize(id));
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

    public static string Canonicalize(string name) =>
        name.Trim().ToLowerInvariant().Replace(' ', '-');

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
