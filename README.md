<!-- SPDX-License-Identifier: GPL-3.0-or-later -->
<!-- Written with Claude Code (Anthropic). -->

# AskiiArt

> "AskiiArt" is a placeholder name.

A cross-platform (Windows, macOS, Linux) editor for text art, with layers, selections, customizable brushes and full undo history. It's in early development and not usable yet.

## Building

You need the .NET 10 SDK (the exact version is pinned in `global.json`).

```
dotnet build
dotnet test
dotnet run --project src/AskiiArt.App
```

## Documentation

- [Architecture](docs/ARCHITECTURE.md)
- [Roadmap](docs/ROADMAP.md)
- [Decision records](docs/decisions/README.md)

## License

GPL-3.0-or-later. See [LICENSE](LICENSE). Third-party components and their licenses are listed in [THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md).
