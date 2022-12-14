// Copyright (c) 2023, Olaf Kober <olaf.kober@outlook.com>

using System.Diagnostics;
using System.Globalization;
using System.IO.Compression;
using Amarok.Diagnostics.Persistence.Tracing.Reader;
using Amarok.Diagnostics.Persistence.Tracing.Writer.Internal;
using Microsoft.Extensions.Logging.Abstractions;


namespace Amarok.Diagnostics.Persistence.Tracing;


[TestFixture]
public class IntegrationTests
{
    private DirectoryInfo mDirectory = null!;
    private FileInfo mArchive = null!;
    private RollingTraceWriter? mWriter;
    private ITraceReader? mReader;


    [SetUp]
    public void Setup()
    {
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));

        mDirectory = new DirectoryInfo(path);

        mArchive = new FileInfo(Path.GetTempFileName());
    }

    [TearDown]
    public async Task Cleanup()
    {
        if (mWriter != null)
        {
            await mWriter.DisposeAsync();
        }

        mReader?.Dispose();

        mDirectory.Refresh();

        if (mDirectory.Exists)
        {
            mDirectory.Delete(true);
        }
    }


    private void _CreateWriter(
        Guid? sessionUuid = null,
        DateTimeOffset? sessionStartTime = null,
        Int32 maxDiskUseInMegaBytes = 100,
        Int32 maxItems = Int16.MaxValue,
        Int32 maxStringLength = 128,
        Int32 maxBytesLength = 128,
        Int32 redefineReferenceTimeInMilliseconds = 60000,
        Int32 flushIntervalInMilliseconds = 15000,
        Boolean useCompression = false
    )
    {
        mWriter = new RollingTraceWriter(
            mDirectory,
            sessionUuid ?? Guid.NewGuid(),
            sessionStartTime ?? DateTimeOffset.Now,
            maxDiskUseInMegaBytes * 1024 * 1024,
            10,
            maxItems,
            maxItems,
            maxStringLength,
            maxBytesLength,
            TimeSpan.FromMilliseconds(redefineReferenceTimeInMilliseconds),
            TimeSpan.FromMilliseconds(flushIntervalInMilliseconds),
            useCompression,
            false,
            NullLogger.Instance
        );

        mWriter.Initialize();
    }

    private void _CreateReaderFromDirectory()
    {
        mReader = TraceReader.OpenFolder(mDirectory.FullName);
    }

    private void _CreateReaderFromArchive()
    {
        mReader = TraceReader.OpenZipArchive(mArchive.FullName);
    }

    private String _MakePath(Int32 ordinal)
    {
        return Path.Combine(mDirectory.FullName, $"{ordinal}.adtx");
    }

    private ZipArchive _MakeArchive()
    {
        mArchive.Refresh();

        if (mArchive.Exists)
        {
            mArchive.Delete();
        }

        ZipFile.CreateFromDirectory(mDirectory.FullName, mArchive.FullName, CompressionLevel.Fastest, false);

        return ZipFile.OpenRead(mArchive.FullName);
    }


    [Test]
    [TestCase(false)]
    [TestCase(true)]
    public async Task Roundtrip_Single_Activity(Boolean useCompression)
    {
        // arrange
        var sessionUuid = Guid.NewGuid();
        var sessionStartTime = DateTimeOffset.Now;

        var activityStartTime = DateTime.UtcNow;
        var activityEndTime = activityStartTime + TimeSpan.FromMilliseconds(1234);

        var activity = new Activity("Foo()")
           .SetParentId(
                ActivityTraceId.CreateFromString("11223344556677881122334455667788"),
                ActivitySpanId.CreateFromString("1122334411223344")
            )
           .SetStartTime(activityStartTime)
           .SetEndTime(activityEndTime);

        // act writing
        _CreateWriter(sessionUuid, sessionStartTime, useCompression: useCompression);

        mWriter!.Write(activity);
        await mWriter!.FlushAsync();
        await mWriter.DisposeAsync();

        // act reading
        _CreateReaderFromDirectory();

        var activities = mReader!.Read().ToArray();

        // assert
        Check.That(activities).HasSize(1);

        Check.That(activities[0].Session.Uuid).IsEqualTo(sessionUuid);
        Check.That(activities[0].Session.StartTime).IsEqualTo(sessionStartTime);
        Check.That(activities[0].Source.Name).IsEmpty();
        Check.That(activities[0].Source.Version).IsNull();
        Check.That(activities[0].OperationName).IsEqualTo("Foo()");
        Check.That(activities[0].StartTime).IsEqualTo(new DateTimeOffset(activityStartTime));
        Check.That(activities[0].Duration).IsEqualTo(TimeSpan.FromMilliseconds(1234));
        Check.That(activities[0].Tags).IsEmpty();
        Check.That(activities[0].TraceId).IsEqualTo("11223344556677881122334455667788");
        Check.That(activities[0].ParentSpanId).IsEqualTo("1122334411223344");
        Check.That(activities[0].SpanId).IsEqualTo("0000000000000000");

        Check.That(File.Exists(_MakePath(1))).IsTrue();
        Check.That(File.Exists(_MakePath(2))).IsFalse();
    }

    [Test]
    [TestCase(false)]
    [TestCase(true)]
    public async Task Roundtrip_Single_Activity_With_Tags(Boolean useCompression)
    {
        // arrange
        var sessionUuid = Guid.NewGuid();
        var sessionStartTime = DateTimeOffset.Now;

        var activityStartTime = DateTime.UtcNow;
        var activityEndTime = activityStartTime + TimeSpan.FromMilliseconds(1234);

        var activity = new Activity("Foo()").SetStartTime(activityStartTime).SetEndTime(activityEndTime);

        var now = DateTimeOffset.Now;
        var guid = Guid.NewGuid();

        activity.AddTag("aaa", null);
        activity.AddTag("bbb", DBNull.Value);
        activity.AddTag("ccc", Byte.MaxValue);
        activity.AddTag("ddd", UInt16.MaxValue);
        activity.AddTag("eee", UInt32.MaxValue);
        activity.AddTag("fff", UInt64.MaxValue);
        activity.AddTag("ggg", SByte.MaxValue);
        activity.AddTag("hhh", Int16.MaxValue);
        activity.AddTag("iii", Int32.MaxValue);
        activity.AddTag("jjj", Int64.MaxValue);
        activity.AddTag("kkk", true);
        activity.AddTag("lll", 'X');
        activity.AddTag("mmm", "The quick brown fox");
        activity.AddTag("nnn", Half.MaxValue);
        activity.AddTag("ooo", Single.MaxValue);
        activity.AddTag("ppp", Double.MaxValue);
        activity.AddTag("qqq", Decimal.MaxValue);
        activity.AddTag("rrr", DateOnly.FromDateTime(now.DateTime));
        activity.AddTag("sss", TimeOnly.FromDateTime(now.DateTime));
        activity.AddTag("ttt", now.DateTime);
        activity.AddTag("uuu", now);
        activity.AddTag("vvv", new Byte[] { 0x11, 0x22, 0x33, 0x44 });
        activity.AddTag("www", guid);
        activity.AddTag("xxx", new Version(1, 2, 3));
        activity.AddTag("yyy", DateTimeKind.Local);

        // act writing
        _CreateWriter(sessionUuid, sessionStartTime, useCompression: useCompression);

        mWriter!.Write(activity);
        await mWriter!.FlushAsync();

        await mWriter.DisposeAsync();

        // act reading
        _CreateReaderFromDirectory();

        var activities = mReader!.Read().ToArray();

        // assert
        Check.That(activities).HasSize(1);

        Check.That(activities[0].Session.Uuid).IsEqualTo(sessionUuid);
        Check.That(activities[0].Session.StartTime).IsEqualTo(sessionStartTime);
        Check.That(activities[0].Source.Name).IsEmpty();
        Check.That(activities[0].Source.Version).IsNull();
        Check.That(activities[0].OperationName).IsEqualTo("Foo()");
        Check.That(activities[0].StartTime).IsEqualTo(new DateTimeOffset(activityStartTime));
        Check.That(activities[0].Duration).IsEqualTo(TimeSpan.FromMilliseconds(1234));

        Check.That(activities[0].Tags).HasSize(25);

        var tags = activities[0].Tags;

        Check.That(tags[0].Key).IsEqualTo("aaa");
        Check.That(tags[0].Value).IsEqualTo(null);

        Check.That(tags[1].Key).IsEqualTo("bbb");
        Check.That(tags[1].Value).IsEqualTo(null);

        Check.That(tags[2].Key).IsEqualTo("ccc");
        Check.That(tags[2].Value).IsEqualTo(Byte.MaxValue);

        Check.That(tags[3].Key).IsEqualTo("ddd");
        Check.That(tags[3].Value).IsEqualTo(UInt16.MaxValue);

        Check.That(tags[4].Key).IsEqualTo("eee");
        Check.That(tags[4].Value).IsEqualTo(UInt32.MaxValue);

        Check.That(tags[5].Key).IsEqualTo("fff");
        Check.That(tags[5].Value).IsEqualTo(UInt64.MaxValue);

        Check.That(tags[6].Key).IsEqualTo("ggg");
        Check.That(tags[6].Value).IsEqualTo(SByte.MaxValue);

        Check.That(tags[7].Key).IsEqualTo("hhh");
        Check.That(tags[7].Value).IsEqualTo(Int16.MaxValue);

        Check.That(tags[8].Key).IsEqualTo("iii");
        Check.That(tags[8].Value).IsEqualTo(Int32.MaxValue);

        Check.That(tags[9].Key).IsEqualTo("jjj");
        Check.That(tags[9].Value).IsEqualTo(Int64.MaxValue);

        Check.That(tags[10].Key).IsEqualTo("kkk");
        Check.That(tags[10].Value).IsEqualTo(true);

        Check.That(tags[11].Key).IsEqualTo("lll");
        Check.That(tags[11].Value).IsEqualTo("X");

        Check.That(tags[12].Key).IsEqualTo("mmm");
        Check.That(tags[12].Value).IsEqualTo("The quick brown fox");

        Check.That(tags[13].Key).IsEqualTo("nnn");
        Check.That(tags[13].Value).IsEqualTo((Double)Half.MaxValue);

        Check.That(tags[14].Key).IsEqualTo("ooo");
        Check.That(tags[14].Value).IsEqualTo(Single.MaxValue);

        Check.That(tags[15].Key).IsEqualTo("ppp");
        Check.That(tags[15].Value).IsEqualTo(Double.MaxValue);

        Check.That(tags[16].Key).IsEqualTo("qqq");
        Check.That(tags[16].Value).IsEqualTo(Decimal.MaxValue);

        Check.That(tags[17].Key).IsEqualTo("rrr");
        Check.That(tags[17].Value).IsEqualTo(DateOnly.FromDateTime(now.DateTime));

        Check.That(tags[18].Key).IsEqualTo("sss");
        Check.That(tags[18].Value).IsEqualTo(TimeOnly.FromDateTime(now.DateTime));

        Check.That(tags[19].Key).IsEqualTo("ttt");
        Check.That(tags[19].Value).IsEqualTo(now.DateTime);

        Check.That(tags[20].Key).IsEqualTo("uuu");
        Check.That(tags[20].Value).IsEqualTo(now);

        Check.That(tags[21].Key).IsEqualTo("vvv");
        Check.That(tags[21].Value).IsEqualTo(new Byte[] { 0x11, 0x22, 0x33, 0x44 });

        Check.That(tags[22].Key).IsEqualTo("www");
        Check.That(tags[22].Value).IsEqualTo(guid);

        Check.That(tags[23].Key).IsEqualTo("xxx");
        Check.That(tags[23].Value).IsEqualTo("1.2.3");

        Check.That(tags[24].Key).IsEqualTo("yyy");
        Check.That(tags[24].Value).IsEqualTo("Local");


        Check.That(File.Exists(_MakePath(1))).IsTrue();
        Check.That(File.Exists(_MakePath(2))).IsFalse();
    }

    [Test]
    [TestCase(false)]
    [TestCase(true)]
    public async Task Roundtrip_via_Archive_Single_Activity_With_Tags(Boolean useCompression)
    {
        // arrange
        var sessionUuid = Guid.NewGuid();
        var sessionStartTime = DateTimeOffset.Now;

        var activityStartTime = DateTime.UtcNow;
        var activityEndTime = activityStartTime + TimeSpan.FromMilliseconds(1234);

        var activity = new Activity("Foo()").SetStartTime(activityStartTime).SetEndTime(activityEndTime);

        var now = DateTimeOffset.Now;
        var guid = Guid.NewGuid();

        activity.AddTag("aaa", null);
        activity.AddTag("bbb", DBNull.Value);
        activity.AddTag("ccc", Byte.MaxValue);
        activity.AddTag("ddd", UInt16.MaxValue);
        activity.AddTag("eee", UInt32.MaxValue);
        activity.AddTag("fff", UInt64.MaxValue);
        activity.AddTag("ggg", SByte.MaxValue);
        activity.AddTag("hhh", Int16.MaxValue);
        activity.AddTag("iii", Int32.MaxValue);
        activity.AddTag("jjj", Int64.MaxValue);
        activity.AddTag("kkk", true);
        activity.AddTag("lll", 'X');
        activity.AddTag("mmm", "The quick brown fox");
        activity.AddTag("nnn", Half.MaxValue);
        activity.AddTag("ooo", Single.MaxValue);
        activity.AddTag("ppp", Double.MaxValue);
        activity.AddTag("qqq", Decimal.MaxValue);
        activity.AddTag("rrr", DateOnly.FromDateTime(now.DateTime));
        activity.AddTag("sss", TimeOnly.FromDateTime(now.DateTime));
        activity.AddTag("ttt", now.DateTime);
        activity.AddTag("uuu", now);
        activity.AddTag("vvv", new Byte[] { 0x11, 0x22, 0x33, 0x44 });
        activity.AddTag("www", guid);
        activity.AddTag("xxx", new Version(1, 2, 3));
        activity.AddTag("yyy", DateTimeKind.Local);

        // act writing
        _CreateWriter(sessionUuid, sessionStartTime, useCompression: useCompression);

        mWriter!.Write(activity);
        await mWriter!.FlushAsync();

        await await mWriter.ExportAsync(mArchive.FullName);

        await mWriter.DisposeAsync();

        // act reading
        using var archive = _MakeArchive();

        _CreateReaderFromArchive();

        var activities = mReader!.Read().ToArray();

        // assert
        Check.That(activities).HasSize(1);

        Check.That(activities[0].Session.Uuid).IsEqualTo(sessionUuid);
        Check.That(activities[0].Session.StartTime).IsEqualTo(sessionStartTime);
        Check.That(activities[0].Source.Name).IsEmpty();
        Check.That(activities[0].Source.Version).IsNull();
        Check.That(activities[0].OperationName).IsEqualTo("Foo()");
        Check.That(activities[0].StartTime).IsEqualTo(new DateTimeOffset(activityStartTime));
        Check.That(activities[0].Duration).IsEqualTo(TimeSpan.FromMilliseconds(1234));

        Check.That(activities[0].Tags).HasSize(25);

        var tags = activities[0].Tags;

        Check.That(tags[0].Key).IsEqualTo("aaa");
        Check.That(tags[0].Value).IsEqualTo(null);

        Check.That(tags[1].Key).IsEqualTo("bbb");
        Check.That(tags[1].Value).IsEqualTo(null);

        Check.That(tags[2].Key).IsEqualTo("ccc");
        Check.That(tags[2].Value).IsEqualTo(Byte.MaxValue);

        Check.That(tags[3].Key).IsEqualTo("ddd");
        Check.That(tags[3].Value).IsEqualTo(UInt16.MaxValue);

        Check.That(tags[4].Key).IsEqualTo("eee");
        Check.That(tags[4].Value).IsEqualTo(UInt32.MaxValue);

        Check.That(tags[5].Key).IsEqualTo("fff");
        Check.That(tags[5].Value).IsEqualTo(UInt64.MaxValue);

        Check.That(tags[6].Key).IsEqualTo("ggg");
        Check.That(tags[6].Value).IsEqualTo(SByte.MaxValue);

        Check.That(tags[7].Key).IsEqualTo("hhh");
        Check.That(tags[7].Value).IsEqualTo(Int16.MaxValue);

        Check.That(tags[8].Key).IsEqualTo("iii");
        Check.That(tags[8].Value).IsEqualTo(Int32.MaxValue);

        Check.That(tags[9].Key).IsEqualTo("jjj");
        Check.That(tags[9].Value).IsEqualTo(Int64.MaxValue);

        Check.That(tags[10].Key).IsEqualTo("kkk");
        Check.That(tags[10].Value).IsEqualTo(true);

        Check.That(tags[11].Key).IsEqualTo("lll");
        Check.That(tags[11].Value).IsEqualTo("X");

        Check.That(tags[12].Key).IsEqualTo("mmm");
        Check.That(tags[12].Value).IsEqualTo("The quick brown fox");

        Check.That(tags[13].Key).IsEqualTo("nnn");
        Check.That(tags[13].Value).IsEqualTo((Double)Half.MaxValue);

        Check.That(tags[14].Key).IsEqualTo("ooo");
        Check.That(tags[14].Value).IsEqualTo(Single.MaxValue);

        Check.That(tags[15].Key).IsEqualTo("ppp");
        Check.That(tags[15].Value).IsEqualTo(Double.MaxValue);

        Check.That(tags[16].Key).IsEqualTo("qqq");
        Check.That(tags[16].Value).IsEqualTo(Decimal.MaxValue);

        Check.That(tags[17].Key).IsEqualTo("rrr");
        Check.That(tags[17].Value).IsEqualTo(DateOnly.FromDateTime(now.DateTime));

        Check.That(tags[18].Key).IsEqualTo("sss");
        Check.That(tags[18].Value).IsEqualTo(TimeOnly.FromDateTime(now.DateTime));

        Check.That(tags[19].Key).IsEqualTo("ttt");
        Check.That(tags[19].Value).IsEqualTo(now.DateTime);

        Check.That(tags[20].Key).IsEqualTo("uuu");
        Check.That(tags[20].Value).IsEqualTo(now);

        Check.That(tags[21].Key).IsEqualTo("vvv");
        Check.That(tags[21].Value).IsEqualTo(new Byte[] { 0x11, 0x22, 0x33, 0x44 });

        Check.That(tags[22].Key).IsEqualTo("www");
        Check.That(tags[22].Value).IsEqualTo(guid);

        Check.That(tags[23].Key).IsEqualTo("xxx");
        Check.That(tags[23].Value).IsEqualTo("1.2.3");

        Check.That(tags[24].Key).IsEqualTo("yyy");
        Check.That(tags[24].Value).IsEqualTo("Local");
    }

    [Test]
    [TestCase(false)]
    [TestCase(true)]
    public async Task Roundtrip_Single_Activity_With_Many_Tags(Boolean useCompression)
    {
        // arrange
        var sessionUuid = Guid.NewGuid();
        var sessionStartTime = DateTimeOffset.Now;

        var activityStartTime = DateTime.UtcNow;
        var activityEndTime = activityStartTime + TimeSpan.FromMilliseconds(1234);

        var activity = new Activity("Foo()").SetStartTime(activityStartTime).SetEndTime(activityEndTime);

        for (var i = 0; i < 10000; i++)
        {
            activity.AddTag($"index_{i}", i);
        }

        // act writing
        _CreateWriter(sessionUuid, sessionStartTime, useCompression: useCompression);

        mWriter!.Write(activity);
        await mWriter!.FlushAsync();

        await mWriter.DisposeAsync();

        // act reading
        _CreateReaderFromDirectory();

        var activities = mReader!.Read().ToArray();

        // assert
        Check.That(activities).HasSize(1);

        Check.That(activities[0].Session.Uuid).IsEqualTo(sessionUuid);
        Check.That(activities[0].Session.StartTime).IsEqualTo(sessionStartTime);
        Check.That(activities[0].Source.Name).IsEmpty();
        Check.That(activities[0].Source.Version).IsNull();
        Check.That(activities[0].OperationName).IsEqualTo("Foo()");
        Check.That(activities[0].StartTime).IsEqualTo(new DateTimeOffset(activityStartTime));
        Check.That(activities[0].Duration).IsEqualTo(TimeSpan.FromMilliseconds(1234));

        Check.That(activities[0].Tags).HasSize(10000);

        var tags = activities[0].Tags;

        for (var i = 0; i < tags.Count; i++)
        {
            Check.That(tags[i].Key).IsEqualTo($"index_{i}");
            Check.That(tags[i].Value).IsEqualTo(i);
        }


        Check.That(File.Exists(_MakePath(1))).IsTrue();
        Check.That(File.Exists(_MakePath(2))).IsFalse();
    }

    [Test]
    [TestCase(false)]
    [TestCase(true)]
    public async Task Roundtrip_Many_Activities_OneWrite_OneFlushAsync(Boolean useCompression)
    {
        // arrange
        var sessionUuid = Guid.NewGuid();
        var sessionStartTime = DateTimeOffset.Now;

        var activityStartTime = DateTime.UtcNow;
        var activityEndTime = activityStartTime + TimeSpan.FromMilliseconds(1234);

        // act writing
        _CreateWriter(sessionUuid, sessionStartTime, useCompression: useCompression);

        for (var i = 0; i < 100000; i++)
        {
            var activity = new Activity("Foo()").SetParentId(
                    ActivityTraceId.CreateFromString(( i + 1 ).ToString("x32", CultureInfo.InvariantCulture)),
                    ActivitySpanId.CreateFromString(( i + 1 ).ToString("x16", CultureInfo.InvariantCulture))
                )
               .SetStartTime(activityStartTime)
               .SetEndTime(activityEndTime)
               .AddTag("index", i);

            mWriter!.Write(activity);

            await mWriter!.FlushAsync();
        }

        await mWriter!.DisposeAsync();

        // act reading
        _CreateReaderFromDirectory();

        var activities = mReader!.Read().ToArray();

        // assert
        Check.That(activities).HasSize(100000);

        for (var i = 0; i < activities.Length; i++)
        {
            Check.That(activities[i].Session.Uuid).IsEqualTo(sessionUuid);
            Check.That(activities[i].Session.StartTime).IsEqualTo(sessionStartTime);
            Check.That(activities[i].Source.Name).IsEmpty();
            Check.That(activities[i].Source.Version).IsNull();
            Check.That(activities[i].OperationName).IsEqualTo("Foo()");
            Check.That(activities[i].StartTime).IsEqualTo(new DateTimeOffset(activityStartTime));
            Check.That(activities[i].Duration).IsEqualTo(TimeSpan.FromMilliseconds(1234));
            Check.That(activities[i].Tags).HasSize(1);
            Check.That(activities[i].Tags[0].Key).IsEqualTo("index");
            Check.That(activities[i].Tags[0].Value).IsEqualTo(i);
            Check.That(activities[i].TraceId).IsEqualTo(( i + 1 ).ToString("x32", CultureInfo.InvariantCulture));
            Check.That(activities[i].ParentSpanId).IsEqualTo(( i + 1 ).ToString("x16", CultureInfo.InvariantCulture));
            Check.That(activities[i].SpanId).IsEqualTo("0000000000000000");
        }

        Check.That(File.Exists(_MakePath(1))).IsTrue();
        Check.That(File.Exists(_MakePath(2))).IsTrue();
        Check.That(File.Exists(_MakePath(3))).IsFalse();
    }

    [Test]
    [TestCase(false)]
    [TestCase(true)]
    public async Task Roundtrip_Many_Activities_ManyWrite_OneFlushAsync(Boolean useCompression)
    {
        // arrange
        var sessionUuid = Guid.NewGuid();
        var sessionStartTime = DateTimeOffset.Now;

        var activityStartTime = DateTime.UtcNow;
        var activityEndTime = activityStartTime + TimeSpan.FromMilliseconds(1234);

        // act writing
        _CreateWriter(sessionUuid, sessionStartTime, useCompression: useCompression);

        var index = 0;

        for (var j = 0; j < 100; j++)
        {
            for (var i = 0; i < 1000; i++)
            {
                var activity = new Activity("Foo()").SetParentId(
                        ActivityTraceId.CreateFromString(( index + 1 ).ToString("x32", CultureInfo.InvariantCulture)),
                        ActivitySpanId.CreateFromString(( index + 1 ).ToString("x16", CultureInfo.InvariantCulture))
                    )
                   .SetStartTime(activityStartTime)
                   .SetEndTime(activityEndTime)
                   .AddTag("index", index++);

                mWriter!.Write(activity);
            }

            await mWriter!.FlushAsync();
        }

        await mWriter!.DisposeAsync();

        // act reading
        _CreateReaderFromDirectory();

        var activities = mReader!.Read().ToArray();

        // assert
        Check.That(activities).HasSize(100000);

        for (var i = 0; i < activities.Length; i++)
        {
            Check.That(activities[i].Session.Uuid).IsEqualTo(sessionUuid);
            Check.That(activities[i].Session.StartTime).IsEqualTo(sessionStartTime);
            Check.That(activities[i].Source.Name).IsEmpty();
            Check.That(activities[i].Source.Version).IsNull();
            Check.That(activities[i].OperationName).IsEqualTo("Foo()");
            Check.That(activities[i].StartTime).IsEqualTo(new DateTimeOffset(activityStartTime));
            Check.That(activities[i].Duration).IsEqualTo(TimeSpan.FromMilliseconds(1234));
            Check.That(activities[i].Tags).HasSize(1);
            Check.That(activities[i].Tags[0].Key).IsEqualTo("index");
            Check.That(activities[i].Tags[0].Value).IsEqualTo(i);
            Check.That(activities[i].TraceId).IsEqualTo(( i + 1 ).ToString("x32", CultureInfo.InvariantCulture));
            Check.That(activities[i].ParentSpanId).IsEqualTo(( i + 1 ).ToString("x16", CultureInfo.InvariantCulture));
            Check.That(activities[i].SpanId).IsEqualTo("0000000000000000");
        }

        Check.That(File.Exists(_MakePath(1))).IsTrue();
        Check.That(File.Exists(_MakePath(2))).IsTrue();
        Check.That(File.Exists(_MakePath(3))).IsFalse();
    }

    [Test]
    [TestCase(false)]
    [TestCase(true)]
    public async Task Roundtrip_Many_Activities_Multiple_Rollover(Boolean useCompression)
    {
        // arrange
        var sessionUuid = Guid.NewGuid();
        var sessionStartTime = DateTimeOffset.Now;

        var activityStartTime = DateTime.UtcNow;
        var activityEndTime = activityStartTime + TimeSpan.FromMilliseconds(1234);

        // act writing
        _CreateWriter(sessionUuid, sessionStartTime, 20, useCompression: useCompression);

        for (var i = 0; i < 100000; i++)
        {
            var activity = new Activity("Foo()").SetParentId(
                    ActivityTraceId.CreateFromString(( i + 1 ).ToString("x32", CultureInfo.InvariantCulture)),
                    ActivitySpanId.CreateFromString(( i + 1 ).ToString("x16", CultureInfo.InvariantCulture))
                )
               .SetStartTime(activityStartTime)
               .SetEndTime(activityEndTime)
               .AddTag("index", i);

            mWriter!.Write(activity);

            await mWriter!.FlushAsync();
        }

        await mWriter!.DisposeAsync();

        // act reading
        _CreateReaderFromDirectory();

        var activities = mReader!.Read().ToArray();

        // assert
        Check.That(activities).HasSize(100000);

        for (var i = 0; i < activities.Length; i++)
        {
            Check.That(activities[i].Session.Uuid).IsEqualTo(sessionUuid);
            Check.That(activities[i].Session.StartTime).IsEqualTo(sessionStartTime);
            Check.That(activities[i].Source.Name).IsEmpty();
            Check.That(activities[i].Source.Version).IsNull();
            Check.That(activities[i].OperationName).IsEqualTo("Foo()");
            Check.That(activities[i].StartTime).IsEqualTo(new DateTimeOffset(activityStartTime));
            Check.That(activities[i].Duration).IsEqualTo(TimeSpan.FromMilliseconds(1234));
            Check.That(activities[i].Tags).HasSize(1);
            Check.That(activities[i].Tags[0].Key).IsEqualTo("index");
            Check.That(activities[i].Tags[0].Value).IsEqualTo(i);
            Check.That(activities[i].TraceId).IsEqualTo(( i + 1 ).ToString("x32", CultureInfo.InvariantCulture));
            Check.That(activities[i].ParentSpanId).IsEqualTo(( i + 1 ).ToString("x16", CultureInfo.InvariantCulture));
            Check.That(activities[i].SpanId).IsEqualTo("0000000000000000");
        }

        Check.That(File.Exists(_MakePath(1))).IsTrue();
        Check.That(File.Exists(_MakePath(2))).IsTrue();
        Check.That(File.Exists(_MakePath(3))).IsTrue();
        Check.That(File.Exists(_MakePath(4))).IsTrue();
        Check.That(File.Exists(_MakePath(5))).IsTrue();
        Check.That(File.Exists(_MakePath(6))).IsTrue();
        Check.That(File.Exists(_MakePath(7))).IsTrue();
        Check.That(File.Exists(_MakePath(8))).IsFalse();
    }

    [Test]
    [TestCase(false)]
    [TestCase(true)]
    public async Task Roundtrip_via_Archive_Many_Activities_Multiple_Rollover(Boolean useCompression)
    {
        // arrange
        var sessionUuid = Guid.NewGuid();
        var sessionStartTime = DateTimeOffset.Now;

        var activityStartTime = DateTime.UtcNow;
        var activityEndTime = activityStartTime + TimeSpan.FromMilliseconds(1234);

        // act writing
        _CreateWriter(sessionUuid, sessionStartTime, 20, useCompression: useCompression);

        for (var i = 0; i < 100000; i++)
        {
            var activity = new Activity("Foo()").SetParentId(
                    ActivityTraceId.CreateFromString(( i + 1 ).ToString("x32", CultureInfo.InvariantCulture)),
                    ActivitySpanId.CreateFromString(( i + 1 ).ToString("x16", CultureInfo.InvariantCulture))
                )
               .SetStartTime(activityStartTime)
               .SetEndTime(activityEndTime)
               .AddTag("index", i);

            mWriter!.Write(activity);

            await mWriter!.FlushAsync();
        }

        await await mWriter!.ExportAsync(mArchive.FullName);

        await mWriter!.DisposeAsync();

        // act reading
        _CreateReaderFromArchive();

        var activities = mReader!.Read().ToArray();

        // assert
        Check.That(activities).HasSize(100000);

        for (var i = 0; i < activities.Length; i++)
        {
            Check.That(activities[i].Session.Uuid).IsEqualTo(sessionUuid);
            Check.That(activities[i].Session.StartTime).IsEqualTo(sessionStartTime);
            Check.That(activities[i].Source.Name).IsEmpty();
            Check.That(activities[i].Source.Version).IsNull();
            Check.That(activities[i].OperationName).IsEqualTo("Foo()");
            Check.That(activities[i].StartTime).IsEqualTo(new DateTimeOffset(activityStartTime));
            Check.That(activities[i].Duration).IsEqualTo(TimeSpan.FromMilliseconds(1234));
            Check.That(activities[i].Tags).HasSize(1);
            Check.That(activities[i].Tags[0].Key).IsEqualTo("index");
            Check.That(activities[i].Tags[0].Value).IsEqualTo(i);
            Check.That(activities[i].TraceId).IsEqualTo(( i + 1 ).ToString("x32", CultureInfo.InvariantCulture));
            Check.That(activities[i].ParentSpanId).IsEqualTo(( i + 1 ).ToString("x16", CultureInfo.InvariantCulture));
            Check.That(activities[i].SpanId).IsEqualTo("0000000000000000");
        }
    }
}
