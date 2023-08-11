// Copyright (c) 2023, Olaf Kober <olaf.kober@outlook.com>

using System.Diagnostics;
using Amarok.Diagnostics.Persistence.Tracing.Protos;
using Google.Protobuf.Collections;


namespace Amarok.Diagnostics.Persistence.Tracing.Writer.Internal;


[TestFixture]
public class TagsSerializerTests
{
    private TraceRecords mRecords = null!;
    private RepeatedField<TraceActivityTag> mTags = null!;
    private TagsSerializer mSerializer = null!;


    [SetUp]
    public void Setup()
    {
        mRecords = new TraceRecords();
        mTags = new RepeatedField<TraceActivityTag>();
        mSerializer = new TagsSerializer(4, 128, 128, ObjectsPool.Create(false));
    }


    [Test]
    public void Serialize_Tags()
    {
        var activity = new Activity("foo").SetTag("aaa", "123");

        mSerializer.Serialize(activity, mTags, mRecords);

        Check.That(mRecords.Items).HasSize(1);

        Check.That(mRecords.Items[0].DefineTag).IsNotNull();
        Check.That(mRecords.Items[0].DefineTag.Id).IsEqualTo(1);
        Check.That(mRecords.Items[0].DefineTag.Key).IsEqualTo("aaa");

        Check.That(mTags).HasSize(1);

        Check.That(mTags[0].KeyId).IsEqualTo(1);
        Check.That(mTags[0].Value.String).IsEqualTo("123");


        activity = new Activity("foo").SetTag("aaa", "xyz");

        mSerializer.Serialize(activity, mTags, mRecords);

        Check.That(mRecords.Items).HasSize(1);

        Check.That(mRecords.Items[0].DefineTag).IsNotNull();
        Check.That(mRecords.Items[0].DefineTag.Id).IsEqualTo(1);
        Check.That(mRecords.Items[0].DefineTag.Key).IsEqualTo("aaa");

        Check.That(mTags).HasSize(2);

        Check.That(mTags[0].KeyId).IsEqualTo(1);
        Check.That(mTags[0].Value.String).IsEqualTo("123");

        Check.That(mTags[1].KeyId).IsEqualTo(1);
        Check.That(mTags[1].Value.String).IsEqualTo("xyz");
    }

    [Test]
    public void Serialize_Tags_Keys_with_Different_Case()
    {
        var activity = new Activity("foo").SetTag("aaa", "123").SetTag("AAA", "456");

        mSerializer.Serialize(activity, mTags, mRecords);

        Check.That(mRecords.Items).HasSize(2);

        Check.That(mRecords.Items[0].DefineTag).IsNotNull();
        Check.That(mRecords.Items[0].DefineTag.Id).IsEqualTo(1);
        Check.That(mRecords.Items[0].DefineTag.Key).IsEqualTo("aaa");

        Check.That(mRecords.Items[1].DefineTag).IsNotNull();
        Check.That(mRecords.Items[1].DefineTag.Id).IsEqualTo(2);
        Check.That(mRecords.Items[1].DefineTag.Key).IsEqualTo("AAA");

        Check.That(mTags).HasSize(2);

        Check.That(mTags[0].KeyId).IsEqualTo(1);
        Check.That(mTags[0].Value.String).IsEqualTo("123");

        Check.That(mTags[1].KeyId).IsEqualTo(2);
        Check.That(mTags[1].Value.String).IsEqualTo("456");
    }

    [Test]
    public void Serialize_Tags_with_Different_Keys()
    {
        var activity = new Activity("foo").SetTag("aaa", "123")
            .SetTag("AAA", "456")
            .SetTag("bbb", "789")
            .SetTag("ccc", "000");

        mSerializer.Serialize(activity, mTags, mRecords);

        Check.That(mRecords.Items).HasSize(4);

        Check.That(mRecords.Items[0].DefineTag).IsNotNull();
        Check.That(mRecords.Items[0].DefineTag.Id).IsEqualTo(1);
        Check.That(mRecords.Items[0].DefineTag.Key).IsEqualTo("aaa");

        Check.That(mRecords.Items[1].DefineTag).IsNotNull();
        Check.That(mRecords.Items[1].DefineTag.Id).IsEqualTo(2);
        Check.That(mRecords.Items[1].DefineTag.Key).IsEqualTo("AAA");

        Check.That(mRecords.Items[2].DefineTag).IsNotNull();
        Check.That(mRecords.Items[2].DefineTag.Id).IsEqualTo(3);
        Check.That(mRecords.Items[2].DefineTag.Key).IsEqualTo("bbb");

        Check.That(mRecords.Items[3].DefineTag).IsNotNull();
        Check.That(mRecords.Items[3].DefineTag.Id).IsEqualTo(4);
        Check.That(mRecords.Items[3].DefineTag.Key).IsEqualTo("ccc");

        Check.That(mTags).HasSize(4);

        Check.That(mTags[0].KeyId).IsEqualTo(1);
        Check.That(mTags[0].Value.String).IsEqualTo("123");

        Check.That(mTags[1].KeyId).IsEqualTo(2);
        Check.That(mTags[1].Value.String).IsEqualTo("456");

        Check.That(mTags[2].KeyId).IsEqualTo(3);
        Check.That(mTags[2].Value.String).IsEqualTo("789");

        Check.That(mTags[3].KeyId).IsEqualTo(4);
        Check.That(mTags[3].Value.String).IsEqualTo("000");
    }

