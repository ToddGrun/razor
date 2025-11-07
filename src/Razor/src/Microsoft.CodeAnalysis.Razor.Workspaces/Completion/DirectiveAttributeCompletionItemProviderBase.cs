// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.AspNetCore.Razor;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Syntax;
using Microsoft.AspNetCore.Razor.PooledObjects;
using Microsoft.CodeAnalysis.Razor.Tooltip;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Editor.Razor;
using RazorSyntaxList = Microsoft.AspNetCore.Razor.Language.Syntax.SyntaxList<Microsoft.AspNetCore.Razor.Language.Syntax.SyntaxNode>;
using RazorSyntaxNode = Microsoft.AspNetCore.Razor.Language.Syntax.SyntaxNode;

namespace Microsoft.CodeAnalysis.Razor.Completion;

internal abstract class DirectiveAttributeCompletionItemProviderBase : IRazorCompletionItemProvider
{
    private static ReadOnlyMemory<char> QuotedAttributeValueSnippet => "=\"$0\"".AsMemory();
    private static ReadOnlyMemory<char> UnquotedAttributeValueSnippet => "=$0".AsMemory();

    private static readonly ImmutableArray<RazorCommitCharacter> s_equalsCommitCharacters = [new("=")];
    private static readonly ImmutableArray<RazorCommitCharacter> s_snippetEqualsCommitCharacters = [new("=", Insert: false)];

    public virtual ImmutableArray<RazorCompletionItem> GetCompletionItems(RazorCompletionContext context)
    {
        if (!context.SyntaxTree.Options.FileKind.IsComponent())
        {
            // Directive attribute parameters are only supported in components
            return [];
        }

        var owner = context.Owner;
        if (owner is null)
        {
            return [];
        }

        if (!TryGetElementInfo(owner.Parent.Parent, out var containingTagName, out var attributes))
        {
            // This should never be the case, it means that we're operating on an attribute that doesn't have a tag.
            return [];
        }

        if (!TryGetAttributeInfo(owner, out _, out var attributeName, out var attributeNameLocation, out var parameterName, out var parameterNameLocation))
        {
            // Either we're not in an attribute or the attribute is so malformed that we can't provide proper completions.
            return [];
        }

        // We don't provide Directive Attribute completions when we're in the middle of
        // another unrelated (doesn't start with @) partially completed attribute.
        // <svg xml:| ></svg> (attributeName = "xml:") should not get any directive attribute completions.
        if (!attributeName.IsNullOrWhiteSpace() && !attributeName.StartsWith('@'))
        {
            return [];
        }

        var isAttributeRequest = attributeNameLocation.IntersectsWith(context.AbsoluteIndex);
        var isParameterRequest = parameterNameLocation.IntersectsWith(context.AbsoluteIndex);

        if (!IsValidCompletionLocation(isAttributeRequest, isParameterRequest))
        {
            // We're not in a valid completion location.
            return [];
        }

        var inSnippetContext = InSnippetContext(owner, context.Options);
        var boundAttributeCompletionContext = new DirectiveAttributeCompletionContext(attributeName, parameterName, attributes, inSnippetContext, isAttributeRequest, isParameterRequest, context.Options);

        return GetAttributeCompletions(containingTagName, boundAttributeCompletionContext, context.TagHelperDocumentContext);

        static bool InSnippetContext(
            RazorSyntaxNode owner,
            RazorCompletionOptions razorCompletionOptions)
        {
            return razorCompletionOptions.SnippetsSupported
                // Don't create snippet text when attribute is already in the tag and we are trying to replace it
                // Otherwise you could have something like @onabort=""=""
                && owner is not (MarkupTagHelperDirectiveAttributeSyntax or MarkupAttributeBlockSyntax)
                && owner.Parent is not (MarkupTagHelperDirectiveAttributeSyntax or MarkupAttributeBlockSyntax);
        }
    }

    protected virtual bool IsValidCompletionLocation(bool isAttributeRequest, bool isParameterRequest)
    {
        return false;
    }

