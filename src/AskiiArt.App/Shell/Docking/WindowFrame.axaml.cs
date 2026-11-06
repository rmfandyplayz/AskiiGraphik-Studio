// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using System.Windows.Input;
using AskiiArt.Docking.Dragging;
using Avalonia;
using Avalonia.Controls;
using Avalonia.VisualTree;

namespace AskiiArt.App.Shell.Docking;

internal sealed partial class WindowFrame : UserControl
{
    public WindowFrame()
    {
        InitializeComponent();
        MenuButton.Click += (_, _) => TitleRow.ContextMenu?.Open(TitleRow);
    }

    /// <summary>The name at the top.</summary>
    public string? Title
    {
        get => TitleText.Text;
        init => TitleText.Text = value;
    }

    /// <summary>The window's view, which doesn't know it's in a frame.</summary>
    public Control? Body
    {
        get => BodyHost.Content as Control;
        init => BodyHost.Content = value;
    }

    /// <summary>Whether only the title shows.</summary>
    public bool IsTitleOnly
    {
        get => !BodyHost.IsVisible;
        set => BodyHost.IsVisible = !value;
    }

    /// <summary>What dragging moves the window by, and what right-clicking opens the menu on.</summary>
    public Control TitleHandle => TitleRow;

    /// <summary>The menu of the title and the menu button.</summary>
    public ContextMenu? TitleMenu
    {
        get => TitleRow.ContextMenu;
        set => TitleRow.ContextMenu = value;
    }

    /// <summary>The button that opens the menu.</summary>
    public Button MenuOpener => MenuButton;

    /// <summary>The button that closes the window.</summary>
    public Button Closer => CloseButton;

    /// <summary>Whether it's on its own, which gives it grips along its edges and corners.</summary>
    public bool IsFloating
    {
        get => GripLayer.IsVisible;
        set => GripLayer.IsVisible = value;
    }

    /// <summary>Whether it's being held over a drop zone, which makes it much more see-through.</summary>
    public bool IsAnchoring
    {
        get => Classes.Contains("anchoring");
        set => Classes.Set("anchoring", value);
    }

    /// <summary>Whether its buttons stay hidden even with the pointer over it, as they do while windows are dragged.</summary>
    public bool AreButtonsHidden
    {
        get => Classes.Contains("buttonsHidden");
        set => Classes.Set("buttonsHidden", value);
    }

    /// <summary>Whether a press is on one of the title row's buttons rather than on the title itself.</summary>
    public bool IsOnAButton(object? source) =>
        source is Visual visual && (MenuButton.IsVisualAncestorOf(visual) || CloseButton.IsVisualAncestorOf(visual) ||
            visual == MenuButton || visual == CloseButton);

    /// <summary>What resizes it while it's floating, and which edges each one moves.</summary>
    public IReadOnlyList<(Control Grip, WindowEdges Edges)> ResizeGrips =>
    [
        (LeftGrip, WindowEdges.Left),
        (RightGrip, WindowEdges.Right),
        (TopGrip, WindowEdges.Top),
        (BottomGrip, WindowEdges.Bottom),
        (TopLeftGrip, WindowEdges.Top | WindowEdges.Left),
        (TopRightGrip, WindowEdges.Top | WindowEdges.Right),
        (BottomLeftGrip, WindowEdges.Bottom | WindowEdges.Left),
        (BottomRightGrip, WindowEdges.Bottom | WindowEdges.Right),
    ];

    /// <summary>Says what the close button does.</summary>
    public void SetCloseCommand(ICommand? command, string windowId)
    {
        CloseButton.Command = command;
        CloseButton.CommandParameter = windowId;
    }
}
