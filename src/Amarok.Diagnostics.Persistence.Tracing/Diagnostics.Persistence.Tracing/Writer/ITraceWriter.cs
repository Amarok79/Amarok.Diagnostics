// Copyright (c) 2024, Olaf Kober <olaf.kober@outlook.com>

using System.Diagnostics;


namespace Amarok.Diagnostics.Persistence.Tracing.Writer;


/// <summary>
///     Represents a writer capable of persisting a stream of activities.
/// </summary>
public interface ITraceWriter : IAsyncDisposable
{
    /// <summary>
    ///     Serializes and enqueues a single <see cref="Activity"/> instance. <see cref="FlushAsync"/> must be called afterward
    ///     to flush enqueued activities to persistence storage. Multiple enqueued activities can be flushed with a single call
    ///     to <see cref="FlushAsync"/>.
    /// </summary>
    void Write(Activity activity);

    /// <summary>
    ///     Flushes all enqueued <see cref="Activity"/> instances to persistence storage. Activities enqueued by multiple calls
    ///     to <see cref="Write"/> can be flushed to persistence storage by calling <see cref="FlushAsync"/> once afterward.
    /// </summary>
    ValueTask FlushAsync();

    /// <summary>
    ///     Exports all trace log files into a Zip file at the given location. Calls to <see cref="Write"/> and
    ///     <see cref="FlushAsync"/> must complete before starting an export. The method returns after starting the export
    ///     operation and returns a second task which is completed at the end of the export operation.
    /// </summary>
    /// <param name="archivePath">
    ///     The path to the output Zip file.
    /// </param>
    Task<Task> ExportAsync(String archivePath);
}
