// Copyright (c) 2024, Olaf Kober <olaf.kober@outlook.com>

using Amarok.Diagnostics.Persistence.Tracing.Protos;


namespace Amarok.Diagnostics.Persistence.Tracing.Writer.Internal;


internal abstract class SerializerBase<T>
    where T : notnull
{
    private const Int32 InitialId = 1;

    private readonly Int32 mMaxNumberOfItems;
    private readonly ObjectsPool mObjectsPool;

    private readonly Dictionary<T, Int32> mItems;
    private Int32 mNextId = InitialId;


    protected ObjectsPool ObjectsPool => mObjectsPool;


    protected SerializerBase(Int32 maxNumberOfItems, ObjectsPool objectsPool)
    {
        mItems = new Dictionary<T, Int32>(maxNumberOfItems);
        mMaxNumberOfItems = maxNumberOfItems;
        mObjectsPool = objectsPool;
    }


    public void Reset()
    {
        mItems.Clear();

        mNextId = InitialId;
    }

    public Int32 Serialize(T value, TraceRecords records)
    {
        if (mItems.TryGetValue(value, out var id))
        {
            return id;
        }

        return _SerializeSlow(value, records);
    }


    private Int32 _SerializeSlow(T value, TraceRecords records)
    {
        var id = _GetAndAdvanceId(records);

        mItems.Add(value, id);

        var record = mObjectsPool.GetRecord();

        AppendDefineRecord(value, id, record);

        records.Items.Add(record);

        return id;
    }

    private Int32 _GetAndAdvanceId(TraceRecords records)
    {
        if (mItems.Count >= mMaxNumberOfItems)
        {
            _ResetDueToOverrun(records);
        }

        return mNextId++;
    }

    private void _ResetDueToOverrun(TraceRecords records)
    {
        var record = mObjectsPool.GetRecord();

        AppendResetRecord(record);

        records.Items.Add(record);

        mItems.Clear();

        mNextId = InitialId;
    }


    protected abstract void AppendDefineRecord(T value, Int32 id, TraceRecord record);

    protected abstract void AppendResetRecord(TraceRecord record);
}
