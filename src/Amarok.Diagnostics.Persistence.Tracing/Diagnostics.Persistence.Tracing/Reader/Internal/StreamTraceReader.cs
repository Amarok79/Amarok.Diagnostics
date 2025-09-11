// Copyright (c) 2024, Olaf Kober <olaf.kober@outlook.com>

using System.Buffers.Binary;
using System.IO.Compression;
using Amarok.Diagnostics.Persistence.Tracing.Protos;
using Google.Protobuf;


namespace Amarok.Diagnostics.Persistence.Tracing.Reader.Internal;


internal sealed class StreamTraceReader : ITraceReader
{
    private readonly Stream mStream;
    private readonly ITraceReaderHooks? mHooks;

    private readonly ActivitySourceMap mActivitySourceMap = new();
    private readonly ActivityTraceIdMap mActivityTraceIdMap = new();
    private readonly ActivityParentSpanIdMap mActivityParentSpanIdMap = new();
    private readonly OperationNameMap mOperationNameMap = new();
    private readonly TagKeyMap mTagKeyMap = new();
    private readonly ReferenceTimeMap mReferenceTimeMap = new();
    private readonly AnyValueDeserializer mAnyValueDeserializer = new();


    public StreamTraceReader(Stream stream, ITraceReaderHooks? hooks = null)
    {
        mStream = stream;
        mHooks  = hooks;
    }


    public void Dispose()
    {
        mStream.Dispose();
    }


    public IEnumerable<ActivityInfo> Read()
    {
        _ReadFileHeader(
            mStream,
            out var version,
            out var isCompressed,
            out var isFinished,
            out var session
        );

        mHooks?.OnReadFileHeader(version, isCompressed, isFinished, session);

        var stream = isCompressed ? new DeflateStream(mStream, CompressionMode.Decompress) : mStream;

        using var reader = new BinaryReader(stream);

        var records = new TraceRecords();
        var buffer  = new Byte[4096];

        while (true)
        {
            var frameLen = _ReadContentFrame(reader, ref buffer);

            if (frameLen == -1)
            {
                break; // expected end of file
            }

            mHooks?.OnBeginReadFrame(buffer, frameLen);


            // deserialize records from frame
            records.Items.Clear();

            records.MergeFrom(buffer.AsSpan(0, frameLen));


            // process records
            var items = records.Items;

            for (var i = 0; i < items.Count; i++)
            {
                var item = items[i];

                mHooks?.OnReadRecord(item);

                var activity = _ProcessRecord(item, session);

                if (activity != null)
                {
                    mHooks?.OnReadActivity(activity);

                    yield return activity;
                }
            }

            mHooks?.OnEndReadFrame();
        }
    }


    private static Int32 _ReadContentFrame(BinaryReader reader, ref Byte[] buffer)
    {
        // content-frame        =  frame-preamble, frame-length , records ;
        // frame-preamble       =  %xAA ;
        // frame-length         =  <7bitEncodedInt32> ;
        // records              =  <protobuf-encoded> ;

        if (!_ReadContentFramePreamble(reader))
        {
            return -1; // expected end of file
        }

        var frameLen = reader.Read7BitEncodedInt();

        if (frameLen > buffer.Length)
        {
            buffer = resizeBuffer(frameLen);
        }

        var bytesRead = readExact(reader, buffer, frameLen);

        if (bytesRead != frameLen)
        {
            throwUnableToRead(frameLen);
        }

        return frameLen;


        static Int32 readExact(BinaryReader reader, Byte[] buffer, Int32 length)
        {
            var totalBytesRead = 0;

            while (true)
            {
                var bytesRead = reader.Read(buffer, totalBytesRead, length - totalBytesRead);

                totalBytesRead += bytesRead;

                if (totalBytesRead == length || bytesRead == 0)
                {
                    return totalBytesRead;
                }
            }
        }

        static Byte[] resizeBuffer(Int32 length)
        {
            return new Byte[(length / 4096 + 1) * 4096];
        }

        static void throwUnableToRead(Int32 length)
        {
            throw new EndOfStreamException($"Unable to read frame of length {length} bytes.");
        }
    }

    private static Boolean _ReadContentFramePreamble(BinaryReader reader)
    {
        const Int32 size = 1;

        Span<Byte> bytes = stackalloc Byte[1];

        var bytesRead = reader.Read(bytes);

        if (bytesRead < size)
        {
            return false; // expected end of file
        }

        if (bytes[0] != 0xAA)
        {
            throwUnexpected();
        }

        return true;


        static void throwUnexpected()
        {
            throw new FormatException("Unexpected content frame preamble.");
        }
    }

