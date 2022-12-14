// Copyright (c) 2023, Olaf Kober <olaf.kober@outlook.com>

namespace Amarok.Diagnostics.Persistence.OpenTelementry.Exporter;


/// <summary>
///     Context for trace (.adtx) exporters.
/// </summary>
public sealed class AdtxTraceExporterContext
{
    /// <summary>
    ///     Provides access to trace log (.adtx) specific functionality.
    /// </summary>
    public required IAdtxTraceExporter Exporter { get; init; }
}
