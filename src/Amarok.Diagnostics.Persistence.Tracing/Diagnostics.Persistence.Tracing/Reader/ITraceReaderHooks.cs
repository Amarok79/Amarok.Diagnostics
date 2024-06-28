// Copyright (c) 2024, Olaf Kober <olaf.kober@outlook.com>

using Amarok.Diagnostics.Persistence.Tracing.Protos;


namespace Amarok.Diagnostics.Persistence.Tracing.Reader;


/// <summary>
///     Defines hook methods for trace readers.
/// </summary>
public interface ITraceReaderHooks
{
    /// <summary>
    ///     Invoked before starting to read the next trace log file.
    /// </summary>
    void OnBeginReadFile(String filePath);

    /// <summary>
    ///     Invoked after reading the file header.
    /// </summary>
    void OnReadFileHeader(Int32 version, Boolean isCompressed, Boolean isFinished, SessionInfo session);

    /// <summary>
    ///     Invoked after starting to read a content frame.
    /// </summary>
    void OnBeginReadFrame(Byte[] buffer, Int32 frameLen);

    /// <summary>
    ///     Invoked after reading a trace record.
    /// </summary>
    void OnReadRecord(TraceRecord record);

    /// <summary>
    ///     Invoked after reading a trace activity.
    /// </summary>
    void OnReadActivity(ActivityInfo activity);

    /// <summary>
    ///     Invoked after completing to read a content frame.
    /// </summary>
    void OnEndReadFrame();

    /// <summary>
    ///     Invoked after completing to read the trace log file.
    /// </summary>
    void OnEndReadFile(String filePath);
}
