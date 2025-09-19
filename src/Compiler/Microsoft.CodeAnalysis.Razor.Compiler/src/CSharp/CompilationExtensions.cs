// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Razor.Language;

namespace Microsoft.CodeAnalysis.Razor;

internal static class CompilationExtensions
{
    public static INamedTypeSymbol? GetWellKnownType(this Compilation compilation, WellKnownType wellKnownType)
        =>  CompilationCache.GetWellKnownTypeData(compilation).GetTypeByMetadataName(wellKnownType, compilation);

    public static bool HasAddComponentParameter(this Compilation compilation)
    {
        var renderTreeBuilder = compilation.GetWellKnownType(WellKnownType.RenderTreeBuilder);

        return renderTreeBuilder != null
            && renderTreeBuilder.DeclaredAccessibility == Accessibility.Public
            && renderTreeBuilder.GetMembers("AddComponentParameter").Any(static m => m.DeclaredAccessibility == Accessibility.Public);
    }
}
