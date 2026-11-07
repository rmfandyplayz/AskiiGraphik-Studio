// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using System.Windows.Input;
using AskiiArt.Docking.Dragging;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.VisualTree;

namespace AskiiArt.App.Shell.Docking;

internal sealed partial class WindowFrame : UserControl
{
    public WindowFrame()
    {
        InitializeComponent();
        MenuButton.Click += (_, _) => OpenMenu();
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

    /// <summary>Whether it's a tab in a dock, which shows a tab strip in place of its title and buttons.</summary>
    public bool IsDocked
    {
        get => Classes.Contains("docked");
        set => Classes.Set("docked", value);
    }

    /// <summary>The tab strip a dock window shows in place of its title.</summary>
    public DockTabStrip Tabs => TabStrip;

    /// <summary>
    /// How far the tab strip is scrolled, counting from the frame's left edge rather than the strip's, for working out where
    /// its tabs are.
    /// </summary>
    public double TabStripOffset =>
        TabStrip.ScrollOffset - (TabStrip.TranslatePoint(default, this) is { } start ? start.X : 0);

    /// <summary>
    /// The window a drag from the title row moves: in a dock, the tab pressed, or the one showing if the press was on the
    /// strip's empty part. Otherwise, this one.
    /// </summary>
    public string WindowToDrag(string ownWindowId) =>
        IsDocked && TabStrip.PressedWindowId is { } pressed ? pressed : ownWindowId;

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

    /// <summary>
    /// Opens the menu as right-clicking the title does, so that it gets the same chance to show only what applies:
    /// opening a menu directly skips that.
    /// </summary>
    /// <param name="at">Where to open it, if not at the title, such as at a dock window's tab while it's hidden.</param>
    public void OpenMenu(Control? at = null)
    {
        if (TitleMenu is not { } menu)
            return;

        menu.PlacementTarget = at;
        TitleRow.RaiseEvent(new ContextRequestedEventArgs());
    }

    /// <summary>Says what the close button does.</summary>
    public void SetCloseCommand(ICommand? command, string windowId)
    {
        CloseButton.Command = command;
        CloseButton.CommandParameter = windowId;
    }
}
