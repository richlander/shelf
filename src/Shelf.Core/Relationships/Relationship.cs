namespace Shelf.Core.Relationships;

/// <summary>
/// A directed edge in the knowledge graph.
/// </summary>
public sealed record Relationship(
    string SubjectId,
    string Verb,
    string? TargetId,
    string? Reason,
    string DateAdded);

/// <summary>
/// Well-known relationship verbs.
/// </summary>
public static class Verbs
{
    public const string Likes = "likes";
    public const string Dislikes = "dislikes";
    public const string SimilarTo = "similar-to";
    public const string Ignored = "ignored";
    public const string Presented = "presented";
}
