// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;

namespace Microsoft.CodeAnalysis.Razor.Compiler.Language.Extensions;

internal static partial class ISymbolExtensions
{
    private static readonly ConditionalWeakTable<ISymbol, Cache> s_instance = new();

    public static string ToCachedDisplayString(this ISymbol symbol, SymbolDisplayFormat? format = null)
    {
        var cache = s_instance.GetValue(symbol, static s => new Cache(s));
        return cache.ToDisplayString(format);
    }
}
