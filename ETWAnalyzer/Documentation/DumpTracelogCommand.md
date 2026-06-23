# -Dump TraceLog
ETW does support manifest free ETW providers which use TraceLogging. Manifest free providers do not need to be registered
in the system. They can be added dynamically to the system (kernel and user space is supported). To make them work they emit their manifest along with the
ETW data during trace rundown which enables proper decoding.
ETWAnalyzer can read these events to make them queryable or dumpable to CSV files.

All C# `EventSource` derived providers are TraceLogging (self describing) providers, but you can also emit such events from C++.
Because the event schema travels with the data you can decode the payload fields without having the original manifest installed on the analysis machine.
This makes TraceLogging the ideal mechanism to add custom diagnostic events to your application and analyze them later with ETWAnalyzer.

The command operates in three display modes which are selected by the `-Provider` filter:

1. **Provider Overview** (default, no `-Provider`) — one line per process and provider with the number of logged events.
2. **Event Summary** (`-Provider name`) — for the selected provider/s an aggregated list of all events with their name, id and count.
3. **Detailed Events** (`-Provider name:event1,event2` or `-Provider name:*`) — the individual events sorted by time with their payload fields.

## Extraction

To make TraceLogging events queryable you first need to extract them from the recorded ETL file. Use the `TraceLog` extractor or
`All` which includes it:

```
ETWAnalyzer -extract All -fd xx.etl or
ETWAnalyzer -extract TraceLog -fd xx.etl
```

This produces a Json7z file which contains the decoded provider/event metadata and the event payloads. This file is the input for the `-dump TraceLog` command.

## 1. Provider Overview (Default)

When no `-Provider` filter is specified a per process/provider summary is printed which shows which TraceLogging providers were
active during the trace and how many events each of them did log.

> ETWAnalyzer -fd xx.json7z -dump TraceLog

This is the fastest way to get an idea which custom providers are present in a trace. Use it to discover provider names which you can
then drill into with the `-Provider` filter.

## 2. Event Summary (-Provider name)

When a provider is selected without an explicit event list an event summary is printed for that provider. Each distinct event is
listed with its name, id and the number of occurrences.

> ETWAnalyzer -fd xx.json7z -dump TraceLog -Provider Microsoft-VisualStudio-Common

The `-Provider` filter matches by provider name or Guid. Multiple filters are separated by `;`, exclusion filters start with `!` and
the wildcards `*` and `?` are supported. The example below selects all Visual Studio providers:

> ETWAnalyzer -fd xx.json7z -dump TraceLog -Provider Microsoft-VisualStudio-*

By default the summary is sorted by event count. Use `-SortBy` to change the order:

| `-SortBy` value | Description |
|-----------------|-------------|
| `Count` | Sort by event count (default) |
| `Name` | Sort by event name |
| `Id` | Sort by event id |

> ETWAnalyzer -fd xx.json7z -dump TraceLog -Provider Microsoft-VisualStudio-Common -SortBy Id

## 3. Detailed Events (-Provider name:event)

To view the individual events of a provider append a `:` followed by a comma separated list of event names or event ids. To select
**all** events of a provider use `:*`.

> ETWAnalyzer -fd xx.json7z -dump TraceLog -Provider Microsoft-VisualStudio-Common:vs/telemetryapi/commandlineflags

Event names are matched as case insensitive substrings (the wildcards `*` and `?` are supported), while event ids are matched
exactly. Names and ids can be mixed and excluded with a leading `!`:

> ETWAnalyzer -fd xx.json7z -dump TraceLog -Provider prov1:evName1,evName2;prov2:5,!Verbose

Each detailed line shows by default the time, PID, TID, provider, event name, id and the message which is built from the event
payload fields as a list of `name=value` pairs.

### Configurable Columns

The detailed output columns can be configured with `-Column`. Prefix a column with `!` to disable it, or with `+` to add it to the
default columns. Wildcards `*` and `?` are supported.

| Column | Enabled by default | Description |
|--------|:------------------:|-------------|
| `Time` | yes | Event time stamp (formatted with `-TimeFmt` / `-TimeDigits`) |
| `PID` | yes | Process id which did log the event |
| `TID` | yes | Thread id which did log the event |
| `ProcessName` | no | Name of the logging process |
| `Provider` | yes | TraceLogging provider name |
| `EventName` | yes | Event name |
| `Id` | yes | Event id |
| `Message` | yes | Payload fields as `name=value` pairs |

The `ProcessName` column is not shown by default. To add it on top of the default columns use:

