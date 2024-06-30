// Copyright (c) 2024, Olaf Kober <olaf.kober@outlook.com>

using System.Diagnostics;
using Amarok.Diagnostics.Persistence.Tracing.Protos;


namespace Amarok.Diagnostics.Persistence.Tracing.Writer.Internal;


[TestFixture]
public class SourceSerializersTests
{
    private TraceRecords mRecords = null!;
    private SourceSerializer mSerializer = null!;


    [SetUp]
    public void Setup()
    {
        mRecords    = new TraceRecords();
        mSerializer = new SourceSerializer(4, ObjectsPool.Create(false));
    }


    [Test]
    public void Serialize_with_ActivitySource()
    {
        using var src = new ActivitySource("source-name");

        Check.That(mSerializer.Serialize(src, mRecords)).IsEqualTo(1);

        Check.That(mSerializer.Serialize(src, mRecords)).IsEqualTo(1);

        Check.That(mRecords.Items).HasSize(1);

        Check.That(mRecords.Items[0].DefineSource).IsNotNull();

        Check.That(mRecords.Items[0].DefineSource.Id).IsEqualTo(1);

        Check.That(mRecords.Items[0].DefineSource.Name).IsEqualTo("source-name");

        Check.That(mRecords.Items[0].DefineSource.Version).IsEqualTo("");
    }

    [Test]
    public void Serialize_with_ActivitySource_and_Version()
    {
        using var src = new ActivitySource("source-name", "1.2");

        Check.That(mSerializer.Serialize(src, mRecords)).IsEqualTo(1);

        Check.That(mSerializer.Serialize(src, mRecords)).IsEqualTo(1);

        Check.That(mRecords.Items).HasSize(1);

        Check.That(mRecords.Items[0].DefineSource).IsNotNull();

        Check.That(mRecords.Items[0].DefineSource.Id).IsEqualTo(1);

        Check.That(mRecords.Items[0].DefineSource.Name).IsEqualTo("source-name");

        Check.That(mRecords.Items[0].DefineSource.Version).IsEqualTo("1.2");
    }

    [Test]
    public void Serialize_with_ActivitySource_and_Null_Version()
    {
        using var src = new ActivitySource("source-name", null);

        Check.That(mSerializer.Serialize(src, mRecords)).IsEqualTo(1);

        Check.That(mSerializer.Serialize(src, mRecords)).IsEqualTo(1);

        Check.That(mRecords.Items).HasSize(1);

        Check.That(mRecords.Items[0].DefineSource).IsNotNull();

        Check.That(mRecords.Items[0].DefineSource.Id).IsEqualTo(1);

        Check.That(mRecords.Items[0].DefineSource.Name).IsEqualTo("source-name");

        Check.That(mRecords.Items[0].DefineSource.Version).IsEqualTo("");
    }

    [Test]
    public void Serialize_with_ActivitySources_and_Same_Name()
    {
        using var src1 = new ActivitySource("source-name");
        using var src2 = new ActivitySource("source-name");

        Check.That(mSerializer.Serialize(src1, mRecords)).IsEqualTo(1);

        Check.That(mSerializer.Serialize(src2, mRecords)).IsEqualTo(2);

        Check.That(mSerializer.Serialize(src1, mRecords)).IsEqualTo(1);

        Check.That(mSerializer.Serialize(src2, mRecords)).IsEqualTo(2);

        Check.That(mRecords.Items).HasSize(2);

        Check.That(mRecords.Items[0].DefineSource).IsNotNull();

        Check.That(mRecords.Items[0].DefineSource.Id).IsEqualTo(1);

        Check.That(mRecords.Items[0].DefineSource.Name).IsEqualTo("source-name");

        Check.That(mRecords.Items[0].DefineSource.Version).IsEqualTo("");

        Check.That(mRecords.Items[1].DefineSource).IsNotNull();

        Check.That(mRecords.Items[1].DefineSource.Id).IsEqualTo(2);

        Check.That(mRecords.Items[1].DefineSource.Name).IsEqualTo("source-name");

        Check.That(mRecords.Items[1].DefineSource.Version).IsEqualTo("");
    }

    [Test]
    public void Serialize_with_ActivitySources_and_Different_Names()
    {
        using var src1 = new ActivitySource("source-name-1");
        using var src2 = new ActivitySource("source-name-2");
        using var src3 = new ActivitySource("source-name-3");
        using var src4 = new ActivitySource("source-name-4");

        Check.That(mSerializer.Serialize(src1, mRecords)).IsEqualTo(1);

        Check.That(mSerializer.Serialize(src2, mRecords)).IsEqualTo(2);

        Check.That(mSerializer.Serialize(src3, mRecords)).IsEqualTo(3);

        Check.That(mSerializer.Serialize(src4, mRecords)).IsEqualTo(4);

        Check.That(mSerializer.Serialize(src1, mRecords)).IsEqualTo(1);

        Check.That(mSerializer.Serialize(src2, mRecords)).IsEqualTo(2);

        Check.That(mSerializer.Serialize(src3, mRecords)).IsEqualTo(3);

        Check.That(mSerializer.Serialize(src4, mRecords)).IsEqualTo(4);

        Check.That(mRecords.Items).HasSize(4);

        Check.That(mRecords.Items[0].DefineSource).IsNotNull();

        Check.That(mRecords.Items[0].DefineSource.Id).IsEqualTo(1);

        Check.That(mRecords.Items[0].DefineSource.Name).IsEqualTo("source-name-1");

        Check.That(mRecords.Items[0].DefineSource.Version).IsEqualTo("");

        Check.That(mRecords.Items[1].DefineSource).IsNotNull();

        Check.That(mRecords.Items[1].DefineSource.Id).IsEqualTo(2);

        Check.That(mRecords.Items[1].DefineSource.Name).IsEqualTo("source-name-2");

        Check.That(mRecords.Items[1].DefineSource.Version).IsEqualTo("");

        Check.That(mRecords.Items[2].DefineSource).IsNotNull();

        Check.That(mRecords.Items[2].DefineSource.Id).IsEqualTo(3);

        Check.That(mRecords.Items[2].DefineSource.Name).IsEqualTo("source-name-3");

        Check.That(mRecords.Items[2].DefineSource.Version).IsEqualTo("");

        Check.That(mRecords.Items[3].DefineSource).IsNotNull();

        Check.That(mRecords.Items[3].DefineSource.Id).IsEqualTo(4);

        Check.That(mRecords.Items[3].DefineSource.Name).IsEqualTo("source-name-4");

        Check.That(mRecords.Items[3].DefineSource.Version).IsEqualTo("");
    }

