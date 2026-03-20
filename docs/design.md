# Shelf: Personal Knowledge Graph

## Pitch

Shelf is a personal knowledge graph that agents and tools write to as they work, and query when they need context. It replaces conversation memory as the durable preference store across domains.

The core insight: if the data is rich enough, recommendations fall out of queries. An agent doesn't need to be smart about music taste if the graph already encodes geography, genre, similarity, listening depth, and preference. The same applies to HN post triage, GitHub notification priority, or any domain where "what does Rich think about X?" is a useful question.

## What it replaces

Today, agent preferences live in Claude's auto-memory — fragile, not queryable, not portable, gone when the server rebuilds. Domain-specific tools (beat-track, HN triage, GitHub triage) each reinvent preference storage or don't have any.

Shelf is the shared infrastructure. Any tool can read "Rich likes dream pop from Montreal" or "Rich ignores startup drama" from one place.

## Architecture

### Items and relationships

Two markdown tables at `~/.local/share/shelf/`:

**items.md** — entities with type, domain, and keywords:

```
| id | name | type | domain | keywords | date_added |
| alvvays | Alvvays | artist | music | dream pop, indie pop, shoegaze, toronto, canada | 2026-03-20 |
| hn-12345 | Show HN: My CLI tool | article | hn | cli, developer-tools | 2026-03-20 |
```

**relationships.md** — typed directed edges:

```
| subject | verb | target | reason | date_added |
| alvvays | likes | | 634 plays, dream-pop staple | 2026-03-20 |
| cocteau-twins | similar-to | alvvays | dream-pop lineage | 2026-03-20 |
| adrianne-lenker | member-of | big-thief | lead vocals, original | 2026-03-20 |
```

### Seen-set

Per-domain bloom filters at `~/.local/share/shelf/seen/`. Fast O(1) membership test for "have I shown this URL before?" Rebuilt periodically to shed expired items.

### Library + CLI

`Shelf.Core` is a NuGet library. Other .NET tools reference it directly — no subprocess overhead, no format drift. The `shelf` CLI is one consumer of the library, used by non-.NET agents and for manual operations.

## Enrichment

The beat-track prototype demonstrated the enrichment pipeline:

1. **Last.fm** → top 100 artists by play count
2. **MusicBrainz** → origin city, country, genre tags, formation year, member-of relationships
3. **ListenBrainz** → similar-to relationships between artists
4. **User preferences** → likes, dislikes, custom similarity links

One API call per artist to MusicBrainz returns city (`begin-area`), country, genre tags (community-voted), formation year, and band membership. No Wikidata or Wikipedia needed.

Result: `shelf query alvvays` returns not just "likes" but also "Toronto, Canada, dream pop, indie pop, shoegaze, 2011, member-of." An agent can then answer "Canadian dream-pop artists Rich listens to" by scanning keywords — no LLM inference required.

### Member-of relationships

MusicBrainz tracks band membership. The enrichment script creates `member-of` edges when a band member is also a known solo artist:

- Adrianne Lenker → member-of → Big Thief (lead vocals, original)
- Morrissey → member-of → The Smiths (lead vocals, original)
- José González → member-of → Junip

This enables queries like "what other bands do members of Broken Social Scene play in?" — traversing person → member-of → band edges.

## Sharding by source and lifecycle

The prototype revealed two sharding axes:

### 1. Source: service-data vs user preferences

| Source | Durability | Example | Can regenerate? |
| --- | --- | --- | --- |
| **User** | Permanent | likes, dislikes, custom links | No — precious |
| **Service** | Reproducible | MusicBrainz metadata, genre tags, member-of | Yes — re-fetch anytime |

This suggests partitioning: `items-user.md` vs `items-musicbrainz.md`. Or a `source` column on items/relationships that enables `shelf reset --source musicbrainz` without touching user data.

### 2. Domain lifecycle

| Domain | TTL | Pattern |
| --- | --- | --- |
| **Music** | Permanent | Artists don't expire |
| **HN** | Days-weeks | `shelf pull --domain hn --older-than 7d` |
| **GitHub** | Weeks-months | Notifications acted on are done |

Per-domain files (`items-music.md`, `items-hn.md`) let each domain manage its own lifecycle. The bloom filter already does this for the seen-set.

### Implementation

The simplest path: add a `source` column to items and relationships. Sharding into separate files is an optimization that can come later — the `source` column enables the key operations (selective reset, selective backup) without changing the storage model.

## Cross-domain queries

With rich enough data, queries that span domains become natural:

- "Montreal artists I listen to" → filter items by `montréal` keyword in music domain
- "Shoegaze artists I haven't explored deeply" → cross-reference genre keyword with beat-track depth data
- "Canadian artists similar to my favorites" → traverse similar-to edges, filter by country keyword
- "HN posts about topics I care about" → match HN item keywords against music/tool interests

The graph doesn't need to understand these questions. It just needs to have the right keywords and edges. The agent composes the query.

## Integration

### beat-track

Already integrated. `KnownMisses`, `UserFavorites`, and `UserSimilarArtists` delegate to `Shelf.Core`'s `ItemStore` and `RelationshipStore`. `beat-track miss add "Coldplay"` writes to shelf. `shelf query coldplay` sees it.

### hn-triage

Before scanning: `shelf opinions --domain hn` loads the taste profile. For each post shown: `shelf seen <url> --domain hn`. When Rich reacts: `shelf like/dislike <url> --domain hn`. Next scan: `shelf seen <url> --check` skips already-shown posts.

### github-notification-triage

Same pattern. `shelf like dotnet/aspire --domain github --reason "active contributor"`. `shelf ignore dependabot --domain github`. The triage agent reads `shelf opinions --domain github` for durable preferences.

## What this changes about "enrichment"

The traditional model: raw data → analysis → recommendation layer on top.

The shelf model: raw data → rich structured graph → recommendations fall out of queries.

Enrichment isn't "add a recommendation on top of data." It's "make the data rich enough that the agent just reads the answer." Geography, genre, similarity, preference, band membership — encode it all, and "what should I listen to next?" becomes a graph traversal, not an inference task.
