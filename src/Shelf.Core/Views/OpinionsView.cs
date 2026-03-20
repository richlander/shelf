using Markout;

namespace Shelf.Core.Views;

[MarkoutSerializable(TitleProperty = nameof(Title))]
public class OpinionsView
{
    [MarkoutIgnore] public string Title { get; set; } = "opinions";

    [MarkoutSection(Name = "Preferences")]
    public List<OpinionRow>? Preferences { get; set; }
}

[MarkoutSerializable]
public record OpinionRow
{
    public string Item { get; init; } = "";
    public string Opinion { get; init; } = "";
    public string? Reason { get; init; }
    public string Date { get; init; } = "";
}