    // Internal for testing
    internal ImmutableArray<RazorCompletionItem> GetAttributeCompletions(
        string containingTagName,
        DirectiveAttributeCompletionContext context,
        TagHelperDocumentContext tagHelperDocumentContext)
    {
        var descriptorsForTag = TagHelperFacts.GetTagHelpersGivenTag(tagHelperDocumentContext, containingTagName, parentTag: null);
        if (descriptorsForTag.Length == 0)
        {
            // If the current tag has no possible descriptors then we can't have any directive attributes.
            return [];
        }

        // Use ordinal dictionary because attributes are case sensitive when matching
        using var _ = SpecializedPools.GetPooledStringDictionary<(ImmutableArray<BoundAttributeDescriptionInfo>, ImmutableArray<RazorCommitCharacter>, RazorCompletionItemKind kind)>(out var attributeCompletions);

        foreach (var descriptor in descriptorsForTag)
        {
            foreach (var attributeDescriptor in descriptor.BoundAttributes)
            {
                if (!attributeDescriptor.IsDirectiveAttribute)
                {
                    // We don't care about non-directive attributes
                    continue;
                }

                AddCompletionsForBoundAttribute(descriptor, attributeDescriptor, context, attributeCompletions);
            }
        }

        using var completionItems = new PooledArrayBuilder<RazorCompletionItem>(capacity: attributeCompletions.Count);

        foreach (var (displayText, (attributeDescriptions, commitCharacters, kind)) in attributeCompletions)
        {
            var insertTextSpan = displayText.AsSpan();
            var originalInsertTextSpan = insertTextSpan;

            // Strip off the @ from the insertion text. This change is here to align the insertion text with the
            // completion hooks into VS and VSCode. Basically, completion triggers when `@` is typed so we don't
            // want to insert `@bind` because `@` already exists.
            if (insertTextSpan.StartsWith('@'))
            {
                insertTextSpan = insertTextSpan[1..];
            }

            var isSnippet = false;
            // Indexer attribute, we don't want to insert with the triple dot.
            if (MemoryExtensions.EndsWith(insertTextSpan, "...".AsSpan()))
            {
                insertTextSpan = insertTextSpan[..^3];
            }
            else
            {
                // We are trying for snippet text only for non-indexer attributes, e.g. *not* something like "@bind-..."
                if (context.UseSnippets)
                {
                    GetSnippetText(insertTextSpan, context.Options, out insertTextSpan);
                    isSnippet = true;
                }
            }

            // Don't create another string unnecessarily, even though ReadOnlySpan.ToString() special-cases the string to avoid allocation
            var insertText = insertTextSpan == originalInsertTextSpan ? displayText : insertTextSpan.ToString();

            var razorCompletionItem = kind switch
            {
                RazorCompletionItemKind.DirectiveAttributeParameter => RazorCompletionItem.CreateDirectiveAttributeParameter(
                    displayText,
                    insertText,
                    descriptionInfo: new(attributeDescriptions),
                    commitCharacters,
                    isSnippet),
                RazorCompletionItemKind.DirectiveAttribute => RazorCompletionItem.CreateDirectiveAttribute(
                    displayText,
                    insertText,
                    descriptionInfo: new(attributeDescriptions),
                    commitCharacters,
                    isSnippet),
                _ => throw new InvalidOperationException($"Unexpected completion item kind '{kind}'"),
            };

            completionItems.Add(razorCompletionItem);
        }

        return completionItems.ToImmutableAndClear();

        static void GetSnippetText(
            ReadOnlySpan<char> baseTextSpan,
            RazorCompletionOptions razorCompletionOptions,
            out ReadOnlySpan<char> snippetTextSpan)
        {
            var suffixTextSpan = razorCompletionOptions.AutoInsertAttributeQuotes ? QuotedAttributeValueSnippet : UnquotedAttributeValueSnippet;

            var buffer = new char[baseTextSpan.Length + suffixTextSpan.Length];
            baseTextSpan.CopyTo(buffer);
            suffixTextSpan.CopyTo(buffer.AsMemory()[baseTextSpan.Length..]);

            snippetTextSpan = buffer.AsSpan();
        }
    }

    protected static bool TryAddCompletion(
        string attributeName,
        BoundAttributeDescriptionInfo descriptionInfo,
        TagHelperDescriptor tagHelperDescriptor,
        DirectiveAttributeCompletionContext context,
        RazorCompletionItemKind kind,
        Dictionary<string, (ImmutableArray<BoundAttributeDescriptionInfo>, ImmutableArray<RazorCommitCharacter>, RazorCompletionItemKind kind)> attributeCompletions)
    {
        if (context.SelectedAttributeName != attributeName &&
            context.ExistingAttributes.Any(attributeName, static (name, attributeName) => name == attributeName))
        {
            // Attribute is already present on this element and it is not the selected attribute.
            // It shouldn't exist in the completion list.
            return false;
        }

        AddCompletion(attributeName, descriptionInfo, tagHelperDescriptor, context, kind, attributeCompletions);
        return true;
    }

    protected virtual void AddCompletionsForBoundAttribute(
        TagHelperDescriptor descriptor,
        BoundAttributeDescriptor attributeDescriptor,
        DirectiveAttributeCompletionContext context,
        Dictionary<string, (ImmutableArray<BoundAttributeDescriptionInfo>, ImmutableArray<RazorCommitCharacter>, RazorCompletionItemKind kind)> attributeCompletions)
    {
    }

