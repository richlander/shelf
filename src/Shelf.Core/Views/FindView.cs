using Markout;

namespace Shelf.Core.Views;

[MarkoutSerializable(TitleProperty = nameof(Title))]
public class FindView
{
    [MarkoutIgnore] public string Title { get; set; } = "find";

    [MarkoutSection(Name = "Results")]
    public List<FindRow>? Results { get; set; }
}

[MarkoutSerializable]
public record FindRow
{
    public string Name { get; init; } = "";
    public string Type { get; init; } = "";
    public string Domain { get; init; } = "";
    public string? Keywords { get; init; }
    public string? Url { get; init; }
}
