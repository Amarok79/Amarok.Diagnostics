// Copyright (c) 2023, Olaf Kober <olaf.kober@outlook.com>

namespace Amarok.Diagnostics.Persistence.Tracing.Reader;


/// <summary>
///     Provides information about an activity source.
/// </summary>
/// 
/// <remarks>
///     Every activity is associated with a single activity source, which is used primarily for
///     categorizing all the activities of an instrumented application based on their origin.
/// </remarks>
/// 
/// <param name="Name">
///     The name of the activity source.
/// </param>
/// <param name="Version">
///     The optional version of the activity source. Can be null.
/// </param>
public sealed record ActivitySourceInfo(String Name, String? Version = null)
{
    /// <inheritdoc/>
    public override String ToString()
    {
        return Version != null ? $"{Name} {Version}" : Name;
    }
}
