// Copyright (c) 2024, Olaf Kober <olaf.kober@outlook.com>

namespace Amarok.Diagnostics.Persistence.Tracing.Reader;


/// <summary>
///     Provides information about an activity.
/// </summary>
/// 
/// <remarks>
///     An activity describes a performed operation, which starts at a specific point in time and lasts
///     for a specific duration. Every activity is associated with a single activity source and belongs
///     to an application session. Activities can carry optional tags (key-value pairs), which provide
///     additional details about the performed operation.
/// </remarks>
/// 
/// <param name="Session">
///     The application session the activity belongs to.
/// </param>
/// <param name="Source">
///     The activity source the activity is associated with.
/// </param>
/// <param name="OperationName">
///     The name of the operation performed.
/// </param>
/// <param name="TraceId">
///     The W3C TraceId of the whole trace forest used to uniquely identify a distributed trace through
///     a system.
/// </param>
/// <param name="ParentSpanId">
///     The W3C SpanId of the parent activity.
/// </param>
/// <param name="SpanId">
///     The W3C SpanId of the current activity.
/// </param>
/// <param name="StartTime">
///     The absolute point in time when the activity started.
/// </param>
/// <param name="Duration">
///     The duration of the activity.
/// </param>
public sealed record ActivityInfo(
    SessionInfo Session,
    ActivitySourceInfo Source,
    String OperationName,
    String TraceId,
    String ParentSpanId,
    String SpanId,
    DateTimeOffset StartTime,
    TimeSpan Duration
)
{
    /// <summary>
    ///     A list of optional tags associated with the activity, providing additional details about the
    ///     performed operation.
    /// </summary>
    public IReadOnlyList<KeyValuePair<String, Object?>> Tags { get; init; } = [ ];


    /// <summary>
    ///     The absolute point in time when the activity ended. Calculated from <see cref="StartTime"/> and
    ///     <see cref="Duration"/>.
    /// </summary>
    public DateTimeOffset EndTime => StartTime + Duration;


    /// <summary>
    ///     The time delta relative to the application session's start time representing the point in time
    ///     when the activity started. Calculated from <see cref="StartTime"/> and
    ///     <see cref="SessionInfo.StartTime"/>.
    /// </summary>
    public TimeSpan StartTimeDelta => StartTime - Session.StartTime;

    /// <summary>
    ///     The time delta relative to the application session's start time representing the point in time
    ///     when the activity ended. Calculated from <see cref="EndTime"/> and
    ///     <see cref="SessionInfo.StartTime"/>.
    /// </summary>
    public TimeSpan EndTimeDelta => EndTime - Session.StartTime;


    /// <inheritdoc/>
    public override String ToString()
    {
        return $"{{ Source: {Source}, Operation: {OperationName}, StartTime: {StartTime}, Duration: {
            Duration.TotalMilliseconds} ms, TraceId: {TraceId}, ParentSpanId: {ParentSpanId}, SpanId: {SpanId} }}";
    }
}
