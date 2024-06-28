// Copyright (c) 2024, Olaf Kober <olaf.kober@outlook.com>

namespace Amarok.Diagnostics.Persistence.Tracing.Reader;


[TestFixture]
public class ActivityInfoTests
{
    [Test]
    [SetCulture("en")]
    public void Usage_with_Source_Operation_StartTime_Duration()
    {
        var session = new SessionInfo(Guid.Empty, new DateTimeOffset(2022, 10, 26, 11, 00, 00, TimeSpan.Zero));

        var source = new ActivitySourceInfo("src");
        var traceId = "11111111111111111111111111111111";
        var parentSpanId = "3333333333333333";
        var spanId = "2222222222222222";

        var start = new DateTimeOffset(2022, 10, 26, 12, 00, 00, TimeSpan.Zero);

        var duration = TimeSpan.FromMilliseconds(1234);

        var info = new ActivityInfo(session, source, "foo", traceId, parentSpanId, spanId, start, duration);

        Check.That(info.Session).IsSameReferenceAs(session);

        Check.That(info.Source).IsSameReferenceAs(source);

        Check.That(info.OperationName).IsEqualTo("foo");

        Check.That(info.TraceId).IsEqualTo(traceId);

        Check.That(info.SpanId).IsEqualTo(spanId);

        Check.That(info.ParentSpanId).IsEqualTo(parentSpanId);

        Check.That(info.StartTime).IsEqualTo(start);

        Check.That(info.Duration).IsEqualTo(duration);

        Check.That(info.Tags).IsEmpty();

        Check.That(info.EndTime).IsEqualTo(info.StartTime + duration);

        Check.That(info.StartTimeDelta).IsEqualTo(info.StartTime - session.StartTime);

        Check.That(info.EndTimeDelta).IsEqualTo(info.EndTime - session.StartTime);

        Check.That(info.ToString())
           .IsEqualTo(
                "{ Source: src, Operation: foo, StartTime: 10/26/2022 12:00:00 PM +00:00, " +
                "Duration: 1234 ms, TraceId: 11111111111111111111111111111111, " +
                "ParentSpanId: 3333333333333333, SpanId: 2222222222222222 }"
            );
    }

    [Test]
    [SetCulture("en")]
    public void Usage_with_Source_Operation_StartTime_Duration_Tags()
    {
        var session = new SessionInfo(Guid.Empty, new DateTimeOffset(2022, 10, 26, 11, 00, 00, TimeSpan.Zero));

        var source = new ActivitySourceInfo("src");
        var traceId = "11111111111111111111111111111111";
        var parentSpanId = "3333333333333333";
        var spanId = "2222222222222222";

        var start = new DateTimeOffset(2022, 10, 26, 12, 00, 00, TimeSpan.Zero);

        var duration = TimeSpan.FromMilliseconds(1234);
        var tags = new KeyValuePair<String, Object?>[] { new("aaa", 123), new("bbb", "xyz") };

        var info = new ActivityInfo(
            session,
            source,
            "foo",
            traceId,
            parentSpanId,
            spanId,
            start,
            duration
        ) { Tags = tags };

        Check.That(info.Session).IsSameReferenceAs(session);

        Check.That(info.Source).IsSameReferenceAs(source);

        Check.That(info.OperationName).IsEqualTo("foo");

        Check.That(info.TraceId).IsEqualTo(traceId);

        Check.That(info.SpanId).IsEqualTo(spanId);

        Check.That(info.ParentSpanId).IsEqualTo(parentSpanId);

        Check.That(info.StartTime).IsEqualTo(start);

        Check.That(info.Duration).IsEqualTo(duration);

        Check.That(info.Tags).HasSize(2);

        Check.That(info.Tags[0].Key).IsEqualTo("aaa");

        Check.That(info.Tags[0].Value).IsEqualTo(123);

        Check.That(info.Tags[1].Key).IsEqualTo("bbb");

        Check.That(info.Tags[1].Value).IsEqualTo("xyz");

        Check.That(info.EndTime).IsEqualTo(info.StartTime + duration);

        Check.That(info.StartTimeDelta).IsEqualTo(info.StartTime - session.StartTime);

        Check.That(info.EndTimeDelta).IsEqualTo(info.EndTime - session.StartTime);

        Check.That(info.ToString())
           .IsEqualTo(
                "{ Source: src, Operation: foo, StartTime: 10/26/2022 12:00:00 PM +00:00, " +
                "Duration: 1234 ms, TraceId: 11111111111111111111111111111111, " +
                "ParentSpanId: 3333333333333333, SpanId: 2222222222222222 }"
            );
    }
}
