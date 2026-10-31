// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using SkiaSharp;

namespace AskiiArt.App.Canvas.Rendering;

/// <summary>
/// An immutable image shared between the UI thread and Avalonia's render thread. Each holder takes
/// a reference and releases it when done; the image is disposed when the last one is released, so
/// neither thread can dispose it while the other is still drawing it.
/// </summary>
internal sealed class SharedImage
{
    private int _references = 1;

    /// <summary>Wraps <paramref name="image"/>, with one reference held by the caller.</summary>
    public SharedImage(SKImage image)
    {
        ArgumentNullException.ThrowIfNull(image);
        Image = image;
    }

    public SKImage Image { get; }

    public SharedImage AddReference()
    {
        if (Interlocked.Increment(ref _references) > 1)
            return this;

        throw new ObjectDisposedException(nameof(SharedImage));
    }

    public void Release()
    {
        switch (Interlocked.Decrement(ref _references))
        {
            case 0:
                Image.Dispose();
                break;
            case < 0:
                throw new InvalidOperationException("The image was released more times than it was referenced.");
        }
    }
}
