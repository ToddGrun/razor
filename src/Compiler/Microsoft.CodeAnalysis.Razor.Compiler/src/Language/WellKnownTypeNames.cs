// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using Microsoft.AspNetCore.Mvc.Razor.Extensions;
using Microsoft.AspNetCore.Razor.Language.Components;

namespace Microsoft.CodeAnalysis.Razor;

internal static class WellKnownTypeNames
{
#if DEBUG
    static WellKnownTypeNames()
    {
        Debug.Assert(Enum.GetNames(typeof(WellKnownType)).Length == Names.Length);
    }
#endif

    // Ensure this stays in sync with the WellKnownType enum
    public static readonly string[] Names =
    [
        ComponentsApi.BindConverter.FullTypeName,
        ComponentsApi.BindElementAttribute.FullTypeName,
        ComponentsApi.BindInputElementAttribute.FullTypeName,
        ComponentsApi.ElementReference.FullTypeName,
        ComponentsApi.EventHandlerAttribute.FullTypeName,
        ComponentsApi.IComponent.FullTypeName,
        ComponentsApi.IComponentRenderMode.FullTypeName,
        ComponentsApi.ParameterAttribute.FullTypeName,
        ComponentsApi.RenderTreeBuilder.FullTypeName,

        TagHelperTypes.ITagHelper,

        ViewComponentTypes.GenericTask,
        ViewComponentTypes.IDictionary,
        ViewComponentTypes.NonViewComponentAttribute,
        ViewComponentTypes.Task,
        ViewComponentTypes.ViewComponentAttribute,

        "Microsoft.AspNetCore.Components.EditorRequiredAttribute"
    ];

    public static string ToFullyQualifiedTypeName(this WellKnownType type)
        => Names[(int)type];
}
