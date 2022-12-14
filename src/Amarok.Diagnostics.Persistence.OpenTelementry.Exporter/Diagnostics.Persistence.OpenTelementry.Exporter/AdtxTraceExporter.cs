// Copyright (c) 2023, Olaf Kober <olaf.kober@outlook.com>

using System.Diagnostics;
using Amarok.Diagnostics.Persistence.Tracing.Writer;
using OpenTelemetry;


namespace Amarok.Diagnostics.Persistence.OpenTelementry.Exporter;


internal sealed class AdtxTraceExporter : BaseExporter<Activity>,
    IAdtxTraceExporter
{
    // settings
    private readonly AdtxTraceExporterOptions mOptions;

    // state
    private readonly ManualResetEventSlim mSnapshotBarrier = new(true);
    private readonly ManualResetEventSlim mExportBarrier = new(true);
    private volatile Boolean mIsSnapshotRunning;
    private volatile Task? mSnapshotTask;

    private ITraceWriter? mWriter;


    public AdtxTraceExporter(AdtxTraceExporterOptions options)
    {
        mOptions = options;
    }


    public void Initialize()
    {
        mWriter = TraceWriter.Create(mOptions.Directory, mOptions.WriterOptions);
    }


    protected override Boolean OnForceFlush(Int32 timeoutMilliseconds)
    {
        return true;
    }

    protected override Boolean OnShutdown(Int32 timeoutMilliseconds)
    {
        if (mWriter == null)
        {
            return true;
        }

        var snapshotTask = mSnapshotTask;

        if (snapshotTask != null && !snapshotTask.IsCompleted)
        {
            snapshotTask.GetAwaiter().GetResult();
        }

        var task = mWriter.DisposeAsync();

        if (!task.IsCompleted)
        {
            task.AsTask().Wait();
        }

        return true;
    }

    public override ExportResult Export(in Batch<Activity> batch)
    {
        if (mWriter == null)
        {
            return ExportResult.Success;
        }

        mSnapshotBarrier.Wait();

        try
        {
            mExportBarrier.Reset();

            foreach (var activity in batch)
            {
                mWriter.Write(activity);
            }

            var task = mWriter.FlushAsync();

            if (!task.IsCompleted)
            {
                task.AsTask().Wait();
            }

            return ExportResult.Success;
        }
        catch (Exception)
        {
            return ExportResult.Failure;
        }
        finally
        {
            mExportBarrier.Set();
        }
    }

    public async Task HotExportAsync(String archivePath)
    {
        if (mIsSnapshotRunning)
        {
            throw new InvalidOperationException("Another snapshot operation is pending.");
        }

        var writer = mWriter;

        if (writer == null)
        {
            throw new InvalidOperationException("Not initialized or already disposed.");
        }

        try
        {
            mIsSnapshotRunning = true;

            await Task.Run(() => _ExecExportToArchiveAsync(writer, archivePath));
        }
        finally
        {
            mIsSnapshotRunning = false;
        }
    }

    private async Task _ExecExportToArchiveAsync(ITraceWriter writer, String archivePath)
    {
        mSnapshotBarrier.Reset();

        mExportBarrier.Wait();


        var taskOfTask = writer.ExportAsync(archivePath);

        mSnapshotTask = await taskOfTask;


        mSnapshotBarrier.Set();


        await mSnapshotTask;

        mSnapshotTask = null;
    }
}
