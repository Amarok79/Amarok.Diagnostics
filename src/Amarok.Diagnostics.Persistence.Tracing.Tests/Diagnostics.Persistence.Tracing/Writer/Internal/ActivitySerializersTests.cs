// Copyright (c) 2024, Olaf Kober <olaf.kober@outlook.com>

using System.Diagnostics;
using Amarok.Diagnostics.Persistence.Tracing.Protos;


namespace Amarok.Diagnostics.Persistence.Tracing.Writer.Internal;


[TestFixture]
public class ActivitySerializersTests
{
    private TraceRecords mRecords = null!;
    private ActivitySerializer mSerializer = null!;


    [SetUp]
    public void Setup()
    {
        mRecords = new TraceRecords();

        mSerializer = new ActivitySerializer(
            4,
            4,
            128,
            128,
            TimeSpan.FromMilliseconds(100),
            ObjectsPool.Create(false)
        );
    }


    [Test]
    public void Serialize_Activity()
    {
        var act1 = new Activity("operation-1").SetParentId(
            ActivityTraceId.CreateFromString("11111111111111111111111111111111"),
            ActivitySpanId.CreateFromString("9999999999999999")
        );

        var act2 = new Activity("operation-2");

        mSerializer.Serialize(act1, mRecords);
        mSerializer.Serialize(act2, mRecords);

        Check.That(mRecords.Items).HasSize(10);

        Check.That(mRecords.Items[0].DefinePointInTime).IsNotNull();

        Check.That(mRecords.Items[1].DefineSource).IsNotNull();

        Check.That(mRecords.Items[1].DefineSource.Id).IsEqualTo(1);

        Check.That(mRecords.Items[1].DefineSource.Name).IsEqualTo("");

        Check.That(mRecords.Items[1].DefineSource.Version).IsEqualTo("");

        Check.That(mRecords.Items[2].DefineOperation).IsNotNull();

        Check.That(mRecords.Items[2].DefineOperation.Id).IsEqualTo(1);

        Check.That(mRecords.Items[2].DefineOperation.Name).IsEqualTo("operation-1");

        Check.That(mRecords.Items[3].DefineTraceId).IsNotNull();

        Check.That(mRecords.Items[3].DefineTraceId.Id).IsEqualTo(1);

        Check.That(mRecords.Items[3].DefineTraceId.TraceId).IsEqualTo("11111111111111111111111111111111");

        Check.That(mRecords.Items[4].DefineParentSpanId).IsNotNull();

        Check.That(mRecords.Items[4].DefineParentSpanId.Id).IsEqualTo(1);

        Check.That(mRecords.Items[4].DefineParentSpanId.SpanId).IsEqualTo("9999999999999999");

        Check.That(mRecords.Items[5].Activity).IsNotNull();

        Check.That(mRecords.Items[5].Activity.SourceId).IsEqualTo(1);

        Check.That(mRecords.Items[5].Activity.OperationId).IsEqualTo(1);

        Check.That(mRecords.Items[5].Activity.TraceId).IsEqualTo(1);

        Check.That(mRecords.Items[5].Activity.SpanId).IsEqualTo("0000000000000000");

        Check.That(mRecords.Items[5].Activity.ParentSpanId).IsEqualTo(1);

        Check.That(mRecords.Items[5].Activity.StartTimeRelativeTicks).Not.IsEqualTo(0);

        Check.That(mRecords.Items[5].Activity.DurationTicks).IsEqualTo(0);

        Check.That(mRecords.Items[5].Activity.Tags.Count).IsEqualTo(0);

        Check.That(mRecords.Items[6].DefineOperation).IsNotNull();

        Check.That(mRecords.Items[6].DefineOperation.Id).IsEqualTo(2);

        Check.That(mRecords.Items[6].DefineOperation.Name).IsEqualTo("operation-2");

        Check.That(mRecords.Items[7].DefineTraceId).IsNotNull();

        Check.That(mRecords.Items[7].DefineTraceId.Id).IsEqualTo(2);

        Check.That(mRecords.Items[7].DefineTraceId.TraceId).IsEqualTo("00000000000000000000000000000000");

        Check.That(mRecords.Items[8].DefineParentSpanId).IsNotNull();

        Check.That(mRecords.Items[8].DefineParentSpanId.Id).IsEqualTo(2);

        Check.That(mRecords.Items[8].DefineParentSpanId.SpanId).IsEqualTo("0000000000000000");

        Check.That(mRecords.Items[9].Activity).IsNotNull();

        Check.That(mRecords.Items[9].Activity.SourceId).IsEqualTo(1);

        Check.That(mRecords.Items[9].Activity.OperationId).IsEqualTo(2);

        Check.That(mRecords.Items[9].Activity.TraceId).IsEqualTo(2);

        Check.That(mRecords.Items[9].Activity.SpanId).IsEqualTo("0000000000000000");

        Check.That(mRecords.Items[9].Activity.ParentSpanId).IsEqualTo(2);

        Check.That(mRecords.Items[9].Activity.StartTimeRelativeTicks).Not.IsEqualTo(0);

        Check.That(mRecords.Items[9].Activity.DurationTicks).IsEqualTo(0);

        Check.That(mRecords.Items[9].Activity.Tags.Count).IsEqualTo(0);
    }

