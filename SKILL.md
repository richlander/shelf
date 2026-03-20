# shelf

Personal knowledge graph CLI for managing preferences,
relationships, and seen-state across domains.

## Commands

```bash
shelf put <name> --type <type> --domain <domain>     # add/update an item
shelf put <name> --source musicbrainz --keywords "…"  # enrichment
shelf like <id> --reason "..."                        # positive preference
shelf dislike <id> --reason "..."                     # negative preference
shelf link <subject> <target> --verb <verb>           # relate two items
shelf seen <value> --domain <domain>                  # mark as seen
shelf seen <value> --domain <domain> --check          # check without adding
shelf query <id>                                      # everything about an item
shelf opinions --domain <domain>                      # all preferences
shelf pull <id>                                       # remove item + relationships
shelf reset <source>                                  # remove a book from the shelf
shelf status                                          # data directory info
```

## Books (sources)

Each source is a book on the shelf. Your personal entries
go in your **journal** (the default). Service data goes in
reference books that can be regenerated.

- `journal` — your preferences (default, precious)
- `musicbrainz` — artist metadata, genres, geography
- `listenbrainz` — artist similarity
- `lastfm` — play counts, scrobble data

## Relationship verbs

likes, dislikes, similar-to, member-of, ignored, presented

## Storage

Data lives in `~/.local/share/shelf/` (override with
`SHELF_DATA_DIR`), sharded by source:

- `items/<source>.md` — entities per book
- `relationships/<source>.md` — edges per book
- `seen/<domain>.bloom` — per-domain bloom filter