    protected static void AddCompletion(
        string attributeName,
        BoundAttributeDescriptionInfo descriptionInfo,
        TagHelperDescriptor tagHelperDescriptor,
        DirectiveAttributeCompletionContext context,
        RazorCompletionItemKind kind,
        Dictionary<string, (ImmutableArray<BoundAttributeDescriptionInfo>, ImmutableArray<RazorCommitCharacter>, RazorCompletionItemKind kind)> attributeCompletions)
    {
        if (!attributeCompletions.TryGetValue(attributeName, out var attributeDetails))
        {
            attributeDetails = ([], [], RazorCompletionItemKind.Attribute);
        }

        (var attributeDescriptions, var commitCharacters, _) = attributeDetails;

        if (!attributeDescriptions.Contains(descriptionInfo))
        {
            attributeDescriptions = attributeDescriptions.Add(descriptionInfo);
        }

        // Verify not an indexer attribute, as those don't commit with standard chars
        if (!attributeName.EndsWith("...", StringComparison.Ordinal))
        {
            var equalsAdded = commitCharacters.Any(static c => c.Character == "=");
            var spaceAdded = commitCharacters.Any(static c => c.Character == " ");

            // We don't add "=" as a commit character when using VSCode trigger characters.
            equalsAdded |= !context.Options.UseVsCodeCompletionCommitCharacters;

            foreach (var boundAttribute in tagHelperDescriptor.BoundAttributes)
            {
                spaceAdded |= boundAttribute.IsBooleanProperty;

                if (spaceAdded)
                {
                    break;
                }
            }

            // Determine if we have a common commit character set
            commitCharacters = (equalsAdded, spaceAdded, context.UseSnippets) switch
            {
                (true, false, false) => s_equalsCommitCharacters,
                (true, false, true) => s_snippetEqualsCommitCharacters,
                _ => []
            };

            if (commitCharacters.IsEmpty)
            {
                if (equalsAdded)
                {
                    commitCharacters = commitCharacters.Add(new("=", Insert: !context.UseSnippets));
                }

                if (spaceAdded)
                {
                    commitCharacters = commitCharacters.Add(new(" "));
                }
            }
        }

        attributeCompletions[attributeName] = (attributeDescriptions, commitCharacters, kind);
    }

    // Internal for testing
    internal static bool TryGetAttributeInfo(
        RazorSyntaxNode attributeLeafOwner,
        out TextSpan? prefixLocation,
        [NotNullWhen(true)] out string? attributeName,
        out TextSpan attributeNameLocation,
        out string? parameterName,
        out TextSpan parameterLocation)
    {
        var attribute = attributeLeafOwner.Parent;

        // The null check on the `NamePrefix` field is required for cases like:
        // `<svg xml:base=""x| ></svg>` where there's no `NamePrefix` available.
        switch (attribute)
        {
            case MarkupMinimizedAttributeBlockSyntax minimizedMarkupAttribute:
                prefixLocation = minimizedMarkupAttribute.NamePrefix?.Span;
                SplitAttributeNameIntoParts(
                    minimizedMarkupAttribute.Name.GetContent(),
                    minimizedMarkupAttribute.Name.Span,
                    out attributeName,
                    out attributeNameLocation,
                    out parameterName,
                    out parameterLocation);
                return true;

            case MarkupAttributeBlockSyntax markupAttribute:
                prefixLocation = markupAttribute.NamePrefix?.Span;
                SplitAttributeNameIntoParts(
                    markupAttribute.Name.GetContent(),
                    markupAttribute.Name.Span,
                    out attributeName,
                    out attributeNameLocation,
                    out parameterName,
                    out parameterLocation);
                return true;

            case MarkupMinimizedTagHelperAttributeSyntax minimizedTagHelperAttribute:
                prefixLocation = minimizedTagHelperAttribute.NamePrefix?.Span;
                SplitAttributeNameIntoParts(
                    minimizedTagHelperAttribute.Name.GetContent(),
                    minimizedTagHelperAttribute.Name.Span,
                    out attributeName,
                    out attributeNameLocation,
                    out parameterName,
                    out parameterLocation);
                return true;

            case MarkupTagHelperAttributeSyntax tagHelperAttribute:
                prefixLocation = tagHelperAttribute.NamePrefix?.Span;
                SplitAttributeNameIntoParts(
                    tagHelperAttribute.Name.GetContent(),
                    tagHelperAttribute.Name.Span,
                    out attributeName,
                    out attributeNameLocation,
                    out parameterName,
                    out parameterLocation);
                return true;

            case MarkupTagHelperDirectiveAttributeSyntax directiveAttribute:
                {
                    var attributeNameNode = directiveAttribute.Name;
                    var directiveAttributeTransition = directiveAttribute.Transition;
                    var nameStart = directiveAttributeTransition?.SpanStart ?? attributeNameNode.SpanStart;
                    var nameEnd = attributeNameNode?.Span.End ?? directiveAttributeTransition.AssumeNotNull().Span.End;
                    prefixLocation = directiveAttribute.NamePrefix?.Span;
                    attributeName = string.Concat(directiveAttributeTransition?.GetContent(), attributeNameNode?.GetContent());
                    attributeNameLocation = new TextSpan(nameStart, nameEnd - nameStart);
                    parameterName = directiveAttribute.ParameterName?.GetContent();
                    parameterLocation = directiveAttribute.ParameterName?.Span ?? default;
                    return true;
                }

            case MarkupMinimizedTagHelperDirectiveAttributeSyntax minimizedDirectiveAttribute:
                {
                    var attributeNameNode = minimizedDirectiveAttribute.Name;
                    var directiveAttributeTransition = minimizedDirectiveAttribute.Transition;
                    var nameStart = directiveAttributeTransition?.SpanStart ?? attributeNameNode.SpanStart;
                    var nameEnd = attributeNameNode?.Span.End ?? directiveAttributeTransition.AssumeNotNull().Span.End;
                    prefixLocation = minimizedDirectiveAttribute.NamePrefix?.Span;
                    attributeName = string.Concat(directiveAttributeTransition?.GetContent(), attributeNameNode?.GetContent());
                    attributeNameLocation = new TextSpan(nameStart, nameEnd - nameStart);
                    parameterName = minimizedDirectiveAttribute.ParameterName?.GetContent();
                    parameterLocation = minimizedDirectiveAttribute.ParameterName?.Span ?? default;
                    return true;
                }
        }

        prefixLocation = null;
        attributeName = null;
        attributeNameLocation = default;
        parameterName = null;
        parameterLocation = default;
        return false;
    }

