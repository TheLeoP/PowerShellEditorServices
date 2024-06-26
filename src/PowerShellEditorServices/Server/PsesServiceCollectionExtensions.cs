﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.PowerShell.EditorServices.Hosting;
using Microsoft.PowerShell.EditorServices.Services;
using Microsoft.PowerShell.EditorServices.Services.Extension;
using Microsoft.PowerShell.EditorServices.Services.PowerShell;
using Microsoft.PowerShell.EditorServices.Services.PowerShell.Debugging;
using Microsoft.PowerShell.EditorServices.Services.PowerShell.Host;
using Microsoft.PowerShell.EditorServices.Services.PowerShell.Runspace;
using OmniSharp.Extensions.LanguageServer.Protocol.Server;

namespace Microsoft.PowerShell.EditorServices.Server
{
    internal static class PsesServiceCollectionExtensions
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "VSTHRD110:Observe result of async calls", Justification = "Using lazy initialization.")]
        public static IServiceCollection AddPsesLanguageServices(
            this IServiceCollection collection,
            HostStartupInfo hostStartupInfo)
        {
            return collection
                .AddSingleton(hostStartupInfo)
                .AddSingleton<WorkspaceService>()
                .AddSingleton<SymbolsService>()
                .AddSingleton<PsesInternalHost>()
                .AddSingleton<IRunspaceContext>(
                    (provider) => provider.GetService<PsesInternalHost>())
                .AddSingleton<IInternalPowerShellExecutionService>(
                    (provider) => provider.GetService<PsesInternalHost>())
                .AddSingleton<ConfigurationService>()
                .AddSingleton<IPowerShellDebugContext>(
                    (provider) => provider.GetService<PsesInternalHost>().DebugContext)
                .AddSingleton<EditorOperationsService>()
                .AddSingleton<RemoteFileManagerService>()
                .AddSingleton((provider) =>
                    {
                        ExtensionService extensionService = new(
                            provider.GetService<ILanguageServerFacade>(),
                            provider,
                            provider.GetService<EditorOperationsService>(),
                            provider.GetService<IInternalPowerShellExecutionService>());

                        // This is where we create the $psEditor variable so that when the console
                        // is ready, it will be available. NOTE: We cannot await this because it
                        // uses a lazy initialization to avoid a race with the dependency injection
                        // framework, see the EditorObject class for that!
                        extensionService.InitializeAsync();
                        return extensionService;
                    })
                .AddSingleton<AnalysisService>();
        }

        public static IServiceCollection AddPsesDebugServices(
            this IServiceCollection collection,
            IServiceProvider languageServiceProvider,
            PsesDebugServer psesDebugServer)
        {
            PsesInternalHost internalHost = languageServiceProvider.GetService<PsesInternalHost>();

            return collection
                .AddSingleton(internalHost)
                .AddSingleton<IRunspaceContext>(internalHost)
                .AddSingleton<IPowerShellDebugContext>(internalHost.DebugContext)
                .AddSingleton(languageServiceProvider.GetService<IInternalPowerShellExecutionService>())
                .AddSingleton(languageServiceProvider.GetService<WorkspaceService>())
                .AddSingleton(languageServiceProvider.GetService<RemoteFileManagerService>())
                .AddSingleton(psesDebugServer)
                .AddSingleton<DebugService>()
                .AddSingleton<BreakpointService>()
                .AddSingleton<DebugStateService>()
                .AddSingleton<DebugEventHandlerService>();
        }
    }
}
