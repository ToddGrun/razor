// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis.Razor.Tooltip;

namespace Microsoft.CodeAnalysis.Razor.Completion;

internal class DirectiveAttributeParameterCompletionItemProvider : DirectiveAttributeCompletionItemProviderBase
{
    protected override bool IsValidCompletionLocation(bool isAttributeRequest, bool isParameterRequest)
    {
        // Support completions in both attribute and parameter locations.
        return isAttributeRequest || isParameterRequest;
    }

    protected override void AddCompletionsForBoundAttribute(
        TagHelperDescriptor descriptor,
        BoundAttributeDescriptor attributeDescriptor,
        DirectiveAttributeCompletionContext context,
        Dictionary<string, (ImmutableArray<BoundAttributeDescriptionInfo>, ImmutableArray<RazorCommitCharacter>, RazorCompletionItemKind)> attributeCompletions)
    {
        var isIndexerNamePresent = !attributeDescriptor.IndexerNamePrefix.IsNullOrEmpty();
        //if (!attributeDescriptor.IndexerNamePrefix.IsNullOrEmpty())
        //{
        //    return;
        //}

        if (context.InParameterName && !TagHelperMatchingConventions.CanSatisfyBoundAttribute(context.SelectedAttributeName, attributeDescriptor))
        {
            return;
        }

        foreach (var parameterDescriptor in attributeDescriptor.Parameters)
        {
            if (context.ExistingAttributes.Any(
                    (parameterDescriptor, attributeDescriptor),
                    static (name, arg) =>
                        TagHelperMatchingConventions.SatisfiesBoundAttributeWithParameter(arg.parameterDescriptor, name, arg.attributeDescriptor)))
            {
                // There's already an existing attribute that satisfies this parameter, don't show it in the completion list.
                continue;
            }

            var descriptionInfo = BoundAttributeDescriptionInfo.From(parameterDescriptor, descriptor.TypeName);
            var displayName = context.InParameterName
                ? parameterDescriptor.Name
                : $"{attributeDescriptor.Name}:{parameterDescriptor.Name}";

            AddCompletion(displayName, descriptionInfo, descriptor, context, RazorCompletionItemKind.DirectiveAttribute, attributeCompletions);
        }
    }
}
