// Copyright (c) 2024, Olaf Kober <olaf.kober@outlook.com>

using System.Diagnostics;
using Amarok.Diagnostics.Persistence.Tracing.Protos;


namespace Amarok.Diagnostics.Persistence.Tracing.Writer.Internal;


[TestFixture]
public class TraceIdSerializerTests
{
    private TraceRecords mRecords = null!;
    private TraceIdSerializer mSerializer = null!;


    [SetUp]
    public void Setup()
    {
        mRecords = new TraceRecords();
        mSerializer = new TraceIdSerializer(4, ObjectsPool.Create(false));
    }


    [Test]
    public void Serialize_with_Default_TraceId()
    {
        var id = default(ActivityTraceId);

        Check.That(mSerializer.Serialize(id, mRecords)).IsEqualTo(1);

        Check.That(mSerializer.Serialize(id, mRecords)).IsEqualTo(1);

        Check.That(mRecords.Items).HasSize(1);

        Check.That(mRecords.Items[0].DefineTraceId).IsNotNull();

        Check.That(mRecords.Items[0].DefineTraceId.Id).IsEqualTo(1);

        Check.That(mRecords.Items[0].DefineTraceId.TraceId).IsEqualTo("00000000000000000000000000000000");
    }

    [Test]
    public void Serialize_with_Custom_TraceId()
    {
        var id = ActivityTraceId.CreateFromString("11111111111111111111111111111111");

        Check.That(mSerializer.Serialize(id, mRecords)).IsEqualTo(1);

        Check.That(mSerializer.Serialize(id, mRecords)).IsEqualTo(1);

        Check.That(mRecords.Items).HasSize(1);

        Check.That(mRecords.Items[0].DefineTraceId).IsNotNull();

        Check.That(mRecords.Items[0].DefineTraceId.Id).IsEqualTo(1);

        Check.That(mRecords.Items[0].DefineTraceId.TraceId).IsEqualTo("11111111111111111111111111111111");
    }

    [Test]
    public void Serialize_with_Multiple_TraceIds()
    {
        var id1 = ActivityTraceId.CreateFromString("11111111111111111111111111111111");
        var id2 = ActivityTraceId.CreateFromString("22222222222222222222222222222222");
        var id3 = ActivityTraceId.CreateFromString("33333333333333333333333333333333");
        var id4 = ActivityTraceId.CreateFromString("44444444444444444444444444444444");

        Check.That(mSerializer.Serialize(id1, mRecords)).IsEqualTo(1);

        Check.That(mSerializer.Serialize(id2, mRecords)).IsEqualTo(2);

        Check.That(mSerializer.Serialize(id3, mRecords)).IsEqualTo(3);

        Check.That(mSerializer.Serialize(id4, mRecords)).IsEqualTo(4);

        Check.That(mSerializer.Serialize(id1, mRecords)).IsEqualTo(1);

        Check.That(mSerializer.Serialize(id2, mRecords)).IsEqualTo(2);

        Check.That(mSerializer.Serialize(id3, mRecords)).IsEqualTo(3);

        Check.That(mSerializer.Serialize(id4, mRecords)).IsEqualTo(4);

        Check.That(mRecords.Items).HasSize(4);

        Check.That(mRecords.Items[0].DefineTraceId).IsNotNull();

        Check.That(mRecords.Items[0].DefineTraceId.Id).IsEqualTo(1);

        Check.That(mRecords.Items[0].DefineTraceId.TraceId).IsEqualTo("11111111111111111111111111111111");

        Check.That(mRecords.Items[1].DefineTraceId).IsNotNull();

        Check.That(mRecords.Items[1].DefineTraceId.Id).IsEqualTo(2);

        Check.That(mRecords.Items[1].DefineTraceId.TraceId).IsEqualTo("22222222222222222222222222222222");

        Check.That(mRecords.Items[2].DefineTraceId).IsNotNull();

        Check.That(mRecords.Items[2].DefineTraceId.Id).IsEqualTo(3);

        Check.That(mRecords.Items[2].DefineTraceId.TraceId).IsEqualTo("33333333333333333333333333333333");

        Check.That(mRecords.Items[3].DefineTraceId).IsNotNull();

        Check.That(mRecords.Items[3].DefineTraceId.Id).IsEqualTo(4);

        Check.That(mRecords.Items[3].DefineTraceId.TraceId).IsEqualTo("44444444444444444444444444444444");
    }

