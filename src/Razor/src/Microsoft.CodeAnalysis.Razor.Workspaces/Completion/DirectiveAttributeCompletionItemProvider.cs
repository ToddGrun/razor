// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis.Razor.Tooltip;

namespace Microsoft.CodeAnalysis.Razor.Completion;

internal class DirectiveAttributeCompletionItemProvider : DirectiveAttributeCompletionItemProviderBase
{
    protected override bool IsValidCompletionLocation(bool isAttributeRequest, bool isParameterRequest)
    {
        // Support completions only in attribute locations.
        return isAttributeRequest;
    }

    protected override void AddCompletionsForBoundAttribute(
        TagHelperDescriptor descriptor,
        BoundAttributeDescriptor attributeDescriptor,
        DirectiveAttributeCompletionContext context,
        Dictionary<string, (ImmutableArray<BoundAttributeDescriptionInfo>, ImmutableArray<RazorCommitCharacter>, RazorCompletionItemKind kind)> attributeCompletions)
    {
        var indexerCompletion = context.SelectedAttributeName.EndsWith("...", StringComparison.Ordinal);
        var tagHelperTypeName = descriptor.TypeName;
        var descriptionInfo = BoundAttributeDescriptionInfo.From(attributeDescriptor, isIndexer: indexerCompletion, tagHelperTypeName);

        if (!TryAddCompletion(attributeDescriptor.Name, descriptionInfo, descriptor, context, RazorCompletionItemKind.DirectiveAttribute, attributeCompletions) && attributeDescriptor.Parameters.Length > 0)
        {
            // This attribute has parameters and the base attribute name (@bind) is already satisfied. We need to check if there are any valid
            // parameters left to be provided, if so, we need to still represent the base attribute name in the completion list.

            foreach (var parameterDescriptor in attributeDescriptor.Parameters)
            {
                if (!context.ExistingAttributes.Any(name => TagHelperMatchingConventions.SatisfiesBoundAttributeWithParameter(parameterDescriptor, name, attributeDescriptor)))
                {
                    // This bound attribute parameter has not had a completion entry added for it, re-represent the base attribute name in the completion list
                    AddCompletion(attributeDescriptor.Name, descriptionInfo, descriptor, context, RazorCompletionItemKind.DirectiveAttribute, attributeCompletions);
                    break;
                }
            }
        }

        if (!attributeDescriptor.IndexerNamePrefix.IsNullOrEmpty())
        {
            TryAddCompletion(attributeDescriptor.IndexerNamePrefix + "...", descriptionInfo, descriptor, context, RazorCompletionItemKind.DirectiveAttribute, attributeCompletions);
        }
    }
}
