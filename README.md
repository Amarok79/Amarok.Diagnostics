[![CI](https://github.com/Amarok79/Amarok.Diagnostics/actions/workflows/main.yml/badge.svg)](https://github.com/Amarok79/Amarok.Diagnostics/actions/workflows/main.yml)
[![NuGet](https://img.shields.io/nuget/v/Amarok.Diagnostics.Persistence.Tracing.svg?logo=)](https://www.nuget.org/packages/Amarok.Diagnostics.Persistence.Tracing/)


Table of contents:
- [Abstract](#abstract)
- [Architecture](#architecture)
- [Distribution](#distribution)
- [Usage](#usage)
  - [Setting up Open Telemetry](#setting-up-open-telemetry)
  - [Instrumenting your application](#instrumenting-your-application)
    - [Which information is persisted?](#which-information-is-persisted)
  - [Exporting traces snapshots](#exporting-traces-snapshots)
  - [Reading traces files](#reading-traces-files)
  - [Analyzing traces using Google Perfetto](#analyzing-traces-using-google-perfetto)


# Abstract

Observability is key to success in today's applications and services. It helps to understand the actual runtime behavior and thus provides a valuable tool for diagnosing application issues.

The .NET ecosystem provides a rich set of open-source and commercial observability/telemetry solutions to choose from. Unfortunately, most existing solutions expect instrumented applications and services to be connected with observability/telemetry middleware and backends. That's okay for cloud or even on-premise services but is problematic for isolated desktop or embedded applications running at customer site in private networks without an internet connection.

This project strives to solve that problem by providing a ready-made implementation of a local file-based storage solution for traces, metrics, and logs that is suitable for embedded and desktop applications.

**NOTE:** Right now, only the persistence of traces (activities) is supported. Adding support for metrics and logs is planned for future releases.


# Architecture

The following diagram illustrates the overall architecture.

![Tracing Architecture.png](/doc/Tracing%20Architecture.png)

An application running on an isolated embedded system uses OpenTelemetry and .NET APIs, e.g., ActivitySource and Activity, for instrumentation.

OpenTelemetry is configured to use the ready-made local storage exporter, which persists traces (activities) into size-optimized binary log files. The exporter rolls log files as necessary to stay within a configured maximum disk space reserved for trace storage.

The local storage implementation provides the ability to hot-export (export while actively writing to the local storage) log files into a zip archive. That allows one to take snapshots and transfer them from the embedded system to another system for further analysis.

On the analysis system, one can use a .NET CLI tool to convert binary log files into Perfetto-compatible (Json or Protobuf) format, which can be loaded into Google Perfetto.

Goggle Perfetto is a web-based trace viewer that allows interactive exploration and analysis of recorded traces.

Conversion into other formats is possible but out of scope for this project.


# Distribution

This project makes three packages available.

[Amarok.Diagnostics.Persistence.Tracing](https://www.nuget.org/packages/Amarok.Diagnostics.Persistence.Tracing/) provides an implementation for persisting [System.Diagnostics.Activity](https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.activity) instances into binary, rolling log files (.adtx). A log file reader is also available.

[Amarok.Diagnostics.Persistence.OpenTelementry.Exporter](https://www.nuget.org/packages/Amarok.Diagnostics.Persistence.OpenTelementry.Exporter/) provides an [Open Telemetry](https://opentelemetry.io/) exporter for traces, which uses the above [Amarok.Diagnostics.Persistence.Tracing](https://www.nuget.org/packages/Amarok.Diagnostics.Persistence.Tracing/) for persisting traces into a local directory.

[dotnet-adtx](https://www.nuget.org/packages/dotnet-adtx/) provides a .NET CLI tool for converting binary traces files (.adtx) into a Google Perfetto-compatible format so that trace logs can be visualized and analyzed via https://ui.perfetto.dev/.

In general, packages provide binaries for *.NET 7.0* only.

**NOTE:** A down-port to *.NET 6* or *.NET Standard 2.0* should be easy, but is currently not scope of this project.


# Usage

## Setting up Open Telemetry

To set up *Open Telemetry* with a local persistence store for traces, you need to call `AddAdtxTraceExporter()` on the `TraceProviderBuilder` and provide an `AdtxTraceExporterOptions` instance.

```cs
var localDir = @"D:\data\traces";

var options = new AdtxTraceExporterOptions(localDir) {
    WriterOptions = new TraceWriterOptions {
        MaxDiskSpaceUsedInMegaBytes = 100,
    },
};

var traceProvider = Sdk.CreateTracerProviderBuilder()
    .AddSource("*")
    .AddAdtxTraceExporter(options, out var context)
    .Build();
```

The main configuration options are:
- `Directory`: A path to the directory used as storage location for the binary, rolling traces files (.adtx).
- `MaxDiskSpaceUsedInMegaBytes`: The maximum amount of disk space to use for keeping traces files.

**NOTE:** A dependency on package [Amarok.Diagnostics.Persistence.OpenTelementry.Exporter](https://www.nuget.org/packages/Amarok.Diagnostics.Persistence.OpenTelementry.Exporter/) is need.


## Instrumenting your application

For instrumenting your application and libraries you can use the .NET supplied APIs related to [Activity](https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.activity). See [Adding distributed tracing instrumentation](https://learn.microsoft.com/en-us/dotnet/core/diagnostics/distributed-tracing-instrumentation-walkthroughs) for more information.

**NOTE** No dependency on package [Amarok.Diagnostics.Persistence.Tracing](https://www.nuget.org/packages/Amarok.Diagnostics.Persistence.Tracing/) or [Amarok.Diagnostics.Persistence.OpenTelementry.Exporter](https://www.nuget.org/packages/Amarok.Diagnostics.Persistence.OpenTelementry.Exporter/) is need.


### Which information is persisted?

Not all properties of `Activity` are persisted.

Right now, the following properties are included:
- `Activity`.`Source`.`Name`
- `Activity`.`Source`.`Version`
- `Activity`.`OperationName`
- `Activity`.`StartTimeUtc`
- `Activity`.`Duration`
- `Activity`.`TagObjects`
- `Activity`.`TraceId`
- `Activity`.`ParentSpanId`
- `Activity`.`SpanId`

Explicitely, not serialized are:
- `Activity`.`Baggage` (you should Tags instead)
- `Activity`.`Events` (probably in a future version)
- `Activity`.`Kind` (probably in a future version)
- `Activity`.`Links` (probably in a future version)
- `Activity`.`Status`

If you want to attach custom data to your activities, you should use `Tags` (or `TagObjects`) but not `Baggage`. Use `SetTag()` on the activity as outlined in [Adding distributed tracing instrumentation](https://learn.microsoft.com/en-us/dotnet/core/diagnostics/distributed-tracing-instrumentation-walkthroughs).

Since `Tags` holds `System.Object` you might wonder which data types are supported in serialization.

Generally speaking all types are supported as on most types `ToString()` is called. But, performance- and size-wise it's recommended to limit tag objects to primitive types which are supported via efficient custom serializers. 

Here the complete list of types supported via custom serializers:
- `null`
- `DBNull`
- `Byte`
- `UInt16`
- `UInt32`
- `UInt64`
- `SByte`
- `Int16`
- `Int32`
- `Int64`
- `Boolean`
- `Char`
- `String` (truncated to the first 256 characters)
- `Half`
- `Single`
- `Double`
- `Decimal`
- `DateOnly`
- `TimeOnly`
- `DateTime`
- `DateTimeOffset`
- `Byte[]` (truncated to the first 256 bytes)
- `Memory<Byte>` (truncated to the first 256 bytes)
- `ReadOnlyMemory<Byte>` (truncated to the first 256 bytes)
- `Guid`
- `Object` (converted to String, then truncated to the first 256 characters)
- `Enum`

**NOTE:** Truncation of strings and byte arrays is done to keep the file size small as tags are serialized with every activity. The limit of 256 is not configurable at the moment.


## Exporting traces snapshots

For exporting a snapshot of the traces persisted to the local storage, you need to obtain the `IAdtxTraceExporter` from the `AdtxTraceExporterContext` returned as out parameter when calling `AddAdtxTraceExporter()`.

The `IAdtxTraceExporer` allows to hot-export (export while writing to the local storage) traces into a Zip archive by calling `HotExportAsync()`.

```cs
var localDir = @"d:\data\traces";

var options = new AdtxTraceExporterOptions(localDir) {
    WriterOptions = new TraceWriterOptions {
        MaxDiskSpaceUsedInMegaBytes = 100,
    },
};

var traceProvider = Sdk.CreateTracerProviderBuilder()
    .AddSource("*")
    .AddAdtxTraceExporter(options, out var context)
    .Build();

await context.Exporter.HotExportAsync(@"d:\export.zip");
```

**NOTE:** `HotExportAsync()` can be invoked anytime, but only one export operation is allowed at a time.


## Reading traces files

Traces files (.adtx) are binary files that can be loaded into memory by using the `TraceReader` API from package `Amarok.Diagnostics.Persistence.Tracing`.

You can read in single .adtx files, all .adtx files from a given directory, or all .adtx files from a given Zip archive. Use `OpenFile()`, `OpenFolder()`, and `OpenZipArchive()` respectively.

```cs
using (var reader = TraceReader.OpenFolder(@"d:\data\traces"))
{
    foreach (var activity in reader.Read())
    {
        Console.WriteLine(activity.OperationName);

        // activity.Source
        // activity.Tags
        // activity.StartTime
        // ...
    }
}
```

This API can be used to implement custom converters to arbitrary formats.


## Analyzing traces using Google Perfetto

Goggle Perfetto (https://ui.perfetto.dev/) is a web-based trace viewer that allows interactive exploration and analysis of recorded traces.

To convert the binary traces (.adtx) to a Perfetto-compatible format, you first need to install the .NET CLI tool `dotnet-adtx`. Instructions for installation, can be found at https://www.nuget.org/packages/dotnet-adtx.

After installation, you can convert a single traces file (.adtx)...

```
dotnet adtx convert d:\test\traces\23.adtx d:\
```

all traces files from a given directory, ...
```
dotnet adtx convert d:\test\traces d:\
```

or all traces files from a given Zip archive.

```
dotnet adtx convert d:\test\export.zip d:\
```

Traces are exported into the given output directory, split by application session. An application session represents an application lifetime from start to end.

The resulting JSON files can be loaded into Google Perfetto.

Open the web site https://ui.perfetto.dev/ and choose "Open trace file".
