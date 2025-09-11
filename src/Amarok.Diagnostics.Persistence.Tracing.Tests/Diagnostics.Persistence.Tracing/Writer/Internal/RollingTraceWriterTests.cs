// Copyright (c) 2024, Olaf Kober <olaf.kober@outlook.com>

using System.Diagnostics;
using System.IO.Compression;
using Microsoft.Extensions.Logging.Abstractions;


namespace Amarok.Diagnostics.Persistence.Tracing.Writer.Internal;


[TestFixture]
public class RollingTraceWriterTests
{
    private DirectoryInfo mDirectory = null!;
    private FileInfo mExportFile = null!;
    private RollingTraceWriter mWriter = null!;
    private Guid mSessionUuid;
    private DateTimeOffset mSessionStartTime;


    [SetUp]
    public void Setup()
    {
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));

        mDirectory = new DirectoryInfo(path);

        mExportFile = new FileInfo(Path.Combine(path, "export.zip"));

        mSessionUuid = new Guid("0C6122357E6344E7915E97D478022F07");

        mSessionStartTime = new DateTimeOffset(
            2022,
            10,
            31,
            11,
            22,
            33,
            TimeSpan.FromHours(2)
        );

        mWriter = new RollingTraceWriter(
            mDirectory,
            mSessionUuid,
            mSessionStartTime,
            128 * 1024,
            10,
            4,
            4,
            128,
            128,
            TimeSpan.FromSeconds(5),
            TimeSpan.Zero,
            false,
            false,
            NullLogger.Instance
        );

        mWriter.Initialize();
    }

    [TearDown]
    public async Task Cleanup()
    {
        await mWriter.DisposeAsync();

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

    private Byte[] _ReadEntry(ZipArchiveEntry entry)
    {
        var bytes = new Byte[entry.Length];

        using var stream = entry.Open();

        _ = stream.Read(bytes, 0, bytes.Length);

        return bytes;
    }


    [Test]
    public async Task Usage_Dispose_Without_Write()
    {
        await mWriter.DisposeAsync();

        mDirectory.Refresh();

        Check.That(mDirectory.Exists).IsTrue();

        Check.That(File.Exists(_MakePath(1))).IsFalse();

        Check.That(File.Exists(_MakePath(2))).IsFalse();
    }

    [Test]
    public async Task Usage_Flush_Without_Write()
    {
        await mWriter.FlushAsync();
        await mWriter.DisposeAsync();

        mDirectory.Refresh();

        Check.That(mDirectory.Exists).IsTrue();

        Check.That(File.Exists(_MakePath(1))).IsFalse();

        Check.That(File.Exists(_MakePath(2))).IsFalse();
    }

    [Test]
    public async Task Usage_Write_Single_Activity_And_Flush()
    {
        var act = new Activity("operation");

        mWriter.Write(act);

        await mWriter.FlushAsync();
        await Task.Delay(500);
        await mWriter.DisposeAsync();

        mDirectory.Refresh();

        Check.That(mDirectory.Exists).IsTrue();

        Check.That(File.Exists(_MakePath(1))).IsTrue();

        var bytes = _ReadTraceFile(1);

        Check.That(bytes[..8])
        .ContainsExactly(
            0x61,
            0x64,
            0x74,
            0x78,
            0x00,
            0x01,
            0x00,
            0x0F
        );

        Check.That(bytes[8..16])
        .ContainsExactly(
            0x35,
            0x22,
            0x61,
            0x0C,
            0x63,
            0x7E,
            0xE7,
            0x44
        );

        Check.That(bytes[16..24])
        .ContainsExactly(
            0x91,
            0x5E,
            0x97,
            0xD4,
            0x78,
            0x02,
            0x2F,
            0x07
        );

        Check.That(bytes[24..32])
        .ContainsExactly(
            0x80,
            0x62,
            0x81,
            0x34,
            0x32,
            0xBB,
            0xDA,
            0x08
        );

        Check.That(bytes[32..34]).ContainsExactly(0x78, 0x00);

        Check.That(bytes.Length).IsStrictlyGreaterThan(90);
    }

    [Test]
    public async Task Usage_Write_Single_Activity_And_Flush_And_Export()
    {
        var act = new Activity("operation");

        mWriter.Write(act);

        await mWriter.FlushAsync();
        await await mWriter.ExportAsync(mExportFile.FullName);

        await Task.Delay(500);
        await mWriter.DisposeAsync();

        mDirectory.Refresh();

        Check.That(mDirectory.Exists).IsTrue();

        Check.That(File.Exists(_MakePath(1))).IsTrue();

        Check.That(File.Exists(mExportFile.FullName)).IsTrue();

        using var zip   = ZipFile.OpenRead(mExportFile.FullName);
        var       entry = zip.GetEntry("1.adtx");
        var       bytes = _ReadEntry(entry!);

        Check.That(bytes[..8])
        .ContainsExactly(
            0x61,
            0x64,
            0x74,
            0x78,
            0x00,
            0x01,
            0x00,
            0x0F
        );

        Check.That(bytes[8..16])
        .ContainsExactly(
            0x35,
            0x22,
            0x61,
            0x0C,
            0x63,
            0x7E,
            0xE7,
            0x44
        );

        Check.That(bytes[16..24])
        .ContainsExactly(
            0x91,
            0x5E,
            0x97,
            0xD4,
            0x78,
            0x02,
            0x2F,
            0x07
        );

        Check.That(bytes[24..32])
        .ContainsExactly(
            0x80,
            0x62,
            0x81,
            0x34,
            0x32,
            0xBB,
            0xDA,
            0x08
        );

        Check.That(bytes[32..34]).ContainsExactly(0x78, 0x00);

        Check.That(bytes.Length).IsStrictlyGreaterThan(90);
    }

    [Test]
    public async Task Usage_Write_Multiple_Activity_And_Flush()
    {
        var act1 = new Activity("operation-1");
        var act2 = new Activity("operation-2");

        mWriter.Write(act1);
        mWriter.Write(act2);

        await mWriter.FlushAsync();
        await Task.Delay(500);
        await mWriter.DisposeAsync();

        mDirectory.Refresh();

        Check.That(mDirectory.Exists).IsTrue();

        Check.That(File.Exists(_MakePath(1))).IsTrue();

        var bytes = _ReadTraceFile(1);

        Check.That(bytes[..8])
        .ContainsExactly(
            0x61,
            0x64,
            0x74,
            0x78,
            0x00,
            0x01,
            0x00,
            0x0F
        );

        Check.That(bytes[8..16])
        .ContainsExactly(
            0x35,
            0x22,
            0x61,
            0x0C,
            0x63,
            0x7E,
            0xE7,
            0x44
        );

        Check.That(bytes[16..24])
        .ContainsExactly(
            0x91,
            0x5E,
            0x97,
            0xD4,
            0x78,
            0x02,
            0x2F,
            0x07
        );

        Check.That(bytes[24..32])
        .ContainsExactly(
            0x80,
            0x62,
            0x81,
            0x34,
            0x32,
            0xBB,
            0xDA,
            0x08
        );

        Check.That(bytes[32..34]).ContainsExactly(0x78, 0x00);

        Check.That(bytes.Length).IsStrictlyGreaterThan(130);
    }

    [Test]
    public async Task Usage_Write_Multiple_Activity_And_Flush_And_Export()
    {
        var act1 = new Activity("operation-1");
        var act2 = new Activity("operation-2");

        mWriter.Write(act1);
        mWriter.Write(act2);

        await mWriter.FlushAsync();
        await await mWriter.ExportAsync(mExportFile.FullName);

        await Task.Delay(500);
        await mWriter.DisposeAsync();

        mDirectory.Refresh();

        Check.That(mDirectory.Exists).IsTrue();

        Check.That(File.Exists(_MakePath(1))).IsTrue();

        Check.That(File.Exists(mExportFile.FullName)).IsTrue();

        using var zip   = ZipFile.OpenRead(mExportFile.FullName);
        var       entry = zip.GetEntry("1.adtx");
        var       bytes = _ReadEntry(entry!);

        Check.That(bytes[..8])
        .ContainsExactly(
            0x61,
            0x64,
            0x74,
            0x78,
            0x00,
            0x01,
            0x00,
            0x0F
        );

        Check.That(bytes[8..16])
        .ContainsExactly(
            0x35,
            0x22,
            0x61,
            0x0C,
            0x63,
            0x7E,
            0xE7,
            0x44
        );

        Check.That(bytes[16..24])
        .ContainsExactly(
            0x91,
            0x5E,
            0x97,
            0xD4,
            0x78,
            0x02,
            0x2F,
            0x07
        );

        Check.That(bytes[24..32])
        .ContainsExactly(
            0x80,
            0x62,
            0x81,
            0x34,
            0x32,
            0xBB,
            0xDA,
            0x08
        );

        Check.That(bytes[32..34]).ContainsExactly(0x78, 0x00);

        Check.That(bytes.Length).IsStrictlyGreaterThan(130);
    }

    [Test]
    public async Task Usage_Write_Many_Activity_And_Flush()
    {
        var act1 = new Activity("operation-1");
        var act2 = new Activity("operation-2");

        for (var i = 0; i < 1000; i++)
        {
            mWriter.Write(act1);
            mWriter.Write(act2);

            await mWriter.FlushAsync();
        }

        await Task.Delay(250);
        await mWriter.DisposeAsync();

        mDirectory.Refresh();

        Check.That(mDirectory.Exists).IsTrue();

        Check.That(File.Exists(_MakePath(1))).IsTrue();

        Check.That(File.Exists(_MakePath(2))).IsTrue();

        Check.That(File.Exists(_MakePath(3))).IsTrue();

        Check.That(File.Exists(_MakePath(4))).IsTrue();

        Check.That(File.Exists(_MakePath(5))).IsTrue();

        Check.That(File.Exists(_MakePath(6))).IsTrue();

        Check.That(File.Exists(_MakePath(7))).IsTrue();

        Check.That(File.Exists(_MakePath(8))).IsFalse();

        var bytes = _ReadTraceFile(1);

        Check.That(bytes[..8])
        .ContainsExactly(
            0x61,
            0x64,
            0x74,
            0x78,
            0x00,
            0x01,
            0x00,
            0x0F
        );

        Check.That(bytes[8..16])
        .ContainsExactly(
            0x35,
            0x22,
            0x61,
            0x0C,
            0x63,
            0x7E,
            0xE7,
            0x44
        );

        Check.That(bytes[16..24])
        .ContainsExactly(
            0x91,
            0x5E,
            0x97,
            0xD4,
            0x78,
            0x02,
            0x2F,
            0x07
        );

        Check.That(bytes[24..32])
        .ContainsExactly(
            0x80,
            0x62,
            0x81,
            0x34,
            0x32,
            0xBB,
            0xDA,
            0x08
        );

        Check.That(bytes[32..34]).ContainsExactly(0x78, 0x00);

        bytes = _ReadTraceFile(2);

        Check.That(bytes[..8])
        .ContainsExactly(
            0x61,
            0x64,
            0x74,
            0x78,
            0x00,
            0x01,
            0x00,
            0x0F
        );

        Check.That(bytes[8..16])
        .ContainsExactly(
            0x35,
            0x22,
            0x61,
            0x0C,
            0x63,
            0x7E,
            0xE7,
            0x44
        );

        Check.That(bytes[16..24])
        .ContainsExactly(
            0x91,
            0x5E,
            0x97,
            0xD4,
            0x78,
            0x02,
            0x2F,
            0x07
        );

        Check.That(bytes[24..32])
        .ContainsExactly(
            0x80,
            0x62,
            0x81,
            0x34,
            0x32,
            0xBB,
            0xDA,
            0x08
        );

        Check.That(bytes[32..34]).ContainsExactly(0x78, 0x00);
    }

    [Test]
    public async Task Usage_Write_Many_Activity_And_Flush_And_Export()
    {
        var act1 = new Activity("operation-1");
        var act2 = new Activity("operation-2");

        for (var i = 0; i < 1000; i++)
        {
            mWriter.Write(act1);
            mWriter.Write(act2);

            await mWriter.FlushAsync();
        }

        await await mWriter.ExportAsync(mExportFile.FullName);

        await Task.Delay(250);
        await mWriter.DisposeAsync();

        mDirectory.Refresh();

        Check.That(mDirectory.Exists).IsTrue();

        Check.That(File.Exists(_MakePath(1))).IsTrue();

        Check.That(File.Exists(_MakePath(2))).IsTrue();

        Check.That(File.Exists(_MakePath(3))).IsTrue();

        Check.That(File.Exists(_MakePath(4))).IsTrue();

        Check.That(File.Exists(_MakePath(5))).IsTrue();

        Check.That(File.Exists(_MakePath(6))).IsTrue();

        Check.That(File.Exists(_MakePath(7))).IsTrue();

        Check.That(File.Exists(_MakePath(8))).IsFalse();

        using var zip   = ZipFile.OpenRead(mExportFile.FullName);
        var       entry = zip.GetEntry("1.adtx");
        var       bytes = _ReadEntry(entry!);

        Check.That(bytes[..8])
        .ContainsExactly(
            0x61,
            0x64,
            0x74,
            0x78,
            0x00,
            0x01,
            0x00,
            0x0F
        );

        Check.That(bytes[8..16])
        .ContainsExactly(
            0x35,
            0x22,
            0x61,
            0x0C,
            0x63,
            0x7E,
            0xE7,
            0x44
        );

        Check.That(bytes[16..24])
        .ContainsExactly(
            0x91,
            0x5E,
            0x97,
            0xD4,
            0x78,
            0x02,
            0x2F,
            0x07
        );

        Check.That(bytes[24..32])
        .ContainsExactly(
            0x80,
            0x62,
            0x81,
            0x34,
            0x32,
            0xBB,
            0xDA,
            0x08
        );

        Check.That(bytes[32..34]).ContainsExactly(0x78, 0x00);

        entry = zip.GetEntry("2.adtx");
        bytes = _ReadEntry(entry!);

        Check.That(bytes[..8])
        .ContainsExactly(
            0x61,
            0x64,
            0x74,
            0x78,
            0x00,
            0x01,
            0x00,
            0x0F
        );

        Check.That(bytes[8..16])
        .ContainsExactly(
            0x35,
            0x22,
            0x61,
            0x0C,
            0x63,
            0x7E,
            0xE7,
            0x44
        );

        Check.That(bytes[16..24])
        .ContainsExactly(
            0x91,
            0x5E,
            0x97,
            0xD4,
            0x78,
            0x02,
            0x2F,
            0x07
        );

        Check.That(bytes[24..32])
        .ContainsExactly(
            0x80,
            0x62,
            0x81,
            0x34,
            0x32,
            0xBB,
            0xDA,
            0x08
        );

        Check.That(bytes[32..34]).ContainsExactly(0x78, 0x00);

        Check.That(zip.Entries).HasSize(7);
    }
}
