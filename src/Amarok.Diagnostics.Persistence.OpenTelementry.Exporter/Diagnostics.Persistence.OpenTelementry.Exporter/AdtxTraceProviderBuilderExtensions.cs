// Copyright (c) 2024, Olaf Kober <olaf.kober@outlook.com>

using System.Diagnostics;
using OpenTelemetry;
using OpenTelemetry.Trace;


namespace Amarok.Diagnostics.Persistence.OpenTelementry.Exporter;


/// <summary>
///     Provides extension methods on <see cref="TracerProviderBuilder"/>.
/// </summary>
public static class AdtxTraceProviderBuilderExtensions
{
    /// <summary>
    ///     Adds a binary rolling trace exporter to the provider.
    /// </summary>
    /// 
    /// <param name="builder">
    ///     The provider builder.
    /// </param>
    /// <param name="options">
    ///     Options for the trace exporter.
    /// </param>
    /// <param name="context">
    ///     [Out] A context object for the trace exporter.
    /// </param>
    /// <param name="useBatchProcessor">
    ///     Determines whether to use a batch or simple export processor.
    /// </param>
    /// <param name="maxQueueSize">
    ///     Defaults to 2048. Used only for batch export processor.
    /// </param>
    /// <param name="scheduledDelayMilliseconds">
    ///     Defaults to 5000 ms. Used only for batch export processor.
    /// </param>
    /// <param name="exporterTimeoutMilliseconds">
    ///     Defaults to 30000 ms. Used only for batch export processor.
    /// </param>
    /// <param name="maxExportBatchSize">
    ///     Defaults to 512. Used only for batch export processor.
    /// </param>
    /// 
    /// <returns>
    ///     Returns the provider builder.
    /// </returns>
    public static TracerProviderBuilder AddAdtxTraceExporter(
        this TracerProviderBuilder builder,
        AdtxTraceExporterOptions options,
        out AdtxTraceExporterContext context,
        Boolean useBatchProcessor = false,
        Int32 maxQueueSize = 2048,
        Int32 scheduledDelayMilliseconds = 5000,
        Int32 exporterTimeoutMilliseconds = 30000,
        Int32 maxExportBatchSize = 512
    )
    {
        var exporter = new AdtxTraceExporter(options);

        exporter.Initialize();

        BaseExportProcessor<Activity> processor = useBatchProcessor
            ? new BatchActivityExportProcessor(
                exporter,
                maxQueueSize,
                scheduledDelayMilliseconds,
                exporterTimeoutMilliseconds,
                maxExportBatchSize
            )
            : new SimpleActivityExportProcessor(exporter);

        context = new AdtxTraceExporterContext {
            Exporter = exporter,
        };

        return builder.AddProcessor(processor);
    }
}
