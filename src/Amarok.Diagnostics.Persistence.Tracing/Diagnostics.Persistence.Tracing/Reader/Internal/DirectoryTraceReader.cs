// Copyright (c) 2024, Olaf Kober <olaf.kober@outlook.com>

namespace Amarok.Diagnostics.Persistence.Tracing.Reader.Internal;


internal sealed class DirectoryTraceReader : ITraceReader
{
    private readonly DirectoryInfo mDirectory;
    private readonly ITraceReaderHooks? mHooks;

    private ITraceReader? mFileReader;


    public DirectoryTraceReader(String directory, ITraceReaderHooks? hooks = null)
    {
        mDirectory = new DirectoryInfo(directory);
        mHooks     = hooks;
    }


    public void Dispose()
    {
        mFileReader?.Dispose();
    }

    public IEnumerable<ActivityInfo> Read()
    {
        foreach (var file in mDirectory.GetTraceFiles())
        {
            var path = file.FileInfo.FullName;

            mHooks?.OnBeginReadFile(path);

            using (mFileReader = TraceReader.OpenFile(path, mHooks))
            {
                foreach (var activity in mFileReader.Read())
                {
                    yield return activity;
                }
            }

            mHooks?.OnEndReadFile(path);
        }
    }
}
