// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.CodeAnalysis.Razor.Compiler.Language.Extensions;

internal static partial class ISymbolExtensions
{
    private sealed partial class Cache
    {
        private sealed class ToDisplayStringResult
        {
            private readonly ISymbol _symbol;

            private string? _emptyDisplayFormat;
            private string? _fullNameTypeDisplayFormat;
            private string? _globallyQualifiedFullNameTypeDisplayFormat;

            public ToDisplayStringResult(ISymbol symbol)
            {
                _symbol = symbol;
            }

            public string ToDisplayString(SymbolDisplayFormat? format)
            {
                if (format == null)
                {
                    return GetToDisplayStringResult(_symbol, format, ref _emptyDisplayFormat);
                }
                else if (format == SymbolExtensions.FullNameTypeDisplayFormat)
                {
                    return GetToDisplayStringResult(_symbol, format, ref _fullNameTypeDisplayFormat);
                }
                else if (format == ComponentTagHelperDescriptorProvider.GloballyQualifiedFullNameTypeDisplayFormat)
                {
                    return GetToDisplayStringResult(_symbol, format, ref _globallyQualifiedFullNameTypeDisplayFormat);
                }

                return _symbol.ToDisplayString(format);

                static string GetToDisplayStringResult(ISymbol symbol, SymbolDisplayFormat? format, ref string? cachedFormat)
                {
                    if (cachedFormat == null)
                    {
                        cachedFormat = symbol.ToDisplayString(format);
                    }

                    return cachedFormat;
                }
            }
        }
    }
}
