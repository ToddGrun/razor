// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.CodeAnalysis.Razor;

// Ensure this stays in sync with WellKnownTypeNames.Names.
internal enum WellKnownType
{
    BindConverter,
    BindElementAttribute,
    BindInputElementAttribute,
    ElementReference,
    EventHandlerAttribute,
    IComponent,
    IComponentRenderMode,
    ParameterAttribute,
    RenderTreeBuilder,

    ITagHelper,

    GenericTask,
    IDictionary,
    NonViewComponentAttribute,
    Task,
    ViewComponentAttribute,

    EditorRequiredAttribute,
}
