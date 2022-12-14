// Copyright (c) 2023, Olaf Kober <olaf.kober@outlook.com>

using System.Diagnostics;
using Amarok.Diagnostics.Persistence.Tracing.Protos;
using Google.Protobuf.Collections;


namespace Amarok.Diagnostics.Persistence.Tracing.Writer.Internal;


internal sealed class TagsSerializer
{
    private const Int32 InitialId = 1;

    private readonly Int32 mMaxNumberOfItems;
    private readonly ObjectsPool mObjectsPool;

    private readonly AnyValueSerializer mAnyValueSerializer;
    private readonly Dictionary<String, Int32> mKeys = new(StringComparer.Ordinal);
    private Int32 mNextId = InitialId;


    public TagsSerializer(Int32 maxNumberOfItems, Int32 maxStringLength, Int32 maxBytesLength, ObjectsPool objectsPool)
    {
        mMaxNumberOfItems = maxNumberOfItems;
        mObjectsPool = objectsPool;

        mAnyValueSerializer = new AnyValueSerializer(maxStringLength, maxBytesLength);
    }


    public void Reset()
    {
        mKeys.Clear();

        mNextId = InitialId;
    }

    public void Serialize(Activity activity, RepeatedField<TraceActivityTag> tagsField, TraceRecords records)
    {
        foreach (var tag in activity.EnumerateTagObjects())
        {
            var keyId = _InternKey(tag.Key, records);

            var traceTag = mObjectsPool.GetActivityTag();

            traceTag.KeyId = keyId;

            mAnyValueSerializer.Serialize(traceTag.Value, tag.Value);

            tagsField.Add(traceTag);
        }
    }


    private Int32 _InternKey(String key, TraceRecords records)
    {
        if (mKeys.TryGetValue(key, out var id))
        {
            return id;
        }

        return _InternKeySlow(key, records);
    }

    private Int32 _InternKeySlow(String key, TraceRecords records)
    {
        var id = _GetAndAdvanceId(records);

        mKeys.Add(key, id);

        _AppendDefineRecord(key, id, records);

        return id;
    }

    private Int32 _GetAndAdvanceId(TraceRecords records)
    {
        if (mKeys.Count >= mMaxNumberOfItems)
        {
            _ResetDueToOverrun(records);
        }

        var id = mNextId;

        mNextId++;

        return id;
    }

    private void _ResetDueToOverrun(TraceRecords records)
    {
        _AppendResetRecord(records);

        mKeys.Clear();

        mNextId = InitialId;
    }

    private void _AppendDefineRecord(String key, Int32 id, TraceRecords records)
    {
        var record = mObjectsPool.GetRecord();
        var define = mObjectsPool.GetDefineTag();

        define.Id = id;
        define.Key = key;

        record.DefineTag = define;

        records.Items.Add(record);
    }

    private void _AppendResetRecord(TraceRecords records)
    {
        var record = mObjectsPool.GetRecord();

        record.ResetTags = new TraceResetTags();

        records.Items.Add(record);
    }
}
