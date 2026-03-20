namespace Shelf.Core.Items;

/// <summary>
/// An entity in the knowledge graph.
/// </summary>
public sealed record Item(
    string Id,
    string Name,
    string Type,
    string Domain,
    string Keywords,
    string Url,
    string Source,
    string DateAdded);
