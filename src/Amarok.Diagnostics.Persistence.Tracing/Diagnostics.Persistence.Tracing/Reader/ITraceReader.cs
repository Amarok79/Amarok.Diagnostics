// Copyright (c) 2023, Olaf Kober <olaf.kober@outlook.com>

namespace Amarok.Diagnostics.Persistence.Tracing.Reader;


/// <summary>
///     Represents a reader capable of reading a stream of activities.
/// </summary>
public interface ITraceReader : IDisposable
{
    /// <summary>
    ///     Reads a stream of activities.
    /// </summary>
    /// 
    /// <exception cref="FormatException">
    ///     A protocol/format error occurred.
    /// </exception>
    /// <exception cref="EndOfStreamException">
    ///     The end of stream was reached unexpectedly.
    /// </exception>
    IEnumerable<ActivityInfo> Read();
}
