// Copyright (c) 2024, Olaf Kober <olaf.kober@outlook.com>

using System.Globalization;


namespace Amarok.Diagnostics.Persistence.Tracing;


/// <summary>
///     Provides extension methods on <see cref="DirectoryInfo"/>.
/// </summary>
public static class DirectoryInfoExtensions
{
    /// <summary>
    ///     Gets an ordered list of trace log files (.adtx) from the given directory.
    /// </summary>
    /// 
    /// <param name="directoryInfo">
    ///     The directory from which to return trace log files. If the directory doesn't exist, an empty list is
    ///     returned.
    /// </param>
    /// 
    /// <returns>
    ///     An ordered list of trace log files, where each list element consists of the file ordinal and file info
    ///     object. The returned list is ordered ascending by file ordinals. If the given directory doesn't exist or
    ///     doesn't contain any trace log file, then an empty list is returned.
    /// </returns>
    public static IList<(Int32 Ordinal, FileInfo FileInfo)> GetTraceFiles(this DirectoryInfo directoryInfo)
    {
        directoryInfo.Refresh();

        if (!directoryInfo.Exists)
            return Array.Empty<(Int32, FileInfo)>();


        return directoryInfo.GetFiles("*.adtx", SearchOption.TopDirectoryOnly)
            .Select(map)
            .Where(x => x.FileInfo != null)
            .OrderBy(x => x.Ordinal)
            .Select(x => (x.Ordinal, x.FileInfo!))
            .ToList();


        static (Int32 Ordinal, FileInfo? FileInfo) map(FileInfo fileInfo)
        {
            var fileName = Path.GetFileNameWithoutExtension(fileInfo.Name);
            var ordinal  = parseOrdinal(fileName);

            if (ordinal == null)
                return (-1, null);

            return (ordinal.Value, fileInfo);
        }

        static Int32? parseOrdinal(String fileName)
        {
            if (Int32.TryParse(fileName, NumberStyles.Integer, CultureInfo.InvariantCulture, out var ordinal))
                return ordinal;

            return null;
        }
    }
}
