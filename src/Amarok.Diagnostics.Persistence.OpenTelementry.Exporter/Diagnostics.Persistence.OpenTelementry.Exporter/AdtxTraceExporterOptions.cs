// Copyright (c) 2024, Olaf Kober <olaf.kober@outlook.com>

using Amarok.Diagnostics.Persistence.Tracing.Writer;


namespace Amarok.Diagnostics.Persistence.OpenTelementry.Exporter;


/// <summary>
///     Options for trace exporters.
/// </summary>
public sealed class AdtxTraceExporterOptions
{
    /// <summary>
    ///     The directory used for storing rolling trace log files (.adtx). The directory gets created if it doesn't
    ///     exist.
    /// </summary>
    public DirectoryInfo Directory { get; }

    /// <summary>
    ///     The options for the trace writer.
    /// </summary>
    public TraceWriterOptions WriterOptions { get; set; } = new();


    /// <summary>
    ///     Initializes a new instance.
    /// </summary>
    /// 
    /// <param name="directoryPath">
    ///     The path to the directory used for storing rolling trace log files (.adtx). The directory gets created if
    ///     it doesn't exist.
    /// </param>
    public AdtxTraceExporterOptions(String directoryPath)
    {
        Directory = new DirectoryInfo(directoryPath);
    }

    /// <summary>
    ///     Initializes a new instance.
    /// </summary>
    /// 
    /// <param name="directory">
    ///     The directory used for storing rolling trace log files (.adtx). The directory gets created if it doesn't
    ///     exist.
    /// </param>
    public AdtxTraceExporterOptions(DirectoryInfo directory)
    {
        Directory = directory;
    }


    /// <inheritdoc/>
    public override String ToString()
    {
        return $"Directory: {Directory.FullName}, WriterOptions: {{ {WriterOptions} }}";
    }
}
