// Copyright (c) 2024, Olaf Kober <olaf.kober@outlook.com>

using Amarok.Diagnostics.Persistence.Tracing.Reader.Internal;


namespace Amarok.Diagnostics.Persistence.Tracing.Reader;


/// <summary>
///     Provides factory methods for constructing <see cref="ITraceReader"/> instances.
/// </summary>
public static class TraceReader
{
    /// <summary>
    ///     Opens the stream for reading.
    /// </summary>
    /// 
    /// <param name="stream">
    ///     The stream to read.
    /// </param>
    /// <param name="hooks">
    ///     A hooks implementation being called during reading.
    /// </param>
    /// 
    /// <returns>
    ///     An object capable of reading a stream of activities. Don't forget to dispose the returned reader
    ///     instance; otherwise the supplied stream will stay open.
    /// </returns>
    public static ITraceReader OpenStream(Stream stream, ITraceReaderHooks? hooks = null)
    {
        return new StreamTraceReader(stream, hooks);
    }

    /// <summary>
    ///     Opens a single trace log file (.adtx) for reading.
    /// </summary>
    /// 
    /// <param name="filePath">
    ///     The file path to the trace log file.
    /// </param>
    /// <param name="hooks">
    ///     A hooks implementation being called during reading.
    /// </param>
    /// 
    /// <returns>
    ///     An object capable of reading a stream of activities. Don't forget to dispose the returned reader
    ///     instance; otherwise the trace log file will stay open.
    /// </returns>
    public static ITraceReader OpenFile(String filePath, ITraceReaderHooks? hooks = null)
    {
        var stream = new FileStream(
            filePath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            8192,
            FileOptions.SequentialScan
        );

        return OpenStream(stream, hooks);
    }

    /// <summary>
    ///     Opens a folder with multiple trace log files (.adtx) for reading.
    /// </summary>
    /// 
    /// <param name="directoryPath">
    ///     The directory path to the source folder.
    /// </param>
    /// <param name="hooks">
    ///     A hooks implementation being called during reading.
    /// </param>
    /// 
    /// <returns>
    ///     An object capable of reading a stream of activities from the trace log files located in the specified
    ///     source folder. Don't forget to dispose the returned reader instance; otherwise some trace log files will
    ///     stay open.
    /// </returns>
    public static ITraceReader OpenFolder(String directoryPath, ITraceReaderHooks? hooks = null)
    {
        return new DirectoryTraceReader(directoryPath, hooks);
    }

    /// <summary>
    ///     Opens a Zip archive with multiple trace log files (.adtx) for reading.
    /// </summary>
    /// 
    /// <param name="filePath">
    ///     The file path to the Zip archive with trace log files.
    /// </param>
    /// <param name="hooks">
    ///     A hooks implementation being called during reading.
    /// </param>
    /// 
    /// <returns>
    ///     An object capable of reading a stream of activities from the Zip archive. Don't forget to dispose the
    ///     returned reader instance; otherwise the Zip archive will stay open.
    /// </returns>
    public static ITraceReader OpenZipArchive(String filePath, ITraceReaderHooks? hooks = null)
    {
        return new ZipArchiveTraceReader(filePath, hooks);
    }
}
