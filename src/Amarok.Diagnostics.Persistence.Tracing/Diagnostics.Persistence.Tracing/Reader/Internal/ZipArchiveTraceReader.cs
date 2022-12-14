// Copyright (c) 2023, Olaf Kober <olaf.kober@outlook.com>

using System.IO.Compression;


namespace Amarok.Diagnostics.Persistence.Tracing.Reader.Internal;


internal sealed class ZipArchiveTraceReader : ITraceReader
{
    private readonly String mZipFile;
    private readonly ITraceReaderHooks? mHooks;

    private ITraceReader? mFileReader;


    public ZipArchiveTraceReader(String zipFile, ITraceReaderHooks? hooks = null)
    {
        mZipFile = zipFile;
        mHooks = hooks;
    }


    public void Dispose()
    {
        mFileReader?.Dispose();
    }

    public IEnumerable<ActivityInfo> Read()
    {
        using var archive = ZipFile.OpenRead(mZipFile);

        foreach (var entry in archive.GetTraceFiles())
        {
            var path = entry.Entry.FullName;

            mHooks?.OnBeginReadFile(path);

            using (var stream = entry.Entry.Open())
            {
                using (mFileReader = TraceReader.OpenStream(stream, mHooks))
                {
                    foreach (var activity in mFileReader.Read())
                    {
                        yield return activity;
                    }
                }
            }

            mHooks?.OnEndReadFile(path);
        }
    }
}