    [Test]
    public void Serialize_with_ActivitySources_and_Different_Names_Overrun()
    {
        using var src1 = new ActivitySource("source-name-1");
        using var src2 = new ActivitySource("source-name-2");
        using var src3 = new ActivitySource("source-name-3");
        using var src4 = new ActivitySource("source-name-4");
        using var src5 = new ActivitySource("source-name-5");
        using var src6 = new ActivitySource("source-name-6");

        Check.That(mSerializer.Serialize(src1, mRecords)).IsEqualTo(1);

        Check.That(mSerializer.Serialize(src2, mRecords)).IsEqualTo(2);

        Check.That(mSerializer.Serialize(src3, mRecords)).IsEqualTo(3);

        Check.That(mSerializer.Serialize(src4, mRecords)).IsEqualTo(4);

        Check.That(mSerializer.Serialize(src5, mRecords)).IsEqualTo(1);

        Check.That(mSerializer.Serialize(src6, mRecords)).IsEqualTo(2);

        Check.That(mRecords.Items).HasSize(7);

        Check.That(mRecords.Items[0].DefineSource).IsNotNull();

        Check.That(mRecords.Items[0].DefineSource.Id).IsEqualTo(1);

        Check.That(mRecords.Items[0].DefineSource.Name).IsEqualTo("source-name-1");

        Check.That(mRecords.Items[0].DefineSource.Version).IsEqualTo("");

        Check.That(mRecords.Items[1].DefineSource).IsNotNull();

        Check.That(mRecords.Items[1].DefineSource.Id).IsEqualTo(2);

        Check.That(mRecords.Items[1].DefineSource.Name).IsEqualTo("source-name-2");

        Check.That(mRecords.Items[1].DefineSource.Version).IsEqualTo("");

        Check.That(mRecords.Items[2].DefineSource).IsNotNull();

        Check.That(mRecords.Items[2].DefineSource.Id).IsEqualTo(3);

        Check.That(mRecords.Items[2].DefineSource.Name).IsEqualTo("source-name-3");

        Check.That(mRecords.Items[2].DefineSource.Version).IsEqualTo("");

        Check.That(mRecords.Items[3].DefineSource).IsNotNull();

        Check.That(mRecords.Items[3].DefineSource.Id).IsEqualTo(4);

        Check.That(mRecords.Items[3].DefineSource.Name).IsEqualTo("source-name-4");

        Check.That(mRecords.Items[3].DefineSource.Version).IsEqualTo("");

        Check.That(mRecords.Items[4].ResetSources).IsNotNull();

        Check.That(mRecords.Items[5].DefineSource).IsNotNull();

        Check.That(mRecords.Items[5].DefineSource.Id).IsEqualTo(1);

        Check.That(mRecords.Items[5].DefineSource.Name).IsEqualTo("source-name-5");

        Check.That(mRecords.Items[5].DefineSource.Version).IsEqualTo("");

        Check.That(mRecords.Items[6].DefineSource).IsNotNull();

        Check.That(mRecords.Items[6].DefineSource.Id).IsEqualTo(2);

        Check.That(mRecords.Items[6].DefineSource.Name).IsEqualTo("source-name-6");

        Check.That(mRecords.Items[6].DefineSource.Version).IsEqualTo("");
    }

    [Test]
    public void Reset()
    {
        using var src1 = new ActivitySource("source-name-1");
        using var src2 = new ActivitySource("source-name-2");

        Check.That(mSerializer.Serialize(src1, mRecords)).IsEqualTo(1);

        mSerializer.Reset();

        Check.That(mSerializer.Serialize(src2, mRecords)).IsEqualTo(1);

        Check.That(mSerializer.Serialize(src1, mRecords)).IsEqualTo(2);

        Check.That(mRecords.Items).HasSize(3);

        Check.That(mRecords.Items[0].DefineSource).IsNotNull();

        Check.That(mRecords.Items[0].DefineSource.Id).IsEqualTo(1);

        Check.That(mRecords.Items[0].DefineSource.Name).IsEqualTo("source-name-1");

        Check.That(mRecords.Items[0].DefineSource.Version).IsEqualTo("");

        Check.That(mRecords.Items[1].DefineSource).IsNotNull();

        Check.That(mRecords.Items[1].DefineSource.Id).IsEqualTo(1);

        Check.That(mRecords.Items[1].DefineSource.Name).IsEqualTo("source-name-2");

        Check.That(mRecords.Items[1].DefineSource.Version).IsEqualTo("");

        Check.That(mRecords.Items[2].DefineSource).IsNotNull();

        Check.That(mRecords.Items[2].DefineSource.Id).IsEqualTo(2);

        Check.That(mRecords.Items[2].DefineSource.Name).IsEqualTo("source-name-1");

        Check.That(mRecords.Items[2].DefineSource.Version).IsEqualTo("");
    }
}
