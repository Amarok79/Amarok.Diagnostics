// Copyright (c) 2024, Olaf Kober <olaf.kober@outlook.com>

namespace Amarok.Diagnostics.Persistence.Tracing.Reader.Internal;


internal sealed class ReferenceTimeMap
{
    private DateTimeOffset? mPointInTime;


    public void Reset()
    {
        mPointInTime = null;
    }


    public void Define(DateTimeOffset absoluteTime)
    {
        mPointInTime = absoluteTime;
    }


    public DateTimeOffset GetAbsolutePointInTime(TimeSpan timeDelta)
    {
        if (mPointInTime != null)
        {
            return mPointInTime.Value + timeDelta;
        }

        throw _MakeNotDefinedException();
    }

    private static Exception _MakeNotDefinedException()
    {
        return new FormatException("Reference point in time not defined.");
    }
}
