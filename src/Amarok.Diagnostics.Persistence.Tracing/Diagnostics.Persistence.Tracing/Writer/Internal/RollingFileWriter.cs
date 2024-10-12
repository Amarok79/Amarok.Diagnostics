// Copyright (c) 2024, Olaf Kober <olaf.kober@outlook.com>

using System.Buffers;
using System.Buffers.Binary;
using System.Diagnostics;
using System.IO.Compression;
using Microsoft.Extensions.Logging;


namespace Amarok.Diagnostics.Persistence.Tracing.Writer.Internal;


internal sealed class RollingFileWriter : IDisposable
{
    private const Byte FileVersion = 0x01;
    private const Int32 HeaderLength = 34;

    private readonly DirectoryInfo mDirectory;
    private readonly Int64 mMaxDiskSpaceUsed;
    private readonly Boolean mUseCompression;
    private readonly ILogger mLogger;

    private Guid mSessionUuid;
    private DateTimeOffset mSessionStartTime;
    private FileStream? mActiveStream;


    public RollingFileWriter(DirectoryInfo directory, Int64 maxDiskSpaceUsed, Boolean useCompression, ILogger logger)
    {
        mDirectory        = directory;
        mMaxDiskSpaceUsed = maxDiskSpaceUsed;
        mUseCompression   = useCompression;
        mLogger           = logger;

        mSessionUuid      = Guid.Empty;
        mSessionStartTime = new DateTimeOffset(0, TimeSpan.Zero);
    }


    public void SetSession(Guid sessionUuid, DateTimeOffset sessionStartTime)
    {
        mSessionUuid      = sessionUuid;
        mSessionStartTime = sessionStartTime;
    }

    public void StartNewLogFile()
    {
        try
        {
            var sw = Stopwatch.StartNew();

            mLogger.LogDebug("RollingFileWriter: Starting new log file...");

            _CreateRootDir();

            var files = mDirectory.GetTraceFiles();

            var deleted = _PurgeFiles(files);

            mLogger.LogTrace(
                "RollingFileWriter: Purged {Deleted} of {Count} files ({Elapsed} ms)",
                deleted,
                files.Count,
                sw.ElapsedMilliseconds
            );

            mActiveStream = _OpenNextFile(files);

            _WriteFileHeader(mActiveStream, FileVersion, false, false, mSessionUuid, mSessionStartTime);

            mLogger.LogDebug(
                "RollingFileWriter: Started new log file '{FileName}' ({Elapsed} ms)",
                mActiveStream.Name,
                sw.ElapsedMilliseconds
            );
        }
        catch (Exception exception)
        {
            mLogger.LogError(exception, "RollingFileWriter: Exception during StartNewLogFile()");

            throw;
        }
    }

    public void CompleteActiveLogFile()
    {
        try
        {
            if (mActiveStream == null)
                return;

            var sw       = Stopwatch.StartNew();
            var fileName = mActiveStream.Name;

            mLogger.LogDebug("RollingFileWriter: Completing active log file '{FileName}'...", fileName);

            if (mActiveStream.Length > HeaderLength)
            {
                mActiveStream.Seek(7, SeekOrigin.Begin);
                mActiveStream.WriteByte(0x0F);

                mActiveStream.Flush();
                mActiveStream.Close();

                mLogger.LogTrace("RollingFileWriter: Closed active log file ({Elapsed} ms)", sw.ElapsedMilliseconds);

                if (mUseCompression)
                {
                    _CompressFile(mActiveStream.Name);

                    mLogger.LogTrace(
                        "RollingFileWriter: Compressed active log file ({Elapsed} ms)",
                        sw.ElapsedMilliseconds
                    );
                }
            }
            else
            {
                mActiveStream.Close();

                File.Delete(mActiveStream.Name);

                mLogger.LogTrace(
                    "RollingFileWriter: Deleted empty active log file ({Elapsed} ms)",
                    sw.ElapsedMilliseconds
                );
            }

            mActiveStream = null;

            mLogger.LogDebug(
                "RollingFileWriter: Completed active log file '{FileName}' ({Elapsed} ms)",
                fileName,
                sw.ElapsedMilliseconds
            );
        }
        catch (Exception exception)
        {
            mLogger.LogError(exception, "RollingFileWriter: Exception during CompleteActiveLogFile()");

            throw;
        }
    }

    public void Write(ReadOnlySpan<Byte> buffer)
    {
        mActiveStream?.Write(buffer);
    }

    public void Flush()
    {
        var sw = Stopwatch.StartNew();

        mLogger.LogDebug("RollingFileWriter: Flushing active log file '{FileName}' to disk...", mActiveStream?.Name);

        mActiveStream?.Flush(true);

        mLogger.LogDebug("RollingFileWriter: Flushed active log file to disk ({Elapsed} ms)", sw.ElapsedMilliseconds);
    }

    public void Export(String archivePath)
    {
        var sw = Stopwatch.StartNew();

        var traceFiles = mDirectory.GetTraceFiles();

        mLogger.LogDebug(
            "RollingFileWriter: Exporting {Count} trace log files into '{ArchivePath}'...",
            traceFiles.Count,
            archivePath
        );

        using var targetStream = new FileStream(archivePath, FileMode.Create, FileAccess.Write, FileShare.None);

        using var archive = new ZipArchive(targetStream, ZipArchiveMode.Create);

        var buffer = new Byte[8192];

        foreach (var traceFile in traceFiles)
        {
            try
            {
                using var sourceStream = new FileStream(
                    traceFile.FileInfo.FullName,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.Read
                );

                var entry = archive.CreateEntry(traceFile.FileInfo.Name, CompressionLevel.Fastest);

                using var entryStream = entry.Open();

                while (true)
                {
                    var read = sourceStream.Read(buffer, 0, buffer.Length);

                    if (read == 0)
                        break;

                    entryStream.Write(buffer, 0, read);
                }
            }
            catch (Exception)
            {
                // ignore; continue with next log file
            }
        }

        mLogger.LogDebug(
            "RollingFileWriter: Exported {Count} log files into '{ArchivePath}' within {Elapsed} ms",
            traceFiles.Count,
            archivePath,
            sw.ElapsedMilliseconds
        );
    }

