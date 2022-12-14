// Copyright (c) 2023, Olaf Kober <olaf.kober@outlook.com>

using System.CommandLine;
using System.Globalization;
using System.IO.Compression;
using System.Text.Json;
using Amarok.Diagnostics.Persistence.Tracing.Reader;
using Spectre.Console;


namespace Amarok.Diagnostics.Persistence.Tracing;


internal sealed class ConvertCommand : Command
{
    internal enum OutputFormat
    {
        PerfettoJson = 1,
    }

    public ConvertCommand()
        : base("convert", "Converts trace log files (.adtx) to another format.")
    {
        var inArgument = new Argument<FileSystemInfo>(
            "in",
            "Path to the input trace log file (.adtx), to a Zip archive containing trace log files, " +
            "or to a directory containing trace log files."
        ) { Arity = ArgumentArity.ExactlyOne };

        var outArgument = new Argument<DirectoryInfo?>(
            "out",
            "Path to the output directory receiving the converted trace files. Defaults to the input directory."
        ) { Arity = ArgumentArity.ZeroOrOne };

        var formatOption = new Option<OutputFormat>("--format", () => OutputFormat.PerfettoJson, "The output format.") {
            Arity = ArgumentArity.ZeroOrOne,
        };

        AddArgument(inArgument);
        AddArgument(outArgument);
        AddOption(formatOption);

        this.SetHandler(
            ctx => _Execute(
                ctx.ParseResult.GetValueForArgument(inArgument),
                ctx.ParseResult.GetValueForArgument(outArgument),
                ctx.ParseResult.GetValueForOption(formatOption)
            )
        );
    }


    private static void _Execute(FileSystemInfo inPath, DirectoryInfo? outDir, OutputFormat format)
    {
        if (File.Exists(inPath.FullName))
        {
            if (_IsZipArchive(inPath.FullName))
            {
                AnsiConsole.MarkupLine($"Converting archive [aqua]{inPath}[/]...");

                _ExecuteCore(
                    TraceReader.OpenZipArchive(inPath.FullName),
                    outDir?.FullName ?? Path.GetDirectoryName(inPath.FullName)!
                );
            }
            else
            {
                AnsiConsole.MarkupLine($"Converting file [aqua]{inPath}[/]...");

                _ExecuteCore(
                    TraceReader.OpenFile(inPath.FullName),
                    outDir?.FullName ?? Path.GetDirectoryName(inPath.FullName)!
                );
            }
        }
        else if (Directory.Exists(inPath.FullName))
        {
            AnsiConsole.MarkupLine($"Converting directory [aqua]{inPath}[/]...");

            _ExecuteCore(TraceReader.OpenFolder(inPath.FullName), outDir?.FullName ?? inPath.FullName);
        }
        else
        {
            AnsiConsole.MarkupLine("[red]ERROR:[/] The given input file or directory does not exist.");
        }
    }


    private static Boolean _IsZipArchive(String filePath)
    {
        try
        {
            ZipFile.OpenRead(filePath).Dispose();

            return true;
        }
        catch
        {
            return false;
        }
    }


    private static void _ExecuteCore(ITraceReader reader, String outDir)
    {
        var tracks = new Dictionary<String, (Int32 Pid, Int32 Tid)>(StringComparer.Ordinal);
        var pids = new Dictionary<String, Int32>(StringComparer.Ordinal);
        var tids = new Dictionary<String, Int32>(StringComparer.Ordinal);

        Utf8JsonWriter? jsonWriter = null;
        SessionInfo? lastSession = null;

        var count = 0;

        var first = true;

        outDir = Path.GetFullPath(outDir);

        if (!Directory.Exists(outDir))
        {
            Directory.CreateDirectory(outDir);
        }

        foreach (var activity in reader.Read())
        {
            if (activity.Session != lastSession)
            {
                var outFileName = String.Format(
                    CultureInfo.InvariantCulture,
                    "{0:yyyyMMdd}_{0:HHmm}_{1:N}.json",
                    activity.Session.StartTime,
                    activity.Session.Uuid
                );

                var outFilePath = Path.Combine(outDir, outFileName);

                jsonWriter?.Dispose();

                jsonWriter = new Utf8JsonWriter(
                    new FileStream(outFilePath, FileMode.Create, FileAccess.Write, FileShare.Read),
                    new JsonWriterOptions { Indented = true }
                );

                AnsiConsole.MarkupLine($"  [grey]Writing {outFileName} to {outDir}[/]");

                lastSession = activity.Session;
                first = true;

                tracks.Clear();
                pids.Clear();
                tids.Clear();
            }

            if (first)
            {
                jsonWriter!.WriteStartObject();

                _WriteOtherData(jsonWriter, activity.Session);

                jsonWriter.WriteStartArray("traceEvents");

                _WriteProcessMetadata(jsonWriter, 0, "Global");
                _WriteThreadMetadata(jsonWriter, 0, 0, "Application Session");

                _WriteCompleteEvent(
                    jsonWriter,
                    0,
                    0,
                    "Session Start",
                    TimeSpan.Zero,
                    TimeSpan.Zero,
                    new KeyValuePair<String, Object?>[] {
                        new("SessionUuid", _Fmt(activity.Session.Uuid)),
                        new("SessionStartTime", _Fmt(activity.Session.StartTime)),
                    }
                );

                first = false;
            }

            if (!tracks.TryGetValue(activity.Source.Name, out var ids))
            {
                var pName = Path.GetDirectoryName(activity.Source.Name);
                var tName = Path.GetFileName(activity.Source.Name);

                Int32 pid;

                if (String.IsNullOrEmpty(pName))
                {
                    pid = 0;
                }
                else
                {
                    if (!pids.TryGetValue(pName, out pid))
                    {
                        pid = ( pids.Count + 1 ) * 1000;

                        pids.Add(pName, pid);

                        _WriteProcessMetadata(jsonWriter!, pid, pName);
                    }
                }

                if (!tids.TryGetValue(activity.Source.Name, out var tid))
                {
                    tid = pid + tids.Count + 1;

                    tids.Add(activity.Source.Name, tid);

                    _WriteThreadMetadata(jsonWriter!, pid, tid, tName);
                }

                ids = ( pid, tid );

                tracks.Add(activity.Source.Name, ids);
            }

            _WriteCompleteEvent(
                jsonWriter!,
                ids.Pid,
                ids.Tid,
                activity.OperationName,
                activity.StartTimeDelta,
                activity.Duration,
                activity.Tags,
                activity.TraceId,
                activity.SpanId,
                activity.ParentSpanId
            );

            count++;
        }

        jsonWriter!.WriteEndArray();
        jsonWriter.WriteEndObject();
        jsonWriter.Dispose();

        reader.Dispose();

        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine($"[green]SUCCESS:[/] Converted [aqua]{count}[/] activities");
    }

