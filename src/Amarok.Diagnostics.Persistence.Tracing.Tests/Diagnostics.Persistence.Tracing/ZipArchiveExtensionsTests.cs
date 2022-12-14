// Copyright (c) 2023, Olaf Kober <olaf.kober@outlook.com>

using System.Collections;
using System.IO.Compression;


namespace Amarok.Diagnostics.Persistence.Tracing;


[TestFixture]
public class ZipArchiveExtensionsTests
{
    private DirectoryInfo mDirectory = null!;
    private FileInfo mArchive = null!;


    [SetUp]
    public void Setup()
    {
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));

        mDirectory = new DirectoryInfo(path);

        mArchive = new FileInfo(Path.GetTempFileName());
    }

    [TearDown]
    public void Cleanup()
    {
        mDirectory.Refresh();

        if (mDirectory.Exists)
        {
            mDirectory.Delete(true);
        }

        mArchive.Refresh();

        if (mArchive.Exists)
        {
            mArchive.Delete();
        }
    }


    private void _MakeTraceFile(Int32 ordinal)
    {
        mDirectory.Create();

        using var stream = File.OpenWrite(Path.Combine(mDirectory.FullName, $"{ordinal}.adtx"));
    }

    private void _MakeForeignFile(String name)
    {
        mDirectory.Create();

        using var stream = File.OpenWrite(Path.Combine(mDirectory.FullName, name));
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
    public void GetTraceFiles_Archive_Is_Empty()
    {
        mDirectory.Create();

        using var archive = _MakeArchive();

        Check.That(archive.GetTraceFiles()).IsEmpty();
    }

    [Test]
    public void GetTraceFiles_Archive_With_Foreign_Files_Only()
    {
        _MakeForeignFile("foo.sdf");
        _MakeForeignFile("abc.adtx");
        _MakeForeignFile("abc123.adtx");
        _MakeForeignFile("123abc.adtx");
        _MakeForeignFile("1A.adtx");

        using var archive = _MakeArchive();

        Check.That(archive.GetTraceFiles()).IsEmpty();
    }

    [Test]
    public void GetTraceFiles_Archive_With_Single_Trace_File()
    {
        _MakeTraceFile(1);

        using var archive = _MakeArchive();

        var files = archive.GetTraceFiles();

        ( (ICheck<IEnumerable>)Check.That(files) ).HasSize(1);

        Check.That(files[0].Ordinal).IsEqualTo(1);
        Check.That(files[0].Entry.Name).IsEqualTo("1.adtx");
    }

    [Test]
    public void GetTraceFiles_Archive_With_Multiple_Trace_Files()
    {
        _MakeTraceFile(1);
        _MakeTraceFile(2);
        _MakeTraceFile(5);
        _MakeTraceFile(10);
        _MakeTraceFile(11);

        using var archive = _MakeArchive();

        var files = archive.GetTraceFiles();

        ( (ICheck<IEnumerable>)Check.That(files) ).HasSize(5);

        Check.That(files[0].Ordinal).IsEqualTo(1);
        Check.That(files[0].Entry.Name).IsEqualTo("1.adtx");

        Check.That(files[1].Ordinal).IsEqualTo(2);
        Check.That(files[1].Entry.Name).IsEqualTo("2.adtx");

        Check.That(files[2].Ordinal).IsEqualTo(5);
        Check.That(files[2].Entry.Name).IsEqualTo("5.adtx");

        Check.That(files[3].Ordinal).IsEqualTo(10);
        Check.That(files[3].Entry.Name).IsEqualTo("10.adtx");

        Check.That(files[4].Ordinal).IsEqualTo(11);
        Check.That(files[4].Entry.Name).IsEqualTo("11.adtx");
    }

    [Test]
    public void GetTraceFiles_Directory_With_Mixed_Trace_And_Foreign_Files()
    {
        _MakeTraceFile(1);
        _MakeTraceFile(2);
        _MakeTraceFile(5);
        _MakeTraceFile(10);
        _MakeTraceFile(11);

        _MakeForeignFile("foo.bar");
        _MakeForeignFile("foo.adtx");
        _MakeForeignFile("foo111.adtx");
        _MakeForeignFile("111foo.adtx");

        using var archive = _MakeArchive();

        var files = archive.GetTraceFiles();

        ( (ICheck<IEnumerable>)Check.That(files) ).HasSize(5);

        Check.That(files[0].Ordinal).IsEqualTo(1);
        Check.That(files[0].Entry.Name).IsEqualTo("1.adtx");

        Check.That(files[1].Ordinal).IsEqualTo(2);
        Check.That(files[1].Entry.Name).IsEqualTo("2.adtx");

        Check.That(files[2].Ordinal).IsEqualTo(5);
        Check.That(files[2].Entry.Name).IsEqualTo("5.adtx");

        Check.That(files[3].Ordinal).IsEqualTo(10);
        Check.That(files[3].Entry.Name).IsEqualTo("10.adtx");

        Check.That(files[4].Ordinal).IsEqualTo(11);
        Check.That(files[4].Entry.Name).IsEqualTo("11.adtx");
    }
}
