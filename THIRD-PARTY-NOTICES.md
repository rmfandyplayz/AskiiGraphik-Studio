<!-- SPDX-License-Identifier: GPL-3.0-or-later -->
<!-- Written with Claude Code (Anthropic). -->

# Third-party notices

AskiiArt uses the components below. Every one is under a license compatible with GPL-3.0-or-later. Versions are pinned in `Directory.Packages.props`.

## Shipped with the app

| Component | License | Source |
|---|---|---|
| Avalonia (Avalonia, Avalonia.Desktop, Avalonia.Themes.Fluent, Avalonia.Skia) | MIT | https://github.com/AvaloniaUI/Avalonia |
| SkiaSharp | MIT | https://github.com/mono/SkiaSharp |
| HarfBuzzSharp (dependency of Avalonia.Skia) | MIT | https://github.com/mono/SkiaSharp |
| CommunityToolkit.Mvvm | MIT | https://github.com/CommunityToolkit/dotnet |
| Dock (Dock.Avalonia, Dock.Avalonia.Themes.Fluent, Dock.Model.Mvvm, Dock.Serializer.SystemTextJson, and the Dock packages they depend on) | MIT | https://github.com/wieslawsoltes/Dock |
| Microsoft.Extensions.DependencyInjection, Microsoft.Extensions.Logging | MIT | https://github.com/dotnet/runtime |
| Serilog, Serilog.Extensions.Logging, Serilog.Sinks.File | Apache-2.0 | https://github.com/serilog/serilog |

## Bundled fonts

Regular weight only, unmodified. Each license text ships next to the app in `Licenses/Fonts/` and lives in the repo under `src/AskiiArt.Imaging/Fonts/Bundled/`.

| Font | Version | License | Source |
|---|---|---|---|
| JetBrains Mono | 2.304 | OFL-1.1 | https://github.com/JetBrains/JetBrainsMono |
| Roboto Mono | 3.001 | OFL-1.1 | https://github.com/googlefonts/RobotoMono |
| IBM Plex Mono | 2.5.0 | OFL-1.1 ("Plex" is a Reserved Font Name) | https://github.com/IBM/plex |
| Fira Code | 6.2 | OFL-1.1 | https://github.com/tonsky/FiraCode |

## Used only for development and testing (not shipped)

| Component | License | Source |
|---|---|---|
| xUnit.net v3 | Apache-2.0 | https://github.com/xunit/xunit |
| Avalonia.Headless.XUnit | MIT | https://github.com/AvaloniaUI/Avalonia |
| Verify (Verify.XunitV3) | MIT | https://github.com/VerifyTests/Verify |
| CsCheck | Apache-2.0 | https://github.com/AnthonyLloyd/CsCheck |
