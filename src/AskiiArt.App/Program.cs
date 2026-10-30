// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using Avalonia;

namespace AskiiArt.App;

internal static class Program
{
    // Avalonia isn't initialized until BuildAvaloniaApp runs, so nothing before that call may
    // touch Avalonia APIs or rely on a SynchronizationContext.
    [STAThread]
    public static void Main(string[] args)
    {
        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    // The IDE previewer finds the app by looking for a method with exactly this name.
    public static AppBuilder BuildAvaloniaApp()
    {
        return AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .LogToTrace();
    }
}