    private static void _ReadFileHeader(
        Stream stream,
        out Int32 version,
        out Boolean isCompressed,
        out Boolean isFinished,
        out SessionInfo session
    )
    {
        _ReadFileSignature(stream);

        _ReadFileVersion(stream, out version);

        if (version != 0x01)
        {
            throwUnsupported(version);
        }

        _ReadFileFlags(stream, out isCompressed, out isFinished);

        _ReadFileSession(stream, out var sessionUuid, out var sessionStart);

        session = new SessionInfo(sessionUuid, sessionStart);


        static void throwUnsupported(Int32 version)
        {
            throw new FormatException($"Unsupported file version {version}.");
        }
    }

    private static void _ReadFileSession(Stream stream, out Guid sessionUuid, out DateTimeOffset sessionStart)
    {
        // file-session    =  session-uuid , session-start ;
        // session-uuid    =  <Guid> ;
        // session-start   =  ticks , offset-minutes ;
        // ticks           =  <Int64> ;
        // offset-minutes  =  <Int16> ;

        const Int32 size = 16 + 8 + 2;

        Span<Byte> bytes = stackalloc Byte[size];

        var bytesRead = stream.Read(bytes);

        if (bytesRead < size)
        {
            throwUnableToRead();
        }

        sessionUuid = new Guid(bytes[..16]);

        var ticks  = BinaryPrimitives.ReadInt64LittleEndian(bytes[16..24]);
        var offset = BinaryPrimitives.ReadInt16LittleEndian(bytes[24..26]);

        sessionStart = new DateTimeOffset(ticks, TimeSpan.FromMinutes(offset));


        static void throwUnableToRead()
        {
            throw new EndOfStreamException("Unable to read file session.");
        }
    }

    private static void _ReadFileFlags(Stream stream, out Boolean isCompressed, out Boolean isFinished)
    {
        // file-flags               =  %x00 , active | isFinished | isCompressed-isFinished ;
        // active                   =  %x0A ;
        // isFinished               =  %x0F ;
        // isCompressed-isFinished  =  %xCF ;

        const Int32 size = 2;

        Span<Byte> bytes = stackalloc Byte[size];

        var bytesRead = stream.Read(bytes);

        if (bytesRead < size)
        {
            throwUnableToRead();
        }

        if (bytes[0] != 0x00)
        {
            throwUnexpected();
        }

        if (bytes[1] is not 0x0A and not 0x0F and not 0xCF)
        {
            throwUnexpected();
        }

        var flags = bytes[1];

        isCompressed = (flags & 0xC0) == 0xC0;
        isFinished   = (flags & 0x0F) == 0x0F;


        static void throwUnableToRead()
        {
            throw new EndOfStreamException("Unable to read file flags.");
        }

        static void throwUnexpected()
        {
            throw new FormatException("Unexpected file flags.");
        }
    }

    private static void _ReadFileVersion(Stream stream, out Int32 version)
    {
        // file-version  =  %x00 , version ;

        const Int32 size = 2;

        Span<Byte> bytes = stackalloc Byte[size];

        var bytesRead = stream.Read(bytes);

        if (bytesRead < size)
        {
            throwUnableToRead();
        }

        if (bytes[0] != 0x00)
        {
            throwUnexpected();
        }

        version = bytes[1];


        static void throwUnableToRead()
        {
            throw new EndOfStreamException("Unable to read file version.");
        }

        static void throwUnexpected()
        {
            throw new FormatException("Unexpected file version.");
        }
    }

    private static void _ReadFileSignature(Stream stream)
    {
        // file-signature  =  %x61 , %x64 , %x74 , %x78 ;      // "adtx"

        const Int32 size = 4;

        Span<Byte> bytes = stackalloc Byte[size];

        var bytesRead = stream.Read(bytes);

        if (bytesRead < size)
        {
            throwUnableToRead();
        }

        if (bytes[0] != 0x61 || bytes[1] != 0x64 || bytes[2] != 0x74 || bytes[3] != 0x78)
        {
            throwUnexpected();
        }


        static void throwUnableToRead()
        {
            throw new EndOfStreamException("Unable to read file signature.");
        }

        static void throwUnexpected()
        {
            throw new FormatException("Unexpected file signature.");
        }
    }


