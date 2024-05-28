﻿namespace Syndiesis.Core.DisplayAnalysis;

public readonly record struct DisplayValueSource(
    DisplayValueSource.SymbolKind Kind, string? Name)
{
    public bool IsDefault
        => Kind is SymbolKind.None
        || Name is null;

    public enum SymbolKind
    {
        None = 0,
        Property = 1,
        Field = 2,
        Method = 3,

        KindMask = 0xFF,

        FlagMask = ~KindMask,

        Async = 1 << 15,
        Internal = 1 << 16,
    }
}
