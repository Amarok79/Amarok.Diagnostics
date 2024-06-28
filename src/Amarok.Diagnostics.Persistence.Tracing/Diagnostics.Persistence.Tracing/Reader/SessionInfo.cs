// Copyright (c) 2024, Olaf Kober <olaf.kober@outlook.com>

namespace Amarok.Diagnostics.Persistence.Tracing.Reader;


/// <summary>
///     Provides information about an application session.
/// </summary>
/// 
/// <remarks>
///     Every time an instrumented application is started a new application session is initialized,
///     which is indicated by a new unique identifier. The absolute start time of the application
///     session can be used to relate activities to that application start.
/// </remarks>
/// 
/// <param name="Uuid">
///     The unique identifier of the application session.
/// </param>
/// <param name="StartTime">
///     The point in time the application started.
/// </param>
public sealed record SessionInfo(Guid Uuid, DateTimeOffset StartTime)
{
    /// <inheritdoc/>
    public override String ToString()
    {
        return $"Uuid: {Uuid}, StartTime: {StartTime}";
    }
}
