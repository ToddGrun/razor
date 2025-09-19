// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.Razor.Language;

namespace Microsoft.AspNetCore.Razor.Microbenchmarks;

public class TagHelperBinderBenchmark
{
    // We create a number of binders to get a measurable time.
    private const int Count = 5000;

    private readonly ReadOnlyDictionary<string, ImmutableArray<TagHelperDescriptor>>[] _tagNameToDescriptorsMaps = new ReadOnlyDictionary<string, ImmutableArray<TagHelperDescriptor>>[Count];
    private ImmutableArray<TagHelperDescriptor> _tagHelpers;

    [ParamsAllValues]
    public TagHelpers TagHelpers { get; set; }

    [IterationSetup]
    public void IterationSetup()
    {
        _tagHelpers = TagHelpers switch
        {
            TagHelpers.BlazorServerApp => TagHelperResources.BlazorServerApp,
            TagHelpers.TelerikMvc => TagHelperResources.TelerikMvc,
            _ => Assumed.Unreachable<ImmutableArray<TagHelperDescriptor>>()
        };
    }

    [IterationCleanup]
    public void IterationCleanUp()
    {
        Array.Clear(_tagNameToDescriptorsMaps);
    }

    [Benchmark(Description = "Construct TagHelperBinders (Original)")]
    public void ConstructTagHelperBinders_Original()
    {
        for (var i = 0; i < Count; i++)
        {
            TagHelperBinder.ProcessDescriptorsOriginal(_tagHelpers, tagNamePrefix: null, out _tagNameToDescriptorsMaps[i], out _);
        }
    }

    [Benchmark(Description = "Construct TagHelperBinders (Original, with prefix)")]
    public void ConstructTagHelperBinderWithPrefix_Original()
    {
        for (var i = 0; i < Count; i++)
        {
            TagHelperBinder.ProcessDescriptorsOriginal(_tagHelpers, tagNamePrefix: "abc", out _tagNameToDescriptorsMaps[i], out _);
        }
    }

    [Benchmark(Description = "Construct TagHelperBinders (Commit 1)")]
    public void ConstructTagHelperBinders_Commit1()
    {
        for (var i = 0; i < Count; i++)
        {
            TagHelperBinder.ProcessDescriptorsCommit1(_tagHelpers, tagNamePrefix: null, out _tagNameToDescriptorsMaps[i], out _);
        }
    }

    [Benchmark(Description = "Construct TagHelperBinders (Commit 1, with prefix)")]
    public void ConstructTagHelperBinderWithPrefix_Commit1()
    {
        for (var i = 0; i < Count; i++)
        {
            TagHelperBinder.ProcessDescriptorsCommit1(_tagHelpers, tagNamePrefix: "abc", out _, out _);
        }
    }

    [Benchmark(Description = "Construct TagHelperBinders (Commit 3)")]
    public void ConstructTagHelperBinders_Commit3()
    {
        for (var i = 0; i < Count; i++)
        {
            TagHelperBinder.ProcessDescriptorsCommit3(_tagHelpers, tagNamePrefix: null, out _, out _);
        }
    }

    [Benchmark(Description = "Construct TagHelperBinders (Commit 3, with prefix)")]
    public void ConstructTagHelperBinderWithPrefix_Commit3()
    {
        for (var i = 0; i < Count; i++)
        {
            TagHelperBinder.ProcessDescriptorsCommit3(_tagHelpers, tagNamePrefix: "abc", out _, out _);
        }
    }
}
