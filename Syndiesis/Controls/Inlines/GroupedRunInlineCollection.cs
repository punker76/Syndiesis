﻿using Avalonia.Collections;
using Avalonia.Controls.Documents;
using Syndiesis.Core.DisplayAnalysis;
using System.Collections.Generic;

namespace Syndiesis.Controls.Inlines;

public sealed class GroupedRunInlineCollection : AvaloniaList<object>
{
    public override void Add(object item)
    {
        var run = RunOrGrouped.FromObject(item);
        Add(run);
    }

    public override void AddRange(IEnumerable<object> items)
    {
        foreach (var item in items)
        {
            Add(item);
        }
    }

    public GroupedRunInlineCollection() { }
    public GroupedRunInlineCollection(IEnumerable<RunOrGrouped> items) 
    {
        AddRange(items);
    }

    public void Add(GroupedRunInline.IBuilder builder)
    {
        base.Add(new RunOrGrouped(builder));
    }

    public void Add(RunOrGrouped run)
    {
        base.Add(run.AvailableObject!);
    }

    public void AddSingle(Run run)
    {
        Add(new SingleRunInline(run));
    }

    public void AddSingle(UIBuilder.Run run)
    {
        Add(new SingleRunInline.Builder(run));
    }

    public void AddRange(IEnumerable<RunOrGrouped> runs)
    {
        foreach (var run in runs)
        {
            Add(run);
        }
    }

    public InlineCollection? AsInlineCollection()
    {
        var result = new InlineCollection();

        int count = Count;
        for (int i = 0; i < count; i++)
        {
            GroupedRunInline.AppendToInlines(result, this[i]);
        }

        return result;
    }

    public GroupedRunInlineCollection Build()
    {
        var result = new GroupedRunInlineCollection();
        foreach (var obj in this)
        {
            result.Add(BuildObject(obj));
        }
        return result;
    }

    private static object BuildObject(object value)
    {
        switch (value)
        {
            case GroupedRunInline.IBuilder builder:
                return builder.Build();

            case RunOrGrouped run:
                return run.Build();

            case UIBuilder.Run run:
                return run.Build();
        }

        return value;
    }

    public new RunOrGrouped this[int index]
    {
        get => RunOrGrouped.FromObject(base[index]);
        set => base[index] = value.AvailableObject!;
    }
}
