// Copyright (c) 2023, Olaf Kober <olaf.kober@outlook.com>

using System.Globalization;
using Amarok.Diagnostics.Persistence.Tracing.Reader;
using Google.Protobuf;
using Perfetto.Protos;
using Spectre.Console;


namespace Amarok.Diagnostics.Persistence.Tracing;


internal sealed class PerfettoProtobufConverter
{
    public void Run(
        ITraceReader reader,
        String outDir
    )
    {
        var processIds = new Dictionary<String, Int32>(StringComparer.Ordinal);
        var trackIds = new Dictionary<String, Int32>(StringComparer.Ordinal);

        const Int32 baseProcessId = 1000000;
        const Int32 baseTrackId = 10;


        FileStream? writer = null!;
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
                    "{0:yyyyMMdd}_{0:HHmm}_{1:N}.bin",
                    activity.Session.StartTime,
                    activity.Session.Uuid
                );

                var outFilePath = Path.Combine(outDir, outFileName);

                writer?.Dispose();

                writer = new FileStream(outFilePath, FileMode.Create, FileAccess.Write, FileShare.Read);

                AnsiConsole.MarkupLine($"  [grey]Writing {outFileName} to {outDir}[/]");

                lastSession = activity.Session;
                first = true;

                processIds.Clear();
                trackIds.Clear();
            }

            if (first)
            {
                _Protobuf_WriteProcessTrack(writer, baseProcessId, "Global");
                _Protobuf_WriteTrack(writer, baseProcessId, baseTrackId, "Application Session");

                _Protobuf_WriteCompleteEvent(
                    writer,
                    baseTrackId,
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

            if (!trackIds.TryGetValue(activity.Source.Name, out var trackId))
            {
                var processName = Path.GetDirectoryName(activity.Source.Name);
                var trackName = Path.GetFileName(activity.Source.Name);

                Int32 processId;

                if (String.IsNullOrEmpty(processName))
                {
                    processId = baseProcessId;
                }
                else
                {
                    if (!processIds.TryGetValue(processName, out processId))
                    {
                        processId = baseProcessId + processIds.Count + 1;

                        processIds.Add(processName, processId);

                        _Protobuf_WriteProcessTrack(writer!, processId, processName);
                    }
                }

                trackId = baseTrackId + trackIds.Count + 1;

                trackIds.Add(activity.Source.Name, trackId);

                _Protobuf_WriteTrack(writer!, processId, trackId, trackName);
            }

            _Protobuf_WriteCompleteEvent(
                writer!,
                trackId,
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

        writer.Dispose();

        reader.Dispose();

        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine($"[green]SUCCESS:[/] Converted [aqua]{count}[/] activities");
    }


    private static void _Protobuf_WriteCompleteEvent(
        Stream writer,
        Int32 trackId,
        String eventName,
        TimeSpan relativeStartTime,
        TimeSpan duration,
        IReadOnlyList<KeyValuePair<String, Object?>>? args = null,
        String? traceId = null,
        String? spanId = null,
        String? parentSpanId = null
    )
    {
        args ??= Array.Empty<KeyValuePair<String, Object?>>();

        var annotations = new DebugAnnotation { Name = "args" };

        if (args.Count > 0 || traceId != null || spanId != null || parentSpanId != null)
        {
            foreach (var arg in args.OrderBy(x => x.Key))
            {
                annotations.DictEntries.Add(
                    new DebugAnnotation {
                        Name = arg.Key,
                        StringValue = arg.Value?.ToString() ?? "<null>",
                    }
                );
            }

            if (parentSpanId != null)
            {
                annotations.DictEntries.Add(
                    new DebugAnnotation {
                        Name = "id.parent",
                        StringValue = parentSpanId,
                    }
                );
            }

            if (spanId != null)
            {
                annotations.DictEntries.Add(
                    new DebugAnnotation {
                        Name = "id.span",
                        StringValue = spanId,
                    }
                );
            }

            if (traceId != null)
            {
                annotations.DictEntries.Add(
                    new DebugAnnotation {
                        Name = "id.trace",
                        StringValue = traceId,
                    }
                );
            }
        }

        var trace = new Trace {
            Packet = {
                new TracePacket {
                    Timestamp = Convert.ToUInt64(relativeStartTime.Ticks / 10 * 1000),
                    TrackEvent = new TrackEvent {
                        TrackUuid = Convert.ToUInt64(trackId),
                        Type = TrackEvent.Types.Type.SliceBegin,
                        Name = eventName,
                        DebugAnnotations = { annotations },
                    },
                    TrustedPacketSequenceId = 0x12345678,
                },
                new TracePacket {
                    Timestamp = Convert.ToUInt64(( relativeStartTime.Ticks + duration.Ticks ) / 10 * 1000),
                    TrackEvent = new TrackEvent {
                        TrackUuid = Convert.ToUInt64(trackId),
                        Type = TrackEvent.Types.Type.SliceEnd,
                    },
                    TrustedPacketSequenceId = 0x12345678,
                },
            },
        };

        trace.WriteTo(writer);
    }

    private static void _Protobuf_WriteProcessTrack(
        Stream writer,
        Int32 processId,
        String processName
    )
    {
        var trace = new Trace {
            Packet = {
                new TracePacket {
                    TrackDescriptor = new TrackDescriptor {
                        Uuid = Convert.ToUInt64(processId),
                        Name = processName,
                    },
                },
            },
        };

        trace.WriteTo(writer);
    }

    private static void _Protobuf_WriteTrack(
        Stream writer,
        Int32 processId,
        Int32 trackId,
        String trackName
    )
    {
        var trace = new Trace {
            Packet = {
                new TracePacket {
                    TrackDescriptor = new TrackDescriptor {
                        Uuid = Convert.ToUInt64(trackId),
                        ParentUuid = Convert.ToUInt64(processId),
                        Name = trackName,
                    },
                },
            },
        };

        trace.WriteTo(writer);
    }

    private static String _Fmt(
        Guid value
    )
    {
        return value.ToString("D", CultureInfo.InvariantCulture);
    }

    private static String _Fmt(
        DateTimeOffset value
    )
    {
        return value.ToString("O", CultureInfo.InvariantCulture);
    }
}
