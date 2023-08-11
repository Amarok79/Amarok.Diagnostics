// Copyright (c) 2023, Olaf Kober <olaf.kober@outlook.com>

using Amarok.Diagnostics.Persistence.Tracing.Writer.Internal;


namespace Amarok.Diagnostics.Persistence.Tracing.Writer;


/// <summary>
///     Provides factory methods for constructing <see cref="ITraceWriter"/> instances.
/// </summary>
public static class TraceWriter
{
    /// <summary>
    ///     Creates a new trace log writer.
    /// </summary>
    /// 
    /// <param name="directoryPath">
    ///     The path to the directory used for storing rolling trace log files (.adtx). The directory gets
    ///     created if it doesn't exist.
    /// </param>
    /// <param name="options">
    ///     Optional options for the trace log writer. If not specified, defaults are used.
    /// </param>
    /// 
    /// <returns>
    ///     An object capable of serializing and persisting activities. Don't forget to dispose the
    ///     returned writer after use.
    /// </returns>
    public static ITraceWriter Create(
        String directoryPath,
        TraceWriterOptions? options = null
    )
    {
        var directory = new DirectoryInfo(directoryPath);

        return Create(directory, options);
    }

    /// <summary>
    ///     Creates a new trace log writer.
    /// </summary>
    /// 
    /// <param name="directory">
    ///     The directory used for storing rolling trace log files (.adtx). The directory gets created if
    ///     it doesn't exist.
    /// </param>
    /// <param name="options">
    ///     Optional options for the trace log writer. If not specified, defaults are used.
    /// </param>
    /// 
    /// <returns>
    ///     An object capable of serializing and persisting activities. Don't forget to dispose the
    ///     returned writer after use.
    /// </returns>
    public static ITraceWriter Create(
        DirectoryInfo directory,
        TraceWriterOptions? options = null
    )
    {
        options ??= new TraceWriterOptions();

        var writer = new RollingTraceWriter(
            directory,
            options.SessionUuid,
            options.SessionStartTime,
            (Int64)options.MaxDiskSpaceUsedInMegaBytes * 1024 * 1024,
            10,
            4096,
            128,
            256,
            256,
            TimeSpan.FromSeconds(60),
            options.AutoFlushInterval,
            options.UseCompression,
            true,
            options.Logger
        );

        writer.Initialize();

        return writer;
    }
}