    private ActivityInfo? _ProcessRecord(TraceRecord record, SessionInfo session)
    {
        var activity = record.DataCase switch {
            TraceRecord.DataOneofCase.Activity           => _ProcessActivity(record.Activity, session),
            TraceRecord.DataOneofCase.DefinePointInTime  => _ProcessDefinePointInTime(record.DefinePointInTime),
            TraceRecord.DataOneofCase.DefineSource       => _ProcessDefineSource(record.DefineSource),
            TraceRecord.DataOneofCase.DefineOperation    => _ProcessDefineOperation(record.DefineOperation),
            TraceRecord.DataOneofCase.DefineTag          => _ProcessDefineTag(record.DefineTag),
            TraceRecord.DataOneofCase.DefineTraceId      => _ProcessDefineTraceId(record.DefineTraceId),
            TraceRecord.DataOneofCase.DefineParentSpanId => _ProcessDefineParentSpanId(record.DefineParentSpanId),
            TraceRecord.DataOneofCase.ResetSources       => _ProcessResetSources(),
            TraceRecord.DataOneofCase.ResetOperations    => _ProcessResetOperations(),
            TraceRecord.DataOneofCase.ResetTags          => _ProcessResetTags(),
            TraceRecord.DataOneofCase.ResetTraceIds      => _ProcessResetTraceIds(),
            TraceRecord.DataOneofCase.ResetParentSpanIds => _ProcessResetParentSpanIds(),
            TraceRecord.DataOneofCase.None               => throw makeUnexpectedException(record.DataCase),
            _                                            => throw makeUnexpectedException(record.DataCase),
        };

        return activity;


        static Exception makeUnexpectedException(TraceRecord.DataOneofCase caseValue)
        {
            return new FormatException($"Unexpected TraceRecord case '{caseValue}.");
        }
    }

    private ActivityInfo? _ProcessDefinePointInTime(TraceDefinePointInTime value)
    {
        var referenceTime = new DateTimeOffset(value.Ticks, TimeSpan.FromMinutes(value.OffsetMinutes));

        mReferenceTimeMap.Define(referenceTime);

        return null;
    }

    private ActivityInfo? _ProcessDefineSource(TraceDefineSource value)
    {
        var version = String.IsNullOrEmpty(value.Version) ? null : value.Version;

        var info = new ActivitySourceInfo(value.Name, version);

        mActivitySourceMap.Define(value.Id, info);

        return null;
    }

    private ActivityInfo? _ProcessResetSources()
    {
        mActivitySourceMap.Reset();

        return null;
    }

    private ActivityInfo? _ProcessDefineOperation(TraceDefineOperation value)
    {
        mOperationNameMap.Define(value.Id, value.Name);

        return null;
    }

    private ActivityInfo? _ProcessResetOperations()
    {
        mOperationNameMap.Reset();

        return null;
    }

    private ActivityInfo? _ProcessDefineTag(TraceDefineTag value)
    {
        mTagKeyMap.Define(value.Id, value.Key);

        return null;
    }

    private ActivityInfo? _ProcessResetTags()
    {
        mTagKeyMap.Reset();

        return null;
    }

    private ActivityInfo? _ProcessDefineTraceId(TraceDefineTraceId value)
    {
        mActivityTraceIdMap.Define(value.Id, value.TraceId);

        return null;
    }

    private ActivityInfo? _ProcessResetTraceIds()
    {
        mActivityTraceIdMap.Reset();

        return null;
    }

    private ActivityInfo? _ProcessDefineParentSpanId(TraceDefineParentSpanId value)
    {
        mActivityParentSpanIdMap.Define(value.Id, value.SpanId);

        return null;
    }

    private ActivityInfo? _ProcessResetParentSpanIds()
    {
        mActivityParentSpanIdMap.Reset();

        return null;
    }

    private ActivityInfo _ProcessActivity(TraceActivity activity, SessionInfo session)
    {
        var source       = mActivitySourceMap.Lookup(activity.SourceId);
        var operation    = mOperationNameMap.Lookup(activity.OperationId);
        var traceId      = mActivityTraceIdMap.Lookup(activity.TraceId);
        var parentSpanId = mActivityParentSpanIdMap.Lookup(activity.ParentSpanId);
        var spanId       = activity.SpanId;
        var timeDelta    = TimeSpan.FromTicks(activity.StartTimeRelativeTicks);
        var startTime    = mReferenceTimeMap.GetAbsolutePointInTime(timeDelta);
        var duration     = TimeSpan.FromTicks(activity.DurationTicks);
        var tags         = _ProcessActivityTags(activity);

        return new ActivityInfo(
            session,
            source,
            operation,
            traceId,
            parentSpanId,
            spanId,
            startTime,
            duration
        ) {
            Tags = tags,
        };
    }

    private IReadOnlyList<KeyValuePair<String, Object?>> _ProcessActivityTags(TraceActivity activity)
    {
        if (activity.Tags.Count == 0)
        {
            return [ ];
        }

        var items = new KeyValuePair<String, Object?>[activity.Tags.Count];

        for (var i = 0; i < items.Length; i++)
        {
            var tag = activity.Tags[i];

            var key   = mTagKeyMap.Lookup(tag.KeyId);
            var value = mAnyValueDeserializer.Deserialize(tag.Value);

            items[i] = new KeyValuePair<String, Object?>(key, value);
        }

        return items;
    }
}
