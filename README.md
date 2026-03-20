# shelf

Personal knowledge graph CLI for managing preferences,
relationships, and seen-state across domains.

```bash
shelf put "Alvvays" --type artist --domain music
shelf like "Alvvays" --reason "dream-pop staple"
shelf put "Alvvays" --source musicbrainz \
  --keywords "dream pop, indie pop, toronto, canada"
shelf query "Alvvays"
```

## Install

### With dotnet-install

```bash
dotnet-install --package Shelf
```

### SDK users

```bash
dotnet tool install -g Shelf
```

### Without the SDK

```bash
curl --proto '=https' --tlsv1.2 -sSf \
  https://github.com/richlander/shelf/raw/refs/heads/main/install.sh | sh
```

Downloads a pre-built Native AOT binary (~4 MB, <1ms startup).

## Quick reference

| Command | Purpose |
| --- | --- |
| `put <name>` | Add or update an item |
| `like <id>` | Record a positive preference |
| `dislike <id>` | Record a negative preference |
| `link <subject> <target>` | Relate two items |
| `query <id>` | Everything known about an item |
| `opinions` | List preferences |
| `seen <value>` | Check or mark something as seen |
| `pull <id>` | Remove an item and its relationships |
| `reset <source>` | Remove a book from the shelf |
| `status` | Data directory info |
| `completion <shell>` | Generate shell completion script |
| `skill` | Print the agent skill definition |

## Books

Each data source is a **book** on the shelf. Your personal
preferences live in the **journal** (the default). Service
data goes in named books that can be rebuilt independently.

```bash
# Journal (default) — your preferences, permanent
shelf like "Radiohead" --reason "amazing songwriting"

# MusicBrainz enrichment — regenerable
shelf put "Radiohead" --source musicbrainz \
  --keywords "art rock, oxford, england"

# Reset a book without touching your journal
shelf reset musicbrainz
```

## Domains

Items are tagged with a domain for cross-cutting queries:

```bash
shelf opinions --domain music
shelf opinions --domain hn --verb dislikes
shelf seen "https://news.ycombinator.com/item?id=12345" \
  --domain hn --check
```

## Storage

Data lives in `~/.local/share/shelf/` (override with
`SHELF_DATA_DIR`), sharded by source:

```text
~/.local/share/shelf/
  items/
    journal.md
    musicbrainz.md
  relationships/
    journal.md
  seen/
    hn.bloom
    hn.json
```

All files are human-readable markdown tables, designed to
be git-friendly.

## Agent integration

Shelf replaces conversation memory as the durable preference
store. Run `shelf skill` for the full agent skill definition.

```bash
# Before scanning HN
shelf opinions --domain hn

# Record what was shown
shelf seen <url> --domain hn

# When the user reacts
shelf like "<title>" --type article --domain hn \
  --reason "practical agent tooling"
```

## Shell completion

```bash
eval "$(shelf completion bash)"   # bash
eval "$(shelf completion zsh)"    # zsh
shelf completion fish | source    # fish
```
