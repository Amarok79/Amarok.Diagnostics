// Copyright (c) 2024, Olaf Kober <olaf.kober@outlook.com>

using System.Diagnostics;
using Amarok.Diagnostics.Persistence.Tracing.Protos;


namespace Amarok.Diagnostics.Persistence.Tracing.Writer.Internal;


internal sealed class TraceIdSerializer : SerializerBase<ActivityTraceId>
{
    public TraceIdSerializer(Int32 maxNumberOfItems, ObjectsPool objectsPool)
        : base(maxNumberOfItems, objectsPool)
    {
    }


    protected override void AppendDefineRecord(ActivityTraceId value, Int32 id, TraceRecord record)
    {
        var define = ObjectsPool.GetDefineTraceId();

        define.Id = id;
        define.TraceId = value.ToHexString();

        record.DefineTraceId = define;
    }

    protected override void AppendResetRecord(TraceRecord record)
    {
        record.ResetTraceIds = new TraceResetTraceIds();
    }
}