    [Test]
    public void Serialize_RedefineReferenceTime()
    {
        var act1 = new Activity("operation-1").SetStartTime(DateTime.UtcNow);
        mSerializer.Serialize(act1, mRecords);

        var act2 = new Activity("operation-2").SetStartTime(DateTime.UtcNow + TimeSpan.FromSeconds(1));
        mSerializer.Serialize(act2, mRecords);

        Check.That(mRecords.Items).HasSize(9);

        Check.That(mRecords.Items[0].DefinePointInTime).IsNotNull();

        Check.That(mRecords.Items[1].DefineSource).IsNotNull();

        Check.That(mRecords.Items[2].DefineOperation).IsNotNull();

        Check.That(mRecords.Items[2].DefineOperation.Id).IsEqualTo(1);

        Check.That(mRecords.Items[2].DefineOperation.Name).IsEqualTo("operation-1");

        Check.That(mRecords.Items[3].DefineTraceId).IsNotNull();

        Check.That(mRecords.Items[3].DefineTraceId.Id).IsEqualTo(1);

        Check.That(mRecords.Items[3].DefineTraceId.TraceId).IsEqualTo("00000000000000000000000000000000");

        Check.That(mRecords.Items[4].DefineParentSpanId).IsNotNull();

        Check.That(mRecords.Items[4].DefineParentSpanId.Id).IsEqualTo(1);

        Check.That(mRecords.Items[4].DefineParentSpanId.SpanId).IsEqualTo("0000000000000000");

        Check.That(mRecords.Items[5].Activity).IsNotNull();

        Check.That(mRecords.Items[5].Activity.SourceId).IsEqualTo(1);

        Check.That(mRecords.Items[5].Activity.OperationId).IsEqualTo(1);

        Check.That(mRecords.Items[5].Activity.TraceId).IsEqualTo(1);

        Check.That(mRecords.Items[5].Activity.SpanId).IsEqualTo("0000000000000000");

        Check.That(mRecords.Items[5].Activity.ParentSpanId).IsEqualTo(1);

        Check.That(mRecords.Items[6].DefinePointInTime).IsNotNull();

        Check.That(mRecords.Items[7].DefineOperation).IsNotNull();

        Check.That(mRecords.Items[7].DefineOperation.Id).IsEqualTo(2);

        Check.That(mRecords.Items[7].DefineOperation.Name).IsEqualTo("operation-2");

        Check.That(mRecords.Items[8].Activity).IsNotNull();

        Check.That(mRecords.Items[8].Activity.SourceId).IsEqualTo(1);

        Check.That(mRecords.Items[8].Activity.OperationId).IsEqualTo(2);

        Check.That(mRecords.Items[8].Activity.TraceId).IsEqualTo(1);

        Check.That(mRecords.Items[8].Activity.SpanId).IsEqualTo("0000000000000000");

        Check.That(mRecords.Items[8].Activity.ParentSpanId).IsEqualTo(1);
    }

