// Copyright (c) 2024, Olaf Kober <olaf.kober@outlook.com>

using System.Diagnostics;
using Amarok.Diagnostics.Persistence.Tracing.Protos;


namespace Amarok.Diagnostics.Persistence.Tracing.Writer.Internal;


internal sealed class ActivitySerializer
{
    private readonly TimeSpan mRedefineReferenceTimeInterval;
    private readonly ObjectsPool mObjectsPool;

    private readonly ReferenceTimeSerializer mReferenceTimeSerializer;
    private readonly SourceSerializer mSourceSerializer;
    private readonly TraceIdSerializer mTraceIdSerializer;
    private readonly ParentSpanIdSerializer mParentSpanIdSerializer;
    private readonly OperationSerializer mOperationSerializer;
    private readonly TagsSerializer mTagsSerializer;


    public ActivitySerializer(
        Int32 maxNumberOfItems,
        Int32 maxNumberOfIds,
        Int32 maxStringLength,
        Int32 maxBytesLength,
        TimeSpan redefineReferenceTimeInterval,
        ObjectsPool objectsPool
    )
    {
        mObjectsPool = objectsPool;

        mReferenceTimeSerializer = new ReferenceTimeSerializer(mObjectsPool);
        mSourceSerializer        = new SourceSerializer(maxNumberOfItems, mObjectsPool);
        mTraceIdSerializer       = new TraceIdSerializer(maxNumberOfIds, mObjectsPool);
        mParentSpanIdSerializer  = new ParentSpanIdSerializer(maxNumberOfIds, mObjectsPool);
        mOperationSerializer     = new OperationSerializer(maxNumberOfItems, mObjectsPool);

        mTagsSerializer = new TagsSerializer(maxNumberOfItems, maxStringLength, maxBytesLength, mObjectsPool);

        mRedefineReferenceTimeInterval = redefineReferenceTimeInterval;
    }


    public void Reset()
    {
        mReferenceTimeSerializer.Reset();
        mSourceSerializer.Reset();
        mTraceIdSerializer.Reset();
        mParentSpanIdSerializer.Reset();
        mOperationSerializer.Reset();
        mTagsSerializer.Reset();
    }

    public void Serialize(Activity activity, TraceRecords records)
    {
        // define reference point in time
        if (!mReferenceTimeSerializer.IsDefined)
        {
            mReferenceTimeSerializer.SetReferencePointInTime(DateTimeOffset.Now, records);
        }

        var timeDelta = mReferenceTimeSerializer.GetRelativeTimeDelta(activity.StartTimeUtc);

        // re-define reference point in time
        if (timeDelta > mRedefineReferenceTimeInterval)
        {
            mReferenceTimeSerializer.SetReferencePointInTime(DateTimeOffset.Now, records);
            timeDelta = mReferenceTimeSerializer.GetRelativeTimeDelta(activity.StartTimeUtc);
        }

        // intern activity source
        var activitySourceId = mSourceSerializer.Serialize(activity.Source, records);

        // intern operation
        var operationId = mOperationSerializer.Serialize(activity.OperationName, records);

        // intern trace id, parent span id
        var traceId      = mTraceIdSerializer.Serialize(activity.TraceId, records);
        var parentSpanId = mParentSpanIdSerializer.Serialize(activity.ParentSpanId, records);

        // define activity
        var record = mObjectsPool.GetRecord();
        var item   = mObjectsPool.GetActivity();

        item.SourceId               = activitySourceId;
        item.OperationId            = operationId;
        item.TraceId                = traceId;
        item.SpanId                 = activity.SpanId.ToHexString();
        item.ParentSpanId           = parentSpanId;
        item.StartTimeRelativeTicks = timeDelta.Ticks;
        item.DurationTicks          = activity.Duration.Ticks;

        mTagsSerializer.Serialize(activity, item.Tags, records);

        record.Activity = item;

        records.Items.Add(record);
    }
}
