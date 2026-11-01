// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using System.Runtime.InteropServices;
using Avalonia.Controls;

namespace AskiiArt.App.Shell;

/// <summary>
/// Sets AppKit's "document edited" state on a macOS window, which puts a dot in its close button
/// while there are unsaved changes, as Mac apps do (ADR 0023). Avalonia doesn't expose it, so it's
/// set on the native window Avalonia hands out. Does nothing on other platforms.
/// </summary>
internal static class MacOSDocumentEdited
{
    private const string ObjectiveCRuntime = "/usr/lib/libobjc.A.dylib";

    // What Avalonia's macOS backend calls a window's handle, which is then the NSWindow.
    private const string NativeWindowDescriptor = "NSWindow";

    public static void Set(Window window, bool isEdited)
    {
        ArgumentNullException.ThrowIfNull(window);

        if (!OperatingSystem.IsMacOS())
            return;

        var handle = window.TryGetPlatformHandle();
        if (handle is not { HandleDescriptor: NativeWindowDescriptor } || handle.Handle == IntPtr.Zero)
            return;

        try
        {
            SendBool(handle.Handle, RegisterSelector("setDocumentEdited:\0"u8.ToArray()), isEdited);
        }
        catch (Exception exception) when (exception is DllNotFoundException or EntryPointNotFoundException)
        {
            // Only the dot is lost; the title still shows unsaved changes.
        }
    }

    [DllImport(ObjectiveCRuntime, EntryPoint = "sel_registerName")]
    private static extern IntPtr RegisterSelector(byte[] nullTerminatedUtf8Name);

    // objc_msgSend must be declared with the exact signature of the method it calls: (BOOL) -> void.
    [DllImport(ObjectiveCRuntime, EntryPoint = "objc_msgSend")]
    private static extern void SendBool(IntPtr receiver, IntPtr selector, [MarshalAs(UnmanagedType.I1)] bool value);
}
