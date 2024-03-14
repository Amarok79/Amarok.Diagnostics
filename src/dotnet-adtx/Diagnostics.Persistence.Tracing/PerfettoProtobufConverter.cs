// Copyright (c) 2023, Olaf Kober <olaf.kober@outlook.com>

using System.Diagnostics;
using System.Globalization;
using Amarok.Diagnostics.Persistence.Tracing.Reader;
using Google.Protobuf;
using Perfetto.Protos;
using Spectre.Console;
using Trace = Perfetto.Protos.Trace;


namespace Amarok.Diagnostics.Persistence.Tracing;


internal sealed class PerfettoProtobufConverter
{
    private const Int32 PacketSequenceId = 0x01;
    private const Int32 ProcessInitialId = 10000000;
    private const Int32 TrackInitialId = 1;
    private const Int32 EventNameInitialId = 1;
    private const Int32 ArgNameInitialId = 1;
    private const Int32 ArgValueInitialId = 1;

    private readonly Dictionary<String, UInt64> mProcessIds = new(StringComparer.Ordinal);
    private readonly Dictionary<String, UInt64> mTrackIds = new(StringComparer.Ordinal);
    private readonly Dictionary<String, UInt64> mEventNames = new(StringComparer.Ordinal);
    private readonly Dictionary<String, UInt64> mArgNames = new(StringComparer.Ordinal);
    private readonly Dictionary<String, UInt64> mArgValues = new(StringComparer.Ordinal);

    private FileStream? mWriter;
    private Int64 mCount;


    public void Run(
        ITraceReader reader,
        String outDir,
        Boolean includeIds
    )
    {
        outDir = Path.GetFullPath(outDir);

        _CreateOutDirectory(outDir);

        _RunCore(reader, outDir, includeIds);
    }


    private void _RunCore(
        ITraceReader reader,
        String outDir,
        Boolean includeIds
    )
    {
        var sw = Stopwatch.StartNew();

        var lastSession = (SessionInfo?)null;
        var isFirst = true;

        foreach (var activity in reader.Read())
        {
            if (activity.Session != lastSession)
            {
                _StartNewSession(activity, outDir);

                lastSession = activity.Session;
                isFirst = true;
            }

            if (isFirst)
            {
                _WriteFileHeader(activity);

                isFirst = false;
            }

            var trackId = _EnsureTrack(activity);

            _Protobuf_WriteCompleteEvent(
                trackId,
                activity.OperationName,
                activity.StartTimeDelta,
                activity.Duration,
                activity.Tags,
                includeIds,
                activity.TraceId,
                activity.SpanId,
                activity.ParentSpanId
            );

            mCount++;
        }

        mWriter?.Dispose();

        reader.Dispose();

        //AnsiConsole.WriteLine();
        //AnsiConsole.MarkupLine("[grey]Statistics:[/]");
        //AnsiConsole.MarkupLine($"[grey]  Processes  :  {mProcessIds.Count}[/]");
        //AnsiConsole.MarkupLine($"[grey]  Tracks     :  {mTrackIds.Count}[/]");
        //AnsiConsole.MarkupLine($"[grey]  Event Names:  {mEventNames.Count}[/]");
        //AnsiConsole.MarkupLine($"[grey]  Arg Names  :  {mArgNames.Count}[/]");
        //AnsiConsole.MarkupLine($"[grey]  Arg Values :  {mArgValues.Count}[/]");

        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine(
            $"[green]SUCCESS:[/] Converted [aqua]{mCount}[/] activities " +
            $"in [aqua]{sw.ElapsedMilliseconds:D}[/] ms"
        );
    }


    private void _StartNewSession(
        ActivityInfo activity,
        String outDir
    )
    {
        var outFileName = String.Format(
            CultureInfo.InvariantCulture,
            "{0:yyyyMMdd}_{0:HHmm}_{1:N}.bin",
            activity.Session.StartTime,
            activity.Session.Uuid
        );

        var outFilePath = Path.Combine(outDir, outFileName);

        mWriter?.Dispose();

        mWriter = new FileStream(outFilePath, FileMode.Create, FileAccess.Write, FileShare.Read);

        mProcessIds.Clear();
        mTrackIds.Clear();
        mEventNames.Clear();
        mArgNames.Clear();
        mArgValues.Clear();

        AnsiConsole.MarkupLine($"  [grey]Writing {outFileName} to {outDir}[/]");
    }

