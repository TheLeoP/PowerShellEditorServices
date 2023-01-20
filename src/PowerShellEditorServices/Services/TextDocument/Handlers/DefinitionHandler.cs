﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.PowerShell.EditorServices.Services;
using Microsoft.PowerShell.EditorServices.Services.Symbols;
using Microsoft.PowerShell.EditorServices.Services.TextDocument;
using Microsoft.PowerShell.EditorServices.Utility;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace Microsoft.PowerShell.EditorServices.Handlers
{
    internal class PsesDefinitionHandler : DefinitionHandlerBase
    {
        private readonly SymbolsService _symbolsService;
        private readonly WorkspaceService _workspaceService;

        public PsesDefinitionHandler(
            SymbolsService symbolsService,
            WorkspaceService workspaceService)
        {
            _symbolsService = symbolsService;
            _workspaceService = workspaceService;
        }

        protected override DefinitionRegistrationOptions CreateRegistrationOptions(DefinitionCapability capability, ClientCapabilities clientCapabilities) => new()
        {
            DocumentSelector = LspUtils.PowerShellDocumentSelector
        };

        public override async Task<LocationOrLocationLinks> Handle(DefinitionParams request, CancellationToken cancellationToken)
        {
            ScriptFile scriptFile = _workspaceService.GetFile(request.TextDocument.Uri);

            SymbolReference foundSymbol =
                SymbolsService.FindSymbolAtLocation(
                    scriptFile,
                    request.Position.Line + 1,
                    request.Position.Character + 1);

            List<LocationOrLocationLink> definitionLocations = new();
            if (foundSymbol != null)
            {
                IEnumerable<SymbolReference> foundDefinitions = await _symbolsService.GetDefinitionOfSymbolAsync(
                    scriptFile,
                    foundSymbol,
                    cancellationToken).ConfigureAwait(false);

                foreach (SymbolReference foundDefinition in foundDefinitions)
                {
                    definitionLocations.Add(
                        new LocationOrLocationLink(
                            new Location
                            {
                                Uri = DocumentUri.From(foundDefinition.FilePath),
                                Range = foundDefinition.ScriptRegion.ToRange()
                            }));
                }
            }

            return new LocationOrLocationLinks(definitionLocations);
        }
    }
}
