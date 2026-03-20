using Markout;

namespace Shelf.Core.Views;

[MarkoutSerializable(TitleProperty = nameof(Title))]
public class StatusView
{
    [MarkoutIgnore] public string Title { get; set; } = "shelf status";

    [MarkoutSection(Name = "Storage")]
    public List<StatusItemRow>? Storage { get; set; }

    [MarkoutSection(Name = "Counts")]
    public List<StatusItemRow>? Counts { get; set; }
}

[MarkoutSerializable]
public record StatusItemRow
{
    public string Name { get; init; } = "";
    public string Value { get; init; } = "";
}
