// Copyright (c) 2024, Olaf Kober <olaf.kober@outlook.com>

using System.IO.Compression;
using Microsoft.Extensions.Logging.Abstractions;


namespace Amarok.Diagnostics.Persistence.Tracing.Writer.Internal;


[TestFixture]
public class RollingFileWriterTests
{
    private DirectoryInfo mDirectory = null!;
    private RollingFileWriter mWriter = null!;


    [SetUp]
    public void Setup()
    {
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));

        mDirectory = new DirectoryInfo(path);

        mWriter = new RollingFileWriter(mDirectory, 512 * 1024, false, NullLogger.Instance);
    }

    [TearDown]
    public void Cleanup()
    {
        mWriter.Dispose();

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

    private void _MakeTraceFile(Int32 ordinal, Int32 size = 0)
    {
        mDirectory.Create();

        using var stream = File.OpenWrite(_MakePath(ordinal));

        var random = new Random();
        var bytes = new Byte[size];
        random.NextBytes(bytes);

        stream.Write(bytes);
    }

    private Byte[] _ReadTraceFile(Int32 ordinal)
    {
        mWriter.Flush();

        using var stream = new FileStream(_MakePath(ordinal), FileMode.Open, FileAccess.Read, FileShare.ReadWrite);

        var bytes = new Byte[stream.Length];
        _ = stream.Read(bytes, 0, bytes.Length);

        return bytes;
    }


    [Test]
    public void StartNewLogFile_Initial_CreatesDirectory_With_DefaultSession()
    {
        Check.That(mDirectory.Exists).IsFalse();

        mWriter.StartNewLogFile();
        mDirectory.Refresh();

        Check.That(mDirectory.Exists).IsTrue();

        Check.That(File.Exists(_MakePath(1))).IsTrue();

        var bytes = _ReadTraceFile(1);

        Check.That(bytes[..8]).ContainsExactly(0x61, 0x64, 0x74, 0x78, 0x00, 0x01, 0x00, 0x0A);

        Check.That(bytes[8..16]).ContainsExactly(0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00);

        Check.That(bytes[16..24]).ContainsExactly(0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00);

        Check.That(bytes[24..32]).ContainsExactly(0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00);

        Check.That(bytes[32..34]).ContainsExactly(0x00, 0x00);

        Check.That(bytes).HasSize(34);
    }

    [Test]
    public void StartNewLogFile_Initial_CreatesDirectory_With_Session()
    {
        Check.That(mDirectory.Exists).IsFalse();

        var guid = new Guid("0C6122357E6344E7915E97D478022F07");

        var start = new DateTimeOffset(2022, 10, 31, 11, 22, 33, TimeSpan.FromHours(2));

        mWriter.SetSession(guid, start);

        mWriter.StartNewLogFile();
        mDirectory.Refresh();

        Check.That(mDirectory.Exists).IsTrue();

        Check.That(File.Exists(_MakePath(1))).IsTrue();

        var bytes = _ReadTraceFile(1);

        Check.That(bytes[..8]).ContainsExactly(0x61, 0x64, 0x74, 0x78, 0x00, 0x01, 0x00, 0x0A);

        Check.That(bytes[8..16]).ContainsExactly(0x35, 0x22, 0x61, 0x0C, 0x63, 0x7E, 0xE7, 0x44);

        Check.That(bytes[16..24]).ContainsExactly(0x91, 0x5E, 0x97, 0xD4, 0x78, 0x02, 0x2F, 0x07);

        Check.That(bytes[24..32]).ContainsExactly(0x80, 0x62, 0x81, 0x34, 0x32, 0xBB, 0xDA, 0x08);

        Check.That(bytes[32..34]).ContainsExactly(0x78, 0x00);

        Check.That(bytes).HasSize(34);
    }

    [Test]
    public void StartNewLogFile_Initial_ExistingFiles()
    {
        _MakeTraceFile(1);
        _MakeTraceFile(2);

        mWriter.StartNewLogFile();
        mDirectory.Refresh();

        Check.That(mDirectory.Exists).IsTrue();

        Check.That(File.Exists(_MakePath(1))).IsTrue();

        Check.That(File.Exists(_MakePath(2))).IsTrue();

        Check.That(File.Exists(_MakePath(3))).IsTrue();

        var bytes = _ReadTraceFile(3);

        Check.That(bytes[..8]).ContainsExactly(0x61, 0x64, 0x74, 0x78, 0x00, 0x01, 0x00, 0x0A);

        Check.That(bytes[8..16]).ContainsExactly(0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00);

        Check.That(bytes[16..24]).ContainsExactly(0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00);

        Check.That(bytes[24..32]).ContainsExactly(0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00);

        Check.That(bytes[32..34]).ContainsExactly(0x00, 0x00);

        Check.That(bytes).HasSize(34);
    }

    [Test]
    public void StartNewLogFile_Initial_ExistingFiles_Purged()
    {
        _MakeTraceFile(1, 300 * 1024);
        _MakeTraceFile(2, 300 * 1024);
        _MakeTraceFile(3, 300 * 1024);

        mWriter.StartNewLogFile();
        mDirectory.Refresh();

        Check.That(mDirectory.Exists).IsTrue();

        Check.That(File.Exists(_MakePath(1))).IsFalse();

        Check.That(File.Exists(_MakePath(2))).IsFalse();

        Check.That(File.Exists(_MakePath(3))).IsTrue();

        Check.That(File.Exists(_MakePath(4))).IsTrue();

        var bytes = _ReadTraceFile(4);

        Check.That(bytes[..8]).ContainsExactly(0x61, 0x64, 0x74, 0x78, 0x00, 0x01, 0x00, 0x0A);

        Check.That(bytes[8..16]).ContainsExactly(0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00);

        Check.That(bytes[16..24]).ContainsExactly(0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00);

        Check.That(bytes[24..32]).ContainsExactly(0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00);

        Check.That(bytes[32..34]).ContainsExactly(0x00, 0x00);

        Check.That(bytes).HasSize(34);
    }

    [Test]
    public void StartNewLogFile_Initial_ExistingFiles_Purged_With_NonNumericFileNames()
    {
        _MakeTraceFile(1, 300 * 1024);
        _MakeTraceFile(2, 300 * 1024);
        _MakeTraceFile(3, 300 * 1024);

        File.Move(_MakePath(3), _MakePath(3) + "foo");

        mWriter.StartNewLogFile();
        mDirectory.Refresh();

        Check.That(mDirectory.Exists).IsTrue();

        Check.That(File.Exists(_MakePath(1))).IsFalse();

        Check.That(File.Exists(_MakePath(2))).IsTrue();

        Check.That(File.Exists(_MakePath(3))).IsTrue();

        Check.That(File.Exists(_MakePath(4))).IsFalse();

        var bytes = _ReadTraceFile(3);

        Check.That(bytes[..8]).ContainsExactly(0x61, 0x64, 0x74, 0x78, 0x00, 0x01, 0x00, 0x0A);

        Check.That(bytes[8..16]).ContainsExactly(0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00);

        Check.That(bytes[16..24]).ContainsExactly(0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00);

        Check.That(bytes[24..32]).ContainsExactly(0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00);

        Check.That(bytes[32..34]).ContainsExactly(0x00, 0x00);

        Check.That(bytes).HasSize(34);
    }

    [Test]
    public void Dispose_Deletes_ActiveFile()
    {
        mWriter.StartNewLogFile();
        mWriter.Dispose();
        mDirectory.Refresh();

        Check.That(mDirectory.Exists).IsTrue();

        Check.That(File.Exists(_MakePath(1))).IsFalse();

        Check.That(File.Exists(_MakePath(2))).IsFalse();
    }

    [Test]
    public void Write()
    {
        Check.That(mDirectory.Exists).IsFalse();

        mWriter.StartNewLogFile();
        mWriter.Write(new Byte[] { 0xaa, 0xbb, 0xcc });
        mDirectory.Refresh();

        Check.That(mDirectory.Exists).IsTrue();

        Check.That(File.Exists(_MakePath(1))).IsTrue();

        var bytes = _ReadTraceFile(1);

        Check.That(bytes[..8]).ContainsExactly(0x61, 0x64, 0x74, 0x78, 0x00, 0x01, 0x00, 0x0A);

        Check.That(bytes[8..16]).ContainsExactly(0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00);

        Check.That(bytes[16..24]).ContainsExactly(0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00);

        Check.That(bytes[24..32]).ContainsExactly(0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00);

        Check.That(bytes[32..34]).ContainsExactly(0x00, 0x00);

        Check.That(bytes[34..37]).ContainsExactly(0xAA, 0xBB, 0xCC);

        Check.That(bytes).HasSize(37);
    }

    [Test]
    public void Export_with_SingleFile_and_NoActiveFile()
    {
        Check.That(mDirectory.Exists).IsFalse();

        mWriter.StartNewLogFile();
        mWriter.Write(new Byte[] { 0xaa, 0xbb, 0xcc });
        mWriter.CompleteActiveLogFile();

        var exportPath = Path.Combine(mDirectory.FullName, "export.zip");

        mWriter.Export(exportPath);

        mDirectory.Refresh();

        Check.That(mDirectory.Exists).IsTrue();

        Check.That(File.Exists(_MakePath(1))).IsTrue();

        Check.That(File.Exists(_MakePath(2))).IsFalse();

        Check.That(File.Exists(exportPath)).IsTrue();

        using var zip = ZipFile.OpenRead(exportPath);

        Check.That(zip.Entries).HasSize(1);

        Check.That(zip.Entries[0].Name).IsEqualTo("1.adtx");

        Check.That(zip.Entries[0].Length).IsEqualTo(37);

        using var entryStream = zip.Entries[0].Open();

        var bytes = new Byte[zip.Entries[0].Length];

        entryStream.ReadExactly(bytes, 0, bytes.Length);

        Check.That(bytes[..8]).ContainsExactly(0x61, 0x64, 0x74, 0x78, 0x00, 0x01, 0x00, 0x0F);

        Check.That(bytes[8..16]).ContainsExactly(0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00);

        Check.That(bytes[16..24]).ContainsExactly(0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00);

        Check.That(bytes[24..32]).ContainsExactly(0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00);

        Check.That(bytes[32..34]).ContainsExactly(0x00, 0x00);

        Check.That(bytes[34..37]).ContainsExactly(0xAA, 0xBB, 0xCC);

        Check.That(bytes).HasSize(37);
    }

    [Test]
    public void Export_with_SingleFile_and_ActiveFile()
    {
        Check.That(mDirectory.Exists).IsFalse();

        mWriter.StartNewLogFile();
        mWriter.Write(new Byte[] { 0xaa, 0xbb, 0xcc });
        mWriter.CompleteActiveLogFile();
        mWriter.StartNewLogFile();
        mWriter.Write(new Byte[] { 0xaa, 0xbb, 0xcc });

        var exportPath = Path.Combine(mDirectory.FullName, "export.zip");

        mWriter.Export(exportPath);

        mDirectory.Refresh();

        Check.That(mDirectory.Exists).IsTrue();

        Check.That(File.Exists(_MakePath(1))).IsTrue();

        Check.That(File.Exists(_MakePath(2))).IsTrue();

        Check.That(File.Exists(exportPath)).IsTrue();

        using var zip = ZipFile.OpenRead(exportPath);

        Check.That(zip.Entries).HasSize(1);

        Check.That(zip.Entries[0].Name).IsEqualTo("1.adtx");

        Check.That(zip.Entries[0].Length).IsEqualTo(37);

        using var entryStream = zip.Entries[0].Open();

        var bytes = new Byte[zip.Entries[0].Length];

        entryStream.ReadExactly(bytes, 0, bytes.Length);

        Check.That(bytes[..8]).ContainsExactly(0x61, 0x64, 0x74, 0x78, 0x00, 0x01, 0x00, 0x0F);

        Check.That(bytes[8..16]).ContainsExactly(0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00);

        Check.That(bytes[16..24]).ContainsExactly(0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00);

        Check.That(bytes[24..32]).ContainsExactly(0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00);

        Check.That(bytes[32..34]).ContainsExactly(0x00, 0x00);

        Check.That(bytes[34..37]).ContainsExactly(0xAA, 0xBB, 0xCC);

        Check.That(bytes).HasSize(37);
    }
}