    public void Dispose()
    {
        CompleteActiveLogFile();
    }


    private void _CreateRootDir()
    {
        mDirectory.Refresh();

        if (!mDirectory.Exists)
            mDirectory.Create();
    }

    private void _CompressFile(String filePath)
    {
        var tmpFilePath = filePath + "-tmp";

        try
        {
            using (var target = new FileStream(tmpFilePath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                using (var source = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    _WriteFileHeader(target, FileVersion, true, true, mSessionUuid, mSessionStartTime);

                    source.Seek(HeaderLength, SeekOrigin.Begin);

                    using (var compressed = new DeflateStream(target, CompressionLevel.Fastest))
                    {
                        var bytes = ArrayPool<Byte>.Shared.Rent(8192);

                        while (true)
                        {
                            var read = source.Read(bytes, 0, bytes.Length);

                            if (read == 0)
                                break;

                            compressed.Write(bytes, 0, read);
                        }

                        ArrayPool<Byte>.Shared.Return(bytes);
                    }
                }
            }

            File.Move(tmpFilePath, filePath, true);
        }
        finally
        {
            if (File.Exists(tmpFilePath))
                File.Delete(tmpFilePath);
        }
    }

    private FileStream _OpenNextFile(IList<(Int32 Ordinal, FileInfo FileInfo)> files)
    {
        var ordinal = 1;

        if (files.Count > 0)
            ordinal = files[^1].Ordinal + 1;

        while (true)
        {
            var stream = _TryOpenFile(ordinal);

            if (stream != null)
                return stream;

            ordinal++;
        }
    }

    private FileStream? _TryOpenFile(Int32 ordinal)
    {
        var filePath = Path.Combine(mDirectory.FullName, $"{ordinal}.adtx");

        if (File.Exists(filePath))
            return null;

        return new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.Read);
    }

    private Int32 _PurgeFiles(ICollection<(Int32 Ordinal, FileInfo FileInfo)> files)
    {
        var totalSize = files.Sum(x => x.FileInfo.Length);

        if (totalSize < mMaxDiskSpaceUsed)
            return 0;

        var deleted = 0;

        foreach (var file in files.ToArray())
        {
            totalSize -= file.FileInfo.Length;

            file.FileInfo.Delete();

            files.Remove(file);

            deleted++;

            if (totalSize < mMaxDiskSpaceUsed)
                break;
        }

        return deleted;
    }

    private static void _WriteFileHeader(
        Stream stream,
        Byte version,
        Boolean compressed,
        Boolean finished,
        Guid sessionUuid,
        DateTimeOffset sessionStart
    )
    {
        _WriteFileSignature(stream);
        _WriteFileVersion(stream, version);
        _WriteFileFlags(stream, compressed, finished);
        _WriteFileSession(stream, sessionUuid, sessionStart);
    }

    private static void _WriteFileSignature(Stream stream)
    {
        // file-signature  =  %x61 , %x64 , %x74 , %x78 ;      // "adtx"

        ReadOnlySpan<Byte> bytes = stackalloc Byte[] {
            0x61, 0x64, 0x74, 0x78,
        };

        stream.Write(bytes);
    }

    private static void _WriteFileVersion(Stream stream, Byte version)
    {
        // file-version  =  %x00 , version ;

        ReadOnlySpan<Byte> bytes = stackalloc Byte[] {
            0x00, version,
        };

        stream.Write(bytes);
    }

    private static void _WriteFileFlags(Stream stream, Boolean compressed, Boolean finished)
    {
        // file-flags           =  %x00 , active | finished | compressed-finished ;
        // active               =  %x0A ;
        // finished             =  %x0F ;
        // compressed-finished  =  %xCF ;

        const Byte activeFlag     = 0x0A;
        const Byte finishedFlag   = 0x0F;
        const Byte compressedFlag = 0xC0;

        var flag = (finished ? finishedFlag : activeFlag) | (compressed ? compressedFlag : 0x00);

        ReadOnlySpan<Byte> bytes = stackalloc Byte[] {
            0x00, (Byte)flag,
        };

        stream.Write(bytes);
    }

    private static void _WriteFileSession(Stream stream, Guid sessionUuid, DateTimeOffset sessionStart)
    {
        // file-session    =  session-uuid , session-start ;
        // session-uuid    =  <Guid> ;
        // session-start   =  ticks , offset-minutes ;
        // ticks           =  <Int64> ;
        // offset-minutes  =  <Int16> ;

        Span<Byte> bytes = stackalloc Byte[16 + 8 + 2];

        sessionUuid.TryWriteBytes(bytes[..16]);

        BinaryPrimitives.TryWriteInt64LittleEndian(bytes[16..24], sessionStart.Ticks);
        BinaryPrimitives.TryWriteInt16LittleEndian(bytes[24..26], (Int16)sessionStart.Offset.TotalMinutes);

        stream.Write(bytes);
    }
}
