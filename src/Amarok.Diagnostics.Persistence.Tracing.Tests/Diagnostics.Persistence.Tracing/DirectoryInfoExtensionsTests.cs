// Copyright (c) 2023, Olaf Kober <olaf.kober@outlook.com>

using System.Collections;


namespace Amarok.Diagnostics.Persistence.Tracing;


[TestFixture]
public class DirectoryInfoExtensionsTests
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


    private void _MakeTraceFile(
        Int32 ordinal
    )
    {
        mDirectory.Create();

        using var stream = File.OpenWrite(Path.Combine(mDirectory.FullName, $"{ordinal}.adtx"));
    }

    private void _MakeForeignFile(
        String name
    )
    {
        mDirectory.Create();

        using var stream = File.OpenWrite(Path.Combine(mDirectory.FullName, name));
    }



    [Test]
    public void GetTraceFiles_Directory_DoesNot_Exist()
    {
        Check.That(mDirectory.GetTraceFiles()).IsEmpty();
    }

    [Test]
    public void GetTraceFiles_Directory_Is_Empty()
    {
        mDirectory.Create();

        Check.That(mDirectory.GetTraceFiles()).IsEmpty();
    }

    [Test]
    public void GetTraceFiles_Directory_With_Foreign_Files_Only()
    {
        _MakeForeignFile("foo.sdf");
        _MakeForeignFile("abc.adtx");
        _MakeForeignFile("abc123.adtx");
        _MakeForeignFile("123abc.adtx");
        _MakeForeignFile("1A.adtx");

        Check.That(mDirectory.GetTraceFiles()).IsEmpty();
    }

    [Test]
    public void GetTraceFiles_Directory_With_Single_Trace_File()
    {
        _MakeTraceFile(1);

        var files = mDirectory.GetTraceFiles();

        ((ICheck<IEnumerable>)Check.That(files)).HasSize(1);

        Check.That(files[0].Ordinal).IsEqualTo(1);
        Check.That(files[0].FileInfo.Name).IsEqualTo("1.adtx");
    }

    [Test]
    public void GetTraceFiles_Directory_With_Multiple_Trace_Files()
    {
        _MakeTraceFile(1);
        _MakeTraceFile(2);
        _MakeTraceFile(5);
        _MakeTraceFile(10);
        _MakeTraceFile(11);

        var files = mDirectory.GetTraceFiles();

        ((ICheck<IEnumerable>)Check.That(files)).HasSize(5);

        Check.That(files[0].Ordinal).IsEqualTo(1);
        Check.That(files[0].FileInfo.Name).IsEqualTo("1.adtx");

        Check.That(files[1].Ordinal).IsEqualTo(2);
        Check.That(files[1].FileInfo.Name).IsEqualTo("2.adtx");

        Check.That(files[2].Ordinal).IsEqualTo(5);
        Check.That(files[2].FileInfo.Name).IsEqualTo("5.adtx");

        Check.That(files[3].Ordinal).IsEqualTo(10);
        Check.That(files[3].FileInfo.Name).IsEqualTo("10.adtx");

        Check.That(files[4].Ordinal).IsEqualTo(11);
        Check.That(files[4].FileInfo.Name).IsEqualTo("11.adtx");
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

        var files = mDirectory.GetTraceFiles();

        ((ICheck<IEnumerable>)Check.That(files)).HasSize(5);

        Check.That(files[0].Ordinal).IsEqualTo(1);
        Check.That(files[0].FileInfo.Name).IsEqualTo("1.adtx");

        Check.That(files[1].Ordinal).IsEqualTo(2);
        Check.That(files[1].FileInfo.Name).IsEqualTo("2.adtx");

        Check.That(files[2].Ordinal).IsEqualTo(5);
        Check.That(files[2].FileInfo.Name).IsEqualTo("5.adtx");

        Check.That(files[3].Ordinal).IsEqualTo(10);
        Check.That(files[3].FileInfo.Name).IsEqualTo("10.adtx");

        Check.That(files[4].Ordinal).IsEqualTo(11);
        Check.That(files[4].FileInfo.Name).IsEqualTo("11.adtx");
    }
}