    [Test]
    public void Serialize_Tags_with_Different_Keys_Overrun()
    {
        var activity = new Activity("foo").SetTag("aaa", "123")
            .SetTag("AAA", "456")
            .SetTag("bbb", "789")
            .SetTag("ccc", "000")
            .SetTag("ddd", "111")
            .SetTag("eee", "222");

        mSerializer.Serialize(activity, mTags, mRecords);

        Check.That(mRecords.Items).HasSize(7);

        Check.That(mRecords.Items[0].DefineTag).IsNotNull();
        Check.That(mRecords.Items[0].DefineTag.Id).IsEqualTo(1);
        Check.That(mRecords.Items[0].DefineTag.Key).IsEqualTo("aaa");

        Check.That(mRecords.Items[1].DefineTag).IsNotNull();
        Check.That(mRecords.Items[1].DefineTag.Id).IsEqualTo(2);
        Check.That(mRecords.Items[1].DefineTag.Key).IsEqualTo("AAA");

        Check.That(mRecords.Items[2].DefineTag).IsNotNull();
        Check.That(mRecords.Items[2].DefineTag.Id).IsEqualTo(3);
        Check.That(mRecords.Items[2].DefineTag.Key).IsEqualTo("bbb");

        Check.That(mRecords.Items[3].DefineTag).IsNotNull();
        Check.That(mRecords.Items[3].DefineTag.Id).IsEqualTo(4);
        Check.That(mRecords.Items[3].DefineTag.Key).IsEqualTo("ccc");

        Check.That(mRecords.Items[4].ResetTags).IsNotNull();

        Check.That(mRecords.Items[5].DefineTag).IsNotNull();
        Check.That(mRecords.Items[5].DefineTag.Id).IsEqualTo(1);
        Check.That(mRecords.Items[5].DefineTag.Key).IsEqualTo("ddd");

        Check.That(mRecords.Items[6].DefineTag).IsNotNull();
        Check.That(mRecords.Items[6].DefineTag.Id).IsEqualTo(2);
        Check.That(mRecords.Items[6].DefineTag.Key).IsEqualTo("eee");

        Check.That(mTags).HasSize(6);

        Check.That(mTags[0].KeyId).IsEqualTo(1);
        Check.That(mTags[0].Value.String).IsEqualTo("123");

        Check.That(mTags[1].KeyId).IsEqualTo(2);
        Check.That(mTags[1].Value.String).IsEqualTo("456");

        Check.That(mTags[2].KeyId).IsEqualTo(3);
        Check.That(mTags[2].Value.String).IsEqualTo("789");

        Check.That(mTags[3].KeyId).IsEqualTo(4);
        Check.That(mTags[3].Value.String).IsEqualTo("000");

        Check.That(mTags[4].KeyId).IsEqualTo(1);
        Check.That(mTags[4].Value.String).IsEqualTo("111");

        Check.That(mTags[5].KeyId).IsEqualTo(2);
        Check.That(mTags[5].Value.String).IsEqualTo("222");
    }

    [Test]
    public void Reset()
    {
        var activity1 = new Activity("foo").SetTag("aaa", "123");
        var activity2 = new Activity("foo").SetTag("bbb", "456");

        mSerializer.Serialize(activity1, mTags, mRecords);

        mSerializer.Reset();

        mSerializer.Serialize(activity2, mTags, mRecords);
        mSerializer.Serialize(activity1, mTags, mRecords);

        Check.That(mRecords.Items).HasSize(3);

        Check.That(mRecords.Items[0].DefineTag).IsNotNull();
        Check.That(mRecords.Items[0].DefineTag.Id).IsEqualTo(1);
        Check.That(mRecords.Items[0].DefineTag.Key).IsEqualTo("aaa");

        Check.That(mRecords.Items[1].DefineTag).IsNotNull();
        Check.That(mRecords.Items[1].DefineTag.Id).IsEqualTo(1);
        Check.That(mRecords.Items[1].DefineTag.Key).IsEqualTo("bbb");

        Check.That(mRecords.Items[2].DefineTag).IsNotNull();
        Check.That(mRecords.Items[2].DefineTag.Id).IsEqualTo(2);
        Check.That(mRecords.Items[2].DefineTag.Key).IsEqualTo("aaa");
    }
}
