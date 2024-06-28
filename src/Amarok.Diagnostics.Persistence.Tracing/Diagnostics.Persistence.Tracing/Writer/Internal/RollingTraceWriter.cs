// Copyright (c) 2024, Olaf Kober <olaf.kober@outlook.com>

using System.Buffers;
using System.Diagnostics;
using System.IO.Pipelines;
using Amarok.Diagnostics.Persistence.Tracing.Protos;
using Google.Protobuf;
using Microsoft.Extensions.Logging;


namespace Amarok.Diagnostics.Persistence.Tracing.Writer.Internal;


internal sealed class RollingTraceWriter : ITraceWriter
{
    private readonly Int64 mMaxFileLength;
    private readonly TimeSpan mFlushInterval;
    private readonly ILogger mLogger;

    private readonly ObjectsPool mObjectsPool;
    private readonly ActivitySerializer mActivitySerializer;
    private readonly RollingFileWriter mFileWriter;
    private readonly Pipe mPipe;
    private readonly CancellationTokenSource mDisposeCts = new();

    private TaskCompletionSource mCompletionDoneTcs = new();
    private TraceRecords? mRecords;
    private Int64 mBytesWritten;


    public RollingTraceWriter(
        DirectoryInfo directory,
        Guid sessionUuid,
        DateTimeOffset sessionStartTime,
        Int64 maxDiskSpaceUsed,
        Int32 minNumberOfFiles,
        Int32 maxNumberOfItems,
        Int32 maxNumberOfIds,
        Int32 maxStringLength,
        Int32 maxBytesLength,
        TimeSpan redefineReferenceTimeInterval,
        TimeSpan flushInterval,
        Boolean useCompression,
        Boolean useObjectsPool,
        ILogger logger
    )
    {
        mObjectsPool = ObjectsPool.Create(useObjectsPool);

        mMaxFileLength = maxDiskSpaceUsed / minNumberOfFiles;

        mFlushInterval = flushInterval;
        mLogger = logger;

        mActivitySerializer = new ActivitySerializer(
            maxNumberOfItems,
            maxNumberOfIds,
            maxStringLength,
            maxBytesLength,
            redefineReferenceTimeInterval,
            mObjectsPool
        );

        mFileWriter = new RollingFileWriter(directory, maxDiskSpaceUsed, useCompression, logger);

        mFileWriter.SetSession(sessionUuid, sessionStartTime);

        mPipe = new Pipe(
            new PipeOptions(
                useSynchronizationContext: false,
                pauseWriterThreshold: 2048 * 1024,
                resumeWriterThreshold: 512 * 1024
            )
        );
    }


    public void Initialize()
    {
        _StartPipeReader();

        mLogger.LogDebug("RollingTraceWriter: Initialized");
    }


    public void Write(Activity activity)
    {
        mRecords ??= mObjectsPool.GetRecords();

        mActivitySerializer.Serialize(activity, mRecords);
    }

    public async ValueTask FlushAsync()
    {
        if (mRecords == null)
        {
            return;
        }

        _WriteToPipe(mPipe.Writer, mRecords);

        mObjectsPool.ReturnRecords(mRecords);

        mRecords = null;


        mBytesWritten += mPipe.Writer.UnflushedBytes;

        await mPipe.Writer.FlushAsync();

        if (mBytesWritten > mMaxFileLength)
        {
            await _RollOver();
        }
    }

    public async Task<Task> ExportAsync(String archivePath)
    {
        var sw = Stopwatch.StartNew();

        mLogger.LogDebug("RollingTraceWriter: Exporting...");

        await _RollOver();

        mLogger.LogDebug("RollingTraceWriter: Exporting... Rolled over ({Elapsed} ms)", sw.ElapsedMilliseconds);

        return Task.Run(
            () => {
                mFileWriter.Export(archivePath);

                mLogger.LogDebug("RollingTraceWriter: Exported ({Elapsed} ms)", sw.ElapsedMilliseconds);
            }
        );
    }

