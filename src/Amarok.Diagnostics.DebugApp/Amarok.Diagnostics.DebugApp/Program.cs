// Copyright (c) 2023, Olaf Kober <olaf.kober@outlook.com>

using System.Diagnostics;
using Amarok.Diagnostics.Persistence.OpenTelementry.Exporter;
using Amarok.Diagnostics.Persistence.Tracing.Protos;
using Amarok.Diagnostics.Persistence.Tracing.Reader;
using Amarok.Diagnostics.Persistence.Tracing.Writer;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Trace;


namespace Amarok.Amarok.Diagnostics.DebugApp;


internal class Program
{
    private const String OutputDir = @"..\..\..\..\..\bin\test\traces";


    public static void Main()
    {
        if (Directory.Exists(OutputDir))
        {
            Directory.Delete(OutputDir, true);
        }

        _Writing();
        _Reading();
    }


    private static void _Writing()
    {
        var logger = LoggerFactory.Create(
                builder => builder.AddSystemdConsole(x => x.TimestampFormat = "|HH:mm:ss.fff|")
                    .SetMinimumLevel(LogLevel.Trace)
            )
            .CreateLogger("TEST");


        Console.WriteLine("writing...");
        Console.WriteLine();

        var options = new AdtxTraceExporterOptions(OutputDir) {
            WriterOptions = new TraceWriterOptions {
                MaxDiskSpaceUsedInMegaBytes = 100,
                Logger = logger,
            },
        };

        using var provider = Sdk.CreateTracerProviderBuilder()
            .AddSource("*")
            .AddAdtxTraceExporter(options, out var context)
            .Build();

        Thread.Sleep(3000);

        var sw = Stopwatch.StartNew();

        var source1 = new ActivitySource("Source-1");
        var source2 = new ActivitySource("Foo/Source-2");
        var source3 = new ActivitySource("Foo/Source-3");

        for (var i = 0; i < 1000000; i++)
        {
            Activity.Current = null;

            using var activity1 = source1.StartActivity("Method()");
            using var activity2 = source2.StartActivity("Foo()");
            using var activity3 = source3.StartActivity("Bar()");

            activity1?.SetTag("aaa", i);

            activity2?.SetTag("aaa", i);
            activity2?.Dispose();

            Thread.Sleep(0);

            activity1?.Dispose();

            activity3?.SetTag("aaa", i);

            Thread.Sleep(0);

            activity3?.Dispose();

            if (i == 500000)
            {
                context.Exporter.HotExportAsync(@"..\..\..\..\..\bin\test\export.zip");
            }
        }

        provider?.Dispose();

        Console.WriteLine();
        Console.WriteLine($"writing done ({sw.ElapsedMilliseconds} ms)");

        Console.WriteLine("Press any key to continue");
        Console.ReadLine();
    }

    private static void _Reading()
    {
        Console.WriteLine();
        Console.WriteLine("reading...");

        var sw = Stopwatch.StartNew();

        var i = 0;

        //var hooks = new MyHooks();
        var hooks = (ITraceReaderHooks?)null;

        using (var reader = TraceReader.OpenFolder(OutputDir, hooks))
        {
            foreach (var info in reader.Read())
            {
                Trace.Assert((Int32?)info.Tags[0].Value == i / 3);

                i++;
            }
        }

        Console.WriteLine();
        Console.WriteLine($"reading done within {sw.ElapsedMilliseconds} ms");
        Console.WriteLine(i);

        Console.WriteLine("Press any key to continue");
        Console.ReadLine();
    }


    internal class MyHooks : ITraceReaderHooks
    {
        public void OnBeginReadFile(
            String filePath
        )
        {
        }

        public void OnReadFileHeader(
            Int32 version,
            Boolean isCompressed,
            Boolean isFinished,
            SessionInfo session
        )
        {
        }

        public void OnBeginReadFrame(
            Byte[] buffer,
            Int32 frameLen
        )
        {
            Console.WriteLine($"FRAME: {frameLen}");
        }

        public void OnReadRecord(
            TraceRecord record
        )
        {
            if (record.DataCase != TraceRecord.DataOneofCase.Activity)
            {
                Console.WriteLine($"  RECORD: {record.DataCase}");
            }
        }

        public void OnReadActivity(
            ActivityInfo activity
        )
        {
            Console.WriteLine("  ACTIVITY");
        }

        public void OnEndReadFrame()
        {
            Console.WriteLine();
        }

        public void OnEndReadFile(
            String filePath
        )
        {
        }
    }
}