    private void _WriteFileHeader(
        ActivityInfo activity
    )
    {
        _Protobuf_WriteFirstPacket();
        _Protobuf_WriteTrack(TrackInitialId, "Application Session");

        _Protobuf_WriteCompleteEvent(
            TrackInitialId,
            "Session Start",
            TimeSpan.Zero,
            TimeSpan.Zero,
            new KeyValuePair<String, Object?>[] {
                new("SessionUuid", activity.Session.Uuid.ToString("D", CultureInfo.InvariantCulture)),
                new(
                    "SessionStartTime",
                    activity.Session.StartTime.ToString("O", CultureInfo.InvariantCulture)
                ),
            }
        );
    }

    private UInt64 _EnsureTrack(
        ActivityInfo activity
    )
    {
        if (mTrackIds.TryGetValue(activity.Source.Name, out var trackId))
        {
            return trackId;
        }

        var processName = Path.GetDirectoryName(activity.Source.Name);
        var trackName = Path.GetFileName(activity.Source.Name);

        UInt64 processId;

        if (String.IsNullOrEmpty(processName))
        {
            processId = ProcessInitialId;
        }
        else
        {
            if (!mProcessIds.TryGetValue(processName, out processId))
            {
                processId = (UInt64)(ProcessInitialId + mProcessIds.Count + 1);

                mProcessIds.Add(processName, processId);

                _Protobuf_WriteProcessTrack(processId, processName);
            }
        }

        trackId = (UInt64)(TrackInitialId + mTrackIds.Count + 1);

        mTrackIds.Add(activity.Source.Name, trackId);

        _Protobuf_WriteTrack(trackId, trackName, processId);

        return trackId;
    }


    private void _Protobuf_WriteFirstPacket()
    {
        var trace = new Trace {
            Packet = {
                new TracePacket {
                    SequenceFlags = (UInt32)TracePacket.Types.SequenceFlags.SeqIncrementalStateCleared,
                    TrustedPacketSequenceId = PacketSequenceId,
                },
            },
        };

        trace.WriteTo(mWriter);
    }

    private void _Protobuf_WriteCompleteEvent(
        UInt64 trackId,
        String eventName,
        TimeSpan relativeStartTime,
        TimeSpan duration,
        IReadOnlyList<KeyValuePair<String, Object?>>? args = null,
        Boolean includeIds = false,
        String? traceId = null,
        String? spanId = null,
        String? parentSpanId = null
    )
    {
        var internedData = (InternedData?)null;

        args ??= Array.Empty<KeyValuePair<String, Object?>>();

        var idAnnotations = new DebugAnnotation {
            NameIid = _InternArgName(ref internedData, "ids"),
        };
        var argsAnnotations = new DebugAnnotation {
            NameIid = _InternArgName(ref internedData, "args"),
        };

        foreach (var arg in args.OrderBy(x => x.Key))
        {
            argsAnnotations.DictEntries.Add(_CreateArg(ref internedData, arg!));
        }

        if (includeIds && parentSpanId != null)
        {
            idAnnotations.DictEntries.Add(
                new DebugAnnotation {
                    NameIid = _InternArgName(ref internedData, "parent"),
                    StringValue = parentSpanId,
                }
            );
        }

        if (includeIds && spanId != null)
        {
            idAnnotations.DictEntries.Add(
                new DebugAnnotation {
                    NameIid = _InternArgName(ref internedData, "span"),
                    StringValue = spanId,
                }
            );
        }

        if (includeIds && traceId != null)
        {
            idAnnotations.DictEntries.Add(
                new DebugAnnotation {
                    NameIid = _InternArgName(ref internedData, "trace"),
                    StringValue = traceId,
                }
            );
        }


        var sliceBegin = new TracePacket {
            Timestamp = Convert.ToUInt64(relativeStartTime.Ticks / 10 * 1000),
            TrackEvent = new TrackEvent {
                TrackUuid = trackId,
                Type = TrackEvent.Types.Type.SliceBegin,
                NameIid = _InternEventName(ref internedData, eventName),
            },
            TrustedPacketSequenceId = PacketSequenceId,
        };

        if (argsAnnotations.DictEntries.Count > 0)
        {
            sliceBegin.TrackEvent.DebugAnnotations.Add(argsAnnotations);
        }

        if (idAnnotations.DictEntries.Count > 0)
        {
            sliceBegin.TrackEvent.DebugAnnotations.Add(idAnnotations);
        }

        if (internedData != null)
        {
            sliceBegin.InternedData = internedData;
        }

        var sliceEnd = new TracePacket {
            Timestamp = Convert.ToUInt64((relativeStartTime.Ticks + duration.Ticks) / 10 * 1000),
            TrackEvent = new TrackEvent {
                TrackUuid = trackId,
                Type = TrackEvent.Types.Type.SliceEnd,
            },
            TrustedPacketSequenceId = PacketSequenceId,
        };

        var trace = new Trace {
            Packet = {
                sliceBegin,
                sliceEnd,
            },
        };

        trace.WriteTo(mWriter);
    }

