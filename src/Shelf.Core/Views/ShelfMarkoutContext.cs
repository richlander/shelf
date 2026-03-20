using Markout;

namespace Shelf.Core.Views;

[MarkoutContext(typeof(QueryView))]
[MarkoutContext(typeof(QueryRelationshipRow))]
[MarkoutContext(typeof(OpinionsView))]
[MarkoutContext(typeof(OpinionRow))]
[MarkoutContext(typeof(FindView))]
[MarkoutContext(typeof(FindRow))]
[MarkoutContext(typeof(StatusView))]
[MarkoutContext(typeof(StatusItemRow))]
public partial class ShelfMarkoutContext : MarkoutSerializerContext { }