    public async ValueTask DisposeAsync()
    {
        var sw = Stopwatch.StartNew();

        mLogger.LogDebug("RollingTraceWriter: Disposing...");

        await mDisposeCts.CancelAsync();

        mLogger.LogTrace("RollingTraceWriter: pipe reader cancelled ({Elapsed} ms)", sw.ElapsedMilliseconds);

        await mPipe.Writer.CompleteAsync();

        mLogger.LogTrace("RollingTraceWriter: pipe writer completed ({Elapsed} ms)", sw.ElapsedMilliseconds);

        await mCompletionDoneTcs.Task;

        mLogger.LogTrace("RollingTraceWriter: pipe reader completed ({Elapsed} ms)", sw.ElapsedMilliseconds);

        mLogger.LogDebug("RollingTraceWriter: Disposed");
    }


    private static void _WriteToPipe(IBufferWriter<Byte> buffer, TraceRecords records)
    {
        // content-frame        =  frame-preamble, frame-length , records ;
        // frame-preamble       =  %xAA ;
        // frame-length         =  <7bitEncodedInt32> ;
        // records              =  <protobuf-encoded> ;

        var size = records.CalculateSize();

        _WriteContentFramePreambleAndLength(buffer, size);

        records.WriteTo(buffer);
    }

    private static void _WriteContentFramePreambleAndLength(IBufferWriter<Byte> buffer, Int32 value)
    {
        var span = buffer.GetSpan(6);

        // preamble
        span[0] = 0xAA;

        // length
        var i = 1;
        var uValue = (UInt32)value;

        while (uValue > 0x7Fu)
        {
            span[i++] = (Byte)(uValue | ~0x7Fu);

            uValue >>= 7;
        }

        span[i++] = (Byte)uValue;

        buffer.Advance(i);
    }

    private async Task _RollOver()
    {
        var sw = Stopwatch.StartNew();

        mLogger.LogDebug("RollingTraceWriter: Rolling over...");

        await mPipe.Writer.CompleteAsync();

        mLogger.LogTrace("RollingTraceWriter: pipe writer completed ({Elapsed} ms)", sw.ElapsedMilliseconds);


        await mCompletionDoneTcs.Task;

        mLogger.LogTrace("RollingTraceWriter: pipe reader completed ({Elapsed} ms)", sw.ElapsedMilliseconds);


        mBytesWritten = 0;

        mActivitySerializer.Reset();

        mPipe.Reset();

        mLogger.LogTrace("RollingTraceWriter: serializer and pipe reset ({Elapsed} ms)", sw.ElapsedMilliseconds);


        _StartPipeReader();

        mLogger.LogDebug("RollingTraceWriter: Rolled over");
    }

    private void _StartPipeReader()
    {
        mCompletionDoneTcs = new TaskCompletionSource();

        _ = Task.Run(
            () => _PipeReaderFunc(
                mLogger,
                mPipe.Reader,
                mFileWriter,
                mFlushInterval,
                mCompletionDoneTcs,
                mDisposeCts.Token
            )
        );
    }


    private static async Task _PipeReaderFunc(
        ILogger logger,
        PipeReader pipeReader,
        RollingFileWriter fileWriter,
        TimeSpan flushInterval,
        TaskCompletionSource completionDoneTcs,
        CancellationToken disposeToken
    )
    {
        logger.LogDebug("PipeReader: Starting...");

        var flushStopwatch = Stopwatch.StartNew();

        fileWriter.StartNewLogFile();


        logger.LogDebug("PipeReader: Running...");

        while (true)
        {
            var result = await pipeReader.ReadAsync(CancellationToken.None);
            var buffer = result.Buffer;

            if (!buffer.IsEmpty)
            {
                foreach (var segment in buffer)
                {
                    fileWriter.Write(segment.Span);
                }

                pipeReader.AdvanceTo(buffer.End);


                if (flushStopwatch.Elapsed > flushInterval)
                {
                    logger.LogDebug("PipeReader: Flushing to disk...");

                    fileWriter.Flush();
                    flushStopwatch.Restart();

                    logger.LogDebug("PipeReader: Flushed to disk");
                }
            }

            if (result.IsCompleted)
            {
                logger.LogDebug("PipeReader: Completing...");

                fileWriter.CompleteActiveLogFile();

                await pipeReader.CompleteAsync();

                if (disposeToken.IsCancellationRequested)
                {
                    fileWriter.Dispose();
                }

                logger.LogDebug("PipeReader: Completed");

                completionDoneTcs.SetResult();

                return;
            }
        }
    }
}
