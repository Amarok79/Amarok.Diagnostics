// Copyright (c) 2024, Olaf Kober <olaf.kober@outlook.com>

using System.Globalization;
using System.IO.Compression;


namespace Amarok.Diagnostics.Persistence.Tracing;


/// <summary>
///     Provides extension methods on <see cref="ZipArchive"/>.
/// </summary>
public static class ZipArchiveExtensions
{
    /// <summary>
    ///     Gets an ordered list of trace log files (.adtx) from the given Zip archive.
    /// </summary>
    /// 
    /// <param name="zipArchive">
    ///     The Zip archive from which to return trace log files.
    /// </param>
    /// 
    /// <returns>
    ///     An ordered list of trace log files, where each list element consists of the file ordinal and archive entry object.
    ///     The returned list is ordered ascending by file ordinals. If the given archive doesn't contain any trace log file,
    ///     then an empty list is returned.
    /// </returns>
    public static IList<(Int32 Ordinal, ZipArchiveEntry Entry)> GetTraceFiles(this ZipArchive zipArchive)
    {
        return zipArchive.Entries.Where(x => inRoot(x) && isAdtx(x))
            .Select(map)
            .Where(x => x.Entry != null)
            .OrderBy(x => x.Ordinal)
            .Select(x => (x.Ordinal, x.Entry!))
            .ToList();


        static Boolean inRoot(ZipArchiveEntry entry)
        {
            var dir = Path.GetDirectoryName(entry.FullName);

            return String.IsNullOrEmpty(dir);
        }

        static Boolean isAdtx(ZipArchiveEntry entry)
        {
            return entry.Name.EndsWith(".adtx", StringComparison.OrdinalIgnoreCase);
        }

        static (Int32 Ordinal, ZipArchiveEntry? Entry) map(ZipArchiveEntry entry)
        {
            var fileName = Path.GetFileNameWithoutExtension(entry.Name);
            var ordinal  = parseOrdinal(fileName);

            if (ordinal == null)
            {
                return (-1, null);
            }

            return (ordinal.Value, entry);
        }

        static Int32? parseOrdinal(String fileName)
        {
            if (Int32.TryParse(fileName, NumberStyles.Integer, CultureInfo.InvariantCulture, out var ordinal))
            {
                return ordinal;
            }

            return null;
        }
    }
}
