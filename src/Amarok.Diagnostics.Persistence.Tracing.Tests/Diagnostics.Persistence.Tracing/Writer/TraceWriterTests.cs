// Copyright (c) 2024, Olaf Kober <olaf.kober@outlook.com>

using System.Diagnostics;


namespace Amarok.Diagnostics.Persistence.Tracing.Writer;


[TestFixture]
public class TraceWriterTests
{
    private DirectoryInfo mDirectory = null!;


    [SetUp]
    public void Setup()
    {
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));

        mDirectory = new DirectoryInfo(path);
    }

    [TearDown]
    public void Cleanup()
    {
        mDirectory.Refresh();

        if (mDirectory.Exists)
        {
            mDirectory.Delete(true);
        }
    }


    private String _MakePath(Int32 ordinal)
    {
        return Path.Combine(mDirectory.FullName, $"{ordinal}.adtx");
    }

    private Byte[] _ReadTraceFile(Int32 ordinal)
    {
        using var stream = new FileStream(_MakePath(ordinal), FileMode.Open, FileAccess.Read, FileShare.ReadWrite);

        var bytes = new Byte[stream.Length];
        _ = stream.Read(bytes, 0, bytes.Length);

        return bytes;
    }


    [Test]
    public async Task Usage_With_Defaults()
    {
        var writer = TraceWriter.Create(mDirectory.FullName);

        writer.Write(new Activity("foo"));
        writer.Write(new Activity("bar"));

        await writer.FlushAsync();

        await writer.DisposeAsync();

        var bytes = _ReadTraceFile(1);

        Check.That(bytes).Not.IsEmpty();
    }

    [Test]
    public async Task Usage_With_Options()
    {
        var guid = new Guid("0C6122357E6344E7915E97D478022F07");

        var start = new DateTimeOffset(
            2022,
            10,
            31,
            11,
            22,
            33,
            TimeSpan.FromHours(2)
        );

        var options = new TraceWriterOptions {
            SessionUuid      = guid,
            SessionStartTime = start,
        };

        var writer = TraceWriter.Create(mDirectory.FullName, options);

        writer.Write(new Activity("foo"));
        writer.Write(new Activity("bar"));

        await writer.FlushAsync();

        await writer.DisposeAsync();

        var bytes = _ReadTraceFile(1);

        Check.That(bytes).Not.IsEmpty();
    }
}
