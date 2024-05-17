﻿using Avalonia.Controls.Documents;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Syndiesis.Controls.Inlines;

public sealed class ComplexGroupedRunInline : GroupedRunInline
{
    public List<RunOrGrouped> Children { get; set; } = new();

    public override IEnumerable<object> InlineObjects => Children.Select(s => s.AvailableObject)!;

    public ComplexGroupedRunInline() { }

    public ComplexGroupedRunInline(IEnumerable<RunOrGrouped> children)
    {
        Children.AddRange(children);
    }

    protected override int CalculatedTextLength()
    {
        int length = 0;
        foreach (var run in Children)
        {
            length += GetTextLength(run);
        }
        return length;
    }

    protected override void CalculateText(StringBuilder builder)
    {
        foreach (var value in Children)
        {
            AppendText(builder, value);
        }
    }

    public override void AppendToInlines(InlineCollection inlines)
    {
        foreach (var value in Children)
        {
            AppendToInlines(inlines, value);
        }
    }
}
