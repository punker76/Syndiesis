using Avalonia;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Input;
using Avalonia.Media;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Syndiesis.Controls.Inlines;
using Syndiesis.Controls.Toast;
using Syndiesis.Core.DisplayAnalysis;
using Syndiesis.Utilities;
using System;

namespace Syndiesis.Controls.AnalysisVisualization;

public partial class AnalysisTreeListNodeLine : UserControl
{
    private readonly CancellationTokenFactory _pulseLineCancellationTokenFactory = new();

    private bool _isExpanded;

    public bool IsExpanded
    {
        get => _isExpanded;
        set
        {
            _isExpanded = value;
            visualExpandToggle.IsExpandingToggle = !value;
        }
    }

    private bool _hasChildren;

    public bool HasChildren
    {
        get => _hasChildren;
        set
        {
            _hasChildren = value;
            visualExpandToggle.IsVisible = value;
        }
    }

    private string _nodeTypeText = string.Empty;

    public string NodeTypeText
    {
        get => _nodeTypeText;
        set
        {
            _nodeTypeText = value;
            nodeTypeIconText.Text = value;
        }
    }

    public static readonly StyledProperty<Color> NodeTypeColorProperty =
        AvaloniaProperty.Register<CodeEditorLine, Color>(
            nameof(NodeTypeColor),
            defaultValue: BaseAnalysisNodeCreator.CommonStyles.ClassMainColor);

    public Color NodeTypeColor
    {
        get => GetValue(NodeTypeColorProperty!);
        set
        {
            SetValue(NodeTypeColorProperty!, value!);
            nodeTypeIconText.Foreground = new SolidColorBrush(value);
        }
    }

    public NodeTypeDisplay NodeTypeDisplay
    {
        get
        {
            return new(NodeTypeText, NodeTypeColor);
        }
        set
        {
            var (text, color) = value;
            NodeTypeText = text;
            NodeTypeColor = color;
        }
    }

    public InlineCollection? Inlines
    {
        get => descriptionText.Inlines;
        set
        {
            descriptionText.Inlines!.ClearSetValues(value!);
        }
    }

    public GroupedRunInlineCollection? GroupedRunInlines
    {
        get => descriptionText.GroupedRunInlines;
        set
        {
            descriptionText.GroupedRunInlines = value;
        }
    }

    public SyntaxObjectInfo? AssociatedSyntaxObject { get; set; }

    public TextSpan DisplaySpan
    {
        get
        {
            var syntaxObject = AssociatedSyntaxObject;
            if (syntaxObject is null)
                return default;

            var nodeType = NodeTypeText;
            switch (nodeType)
            {
                case SyntaxAnalysisNodeCreator.Types.DisplayValue:
                    return syntaxObject.Span;
            }

            return syntaxObject.FullSpan;
        }
    }

    public AnalysisNodeKind AnalysisNodeKind { get; set; }

    public AnalysisTreeListNodeLine()
    {
        InitializeComponent();
    }

    public LinePositionSpan DisplayLineSpan(SyntaxTree? tree)
    {
        if (tree is null)
            return default;

        var displaySpan = DisplaySpan;
        if (displaySpan == default)
            return default;

        return tree!.GetLineSpan(displaySpan).Span;
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        var pointerPoint = e.GetCurrentPoint(this);
        var properties = pointerPoint.Properties;
        if (properties.IsLeftButtonPressed)
        {
            var modifiers = e.KeyModifiers.NormalizeByPlatform();
            switch (modifiers)
            {
                case KeyModifiers.Control:
                {
                    CopyEntireLineContent();
                    break;
                }
            }
        }
    }

    private void CopyEntireLineContent()
    {
        var text = descriptionText.Inlines!.Text;
        _ = this.SetClipboardTextAsync(text)
            .ConfigureAwait(false);
        PulseCopiedLine();

        var toastContainer = ToastNotificationContainer.GetFromMainWindowTopLevel(this);
        if (toastContainer is not null)
        {
            var popup = new ToastNotificationPopup();
            popup.defaultTextBlock.Text = $"""
                Copied entire line content:
                {text}
                """;
            var animation = new BlurOpenDropCloseToastAnimation(TimeSpan.FromSeconds(2));
            _ = toastContainer.Show(popup, animation);
        }
    }

    private void PulseCopiedLine()
    {
        _pulseLineCancellationTokenFactory.Cancel();
        var color = Color.FromArgb(192, 128, 128, 128);
        var animation = Animations.CreateColorPulseAnimation(this, color, BackgroundProperty);
        animation.Duration = TimeSpan.FromMilliseconds(750);
        animation.Easing = Singleton<CubicEaseOut>.Instance;
        _ = animation.RunAsync(this, _pulseLineCancellationTokenFactory.CurrentToken);
    }
}