    // Internal for testing
    internal static bool TryGetElementInfo(
        RazorSyntaxNode element,
        [NotNullWhen(true)] out string? containingTagName,
        out ImmutableArray<string> attributeNames)
    {
        if (element is MarkupStartTagSyntax startTag)
        {
            containingTagName = startTag.Name.Content;
            attributeNames = ExtractAttributeNames(startTag.Attributes);
            return true;
        }

        if (element is MarkupTagHelperStartTagSyntax startTagHelper)
        {
            containingTagName = startTagHelper.Name.Content;
            attributeNames = ExtractAttributeNames(startTagHelper.Attributes);
            return true;
        }

        containingTagName = null;
        attributeNames = default;
        return false;
    }

    private static ImmutableArray<string> ExtractAttributeNames(RazorSyntaxList attributes)
    {
        using var attributeNames = new PooledArrayBuilder<string>(capacity: attributes.Count);

        foreach (var attribute in attributes)
        {
            switch (attribute)
            {
                case MarkupTagHelperAttributeSyntax tagHelperAttribute:
                    attributeNames.Add(tagHelperAttribute.Name.GetContent());
                    break;

                case MarkupMinimizedTagHelperAttributeSyntax minimizedTagHelperAttribute:
                    attributeNames.Add(minimizedTagHelperAttribute.Name.GetContent());
                    break;

                case MarkupAttributeBlockSyntax markupAttribute:
                    attributeNames.Add(markupAttribute.Name.GetContent());
                    break;

                case MarkupMinimizedAttributeBlockSyntax minimizedMarkupAttribute:
                    attributeNames.Add(minimizedMarkupAttribute.Name.GetContent());
                    break;

                case MarkupTagHelperDirectiveAttributeSyntax directiveAttribute:
                    attributeNames.Add(directiveAttribute.FullName);
                    break;

                case MarkupMinimizedTagHelperDirectiveAttributeSyntax minimizedDirectiveAttribute:
                    attributeNames.Add(minimizedDirectiveAttribute.FullName);
                    break;
            }
        }

        return attributeNames.ToImmutableAndClear();
    }

    private static void SplitAttributeNameIntoParts(
        string attributeName,
        TextSpan attributeNameLocation,
        out string name,
        out TextSpan nameLocation,
        out string? parameterName,
        out TextSpan parameterLocation)
    {
        name = attributeName;
        nameLocation = attributeNameLocation;
        parameterName = null;
        parameterLocation = default;

        // It's possible that the attribute looks like a directive attribute but is incomplete.
        // We should try and extract out the transition and parameter.

        if (attributeName.Length == 0 || attributeName[0] != '@')
        {
            // Doesn't look like a directive attribute. Not an incomplete directive attribute.
            return;
        }

        var colonIndex = attributeName.IndexOf(':');
        if (colonIndex == -1)
        {
            // There's no parameter, the existing attribute name and location is sufficient.
            return;
        }

        name = attributeName[..colonIndex];
        nameLocation = new TextSpan(attributeNameLocation.Start, name.Length);
        parameterName = attributeName[(colonIndex + 1)..];
        parameterLocation = new TextSpan(attributeNameLocation.Start + colonIndex + 1, parameterName.Length);
    }
}