> ETWAnalyzer -fd xx.json7z -dump TraceLog -Provider prov1:* -Column +ProcessName

To restrict the output to just a few columns list them explicitly:

> ETWAnalyzer -fd xx.json7z -dump TraceLog -Provider prov1:* -Column Time;EventName;Message

## Filtering

The following filters apply to the detailed event output and can be combined.

### -Message
Filter for events whose message (the payload `name=value` pairs) contains a substring. Multiple filters are separated by `;`,
exclusion filters start with `!` and the wildcards `*` and `?` are supported. Plain tokens are matched as substrings.

> ETWAnalyzer -fd xx.json7z -dump TraceLog -Provider Microsoft-VisualStudio-Common:vs* -Message *18.5*

### -TID
Filter for events which were logged by the given thread ids. Multiple thread ids are separated by `;`, exclusion filters start with
`!` and the wildcards `*` and `?` are supported.

> ETWAnalyzer -fd xx.json7z -dump TraceLog -Provider prov1:* -TID 4711;1234

### -MinMaxTime
Remove all events which are not within the time filter. The time is specified in ETW session time in seconds since trace start
(use `-TimeFmt s` to display the event time in the same unit). The maximum value is optional.

> ETWAnalyzer -fd xx.json7z -dump TraceLog -Provider prov1:* -MinMaxTime 10 60 -TimeFmt s

### -MaxCount
Display at most the given number of trace messages. This is useful to limit the amount of data when a provider logs a lot of events.

> ETWAnalyzer -fd xx.json7z -dump TraceLog -Provider Microsoft-VisualStudio-Common:vs* -Message *18.5* -MaxCount 10

### -ProcessName/pn
Filter for the processes which did log the events. See the [Dump Process -TimeFmt](DumpProcessCommand.md) section for the start/stop
markers and process time formatting which also apply here.

> ETWAnalyzer -fd xx.json7z -dump TraceLog -Provider prov1:* -ProcessName devenv.exe

## CSV Export

To export the selected events to a CSV file for further processing in Excel or other tools add the `-csv` option. The CSV contains
the process, provider, event name, time, thread id, stack and one column per payload field.

> ETWAnalyzer -fd xx.json7z -dump TraceLog -Provider prov1:* -csv TraceLogData.csv

## Common Options

| Flag | Description |
|------|-------------|
| `-Provider prov[:ev]` | Filter by provider name/Guid and optionally event names/ids. `;` separates, `!` excludes, `*`/`?` wildcards, `:*` selects all events |
| `-SortBy Count/Name/Id` | Sort order of the event summary (default: Count) |
| `-Column col1;col2` | Configure visible columns of the detailed output (`!` disable, `+` add) |
| `-Message string` | Filter for events whose message contains the string |
| `-TID tid1;tid2` | Filter for events logged by the given thread ids |
| `-MinMaxTime minS [maxS]` | Filter events by ETW session time in seconds since trace start |
| `-MaxCount dd` | Display at most dd trace messages |
| `-ProcessName/pn filter` | Filter by the logging process name |
| `-CmdLine substring` | Filter by command line content |
| `-NoCmdLine` | Suppress command line output |
| `-ShowTotal Total/None` | Control the per file total summary display |
| `-TimeFmt` | Time format (s, Local, LocalTime, UTC, UTCTime, Here, HereTime) |
| `-TimeDigits d` | Time precision digits (0-6, default: 3) |
| `-NoDigitSep` | Print numbers without digit grouping separators |
| `-csv xx.csv` | Export the selected events to a CSV file |
| `-Clip` | Copy the console output to the clipboard |

## Recording Hints

TraceLogging providers are emitted by the application itself, so you only need to enable the provider/s of interest during recording.
For a C# `EventSource` the ETW provider name is the `EventSource` name (or the name passed to its constructor). You can enable it with
`wpr` via a recording profile, or with `xperf`/`PerfView` by its provider name or Guid.

A minimal `wpr` recording profile just needs an `EventProvider` entry with the TraceLogging provider name. After recording stop the
session to an ETL file:

The C# defintion would look like this:
```
  [EventSource(Name = "Microsoft-VisualStudio-Common")]
  public sealed class VisualStudioSource : EventSource
```
and the corresponding recording profile would need to contain the following entry:
```
<EventProvider Id="MicrosoftVisualStudioCommon" Name="*Microsoft-VisualStudio-Common" />
```
Please note the * at the beginning of the provider name is the signal to wpr and xperf that this
is a TraceLogging provider which needs during rundown special treatment to emit the manifest along with the ETW data.

Then extract and dump the data as described above.
