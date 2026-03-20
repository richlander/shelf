using System.CommandLine;
using Markout;
using Shelf.Core;
using Shelf.Core.Items;
using Shelf.Core.Relationships;
using Shelf.Core.SeenSet;
using Shelf.Core.Views;

var rootCommand = new RootCommand("shelf — personal knowledge graph for preferences, relationships, and seen-state");

// ============================================================
// put — add an item
// ============================================================
{
    var nameArg = new Argument<string>("name") { Description = "Item name" };
    var typeOpt = new Option<string>("--type") { Description = "Item type (artist, repo, article, topic, etc.)", DefaultValueFactory = _ => "item" };
    var domainOpt = new Option<string>("--domain") { Description = "Domain (music, hn, github, etc.)", DefaultValueFactory = _ => "general" };
    var keywordsOpt = new Option<string?>("--keywords") { Description = "Comma-separated keywords" };

    var cmd = new Command("put", "Add or update an item") { nameArg, typeOpt, domainOpt, keywordsOpt };
    cmd.SetAction((pr) =>
    {
        ShelfPaths.EnsureDirectories();
        var store = new ItemStore(ShelfPaths.ItemsFile);

        var name = pr.GetValue(nameArg)!;
        var item = store.Put(name, pr.GetValue(typeOpt)!, pr.GetValue(domainOpt)!, pr.GetValue(keywordsOpt));
        store.Save();

        Console.WriteLine($"put {item.Id} ({item.Type}, {item.Domain})");
    });
    rootCommand.Subcommands.Add(cmd);
}

// ============================================================
// like — express positive preference
// ============================================================
{
    var idArg = new Argument<string>("id") { Description = "Item name or ID" };
    var reasonOpt = new Option<string?>("--reason") { Description = "Why you like it" };
    var typeOpt = new Option<string>("--type") { Description = "Item type if auto-creating", DefaultValueFactory = _ => "item" };
    var domainOpt = new Option<string>("--domain") { Description = "Domain if auto-creating", DefaultValueFactory = _ => "general" };

    var cmd = new Command("like", "Record that you like something") { idArg, reasonOpt, typeOpt, domainOpt };
    cmd.SetAction((pr) =>
    {
        ShelfPaths.EnsureDirectories();
        var items = new ItemStore(ShelfPaths.ItemsFile);
        var rels = new RelationshipStore(ShelfPaths.RelationshipsFile);

        var name = pr.GetValue(idArg)!;
        var (item, created) = items.GetOrCreate(name, pr.GetValue(typeOpt)!, pr.GetValue(domainOpt)!);
        if (created) items.Save();

        // Remove any existing like/dislike for this item, then add
        rels.RemoveBySubjectAndVerb(item.Id, Verbs.Likes);
        rels.RemoveBySubjectAndVerb(item.Id, Verbs.Dislikes);
        rels.Add(item.Id, Verbs.Likes, reason: pr.GetValue(reasonOpt));
        rels.Save();

        Console.WriteLine($"likes {item.Id}{(created ? " (created)" : "")}");
    });
    rootCommand.Subcommands.Add(cmd);
}

// ============================================================
// dislike — express negative preference
// ============================================================
{
    var idArg = new Argument<string>("id") { Description = "Item name or ID" };
    var reasonOpt = new Option<string?>("--reason") { Description = "Why you dislike it" };
    var typeOpt = new Option<string>("--type") { Description = "Item type if auto-creating", DefaultValueFactory = _ => "item" };
    var domainOpt = new Option<string>("--domain") { Description = "Domain if auto-creating", DefaultValueFactory = _ => "general" };

    var cmd = new Command("dislike", "Record that you dislike something") { idArg, reasonOpt, typeOpt, domainOpt };
    cmd.SetAction((pr) =>
    {
        ShelfPaths.EnsureDirectories();
        var items = new ItemStore(ShelfPaths.ItemsFile);
        var rels = new RelationshipStore(ShelfPaths.RelationshipsFile);

        var name = pr.GetValue(idArg)!;
        var (item, created) = items.GetOrCreate(name, pr.GetValue(typeOpt)!, pr.GetValue(domainOpt)!);
        if (created) items.Save();

        rels.RemoveBySubjectAndVerb(item.Id, Verbs.Likes);
        rels.RemoveBySubjectAndVerb(item.Id, Verbs.Dislikes);
        rels.Add(item.Id, Verbs.Dislikes, reason: pr.GetValue(reasonOpt));
        rels.Save();

        Console.WriteLine($"dislikes {item.Id}{(created ? " (created)" : "")}");
    });
    rootCommand.Subcommands.Add(cmd);
}

