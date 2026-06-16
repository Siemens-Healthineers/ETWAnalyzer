//// SPDX-FileCopyrightText:  © 2026 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT

#nullable enable

using ETWAnalyzer.Commands.MCPServer.Tools;
using ETWAnalyzer.ProcessTools;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;

namespace ETWAnalyzer.Commands.MCPServer
{
    /// <summary>
    /// Runs ETWAnalyzer as a Model Context Protocol (MCP) server over stdio.
    /// Activated via the -mcp command line switch. The server exposes the ETWAnalyzer
    /// dump functionality as MCP tools (see <see cref="EtwAnalyzerTools"/>) that operate
    /// on a shared in-process session.
    /// </summary>
    internal class MCPServerCommand : ArgParser
    {
        private static readonly string MCPServerHelpStringHeader =
            "  -mcp                       Run ETWAnalyzer as an MCP (Model Context Protocol) server over stdio." + Environment.NewLine;

        internal static readonly string HelpString =
            MCPServerHelpStringHeader +
            "            Starts a Model Context Protocol server which exposes the ETWAnalyzer dump commands as MCP tools." + Environment.NewLine +
            "            The server communicates over stdio and is intended to be launched by an MCP capable host (e.g. an AI agent)." + Environment.NewLine;

        /// <summary>
        /// Command specific help.
        /// </summary>
        public override string Help => HelpString;

        /// <summary>
        /// Create MCP server command from command line arguments.
        /// </summary>
        /// <param name="args">Command line arguments.</param>
        public MCPServerCommand(string[] args) : base(args)
        {
        }

        /// <summary>
        /// Parse command line arguments. Currently the -mcp switch takes no additional arguments.
        /// </summary>
        public override void Parse()
        {
            while (myInputArguments.Count > 0)
            {
                string curArg = myInputArguments.Dequeue();
                switch (curArg?.ToLowerInvariant())
                {
                    case CommandFactory.MCPServerCommand:  // -mcp (already known, just consume it)
                        break;
                    case NoColorArg:
                        ColorConsole.EnableColor = false;
                        break;
                    default:
                        break;
                }
            }
        }

        /// <summary>
        /// Run the MCP server. This blocks until the host is shut down (e.g. when the
        /// connected MCP client closes the stdio transport).
        /// </summary>
        public override void Run()
        {
            // Disable colored console output. The stdio transport is used for the MCP
            // protocol so any ANSI color escape sequences would corrupt the protocol stream.
            ColorConsole.EnableColor = false;

            HostApplicationBuilder builder = Host.CreateApplicationBuilder(myOriginalInputArguments);

            builder.Services.AddMcpServer()
                .WithStdioServerTransport()
                .WithTools<EtwAnalyzerTools>();

            IHost app = builder.Build();
            app.Run();
        }
    }
}
