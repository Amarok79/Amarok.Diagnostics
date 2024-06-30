// Copyright (c) 2024, Olaf Kober <olaf.kober@outlook.com>

using Amarok.Diagnostics.Persistence.Tracing.Protos;


namespace Amarok.Diagnostics.Persistence.Tracing.Writer.Internal;


internal sealed class ReferenceTimeSerializer
{
    private readonly ObjectsPool mObjectsPool;

    private DateTimeOffset mPointInTime;
    private DateTime mPointInTimeUtc;


    public Boolean IsDefined { get; private set; }


    public ReferenceTimeSerializer(ObjectsPool objectsPool)
    {
        mObjectsPool = objectsPool;
    }


    public void Reset()
    {
        IsDefined = false;
    }

    public TimeSpan GetRelativeTimeDelta(DateTimeOffset now)
    {
        return now - mPointInTime;
    }

    public TimeSpan GetRelativeTimeDelta(DateTime utcNow)
    {
        return utcNow - mPointInTimeUtc;
    }

    public void SetReferencePointInTime(DateTimeOffset timestamp, TraceRecords records)
    {
        mPointInTime    = timestamp;
        mPointInTimeUtc = timestamp.UtcDateTime;

        IsDefined = true;


        var record = mObjectsPool.GetRecord();

        record.DefinePointInTime = new TraceDefinePointInTime {
            Ticks         = mPointInTime.LocalDateTime.Ticks,
            OffsetMinutes = (Int32)mPointInTime.Offset.TotalMinutes,
        };

        records.Items.Add(record);
    }
}