// ============================================================
// link — create a relationship between two items
// ============================================================
{
    var subjectArg = new Argument<string>("subject") { Description = "Source item name or ID" };
    var targetArg = new Argument<string>("target") { Description = "Target item name or ID" };
    var verbOpt = new Option<string>("--verb") { Description = "Relationship type", DefaultValueFactory = _ => Verbs.SimilarTo };
    var reasonOpt = new Option<string?>("--reason") { Description = "Why they're related" };

    var cmd = new Command("link", "Create a relationship between two items") { subjectArg, targetArg, verbOpt, reasonOpt };
    cmd.SetAction((pr) =>
    {
        ShelfPaths.EnsureDirectories();
        var items = new ItemStore(ShelfPaths.ItemsFile);
        var rels = new RelationshipStore(ShelfPaths.RelationshipsFile);

        var subjectName = pr.GetValue(subjectArg)!;
        var targetName = pr.GetValue(targetArg)!;

        var subject = items.Get(subjectName);
        var target = items.Get(targetName);

        if (subject is null)
        {
            Console.Error.WriteLine($"item not found: {subjectName}");
            return;
        }
        if (target is null)
        {
            Console.Error.WriteLine($"item not found: {targetName}");
            return;
        }

        rels.Add(subject.Id, pr.GetValue(verbOpt)!, target.Id, pr.GetValue(reasonOpt));
        rels.Save();

        Console.WriteLine($"{subject.Name} {pr.GetValue(verbOpt)} {target.Name}");
    });
    rootCommand.Subcommands.Add(cmd);
}

// ============================================================
// query — what do I think about X?
// ============================================================
{
    var idArg = new Argument<string>("id") { Description = "Item name or ID" };

    var cmd = new Command("query", "Show everything known about an item") { idArg };
    cmd.SetAction((pr) =>
    {
        var items = new ItemStore(ShelfPaths.ItemsFile);
        var rels = new RelationshipStore(ShelfPaths.RelationshipsFile);

        var name = pr.GetValue(idArg)!;
        var item = items.Get(name);

        if (item is null)
        {
            Console.Error.WriteLine($"item not found: {name}");
            return;
        }

        var view = new QueryView
        {
            Title = item.Name,
            Type = item.Type,
            Domain = item.Domain,
            Added = item.DateAdded,
            Keywords = string.IsNullOrWhiteSpace(item.Keywords) ? null : item.Keywords,
        };

        string ResolveName(string? id) =>
            id is not null ? items.Get(id)?.Name ?? id : "";

        var itemRels = rels.GetBySubject(item.Id);
        if (itemRels.Count > 0)
        {
            view.Relationships = itemRels.Select(r => new QueryRelationshipRow
            {
                Subject = item.Name,
                Verb = r.Verb,
                Target = r.TargetId is not null ? ResolveName(r.TargetId) : null,
                Reason = r.Reason,
                Date = r.DateAdded,
            }).ToList();
        }

        var inbound = rels.GetAll()
            .Where(r => string.Equals(r.TargetId, item.Id, StringComparison.OrdinalIgnoreCase))
            .ToList();
        if (inbound.Count > 0)
        {
            view.ReferencedBy = inbound.Select(r => new QueryRelationshipRow
            {
                Subject = ResolveName(r.SubjectId),
                Verb = r.Verb,
                Target = item.Name,
                Reason = r.Reason,
                Date = r.DateAdded,
            }).ToList();
        }

        MarkoutSerializer.Serialize(view, Console.Out, ShelfMarkoutContext.Default);
    });
    rootCommand.Subcommands.Add(cmd);
}

// ============================================================
// opinions — all preferences in a domain
// ============================================================
{
    var domainOpt = new Option<string?>("--domain") { Description = "Filter by domain" };
    var verbOpt = new Option<string?>("--verb") { Description = "Filter by verb (likes, dislikes, etc.)" };

    var cmd = new Command("opinions", "List preferences") { domainOpt, verbOpt };
    cmd.SetAction((pr) =>
    {
        var items = new ItemStore(ShelfPaths.ItemsFile);
        var rels = new RelationshipStore(ShelfPaths.RelationshipsFile);

        var domain = pr.GetValue(domainOpt);
        var verb = pr.GetValue(verbOpt);

        var relevantItems = domain is not null ? items.GetByDomain(domain) : items.GetAll();
        var itemIds = relevantItems.ToDictionary(i => i.Id, StringComparer.OrdinalIgnoreCase);

        var allRels = rels.GetAll()
            .Where(r => itemIds.ContainsKey(r.SubjectId))
            .Where(r => verb is null || string.Equals(r.Verb, verb, StringComparison.OrdinalIgnoreCase))
            .Where(r => r.Verb is Verbs.Likes or Verbs.Dislikes or Verbs.Ignored)
            .ToList();

        var view = new OpinionsView
        {
            Title = domain is not null ? $"opinions — {domain}" : "opinions",
        };

        if (allRels.Count > 0)
        {
            view.Preferences = allRels
                .OrderBy(r => r.SubjectId)
                .Select(r => new OpinionRow
                {
                    Item = itemIds.TryGetValue(r.SubjectId, out var item) ? item.Name : r.SubjectId,
                    Opinion = r.Verb,
                    Reason = r.Reason,
                    Date = r.DateAdded,
                }).ToList();
        }

        MarkoutSerializer.Serialize(view, Console.Out, ShelfMarkoutContext.Default);
    });
    rootCommand.Subcommands.Add(cmd);
}

