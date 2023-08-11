// Copyright (c) 2023, Olaf Kober <olaf.kober@outlook.com>

using Amarok.Diagnostics.Persistence.Tracing.Writer;
using NFluent;
using NUnit.Framework;


namespace Amarok.Diagnostics.Persistence.OpenTelementry.Exporter;


[TestFixture]
public class AdtxTraceExporterOptionsTests
{
    [Test]
    public void Usage_DirectoryPath_with_Defaults()
    {
        var dir = Path.GetTempPath();
        var options = new AdtxTraceExporterOptions(dir);

        Check.That(options.Directory.FullName).IsEqualTo(dir);
        Check.That(options.WriterOptions.MaxDiskSpaceUsedInMegaBytes).IsEqualTo(100);
        Check.That(options.WriterOptions.UseCompression).IsTrue();

        Check.That(options.ToString()).MatchesWildcards("Directory: *, WriterOptions: { * }");
    }

    [Test]
    public void Usage_DirectoryInfo_with_WriterOptions()
    {
        var dir = new DirectoryInfo(Path.GetTempPath());

        var options = new AdtxTraceExporterOptions(dir) {
            WriterOptions = new TraceWriterOptions {
                MaxDiskSpaceUsedInMegaBytes = 123,
                UseCompression = false,
                SessionUuid = Guid.Empty,
                SessionStartTime = DateTimeOffset.MinValue,
            },
        };

        Check.That(options.Directory).IsSameReferenceAs(dir);
        Check.That(options.WriterOptions.MaxDiskSpaceUsedInMegaBytes).IsEqualTo(123);
        Check.That(options.WriterOptions.UseCompression).IsFalse();
        Check.That(options.WriterOptions.SessionUuid).IsEqualTo(Guid.Empty);
        Check.That(options.WriterOptions.SessionStartTime).IsEqualTo(DateTimeOffset.MinValue);

        Check.That(options.ToString()).MatchesWildcards("Directory: *, WriterOptions: { * }");
    }
}
