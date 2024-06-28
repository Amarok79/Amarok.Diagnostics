// Copyright (c) 2024, Olaf Kober <olaf.kober@outlook.com>

namespace Amarok.Diagnostics.Persistence.OpenTelementry.Exporter;


/// <summary>
///     Provides trace export (.adtx) specific functionality.
/// </summary>
public interface IAdtxTraceExporter
{
    /// <summary>
    ///     Exports all trace log files into a Zip file at the given location. This method can be invoked
    ///     at any time.
    /// </summary>
    /// <param name="archivePath">
    ///     The path to the output Zip file.
    /// </param>
    /// <exception cref="InvalidOperationException">
    ///     Another snapshot operation is pending.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    ///     Not initialized or already disposed.
    /// </exception>
    Task HotExportAsync(String archivePath);
}
