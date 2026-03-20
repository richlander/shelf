# shelf

Personal knowledge graph CLI for managing preferences,
relationships, and seen-state across domains.

## Commands

```bash
shelf put <name> --type <type> --domain <domain>    # add/update an item
shelf like <id> --reason "..."                       # positive preference
shelf dislike <id> --reason "..."                    # negative preference
shelf link <subject> <target> --verb <verb>          # relate two items
shelf seen <value> --domain <domain>                  # mark as seen (bloom filter)
shelf seen <value> --domain <domain> --check          # check without adding
shelf query <id>                                     # everything about an item
shelf opinions --domain <domain>                     # all preferences
shelf pull <id>                                      # remove item + relationships
shelf status                                         # data directory info
```

## Item types

artist, album, track, repo, article, topic, author, tool, item

## Domains

music, hn, github, general

## Relationship verbs

likes, dislikes, similar-to, ignored, presented

## Storage

Data lives in `~/.local/share/shelf/` (override with `SHELF_DATA_DIR`):

- `items.md` — markdown table of entities
- `relationships.md` — markdown table of directed edges
- `seen/<domain>.bloom` — per-domain bloom filter for seen-state
