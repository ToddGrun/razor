// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Razor;

namespace Microsoft.AspNetCore.Razor.Language;

internal partial class CompilationCache
{
    internal sealed class WellKnownTypeData
    {
        private readonly INamedTypeSymbol?[] _getTypeByMetadataNameCache = new INamedTypeSymbol?[WellKnownTypeNames.Names.Length];

        public INamedTypeSymbol? GetTypeByMetadataName(WellKnownType wellKnownType, Compilation compilation)
        {
            var cachedSymbol = _getTypeByMetadataNameCache[(int)wellKnownType];

            // The assumption here is that it should be rare that a compilation doesn't have a well-known type.
            // If that happens, we'll pay the cost of looking it up every time.
            if (cachedSymbol is null)
            {
                var fullyQualifiedMetadataName = wellKnownType.ToFullyQualifiedTypeName();
                cachedSymbol = compilation.GetTypeByMetadataName(fullyQualifiedMetadataName);

                _getTypeByMetadataNameCache[(int)wellKnownType] = cachedSymbol;
            }

            return cachedSymbol;
        }
    }
}
