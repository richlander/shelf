using Markout;

namespace Shelf.Core.Views;

[MarkoutSerializable(TitleProperty = nameof(Title), FieldLayout = FieldLayout.Inline)]
public class QueryView
{
    [MarkoutIgnore] public string Title { get; set; } = "";

    public string Type { get; set; } = "";
    public string Domain { get; set; } = "";
    public string Source { get; set; } = "";
    public string Added { get; set; } = "";
    public string? Url { get; set; }
    public string? Keywords { get; set; }

    [MarkoutSection(Name = "Relationships")]
    public List<QueryRelationshipRow>? Relationships { get; set; }

    [MarkoutSection(Name = "Referenced By")]
    public List<QueryRelationshipRow>? ReferencedBy { get; set; }
}

[MarkoutSerializable]
public record QueryRelationshipRow
{
    public string Subject { get; init; } = "";
    public string Verb { get; init; } = "";
    public string? Target { get; init; }
    public string? Reason { get; init; }
    public string Source { get; init; } = "";
    public string Date { get; init; } = "";
}
