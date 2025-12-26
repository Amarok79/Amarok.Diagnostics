// Copyright (c) 2024, Olaf Kober <olaf.kober@outlook.com>

using System.CommandLine;
using System.IO.Compression;
using Amarok.Diagnostics.Persistence.Tracing.Reader;
using Spectre.Console;


namespace Amarok.Diagnostics.Persistence.Tracing;


internal sealed class ConvertCommand : Command
{
    internal enum OutputFormat
    {
        PerfettoJson = 1,
        PerfettoProtobuf = 2,
    }

    public ConvertCommand()
        : base("convert", "Converts traces log files (.adtx) to another format.")
    {
        var inArgument = new Argument<FileSystemInfo>("in") {
            Description = "Path to the input traces log file (.adtx), to a Zip archive containing traces log files, " +
                          "or to a directory containing traces log files.",
            Arity = ArgumentArity.ExactlyOne,
        };

        var outArgument = new Argument<DirectoryInfo?>("out") {
            Description =
                "Path to the output directory receiving the converted traces files. Defaults to the input directory.",
            Arity = ArgumentArity.ZeroOrOne,
        };

        var formatOption = new Option<OutputFormat>("--format") {
            DefaultValueFactory = _ => OutputFormat.PerfettoProtobuf,
            Description         = "The output format. Defaults to 'PerfettoProtobuf'.",
            Arity               = ArgumentArity.ZeroOrOne,
        };

        var includeIdsOptions = new Option<Boolean>("--include-ids") {
            DefaultValueFactory = _ => false,
            Description         = "If specified, includes trace and span ids. Valid only for Perfetto Protobuf format.",
            Arity               = ArgumentArity.ZeroOrOne,
        };

        Arguments.Add(inArgument);
        Arguments.Add(outArgument);
        Options.Add(formatOption);
        Options.Add(includeIdsOptions);

        SetAction(
            x => _Execute(
                x.GetRequiredValue(inArgument),
                x.GetValue(outArgument),
                x.GetRequiredValue(formatOption),
                x.GetRequiredValue(includeIdsOptions)
            )
        );
    }


    private static void _Execute(FileSystemInfo inPath, DirectoryInfo? outDir, OutputFormat format, Boolean includeIds)
    {
        if (File.Exists(inPath.FullName))
        {
            if (_IsZipArchive(inPath.FullName))
            {
                AnsiConsole.MarkupLine($"Converting archive [aqua]{inPath}[/]...");

                _ExecuteCore(
                    TraceReader.OpenZipArchive(inPath.FullName),
                    outDir?.FullName ?? Path.GetDirectoryName(inPath.FullName)!,
                    format,
                    includeIds
                );
            }
            else
            {
                AnsiConsole.MarkupLine($"Converting file [aqua]{inPath}[/]...");

                _ExecuteCore(
                    TraceReader.OpenFile(inPath.FullName),
                    outDir?.FullName ?? Path.GetDirectoryName(inPath.FullName)!,
                    format,
                    includeIds
                );
            }
        }
        else if (Directory.Exists(inPath.FullName))
        {
            AnsiConsole.MarkupLine($"Converting directory [aqua]{inPath}[/]...");

            _ExecuteCore(
                TraceReader.OpenFolder(inPath.FullName),
                outDir?.FullName ?? inPath.FullName,
                format,
                includeIds
            );
        }
        else
        {
            AnsiConsole.MarkupLine("[red]ERROR:[/] The given input file or directory does not exist.");
        }
    }


    private static Boolean _IsZipArchive(String filePath)
    {
        try
        {
            ZipFile.OpenRead(filePath).Dispose();

            return true;
        }
        catch
        {
            return false;
        }
    }

    private static void _ExecuteCore(ITraceReader reader, String outDir, OutputFormat format, Boolean includeIds)
    {
        if (format == OutputFormat.PerfettoJson)
        {
            new PerfettoJsonConverter().Run(reader, outDir);
        }
        else if (format == OutputFormat.PerfettoProtobuf)
        {
            new PerfettoProtobufConverter().Run(reader, outDir, includeIds);
        }
    }
}