    private static void _WriteCompleteEvent(
        Utf8JsonWriter writer,
        Int32 pid,
        Int32 tid,
        String name,
        TimeSpan relativeStartTime,
        TimeSpan duration,
        IReadOnlyList<KeyValuePair<String, Object?>>? args = null,
        String? traceId = null,
        String? spanId = null,
        String? parentSpanId = null
    )
    {
        args ??= Array.Empty<KeyValuePair<String, Object?>>();

        writer.WriteStartObject();
        writer.WriteString("ph", "X");
        writer.WriteString("name", name);
        writer.WriteNumber("pid", pid);
        writer.WriteNumber("tid", tid);
        writer.WriteNumber("ts", relativeStartTime.Ticks / 10);
        writer.WriteNumber("dur", duration.Ticks / 10);

        if (args.Count > 0 || traceId != null || spanId != null || parentSpanId != null)
        {
            writer.WriteStartObject("args");

            foreach (var arg in args)
            {
                writer.WriteString(arg.Key, arg.Value?.ToString() ?? "<null>");
            }

            if (parentSpanId != null)
            {
                writer.WriteString("id.parent", parentSpanId);
            }

            if (spanId != null)
            {
                writer.WriteString("id.span", spanId);
            }

            if (traceId != null)
            {
                writer.WriteString("id.trace", traceId);
            }

            writer.WriteEndObject();
        }

        writer.WriteEndObject();
    }

    private static void _WriteProcessMetadata(Utf8JsonWriter writer, Int32 pid, String name, Int32? sortIndex = null)
    {
        writer.WriteStartObject();
        writer.WriteString("ph", "M");
        writer.WriteString("name", "process_name");
        writer.WriteNumber("pid", pid);
        writer.WriteStartObject("args");
        writer.WriteString("name", name);
        writer.WriteEndObject();
        writer.WriteEndObject();

        if (sortIndex.HasValue)
        {
            writer.WriteStartObject();
            writer.WriteString("ph", "M");
            writer.WriteString("name", "process_sort_index");
            writer.WriteNumber("pid", pid);
            writer.WriteStartObject("args");
            writer.WriteNumber("sort_index", sortIndex.Value);
            writer.WriteEndObject();
            writer.WriteEndObject();
        }
    }

    private static void _WriteThreadMetadata(
        Utf8JsonWriter writer,
        Int32 pid,
        Int32 tid,
        String name,
        Int32? sortIndex = null
    )
    {
        writer.WriteStartObject();
        writer.WriteString("ph", "M");
        writer.WriteString("name", "thread_name");
        writer.WriteNumber("pid", pid);
        writer.WriteNumber("tid", tid);
        writer.WriteStartObject("args");
        writer.WriteString("name", name);
        writer.WriteEndObject();
        writer.WriteEndObject();

        if (sortIndex.HasValue)
        {
            writer.WriteStartObject();
            writer.WriteString("ph", "M");
            writer.WriteString("name", "thread_sort_index");
            writer.WriteNumber("pid", pid);
            writer.WriteNumber("tid", tid);
            writer.WriteStartObject("args");
            writer.WriteNumber("sort_index", sortIndex.Value);
            writer.WriteEndObject();
            writer.WriteEndObject();
        }
    }

    private static void _WriteOtherData(Utf8JsonWriter writer, SessionInfo sessionInfo)
    {
        writer.WriteStartObject("otherData");
        writer.WriteString("SessionUuid", _Fmt(sessionInfo.Uuid));
        writer.WriteString("SessionStartTime", _Fmt(sessionInfo.StartTime));
        writer.WriteEndObject();
    }

    private static String _Fmt(Guid value)
    {
        return value.ToString("D", CultureInfo.InvariantCulture);
    }

    private static String _Fmt(DateTimeOffset value)
    {
        return value.ToString("O", CultureInfo.InvariantCulture);
    }
}
