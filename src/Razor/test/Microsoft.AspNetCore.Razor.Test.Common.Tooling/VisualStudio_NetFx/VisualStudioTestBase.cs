﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Razor.Language.Legacy;
using Microsoft.AspNetCore.Razor.ProjectEngineHost;
using Microsoft.AspNetCore.Razor.Test.Common.ProjectSystem;
using Microsoft.CodeAnalysis.Razor;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;
using Microsoft.VisualStudio.LanguageServices.Razor;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Razor.Test.Common.VisualStudio;

public abstract class VisualStudioTestBase(ITestOutputHelper testOutput) : ToolingParserTestBase(testOutput)
{
    private protected override ProjectSnapshotManagerDispatcher CreateDispatcher()
    {
        var dispatcher = new VisualStudioProjectSnapshotManagerDispatcher(ErrorReporter);
        AddDisposable(dispatcher);

        return dispatcher;
    }

    private protected TestProjectSnapshotManager CreateProjectSnapshotManager()
        => CreateProjectSnapshotManager(triggers: [], ProjectEngineFactories.DefaultProvider);

    private protected TestProjectSnapshotManager CreateProjectSnapshotManager(IProjectSnapshotChangeTrigger[] triggers)
        => CreateProjectSnapshotManager(triggers, ProjectEngineFactories.DefaultProvider);

    private protected TestProjectSnapshotManager CreateProjectSnapshotManager(IProjectEngineFactoryProvider projectEngineFactoryProvider)
        => CreateProjectSnapshotManager(triggers: [], projectEngineFactoryProvider);

    private protected TestProjectSnapshotManager CreateProjectSnapshotManager(IProjectSnapshotChangeTrigger[] triggers, IProjectEngineFactoryProvider projectEngineFactoryProvider)
        => new(triggers, projectEngineFactoryProvider, Dispatcher, ErrorReporter);
}
