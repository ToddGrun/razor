// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis;

namespace Microsoft.AspNetCore.Razor.Language;

internal sealed partial class CompilationCache
{
    private static readonly ConditionalWeakTable<Compilation, Entry> s_instance = new();

    public static WellKnownTypeData GetWellKnownTypeData(Compilation compilation)
    {
        var entry = GetCacheEntry(compilation);
        entry.GetWellKnownTypeData ??= new WellKnownTypeData();

        return entry.GetWellKnownTypeData;
    }

    private static Entry GetCacheEntry(Compilation compilation)
        => s_instance.GetValue(compilation, static s => new Entry());
}
