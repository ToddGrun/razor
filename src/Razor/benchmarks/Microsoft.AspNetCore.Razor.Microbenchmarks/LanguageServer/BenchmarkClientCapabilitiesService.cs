﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

#nullable disable

using Microsoft.CodeAnalysis.Razor.Protocol;

namespace Microsoft.AspNetCore.Razor.Microbenchmarks.LanguageServer;

internal class BenchmarkClientCapabilitiesService(VSInternalClientCapabilities capabilities) : IClientCapabilitiesService
{
    public bool CanGetClientCapabilities => true;

    public VSInternalClientCapabilities ClientCapabilities => capabilities;
}
