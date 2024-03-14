// Copyright (c) 2023, Olaf Kober <olaf.kober@outlook.com>

namespace Amarok.Diagnostics.Persistence.Tracing.Writer;


[TestFixture]
public class TraceWriterOptionsTests
{
    [Test]
    public void Usage_Defaults()
    {
        var options = new TraceWriterOptions();

        Check.That(options.SessionUuid).Not.IsEqualTo(Guid.Empty);
        Check.That(options.SessionStartTime).IsCloseTo(DateTimeOffset.Now, TimeSpan.FromSeconds(1));
        Check.That(options.MaxDiskSpaceUsedInMegaBytes).IsEqualTo(100);
        Check.That(options.UseCompression).IsTrue();
        Check.That(options.AutoFlushInterval).IsEqualTo(15, TimeUnit.Seconds);
    }

    [Test]
    [SetCulture("en")]
    public void Usage()
    {
        var guid = new Guid("94c8c239-d282-4002-a569-cfe2f811b336");
        var time = new DateTimeOffset(2022, 10, 31, 11, 22, 33, TimeSpan.Zero);

        var options = new TraceWriterOptions {
            SessionUuid = guid,
            SessionStartTime = time,
            MaxDiskSpaceUsedInMegaBytes = 55,
            UseCompression = false,
            AutoFlushInterval = TimeSpan.FromSeconds(33),
        };

        Check.That(options.SessionUuid).IsEqualTo(guid);
        Check.That(options.SessionStartTime).IsEqualTo(time);
        Check.That(options.MaxDiskSpaceUsedInMegaBytes).IsEqualTo(55);
        Check.That(options.UseCompression).IsFalse();
        Check.That(options.AutoFlushInterval).IsEqualTo(33, TimeUnit.Seconds);

        Check.That(options.ToString())
            .IsEqualTo(
                "SessionUuid: 94c8c239-d282-4002-a569-cfe2f811b336, " +
                "SessionStartTime: 10/31/2022 11:22:33 AM +00:00, " + "MaxDiskSpaceUsedInMegaBytes: 55 MB, " +
                "UseCompression: False, " + "AutoFlushInterval: 33 s"
            );
    }

    [Test]
    public void Usage_CopyConstructor()
    {
        var guid = new Guid("94c8c239-d282-4002-a569-cfe2f811b336");
        var time = new DateTimeOffset(2022, 10, 31, 11, 22, 33, TimeSpan.Zero);

        var inner = new TraceWriterOptions {
            SessionUuid = guid,
            SessionStartTime = time,
            MaxDiskSpaceUsedInMegaBytes = 55,
            UseCompression = false,
            AutoFlushInterval = TimeSpan.FromSeconds(33),
        };

        var options = new TraceWriterOptions(inner);

        Check.That(options.SessionUuid).IsEqualTo(guid);
        Check.That(options.SessionStartTime).IsEqualTo(time);
        Check.That(options.MaxDiskSpaceUsedInMegaBytes).IsEqualTo(55);
        Check.That(options.UseCompression).IsFalse();
        Check.That(options.AutoFlushInterval).IsEqualTo(33, TimeUnit.Seconds);
    }
}