    private DebugAnnotation _CreateArg(
        ref InternedData? internedData,
        KeyValuePair<String, Object> arg
    )
    {
        var value = new DebugAnnotation {
            NameIid = _InternArgName(ref internedData, arg.Key),
        };

        if (arg.Value is Boolean boolValue)
        {
            value.BoolValue = boolValue;
        }
        else if (arg.Value is Int32 int32Value)
        {
            value.IntValue = int32Value;
        }
        else if (arg.Value is Int64 int64Value)
        {
            value.IntValue = int64Value;
        }
        else if (arg.Value is UInt32 uint32Value)
        {
            value.UintValue = uint32Value;
        }
        else if (arg.Value is UInt64 uint64Value)
        {
            value.UintValue = uint64Value;
        }
        else if (arg.Value is Double doubleValue)
        {
            value.DoubleValue = doubleValue;
        }
        else if (arg.Value is Byte[] bytesValue)
        {
            value.StringValue = String.Join(",", bytesValue.Select(x => x.ToString("X2")));
        }
        else if (arg.Value is Guid guidValue)
        {
            value.StringValue = guidValue.ToString("N");
        }
        else
        {
            value.StringValue = arg.Value.ToString();
        }

        return value;
    }

    private void _Protobuf_WriteProcessTrack(
        UInt64 processId,
        String processName
    )
    {
        var trace = new Trace {
            Packet = {
                new TracePacket {
                    TrackDescriptor = new TrackDescriptor {
                        Uuid = processId,
                        Name = processName,
                    },
                    TrustedPacketSequenceId = PacketSequenceId,
                },
            },
        };

        trace.WriteTo(mWriter);
    }

    private void _Protobuf_WriteTrack(
        UInt64 trackId,
        String trackName,
        UInt64? processTrackId = null
    )
    {
        var desc = new TrackDescriptor {
            Uuid = trackId,
            Name = trackName,
        };

        if (processTrackId != null)
        {
            desc.ParentUuid = processTrackId.Value;
        }

        var trace = new Trace {
            Packet = {
                new TracePacket {
                    TrackDescriptor = desc,
                    TrustedPacketSequenceId = PacketSequenceId,
                },
            },
        };

        trace.WriteTo(mWriter);
    }


    private UInt64 _InternEventName(
        ref InternedData? internedData,
        String eventName
    )
    {
        if (mEventNames.TryGetValue(eventName, out var eventNameId))
        {
            return eventNameId;
        }

        eventNameId = (UInt64)(EventNameInitialId + mEventNames.Count);

        mEventNames.Add(eventName, eventNameId);

        internedData ??= new InternedData();

        internedData.EventNames.Add(
            new EventName {
                Iid = eventNameId,
                Name = eventName,
            }
        );

        return eventNameId;
    }

    private UInt64 _InternArgName(
        ref InternedData? internedData,
        String argName
    )
    {
        if (mArgNames.TryGetValue(argName, out var argNameId))
        {
            return argNameId;
        }

        argNameId = (UInt64)(ArgNameInitialId + mArgNames.Count);

        mArgNames.Add(argName, argNameId);

        internedData ??= new InternedData();

        internedData.DebugAnnotationNames.Add(
            new DebugAnnotationName {
                Name = argName,
                Iid = argNameId,
            }
        );

        return argNameId;
    }

    private UInt64 _InternArgValue(
        ref InternedData? internedData,
        String argValue
    )
    {
        if (mArgValues.TryGetValue(argValue, out var argValueId))
        {
            return argValueId;
        }

        argValueId = (UInt64)(ArgValueInitialId + mArgValues.Count);

        mArgValues.Add(argValue, argValueId);

        internedData ??= new InternedData();

        internedData.DebugAnnotationStringValues.Add(
            new InternedString {
                Str = ByteString.CopyFromUtf8(argValue),
                Iid = argValueId,
            }
        );

        return argValueId;
    }


    private static void _CreateOutDirectory(
        String outDir
    )
    {
        if (!Directory.Exists(outDir))
        {
            Directory.CreateDirectory(outDir);
        }
    }
}
