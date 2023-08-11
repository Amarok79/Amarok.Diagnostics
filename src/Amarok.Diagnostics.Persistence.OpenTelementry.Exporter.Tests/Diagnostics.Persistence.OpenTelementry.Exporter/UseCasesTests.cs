// Copyright (c) 2023, Olaf Kober <olaf.kober@outlook.com>

using System.Diagnostics;
using Amarok.Diagnostics.Persistence.Tracing.Reader;
using Amarok.Diagnostics.Persistence.Tracing.Writer;
using NFluent;
using NUnit.Framework;
using OpenTelemetry;
using OpenTelemetry.Trace;


namespace Amarok.Diagnostics.Persistence.OpenTelementry.Exporter;


[TestFixture]
public class IntegrationTests
{
    private DirectoryInfo mDirectory = null!;
    private ITraceReader? mReader;


    [SetUp]
    public void Setup()
    {
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));

        mDirectory = new DirectoryInfo(path);
    }

    [TearDown]
    public void Cleanup()
    {
        mReader?.Dispose();

        mDirectory.Refresh();

        if (mDirectory.Exists)
        {
            mDirectory.Delete(true);
        }
    }


    private void _CreateReader()
    {
        mReader = TraceReader.OpenFolder(mDirectory.FullName);
    }

    private String _MakePath(
        Int32 ordinal
    )
    {
        return Path.Combine(mDirectory.FullName, $"{ordinal}.adtx");
    }


    [Test]
    public void UseCase_Single_Activity_Single_Source()
    {
        // arrange
        var options = new AdtxTraceExporterOptions(mDirectory) {
            WriterOptions = new TraceWriterOptions {
                MaxDiskSpaceUsedInMegaBytes = 100,
            },
        };

        // act writing
        using (var provider = Sdk.CreateTracerProviderBuilder()
            .AddSource("*")
            .AddAdtxTraceExporter(options, out var ctx)
            .Build())
        {
            Check.That(ctx).IsNotNull();
            Check.That(ctx.Exporter).IsNotNull();

            using (var activitySource = new ActivitySource("Source"))
            {
                using (var activity = activitySource.StartActivity("Operation"))
                {
                    activity?.AddTag("#", 0);
                    activity?.Dispose();
                }
            }

            provider?.ForceFlush();
            provider?.Shutdown();
        }

        // act reading
        _CreateReader();

        var activities = mReader!.Read().ToArray();

        // assert
        Check.That(activities).HasSize(1);

        for (var i = 0; i < activities.Length; i++)
        {
            Check.That(activities[i].Session.Uuid).IsEqualTo(options.WriterOptions.SessionUuid);
            Check.That(activities[i].Session.StartTime).IsEqualTo(options.WriterOptions.SessionStartTime);
            Check.That(activities[i].Source.Name).IsEqualTo("Source");
            Check.That(activities[i].Source.Version).IsNull();
            Check.That(activities[i].OperationName).IsEqualTo("Operation");
            Check.That(activities[i].Tags).HasSize(1);
            Check.That(activities[i].Tags[0].Key).IsEqualTo("#");
            Check.That(activities[i].Tags[0].Value).IsEqualTo(i);
        }

        Check.That(File.Exists(_MakePath(1))).IsTrue();
        Check.That(File.Exists(_MakePath(2))).IsFalse();
    }

    [Test]
    public void UseCase_Many_Activity_Single_Source()
    {
        // arrange
        var options = new AdtxTraceExporterOptions(mDirectory) {
            WriterOptions = new TraceWriterOptions {
                MaxDiskSpaceUsedInMegaBytes = 100,
            },
        };

        // act writing
        using (var provider = Sdk.CreateTracerProviderBuilder()
            .AddSource("*")
            .AddAdtxTraceExporter(options, out var ctx)
            .Build())
        {
            Check.That(ctx).IsNotNull();
            Check.That(ctx.Exporter).IsNotNull();

            for (var i = 0; i < 10000; i++)
            {
                using (var activitySource = new ActivitySource("Source"))
                {
                    using (var activity = activitySource.StartActivity("Operation"))
                    {
                        activity?.AddTag("#", i);
                        activity?.Dispose();
                    }
                }
            }

            provider?.ForceFlush();
            provider?.Shutdown();
        }

        // act reading
        _CreateReader();

        var activities = mReader!.Read().ToArray();

        // assert
        Check.That(activities).HasSize(10000);

        for (var i = 0; i < activities.Length; i++)
        {
            Check.That(activities[i].Session.Uuid).IsEqualTo(options.WriterOptions.SessionUuid);
            Check.That(activities[i].Session.StartTime).IsEqualTo(options.WriterOptions.SessionStartTime);
            Check.That(activities[i].Source.Name).IsEqualTo("Source");
            Check.That(activities[i].Source.Version).IsNull();
            Check.That(activities[i].OperationName).IsEqualTo("Operation");
            Check.That(activities[i].Tags).HasSize(1);
            Check.That(activities[i].Tags[0].Key).IsEqualTo("#");
            Check.That(activities[i].Tags[0].Value).IsEqualTo(i);
        }

        Check.That(File.Exists(_MakePath(1))).IsTrue();
        Check.That(File.Exists(_MakePath(2))).IsFalse();
    }

    [Test]
    public async Task UseCase_Many_Activity_Single_Source_Single_Export()
    {
        // arrange
        var options = new AdtxTraceExporterOptions(mDirectory) {
            WriterOptions = new TraceWriterOptions {
                MaxDiskSpaceUsedInMegaBytes = 100,
            },
        };

        // act writing
        using (var provider = Sdk.CreateTracerProviderBuilder()
            .AddSource("*")
            .AddAdtxTraceExporter(options, out var ctx)
            .Build())
        {
            Check.That(ctx).IsNotNull();
            Check.That(ctx.Exporter).IsNotNull();

            for (var i = 0; i < 10000; i++)
            {
                using (var activitySource = new ActivitySource("Source"))
                {
                    using (var activity = activitySource.StartActivity("Operation"))
                    {
                        activity?.AddTag("#", i);
                        activity?.Dispose();
                    }
                }

                if (i == 3333)
                {
                    await ctx.Exporter.HotExportAsync(Path.Combine(mDirectory.FullName, "export.zip"));
                }
            }

            provider?.ForceFlush();
            provider?.Shutdown();
        }

        // act reading
        _CreateReader();

        var activities = mReader!.Read().ToArray();

        // assert
        Check.That(activities).HasSize(10000);

        for (var i = 0; i < activities.Length; i++)
        {
            Check.That(activities[i].Session.Uuid).IsEqualTo(options.WriterOptions.SessionUuid);
            Check.That(activities[i].Session.StartTime).IsEqualTo(options.WriterOptions.SessionStartTime);
            Check.That(activities[i].Source.Name).IsEqualTo("Source");
            Check.That(activities[i].Source.Version).IsNull();
            Check.That(activities[i].OperationName).IsEqualTo("Operation");
            Check.That(activities[i].Tags).HasSize(1);
            Check.That(activities[i].Tags[0].Key).IsEqualTo("#");
            Check.That(activities[i].Tags[0].Value).IsEqualTo(i);
        }

        Check.That(File.Exists(_MakePath(1))).IsTrue();
        Check.That(File.Exists(_MakePath(2))).IsTrue();
        Check.That(File.Exists(_MakePath(3))).IsFalse();
    }

    [Test]
    public async Task UseCase_Many_Activity_Single_Source_Many_Export()
    {
        // arrange
        var options = new AdtxTraceExporterOptions(mDirectory) {
            WriterOptions = new TraceWriterOptions {
                MaxDiskSpaceUsedInMegaBytes = 100,
            },
        };

        // act writing
        using (var provider = Sdk.CreateTracerProviderBuilder()
            .AddSource("*")
            .AddAdtxTraceExporter(options, out var ctx)
            .Build())
        {
            Check.That(ctx).IsNotNull();
            Check.That(ctx.Exporter).IsNotNull();

            for (var i = 0; i < 10000; i++)
            {
                using (var activitySource = new ActivitySource("Source"))
                {
                    using (var activity = activitySource.StartActivity("Operation"))
                    {
                        activity?.AddTag("#", i);
                        activity?.Dispose();
                    }
                }

                if (i % 1000 == 0)
                {
                    await ctx.Exporter.HotExportAsync(Path.Combine(mDirectory.FullName, "export.zip"));
                }
            }

            provider?.ForceFlush();
            provider?.Shutdown();
        }

        // act reading
        _CreateReader();

        var activities = mReader!.Read().ToArray();

        // assert
        Check.That(activities).HasSize(10000);

        for (var i = 0; i < activities.Length; i++)
        {
            Check.That(activities[i].Session.Uuid).IsEqualTo(options.WriterOptions.SessionUuid);
            Check.That(activities[i].Session.StartTime).IsEqualTo(options.WriterOptions.SessionStartTime);
            Check.That(activities[i].Source.Name).IsEqualTo("Source");
            Check.That(activities[i].Source.Version).IsNull();
            Check.That(activities[i].OperationName).IsEqualTo("Operation");
            Check.That(activities[i].Tags).HasSize(1);
            Check.That(activities[i].Tags[0].Key).IsEqualTo("#");
            Check.That(activities[i].Tags[0].Value).IsEqualTo(i);
        }
    }
}
