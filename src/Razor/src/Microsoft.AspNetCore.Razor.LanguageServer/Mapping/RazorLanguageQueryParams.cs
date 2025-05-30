﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

using System.Text.Json.Serialization;

namespace Microsoft.AspNetCore.Razor.LanguageServer.Mapping;

internal class RazorLanguageQueryParams
{
    [JsonPropertyName("uri")]
    [JsonConverter(typeof(DocumentUriConverter))]
    public required DocumentUri Uri { get; set; }

    [JsonPropertyName("position")]
    public required Position Position { get; set; }
}