    [Test]
    public void Serialize_with_Multiple_TraceIds_Overrun()
    {
        var id1 = ActivityTraceId.CreateFromString("11111111111111111111111111111111");
        var id2 = ActivityTraceId.CreateFromString("22222222222222222222222222222222");
        var id3 = ActivityTraceId.CreateFromString("33333333333333333333333333333333");
        var id4 = ActivityTraceId.CreateFromString("44444444444444444444444444444444");
        var id5 = ActivityTraceId.CreateFromString("55555555555555555555555555555555");
        var id6 = ActivityTraceId.CreateFromString("66666666666666666666666666666666");

        Check.That(mSerializer.Serialize(id1, mRecords)).IsEqualTo(1);

        Check.That(mSerializer.Serialize(id2, mRecords)).IsEqualTo(2);

        Check.That(mSerializer.Serialize(id3, mRecords)).IsEqualTo(3);

        Check.That(mSerializer.Serialize(id4, mRecords)).IsEqualTo(4);

        Check.That(mSerializer.Serialize(id5, mRecords)).IsEqualTo(1);

        Check.That(mSerializer.Serialize(id6, mRecords)).IsEqualTo(2);

        Check.That(mRecords.Items).HasSize(7);

        Check.That(mRecords.Items[0].DefineTraceId).IsNotNull();

        Check.That(mRecords.Items[0].DefineTraceId.Id).IsEqualTo(1);

        Check.That(mRecords.Items[0].DefineTraceId.TraceId).IsEqualTo("11111111111111111111111111111111");

        Check.That(mRecords.Items[1].DefineTraceId).IsNotNull();

        Check.That(mRecords.Items[1].DefineTraceId.Id).IsEqualTo(2);

        Check.That(mRecords.Items[1].DefineTraceId.TraceId).IsEqualTo("22222222222222222222222222222222");

        Check.That(mRecords.Items[2].DefineTraceId).IsNotNull();

        Check.That(mRecords.Items[2].DefineTraceId.Id).IsEqualTo(3);

        Check.That(mRecords.Items[2].DefineTraceId.TraceId).IsEqualTo("33333333333333333333333333333333");

        Check.That(mRecords.Items[3].DefineTraceId).IsNotNull();

        Check.That(mRecords.Items[3].DefineTraceId.Id).IsEqualTo(4);

        Check.That(mRecords.Items[3].DefineTraceId.TraceId).IsEqualTo("44444444444444444444444444444444");

        Check.That(mRecords.Items[4].ResetTraceIds).IsNotNull();

        Check.That(mRecords.Items[5].DefineTraceId).IsNotNull();

        Check.That(mRecords.Items[5].DefineTraceId.Id).IsEqualTo(1);

        Check.That(mRecords.Items[5].DefineTraceId.TraceId).IsEqualTo("55555555555555555555555555555555");

        Check.That(mRecords.Items[6].DefineTraceId).IsNotNull();

        Check.That(mRecords.Items[6].DefineTraceId.Id).IsEqualTo(2);

        Check.That(mRecords.Items[6].DefineTraceId.TraceId).IsEqualTo("66666666666666666666666666666666");
    }

    [Test]
    public void Reset()
    {
        var id1 = ActivityTraceId.CreateFromString("11111111111111111111111111111111");
        var id2 = ActivityTraceId.CreateFromString("22222222222222222222222222222222");

        Check.That(mSerializer.Serialize(id1, mRecords)).IsEqualTo(1);

        mSerializer.Reset();

        Check.That(mSerializer.Serialize(id2, mRecords)).IsEqualTo(1);

        Check.That(mSerializer.Serialize(id1, mRecords)).IsEqualTo(2);

        Check.That(mRecords.Items).HasSize(3);

        Check.That(mRecords.Items[0].DefineTraceId).IsNotNull();

        Check.That(mRecords.Items[0].DefineTraceId.Id).IsEqualTo(1);

        Check.That(mRecords.Items[0].DefineTraceId.TraceId).IsEqualTo("11111111111111111111111111111111");

        Check.That(mRecords.Items[1].DefineTraceId).IsNotNull();

        Check.That(mRecords.Items[1].DefineTraceId.Id).IsEqualTo(1);

        Check.That(mRecords.Items[1].DefineTraceId.TraceId).IsEqualTo("22222222222222222222222222222222");

        Check.That(mRecords.Items[2].DefineTraceId).IsNotNull();

        Check.That(mRecords.Items[2].DefineTraceId.Id).IsEqualTo(2);

        Check.That(mRecords.Items[2].DefineTraceId.TraceId).IsEqualTo("11111111111111111111111111111111");
    }
}
