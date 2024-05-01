using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Syndiesis.Controls.Extensions;
using Syndiesis.Controls.SyntaxVisualization.Creation;
using Syndiesis.Utilities;
using System;
using System.Collections.Generic;

namespace Syndiesis.Controls;

public partial class SyntaxTreeListNode : UserControl
{
    private static readonly CancellationTokenFactory _expansionAnimationCancellationTokenFactory = new();

    public object? AssociatedSyntaxObjectContent
    {
        get => AssociatedSyntaxObject?.SyntaxObject;
        set
        {
            var o = SyntaxObjectInfo.GetInfoForObject(value);
            AssociatedSyntaxObject = o;
        }
    }

    public SyntaxObjectInfo? AssociatedSyntaxObject
    {
        get => NodeLine.AssociatedSyntaxObject;
        set => NodeLine.AssociatedSyntaxObject = value;
    }

    public static readonly StyledProperty<SyntaxTreeListNodeLine> NodeLineProperty =
        AvaloniaProperty.Register<CodeEditorLine, SyntaxTreeListNodeLine>(
            nameof(NodeLine),
            defaultValue: new());

    public SyntaxTreeListNodeLine NodeLine
    {
        get => GetValue(NodeLineProperty);
        set
        {
            SetValue(NodeLineProperty, value);
            topNodeContent.Content = value;
            value.HasChildren = _childRetriever is not null;
        }
    }

    // Only here for the designer preview
    public AvaloniaList<SyntaxTreeListNode> ChildNodes
    {
        set
        {
            innerStackPanel.Children.ClearSetValues(value);
            NodeLine.HasChildren = value.Count > 0;
        }
    }

    private AdvancedLazy<IReadOnlyList<SyntaxTreeListNode>>? _childRetriever;

    public Func<IReadOnlyList<SyntaxTreeListNode>>? ChildRetriever
    {
        get => _childRetriever?.Factory;
        set
        {
            if (value is null)
            {
                _childRetriever = null;
            }
            else
            {
                _childRetriever = new(value);
            }

            NodeLine.HasChildren = value is not null;
        }
    }

    public IReadOnlyList<SyntaxTreeListNode> LazyChildren
    {
        get
        {
            return _childRetriever?.ValueOrDefault ?? [];
        }
    }

    public IReadOnlyList<SyntaxTreeListNode> DemandedChildren
    {
        get
        {
            EnsureInitializedChildren();
            return _childRetriever?.Value ?? [];
        }
    }

    private void EnsureInitializedChildren()
    {
        if (_childRetriever is null)
            return;

        if (_childRetriever.IsValueCreated)
            return;

        var value = _childRetriever.Value;
        innerStackPanel.Children.ClearSetValues(value);
        // this is necessary to avoid overriding the height of the node
        expandableCanvas.SetExpansionStateWithoutAnimation(ExpansionState.Collapsed);
        foreach (var child in value)
        {
            child.ListView = ListView;
        }
    }

    internal SyntaxTreeListView? ListView { get; set; }

    public SyntaxTreeListNode()
    {
        InitializeComponent();
        expandableCanvas.SetExpansionStateWithoutAnimation(ExpansionState.Expanded);
    }

    private static readonly Color _topLineHoverColor = Color.FromArgb(64, 128, 128, 128);
    private static readonly Color _expandableCanvasHoverColor = Color.FromArgb(64, 96, 96, 96);

    private readonly SolidColorBrush _topLineBackgroundBrush = new(Colors.Transparent);
    private readonly SolidColorBrush _expandableCanvasBackgroundBrush = new(Colors.Transparent);

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        EvaluateHoveringRecursively(e);
    }

    protected override void OnPointerEntered(PointerEventArgs e)
    {
        base.OnPointerEntered(e);
        EvaluateHovering(e);
    }

    protected override void OnPointerExited(PointerEventArgs e)
    {
        base.OnPointerExited(e);
        EvaluateHoveringRecursively(e);
    }

    private void EvaluateHovering(PointerEventArgs e)
    {
        var allowedHover = ListView?.RequestHover(this) ?? true;
        if (!allowedHover)
        {
            UpdateHovering(false);
            return;
        }

        var nodeLine = NodeLine;
        var bounds = nodeLine.Bounds;
        var restrictedBounds = bounds
            .WithHeight(bounds.Height - 1);
        var position = e.GetCurrentPoint(nodeLine).Position;
        var isHovered = restrictedBounds.Contains(position);

        if (isHovered)
        {
            ListView?.OverrideHover(this);
            // when the user clicks on this node, the animation must not be broken
            EnsureInitializedChildren();
        }
        else
        {
            ListView?.RemoveHover(this);
        }
    }

    internal void SetListViewRecursively(SyntaxTreeListView listView)
    {
        ListView = listView;

        foreach (var child in LazyChildren)
        {
            child.SetListViewRecursively(listView);
        }
    }

    internal void SetHoveringRecursively(bool isHovered)
    {
        UpdateHovering(isHovered);

        foreach (var child in LazyChildren)
        {
            child.UpdateHovering(isHovered);
        }
    }

    internal void UpdateHovering(bool isHovered)
    {
        var topLineBackgroundColor = isHovered ? _topLineHoverColor : Colors.Transparent;
        var expandableCanvasBackgroundColor = isHovered ? _expandableCanvasHoverColor : Colors.Transparent;
        _topLineBackgroundBrush.Color = topLineBackgroundColor;
        _expandableCanvasBackgroundBrush.Color = expandableCanvasBackgroundColor;

        NodeLine.Background = _topLineBackgroundBrush;
        expandableCanvas.Background = _expandableCanvasBackgroundBrush;
    }

    internal void EvaluateHoveringRecursively(PointerEventArgs e)
    {
        EvaluateHovering(e);

        foreach (var child in LazyChildren)
        {
            child.EvaluateHoveringRecursively(e);
        }
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        EvaluateHovering(e);
        if (ListView?.IsHovered(this) is true)
        {
            ToggleExpansion();
        }
    }

    public void ToggleExpansion()
    {
        var nodeLine = NodeLine;
        if (!nodeLine.HasChildren)
            return;

        bool newToggle = !nodeLine.IsExpanded;
        ExpandOrCollapse(newToggle);
    }

    public void Expand()
    {
        ExpandOrCollapse(true);
    }

    public void Collapse()
    {
        ExpandOrCollapse(false);
    }

    private void ExpandOrCollapse(bool expand)
    {
        var nodeLine = NodeLine;
        if (!nodeLine.HasChildren)
            return;

        if (nodeLine.IsExpanded == expand)
            return;

        nodeLine.IsExpanded = expand;

        // cancel any currently running animation
        _expansionAnimationCancellationTokenFactory.Cancel();

        var animationToken = _expansionAnimationCancellationTokenFactory.CurrentToken;
        if (expand)
        {
            EnsureInitializedChildren();
        }
        _ = expandableCanvas.SetExpansionState(expand, animationToken);
    }
}
