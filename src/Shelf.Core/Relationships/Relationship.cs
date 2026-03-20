namespace Shelf.Core.Relationships;

/// <summary>
/// A directed edge in the knowledge graph.
/// </summary>
public sealed record Relationship(
    string SubjectId,
    string Verb,
    string? TargetId,
    string? Reason,
    string Source,
    string DateAdded);

/// <summary>
/// Well-known relationship verbs.
/// </summary>
public static class Verbs
{
    public const string Likes = "likes";
    public const string Dislikes = "dislikes";
    public const string SimilarTo = "similar-to";
    public const string MemberOf = "member-of";
    public const string Ignored = "ignored";
    public const string Presented = "presented";
}

/// <summary>
/// Well-known data sources. User data is precious; service data is regenerable.
/// </summary>
public static class Sources
{
    public const string Journal = "journal";
    public const string MusicBrainz = "musicbrainz";
    public const string ListenBrainz = "listenbrainz";
    public const string LastFm = "lastfm";
}
