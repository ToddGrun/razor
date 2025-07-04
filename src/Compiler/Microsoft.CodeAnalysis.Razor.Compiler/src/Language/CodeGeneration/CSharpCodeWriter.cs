// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.AspNetCore.Razor.Language.CodeGeneration;

public partial class CSharpCodeWriter : CodeWriter
{
    private readonly SourceText? _previousCSharpSourceText;

    public CSharpCodeWriter(RazorCodeGenerationOptions options, RazorCSharpDocument? previousCSharpDocument)
        : base(options)
    {
        _previousCSharpSourceText = previousCSharpDocument?.Text;
    }

    public override SourceText GetText()
    {
        using var reader = new Reader(_pages, Length);
        if (_previousCSharpSourceText is not null)
        {
            var changes = Differ.GetMinimalTextChanges(_previousCSharpSourceText, reader);
            return _previousCSharpSourceText.WithChanges(changes);
        }

        return SourceText.From(reader, Length, Encoding.UTF8);
    }

    public void CopyTo(int sourceIndex, char[] destination, int destinationIndex, int count)
    {

    }
}