    [Test]
    public void Reset()
    {
        var act1 = new Activity("operation-1");
        var act2 = new Activity("operation-2");

        mSerializer.Serialize(act1, mRecords);
        mSerializer.Serialize(act2, mRecords);

        mSerializer.Reset();

        mSerializer.Serialize(act2, mRecords);
        mSerializer.Serialize(act1, mRecords);

        Check.That(mRecords.Items).HasSize(16);

        Check.That(mRecords.Items[0].DefinePointInTime).IsNotNull();

        Check.That(mRecords.Items[1].DefineSource).IsNotNull();

        Check.That(mRecords.Items[2].DefineOperation).IsNotNull();

        Check.That(mRecords.Items[2].DefineOperation.Id).IsEqualTo(1);

        Check.That(mRecords.Items[2].DefineOperation.Name).IsEqualTo("operation-1");

        Check.That(mRecords.Items[3].DefineTraceId).IsNotNull();

        Check.That(mRecords.Items[3].DefineTraceId.Id).IsEqualTo(1);

        Check.That(mRecords.Items[3].DefineTraceId.TraceId).IsEqualTo("00000000000000000000000000000000");

        Check.That(mRecords.Items[4].DefineParentSpanId).IsNotNull();

        Check.That(mRecords.Items[4].DefineParentSpanId.Id).IsEqualTo(1);

        Check.That(mRecords.Items[4].DefineParentSpanId.SpanId).IsEqualTo("0000000000000000");

        Check.That(mRecords.Items[5].Activity).IsNotNull();

        Check.That(mRecords.Items[5].Activity.SourceId).IsEqualTo(1);

        Check.That(mRecords.Items[5].Activity.OperationId).IsEqualTo(1);

        Check.That(mRecords.Items[5].Activity.TraceId).IsEqualTo(1);

        Check.That(mRecords.Items[5].Activity.SpanId).IsEqualTo("0000000000000000");

        Check.That(mRecords.Items[5].Activity.ParentSpanId).IsEqualTo(1);

        Check.That(mRecords.Items[6].DefineOperation).IsNotNull();

        Check.That(mRecords.Items[6].DefineOperation.Id).IsEqualTo(2);

        Check.That(mRecords.Items[6].DefineOperation.Name).IsEqualTo("operation-2");

        Check.That(mRecords.Items[7].Activity).IsNotNull();

        Check.That(mRecords.Items[7].Activity.SourceId).IsEqualTo(1);

        Check.That(mRecords.Items[7].Activity.OperationId).IsEqualTo(2);

        Check.That(mRecords.Items[7].Activity.TraceId).IsEqualTo(1);

        Check.That(mRecords.Items[7].Activity.SpanId).IsEqualTo("0000000000000000");

        Check.That(mRecords.Items[7].Activity.ParentSpanId).IsEqualTo(1);

        Check.That(mRecords.Items[8].DefinePointInTime).IsNotNull();

        Check.That(mRecords.Items[9].DefineSource).IsNotNull();

        Check.That(mRecords.Items[10].DefineOperation).IsNotNull();

        Check.That(mRecords.Items[10].DefineOperation.Id).IsEqualTo(1);

        Check.That(mRecords.Items[10].DefineOperation.Name).IsEqualTo("operation-2");

        Check.That(mRecords.Items[11].DefineTraceId).IsNotNull();

        Check.That(mRecords.Items[11].DefineTraceId.Id).IsEqualTo(1);

        Check.That(mRecords.Items[11].DefineTraceId.TraceId).IsEqualTo("00000000000000000000000000000000");

        Check.That(mRecords.Items[12].DefineParentSpanId).IsNotNull();

        Check.That(mRecords.Items[12].DefineParentSpanId.Id).IsEqualTo(1);

        Check.That(mRecords.Items[12].DefineParentSpanId.SpanId).IsEqualTo("0000000000000000");

        Check.That(mRecords.Items[13].Activity).IsNotNull();

        Check.That(mRecords.Items[13].Activity.SourceId).IsEqualTo(1);

        Check.That(mRecords.Items[13].Activity.OperationId).IsEqualTo(1);

        Check.That(mRecords.Items[13].Activity.TraceId).IsEqualTo(1);

        Check.That(mRecords.Items[13].Activity.SpanId).IsEqualTo("0000000000000000");

        Check.That(mRecords.Items[13].Activity.ParentSpanId).IsEqualTo(1);

        Check.That(mRecords.Items[14].DefineOperation).IsNotNull();

        Check.That(mRecords.Items[14].DefineOperation.Id).IsEqualTo(2);

        Check.That(mRecords.Items[14].DefineOperation.Name).IsEqualTo("operation-1");

        Check.That(mRecords.Items[15].Activity).IsNotNull();

        Check.That(mRecords.Items[15].Activity.SourceId).IsEqualTo(1);

        Check.That(mRecords.Items[15].Activity.OperationId).IsEqualTo(2);

        Check.That(mRecords.Items[15].Activity.TraceId).IsEqualTo(1);

        Check.That(mRecords.Items[15].Activity.SpanId).IsEqualTo("0000000000000000");

        Check.That(mRecords.Items[15].Activity.ParentSpanId).IsEqualTo(1);
    }
}
