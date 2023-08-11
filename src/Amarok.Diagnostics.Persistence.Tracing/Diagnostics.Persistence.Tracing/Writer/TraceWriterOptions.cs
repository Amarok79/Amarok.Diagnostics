// Copyright (c) 2023, Olaf Kober <olaf.kober@outlook.com>

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;


namespace Amarok.Diagnostics.Persistence.Tracing.Writer;


/// <summary>
///     Options for trace log writers.
/// </summary>
public sealed class TraceWriterOptions
{
    /// <summary>
    ///     The unique identifier of the current application session. Defaults to
    ///     <see cref="Guid.NewGuid"/>.
    /// </summary>
    public Guid SessionUuid { get; set; } = Guid.NewGuid();

    /// <summary>
    ///     The start date and time of the current application session. Defaults to
    ///     <see cref="DateTimeOffset.Now"/> .
    /// </summary>
    public DateTimeOffset SessionStartTime { get; set; } = DateTimeOffset.Now;

    /// <summary>
    ///     The maximum size in megabytes (MB) used for storing trace log files. Defaults to 100 MB.
    /// </summary>
    public Int32 MaxDiskSpaceUsedInMegaBytes { get; set; } = 100;

    /// <summary>
    ///     Indicates whether trace log files are compressed. Defaults to true.
    /// </summary>
    public Boolean UseCompression { get; set; } = true;

    /// <summary>
    ///     The minimum interval in which written file content is flushed to persistence storage. Defaults
    ///     to 15 seconds.
    /// </summary>
    public TimeSpan AutoFlushInterval { get; set; } = TimeSpan.FromSeconds(15);

    /// <summary>
    ///     The logger for this trace writer.
    /// </summary>
    public ILogger Logger { get; set; } = NullLogger.Instance;


    /// <summary>
    ///     Initializes a new instance with defaults.
    /// </summary>
    public TraceWriterOptions()
    {
    }

    /// <summary>
    ///     Initializes a new instance based on the given options.
    /// </summary>
    public TraceWriterOptions(
        TraceWriterOptions other
    )
    {
        SessionUuid = other.SessionUuid;
        SessionStartTime = other.SessionStartTime;
        MaxDiskSpaceUsedInMegaBytes = other.MaxDiskSpaceUsedInMegaBytes;
        UseCompression = other.UseCompression;
        AutoFlushInterval = other.AutoFlushInterval;
        Logger = other.Logger;
    }


    /// <inheritdoc/>
    public override String ToString()
    {
        return $"SessionUuid: {SessionUuid}, SessionStartTime: {SessionStartTime}, " +
               $"MaxDiskSpaceUsedInMegaBytes: {MaxDiskSpaceUsedInMegaBytes} MB, UseCompression: {UseCompression}, " +
               $"AutoFlushInterval: {AutoFlushInterval.Seconds} s";
    }
}
