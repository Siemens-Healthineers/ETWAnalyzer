---
name: ETWAnalyzer
description: This skill should be used to interact with the ETWAnalyzer (Model Context Protocol) server. The MCP server provides tools to analyze ETW (Event Tracing for Windows) performance traces without needing to manually run command-line tools.
version: 0.1.0
---

# ETWAnalyzer MCP Server - Agent Documentation

This document describes how to use the ETWAnalyzer MCP (Model Context Protocol). The MCP server provides tools to analyze ETW (Event Tracing for Windows) performance traces without needing to manually run command-line tools.

## Overview

The ETWAnalyzer MCP server exposes ETWAnalyzer functionality as callable tools that can:
- Load and manage ETW trace files (.json7z/.json extracts)
- Query CPU, memory, disk, network, exception, and other performance data
- Analyze cpu issues, memory and handle leaks, thread pool starvation, and other runtime issues
- All without switching to command-line tools manually

## Table of Contents

- [Getting Started](#getting-started)
- [Session Management](#session-management)
- [Available Tools](#available-tools)
- [Common Patterns](#common-patterns)
- [Real-World Examples](#real-world-examples)
- [Troubleshooting](#troubleshooting)

---

## Getting Started

### Prerequisites

1. **Extracted ETW data**: ETWAnalyzer works with extracted JSON files (.json7z or .json), not raw .etl files
2. **File path**: Know the full path to your extracted trace file(s)

### Basic Workflow

```
1. Load trace file(s)    → etw_load
2. Query data            → etw_dump_* (cpu, memory, process, etc.)
3. Analyze results       → Interpret output
4. Optional: Unload      → etw_unload
```

---

## Session Management

### Loading Files

**Load a single extracted trace file:**
```
User: "E:\Unsynced\Carbon\UserHandleLeak\Extract\ContainerStart.json7z"
```

The agent will call `etw_load` automatically. Files remain loaded for the entire session.

**Load multiple files or directories:**
```
etw_load:
  filePaths: "C:\Extract\Test1.json7z,C:\Extract\Test2.json7z"
  
etw_load:
  filePaths: "C:\Extract"
  recursive: true
```

**Add files to existing session (don't replace):**
```
etw_load:
  filePaths: "C:\Extract\NewTrace.json7z"
  keepOldFiles: true
```

### Listing Loaded Files

**Check what's currently loaded:**
```
User: "What files are loaded?"
→ Agent calls: etw_list
```

### Unloading Files

**Unload all files:**
```
etw_unload
```

**Unload specific files:**
```
etw_unload:
  filePaths: "C:\Extract\Test1.json7z"
```

---

## Available Tools

### Core Analysis Tools

| Tool | Purpose | Key Arguments |
|------|---------|---------------|
| `etw_dump_cpu` | CPU consumption by process/method | `-topN`, `-topNMethods`, `-Methods`, `-StackTags`, `-ProcessName` |
| `etw_dump_memory` | Memory usage (WorkingSet, Commit) | `-topN`, `-ProcessName`, `-SortBy`, `-GlobalDiffMB` |
| `etw_dump_process` | Process info (start/stop, command line, return code) | `-ProcessName`, `-NewProcess`, `-SortBy`, `-Crash` |
| `etw_dump_exception` | .NET exceptions with stacks | `-Type`, `-ShowStack`, `-ProcessName` |
| `etw_dump_disk` | Disk I/O per directory/file/process | `-DirLevel`, `-ProcessName`, `-PerProcess`, `-MinMaxSize` |
| `etw_dump_file` | File I/O operations (read/write/open/close) | `-DirLevel`, `-ProcessName`, `-PerProcess`, `-FileOperation`, `-fileName` |
| `etw_dump_tcp` | TCP connection statistics | `-SortBy`, `-MinMaxRetransCount` |
| `etw_dump_dns` | DNS query latency and results | `-DnsQueryFilter`, `-MinMaxTime` |
| `etw_dump_stats` | Trace metadata (OS, CPU, duration, etc.) | `-Properties *` |
| `etw_dump_version` | Module/DLL versions | `-dll`, `-ProcessName` |
| `etw_dump_objectref` | Handle/object tracking (leaks) | `-Leak`, `-ShowStack`, `-ProcessName` |
| `etw_dump_virtualalloc` | VirtualAlloc memory allocations | `-Details`, `-ShowStack`, `-TopNStacks` |
| `etw_dump_threadpool` | .NET ThreadPool starvation events | `-ProcessName` |
| `etw_dump_marker` | ETW marker events | `-MarkerFilter`, `-ZeroTime` |
| `etw_dump_power` | Power profile settings | `-details`, `-Diff` |
| `etw_dump_pmc` | Performance Monitoring Counters | `-pn` |
| `etw_dump_lbr` | Last Branch Record CPU profiling | `-pn`, `-topnmethods`, `-showcaller` |

### Getting Help

**Get help for a specific command:**
```
etw_help:
  command: "CPU"
```

---

## Common Patterns

### 1. Initial Trace Investigation

**Load and get basic stats:**
```
etw_load → "E:\Traces\Issue.json7z"
etw_dump_stats → arguments: "-Properties *"
```

**Find top CPU consumers:**
```
etw_dump_cpu:
  arguments: "-topN 10"
```

**See process timeline:**
```
etw_dump_process:
  arguments: "-ProcessFmt s -SortBy StartTime"
```

### 2. CPU Analysis

**Top processes with method breakdown:**
```
etw_dump_cpu:
  arguments: "-topN 5 -topNMethods 150"
```

**Analyze specific process with stacktags:**
```
etw_dump_cpu:
  arguments: "-ProcessName *syngo.Viewing* -StackTags * -ShowTotal Process"
```

**Find methods waiting longest:**
```
etw_dump_cpu:
  arguments: "-Methods * -topNMethods 150 -SortBy wait"
```

**Find top CPU consumer methods deepest in stacktrace. Useful to find CPU hogs which a nearly stacktrace view by using aggregates.**
```
etw_dump_cpu:
  arguments: "-Methods * -topNMethods 150 -SortBy stackdepth"
```

**Find methods consuming most CPU:**
```
etw_dump_cpu:
  arguments: "-Methods * -topNMethods 150 -SortBy CPU"
```

**Show first/last occurrence of methods (for duration analysis):**
```
etw_dump_cpu:
  arguments: "-ProcessName myapp.exe -Methods *Initialize* -fld s s"
```

**Include thread count and module info:**
```
etw_dump_cpu:
  arguments: "-ProcessName myapp.exe -Methods * -ThreadCount -ShowModuleInfo"
```

**Filter by CPU time:**
```
etw_dump_cpu:
  arguments: "-MinMaxCpuMs 100 -ProcessName myapp.exe"
```

### 3. Memory Analysis

**Top memory consumers:**
```
etw_dump_memory:
  arguments: "-topN 10 -SortBy Commit"
```

**Find memory leaks across multiple traces:**
```
etw_dump_memory:
  arguments: "-GlobalDiffMB 500"
```

**Total machine memory:**
```
etw_dump_memory:
  arguments: "-TotalMemory"
```

### 4. Process Analysis

**Find crashed processes:**
```
etw_dump_process:
  arguments: "-Crash"
```

**Show process tree:**
```
etw_dump_process:
  arguments: "-SortBy Tree"
```

**New processes started during trace:**
```
etw_dump_process:
  arguments: "-NewProcess 1"
```

**Filter by process name:**
```
etw_dump_process:
  arguments: "-ProcessName *chrome*;!*chromers*"
```

### 5. Exception Analysis

**All exceptions with stacks:**
```
etw_dump_exception:
  arguments: "-ShowStack"
```

**Filter by exception type:**
```
etw_dump_exception:
  arguments: "-Type *TimeoutException* -ShowStack"
```

### 6. Handle Leak Investigation

**Find handle leaks:**
```
etw_dump_objectref:
  arguments: "-Leak -ShowStack -ProcessName myapp.exe"
```

**Show top stack traces:**
```
etw_dump_virtualalloc:
  arguments: "-Details -ShowStack -TopNStacks 10 -ProcessName myapp.exe"
```

### 7. Disk and File I/O

**Disk I/O by directory:**
```
etw_dump_disk:
  arguments: "-DirLevel 3 -PerProcess"
```

**File operations:**
```
etw_dump_file:
  arguments: "-PerProcess -fileName E:\\Data\\*"
```

### 8. Network Analysis

**TCP connections with retransmissions:**
```
etw_dump_tcp:
  arguments: "-SortBy RetransmissionCount -MinMaxRetransCount 1"
```

**DNS queries taking too long:**
```
etw_dump_dns:
  arguments: "-MinMaxTime 20ms"
```

### 9. Extended CPU Metrics

**Show detailed CPU data with frequency and ready percentile times:**
```
etw_dump_cpu:
  arguments: "-topN 3 -topNMethods 10 -Details"
```

**Normalized CPU time (for frequency comparison):**
```
etw_dump_cpu:
  arguments: "-topN 5 -Details -Normalize"
```

**Hide specific columns:**
```
etw_dump_cpu:
  arguments: "-topN 5 -Details -NoFrequency -NoReady"
```

---

## Real-World Examples

### Example 1: Investigating UI Hang

**Scenario:** Application UI becomes unresponsive for several seconds

```
1. Load trace
   etw_load → "C:\Traces\UIHang.json7z"

2. Check CPU during hang window
   etw_dump_cpu → "-Methods * -MinMaxFirst 45 50 -topNMethods 30"
   
3. Look for long waits
   etw_dump_cpu → "-SortBy Wait -topN 5 -topNMethods 50"
   
4. Check for exceptions
   etw_dump_exception → "-ShowStack"
```

### Example 2: Memory Leak Detection

**Scenario:** Process memory grows over time

```
1. Load multiple sequential traces
   etw_load → "C:\Traces\LongRun" (directory with multiple files)
   recursive: true
   
2. Find leaking processes
   etw_dump_memory → "-GlobalDiffMB 100"
   
3. Investigate VirtualAlloc patterns
   etw_dump_virtualalloc → "-ProcessName leaky.exe -Details -ShowStack -TopNStacks 20"
   
4. Check handle leaks
   etw_dump_objectref → "-Leak -ProcessName leaky.exe -ShowStack"
```

### Example 3: Slow Startup Investigation

**Scenario:** Application startup time has regressed

```
1. Load trace
   etw_load → "C:\Traces\Startup.json7z"
   
2. See process start order
   etw_dump_process → "-ProcessFmt s -SortBy StartTime"
   
3. Analyze startup CPU
   etw_dump_cpu → "-ProcessName myapp.exe -Methods * -fld s s -SortBy CPU -topnmethods 150"

4. Analyze startup wait
   etw_dump_cpu → "-ProcessName myapp.exe -Methods * -fld s s -SortBy wait -topnmethods 150"
   
5. Check for file I/O delays
   etw_dump_file → "-ProcessName myapp.exe -PerProcess"
   
6. Look for network delays
   etw_dump_tcp → "-ProcessName myapp.exe"
   etw_dump_dns → "-MinMaxTime 50ms"
```

### Example 4: Finding Virus Scanner Impact

**Scenario:** Suspecting AV software slowing down operations

```
1. Load trace
   etw_load → "C:\Traces\Performance.json7z"
   
2. Check for AV drivers
   etw_dump_cpu → "-ShowModuleInfo Driver -topN 50"
   
3. Look for antivirus stacktaqs
   etw_dump_cpu → "-StackTags *Virus*"
   
4. Check disk I/O patterns
   etw_dump_disk → "-PerProcess"
```

### Example 5: Thread Pool Starvation

**Scenario:** .NET application has delayed task execution

```
1. Load trace
   etw_load → "C:\Traces\ThreadIssue.json7z"
   
2. Check for thread starvation events
   etw_dump_threadpool → "-ProcessName myapp.exe"
  
3. Analyze wait times
   etw_dump_cpu → "-ProcessName myapp.exe -SortBy Wait -topNMethods 150"
```

### Example 6: Comparing Performance Between Versions

**Scenario:** Need to compare two builds

```
1. Load both traces
   etw_load → "C:\Traces\Baseline.json7z"
   etw_load → "C:\Traces\New.json7z" (keepOldFiles: true)
   
2. Compare CPU totals
   etw_dump_cpu → "-ProcessName myapp.exe -ShowTotal Process"
   
3. Compare specific methods
   etw_dump_cpu → "-Methods *Initialize* -ProcessName myapp.exe"
   
4. Compare memory usage
   etw_dump_memory → "-ProcessName myapp.exe"
```

---

## Troubleshooting

### Common Issues

**1. "No files loaded"**
- Make sure to call `etw_load` before dump commands
- Check file path is correct (use full absolute path)
- Verify file extension is .json7z or .json

**2. "No data found"**
- The extractor may not have included this data type
- Check what was extracted: `etw_dump_stats`
- Re-extract with needed extractors (see ETWAnalyzer documentation)

**3. Empty results**
- Process name filter may be too restrictive
- Try without filters first: `etw_dump_process` to see all processes
- Use wildcards: `-ProcessName *partial*`

**4. Too much output**
- Use `-topN` to limit processes
- Use `-topNMethods` to limit methods
- Filter by CPU/time: `-MinMaxCpuMs 100`

**5. Need more detail**
- Add `-Details` for extended metrics
- Use `-ShowStack` for exception/objectref
- Use `-ShowModuleInfo` for version info
- Use `-ThreadCount` for thread information

### Filter Syntax

ETWAnalyzer uses consistent filter syntax:

- **Multiple filters:** Separate with `;`
  - Example: `*chrome*;*firefox*`

- **Exclusions:** Prefix with `!`
  - Example: `*chrome*;!*chromers*`

- **Wildcards:** 
  - `*` matches zero or more characters
  - `?` matches zero or one character

- **Case-insensitive:** All filters are case-insensitive

### Time Formats

Many commands support `-TimeFmt` for time display:

| Format | Description | Example |
|--------|-------------|---------|
| `s` | Seconds since trace start | `45.123` |
| `Local` | Local time with date | `2024-04-28 11:55:20.123` |
| `LocalTime` | Local time without date | `11:55:20.123` |
| `UTC` | UTC time with date | `2024-04-28 09:55:20.123` |
| `UTCTime` | UTC time without date | `09:55:20.123` |

### Process Filters

`-NewProcess` values:

| Value | Meaning |
|-------|---------|
| `0` | Processes running entire trace |
| `1` | Processes started during trace |
| `-1` | Processes exited during trace |
| `2` | Started but not stopped |
| `-2` | Stopped but not started |

---

## Advanced Features

### StackTags

StackTags group CPU consumption by semantic categories (e.g., .NET GC, JIT, WPF rendering, SQL queries).

**View all stacktags:**
```
etw_dump_cpu:
  arguments: "-StackTags * -ProcessName myapp.exe -ShowTotal Method"
```

**Filter specific categories:**
```
etw_dump_cpu:
  arguments: "-StackTags *GC*;*JIT* -ShowTotal Process"
```

### Zero Time

Shift time baseline for relative analysis:

**Relative to first method occurrence:**
```
etw_dump_cpu:
  arguments: "-Methods *Initialize* -ZeroTime First *OnClick* -fld s s"
```

**Relative to process start:**
```
etw_dump_cpu:
  arguments: "-ZeroTime ProcessStart -ZeroProcessName myapp.exe -fld s s"
```

### Marker Events

Correlate with custom ETW markers:

```
etw_dump_marker:
  arguments: "-MarkerFilter *TestStart* -ZeroTime marker *TestStart*"
```

### CSV Export

All dump commands support `-csv` for exporting to files. These can be examined further. 

```
etw_dump_cpu:
  arguments: "-ProcessName myapp.exe -Methods * -csv C:\\Results\\cpu_data.csv"
```

---

## Best Practices

1. **Start broad, then narrow:**
   - Begin with `-topN 10` to see big picture
   - Drill down with `-ProcessName` filters
   - Use `-Methods *pattern*` or `-Stacktags *pattern*` for specific investigations

2. **Use appropriate metrics:**
   - CPU analysis: Focus on `-topNMethods` and stacktags
   - Memory leaks: Use `-GlobalDiffMB` across traces
   - Hangs: Check `-SortBy Wait` and exception data

3. **Leverage multiple data sources:**
   - CPU + Exception + File I/O gives complete picture
   - Process + Memory + ObjectRef for leak investigations
   - TCP + DNS for network issues

4. **Time correlation:**
   - Use `-fld s s` to see when methods execute
   - Use `-ProcessFmt s` to see process timelines
   - Use `-ZeroTime` to align multiple traces

5. **Documentation:**
   - Use `etw_help` for command-specific details
   - Check `etw_dump_stats` for trace metadata
   - Verify data availability before deep analysis

---

## Useful Queries Quick Reference

```bash
# Quick overview
etw_dump_stats → "-Properties *"
etw_dump_process → "-SortBy StartTime"
etw_dump_cpu → "-topN 10"

# CPU deep dive
etw_dump_cpu → "-ProcessName myapp* -StackTags * -ShowTotal Method -topNMethods 150"

# Memory investigation
etw_dump_memory → "-topN 10 -Details"
etw_dump_virtualalloc → "-Details -ShowStack -TopNStacks 10"

# Exception analysis
etw_dump_exception → "-Type * -ShowStack"

# Network issues
etw_dump_tcp → "-SortBy RetransmissionCount"
etw_dump_dns → "-MinMaxTime 100ms"

# File I/O
etw_dump_file → "-PerProcess -fileName C:\\*"
etw_dump_disk → "-DirLevel 3"

# Handle leaks
etw_dump_objectref → "-Leak -ShowStack"

# Process crashes
etw_dump_process → "-Crash"
```

---

## Additional Resources

- **ETWAnalyzer GitHub:** https://github.com/Alois-xx/ETWAnalyzer
- **WPA (Windows Performance Analyzer):** For visual correlation with trace data
- **ETW Recording:** Use ETWController or wpr.exe to capture traces
- **Symbol Servers:** Configure `-SymServer` during extraction for full method names

---

## Version Information

This documentation is based on ETWAnalyzer version 4.5.0.1+ with MCP server integration.

For questions or issues with the MCP server, contact the ETWAnalyzer team or file an issue on the GitHub repository.