// ============================================================
// seen — check/add to per-domain seen-set
// ============================================================
{
    var valueArg = new Argument<string>("value") { Description = "URL, ID, or string to check" };
    var domainOpt = new Option<string>("--domain") { Description = "Domain for the seen-set", DefaultValueFactory = _ => "general" };
    var checkOpt = new Option<bool>("--check") { Description = "Only check, don't add" };

    var cmd = new Command("seen", "Check or mark something as seen") { valueArg, domainOpt, checkOpt };
    cmd.SetAction((pr) =>
    {
        ShelfPaths.EnsureDirectories();
        var seen = new SeenStore(ShelfPaths.SeenDir);

        var value = pr.GetValue(valueArg)!;
        var domain = pr.GetValue(domainOpt)!;
        var checkOnly = pr.GetValue(checkOpt);

        if (checkOnly)
        {
            var isSeen = seen.Check(domain, value);
            Console.WriteLine(isSeen ? "seen" : "not seen");
        }
        else
        {
            var wasSeen = seen.Add(domain, value);
            seen.Save();
            Console.WriteLine(wasSeen ? "already seen" : "marked seen");
        }
    });
    rootCommand.Subcommands.Add(cmd);
}

// ============================================================
// pull — remove an item and its relationships
// ============================================================
{
    var idArg = new Argument<string>("id") { Description = "Item name or ID" };

    var cmd = new Command("pull", "Remove an item and all its relationships") { idArg };
    cmd.SetAction((pr) =>
    {
        ShelfPaths.EnsureDirectories();
        var items = new ItemStore(ShelfPaths.ItemsFile);
        var rels = new RelationshipStore(ShelfPaths.RelationshipsFile);

        var name = pr.GetValue(idArg)!;
        var item = items.Get(name);

        if (item is null)
        {
            Console.Error.WriteLine($"item not found: {name}");
            return;
        }

        items.Remove(item.Id);
        var removed = rels.RemoveBySubject(item.Id);
        items.Save();
        rels.Save();

        Console.WriteLine($"removed {item.Name} ({removed} relationship(s))");
    });
    rootCommand.Subcommands.Add(cmd);
}

// ============================================================
// status — data directory info
// ============================================================
{
    var cmd = new Command("status", "Show data directory info");
    cmd.SetAction((_) =>
    {
        var view = new StatusView();

        view.Storage =
        [
            new() { Name = "data_dir", Value = ShelfPaths.DataDir },
            new() { Name = "items", Value = $"{ShelfPaths.ItemsFile}{(File.Exists(ShelfPaths.ItemsFile) ? "" : " (not created)")}" },
            new() { Name = "relationships", Value = $"{ShelfPaths.RelationshipsFile}{(File.Exists(ShelfPaths.RelationshipsFile) ? "" : " (not created)")}" },
            new() { Name = "seen", Value = $"{ShelfPaths.SeenDir}{(Directory.Exists(ShelfPaths.SeenDir) ? "" : " (not created)")}" },
        ];

        var counts = new List<StatusItemRow>();
        if (File.Exists(ShelfPaths.ItemsFile))
        {
            var items = new ItemStore(ShelfPaths.ItemsFile);
            counts.Add(new() { Name = "items", Value = items.Count.ToString() });
        }
        if (File.Exists(ShelfPaths.RelationshipsFile))
        {
            var rels = new RelationshipStore(ShelfPaths.RelationshipsFile);
            counts.Add(new() { Name = "relationships", Value = rels.Count.ToString() });
        }
        var seen = new SeenStore(ShelfPaths.SeenDir);
        var domains = seen.ListDomains();
        foreach (var domain in domains)
        {
            var stats = seen.GetStats(domain);
            if (stats is not null)
                counts.Add(new() { Name = $"seen ({domain})", Value = $"{stats.Value.Count} items, {stats.Value.FillRatio:P0} full, ~{stats.Value.EstimatedFPR:P2} FPR" });
        }

        if (counts.Count > 0)
            view.Counts = counts;

        MarkoutSerializer.Serialize(view, Console.Out, ShelfMarkoutContext.Default);
    });
    rootCommand.Subcommands.Add(cmd);
}

// ============================================================
// completion — generate shell completion script
// ============================================================
{
    var shellArg = new Argument<string>("shell") { Description = "Shell type (bash, zsh, fish, powershell)" };
    var cmd = new Command("completion", "Generate shell completion script") { shellArg };
    cmd.SetAction((pr) =>
    {
        ShellComplete.CompletionScripts.WriteToConsole("shelf", pr.GetValue(shellArg)!);
    });
    rootCommand.Subcommands.Add(cmd);
}

// ============================================================
// skill — print agent skill definition
// ============================================================
{
    var cmd = new Command("skill", "Print the agent skill definition");
    cmd.SetAction((_) =>
    {
        using var stream = typeof(Program).Assembly.GetManifestResourceStream("SKILL.md");
        if (stream is null)
        {
            Console.Error.WriteLine("SKILL.md not found in embedded resources");
            return;
        }

        using var reader = new StreamReader(stream);
        Console.Write(reader.ReadToEnd());
    });
    rootCommand.Subcommands.Add(cmd);
}

return rootCommand.Parse(args).Invoke();
