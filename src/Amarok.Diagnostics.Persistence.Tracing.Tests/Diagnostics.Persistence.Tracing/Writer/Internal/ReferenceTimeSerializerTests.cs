// Copyright (c) 2024, Olaf Kober <olaf.kober@outlook.com>

using Amarok.Diagnostics.Persistence.Tracing.Protos;


namespace Amarok.Diagnostics.Persistence.Tracing.Writer.Internal;


[TestFixture]
public class ReferenceTimeSerializerTests
{
    private TraceRecords mRecords = null!;
    private ReferenceTimeSerializer mSerializer = null!;


    [SetUp]
    public void Setup()
    {
        mRecords = new TraceRecords();
        mSerializer = new ReferenceTimeSerializer(ObjectsPool.Create(false));
    }


    [Test]
    public void Initially_Not_Defined()
    {
        Check.That(mSerializer.IsDefined).IsFalse();
    }

    [Test]
    public void SetReferencePointInTime()
    {
        var now = new DateTimeOffset(2022, 10, 01, 20, 06, 53, 123, TimeSpan.FromHours(4));

        mSerializer.SetReferencePointInTime(now, mRecords);

        Check.That(mSerializer.IsDefined).IsTrue();

        Check.That(mSerializer.GetRelativeTimeDelta(now)).IsEqualTo(TimeSpan.Zero);

        Check.That(mRecords.Items).HasSize(1);

        Check.That(mRecords.Items[0].DefinePointInTime).IsNotNull();

        Check.That(mRecords.Items[0].DefinePointInTime.Ticks).IsEqualTo(now.LocalDateTime.Ticks);

        Check.That(mRecords.Items[0].DefinePointInTime.OffsetMinutes).IsEqualTo((Int32)now.Offset.TotalMinutes);
    }

    [Test]
    public void GetRelativeTimeDelta_With_DateTimeOffset()
    {
        var now = new DateTimeOffset(2022, 10, 01, 20, 06, 53, 123, TimeSpan.FromHours(4));

        mSerializer.SetReferencePointInTime(now, mRecords);

        var timeDelta = mSerializer.GetRelativeTimeDelta(now + TimeSpan.FromSeconds(23));

        Check.That(timeDelta).IsEqualTo(TimeSpan.FromSeconds(23));
    }

    [Test]
    public void GetRelativeTimeDelta_With_UtcDateTime()
    {
        var now = new DateTimeOffset(2022, 10, 01, 20, 06, 53, 123, TimeSpan.FromHours(4));

        mSerializer.SetReferencePointInTime(now, mRecords);

        var utc = now.UtcDateTime + TimeSpan.FromSeconds(23);
        var timeDelta = mSerializer.GetRelativeTimeDelta(utc);

        Check.That(timeDelta).IsEqualTo(TimeSpan.FromSeconds(23));
    }

    [Test]
    public void Reset()
    {
        mSerializer.SetReferencePointInTime(DateTimeOffset.Now, mRecords);

        mSerializer.Reset();

        Check.That(mSerializer.IsDefined).IsFalse();
    }
}
