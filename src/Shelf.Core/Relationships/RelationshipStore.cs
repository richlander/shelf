using Shelf.Core.Storage;

namespace Shelf.Core.Relationships;

/// <summary>
/// Manages relationships backed by a markdown table.
/// </summary>
public sealed class RelationshipStore
{
    private static readonly string[] Headers = ["subject", "verb", "target", "reason", "date_added"];
    private readonly string _filePath;
    private readonly List<Relationship> _relationships = [];

    public RelationshipStore(string filePath)
    {
        _filePath = filePath;
        Load();
    }

    public int Count => _relationships.Count;

    public IReadOnlyList<Relationship> GetAll() => _relationships;

    public IReadOnlyList<Relationship> GetBySubject(string subjectId) =>
        _relationships.Where(r =>
            string.Equals(r.SubjectId, subjectId, StringComparison.OrdinalIgnoreCase)).ToList();

    public IReadOnlyList<Relationship> GetByVerb(string verb) =>
        _relationships.Where(r =>
            string.Equals(r.Verb, verb, StringComparison.OrdinalIgnoreCase)).ToList();

    public IReadOnlyList<Relationship> GetBySubjectAndVerb(string subjectId, string verb) =>
        _relationships.Where(r =>
            string.Equals(r.SubjectId, subjectId, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(r.Verb, verb, StringComparison.OrdinalIgnoreCase)).ToList();

    public Relationship Add(string subjectId, string verb, string? targetId = null, string? reason = null)
    {
        var rel = new Relationship(subjectId, verb, targetId, reason, DateTime.UtcNow.ToString("yyyy-MM-dd"));
        _relationships.Add(rel);
        return rel;
    }

    public int RemoveBySubject(string subjectId)
    {
        return _relationships.RemoveAll(r =>
            string.Equals(r.SubjectId, subjectId, StringComparison.OrdinalIgnoreCase));
    }

    public int RemoveBySubjectAndVerb(string subjectId, string verb)
    {
        return _relationships.RemoveAll(r =>
            string.Equals(r.SubjectId, subjectId, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(r.Verb, verb, StringComparison.OrdinalIgnoreCase));
    }

    public void Save()
    {
        var rows = _relationships
            .OrderBy(r => r.SubjectId, StringComparer.OrdinalIgnoreCase)
            .ThenBy(r => r.Verb, StringComparer.OrdinalIgnoreCase)
            .Select(static r => new[] { r.SubjectId, r.Verb, r.TargetId ?? "", r.Reason ?? "", r.DateAdded });

        MarkdownTableStore.Write(_filePath, Headers, rows);
    }

    private void Load()
    {
        var (_, rows) = MarkdownTableStore.Read(_filePath);
        foreach (var row in rows)
        {
            if (row.Length < 2)
                continue;

            var subjectId = row[0];
            var verb = row[1];
            if (string.IsNullOrWhiteSpace(subjectId) || string.IsNullOrWhiteSpace(verb))
                continue;

            var rel = new Relationship(
                subjectId,
                verb,
                row.Length > 2 && !string.IsNullOrWhiteSpace(row[2]) ? row[2] : null,
                row.Length > 3 && !string.IsNullOrWhiteSpace(row[3]) ? row[3] : null,
                row.Length > 4 ? row[4] : "");

            _relationships.Add(rel);
        }
    }
}
