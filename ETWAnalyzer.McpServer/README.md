# ETWAnalyzer MCP Server

An [MCP (Model Context Protocol)](https://modelcontextprotocol.io/) server that exposes ETWAnalyzer's ETW trace analysis capabilities as tools for AI assistants.

## Overview

This server allows any MCP-compatible AI client (Claude Desktop, GitHub Copilot, etc.) to query ETW trace data by calling tools that map to ETWAnalyzer's dump commands. The server operates **in-process** by directly referencing the ETWAnalyzer library, using the same internal command pattern as the interactive console mode (`-console`).

### Performance

Files are loaded once via `etw_load` and remain in memory for all subsequent queries. This avoids re-reading and decompressing Json7z files on every command—the same optimization used in ETWAnalyzer's console mode.

## Prerequisites

- .NET 10 Runtime (Windows x64)
- Extracted Json7z files (use ETWAnalyzer `-extract` to produce them from ETL files)

## Configuration

### Claude Desktop (`claude_desktop_config.json`)

```json
{
  "mcpServers": {
    "etwanalyzer": {
      "command": "C:\\path\\to\\ETWAnalyzer.McpServer.exe",
    }
  }
}
```

### Visual Studio / GitHub Copilot

Add to your `.vscode/mcp.json` or Visual Studio MCP configuration:

```json
{
  "servers": {
    "etwanalyzer": {
      "type": "stdio",
      "command": "C:\\path\\to\\ETWAnalyzer.McpServer.exe",
    }
  }
}
```

## Available Tools

| Tool | Description |
|------|-------------|
| `etw_load` | Load Json7z/Json files into the session (kept in memory) |
| `etw_unload` | Unload files from the session |
| `etw_list` | List currently loaded files |
| `etw_dump_cpu` | Dump CPU consumption data (sampling/context switch) |
| `etw_dump_process` | Dump process information (name, PID, command line, times) |
| `etw_dump_exception` | Dump .NET exceptions with types, messages, counts |
| `etw_dump_memory` | Dump memory usage (Working Set, Committed Memory) |
| `etw_dump_disk` | Dump Disk I/O data |
| `etw_dump_file` | Dump File I/O data |
| `etw_dump_dns` | Dump DNS query latency data |
| `etw_dump_tcp` | Dump TCP connection data |
| `etw_dump_marker` | Dump ETW marker events |
| `etw_dump_threadpool` | Dump .NET ThreadPool starvation events |
| `etw_dump_power` | Dump Windows Power Profile settings |
| `etw_dump_stats` | Dump ETW trace statistics |
| `etw_dump_version` | Dump module/DLL version information |
| `etw_dump_objectref` | Dump kernel object handle events |
| `etw_dump_virtualalloc` | Dump VirtualAlloc memory allocation events |
| `etw_dump_lbr` | Dump Last Branch Record profiling data |
| `etw_dump_pmc` | Dump Performance Monitor Counter data |
| `etw_help` | Get help for ETWAnalyzer dump commands |

## Workflow

1. **Load** extracted Json7z files into the session:
   ```
   etw_load(filePaths: "C:\\traces\\Extract\\mytrace.json7z")
   ```
   Or load an entire directory:
   ```
   etw_load(filePaths: "C:\\traces\\Extract", recursive: true)
   ```

2. **Query** the loaded data using dump tools (files stay in memory):
   ```
   etw_dump_cpu(arguments: "-Methods *Initialize* -topN 5 -ProcessName myapp.exe")
   ```

3. **Investigate** specific issues:
   ```
   etw_dump_exception(arguments: "-Type *TimeoutException* -ShowStack")
   ```

4. **Filter** loaded files with `-fd` in any dump command:
   ```
   etw_dump_cpu(arguments: "-fd *testcase1* -Methods *Render*")
   ```

5. **Unload** when done:
   ```
   etw_unload()
   ```

## Building

```bash
dotnet build ETWAnalyzer.McpServer\ETWAnalyzer.McpServer.csproj
```

## Running Standalone

```bash
dotnet run --project ETWAnalyzer.McpServer
```

The server will start and listen on stdin/stdout for MCP protocol messages.
