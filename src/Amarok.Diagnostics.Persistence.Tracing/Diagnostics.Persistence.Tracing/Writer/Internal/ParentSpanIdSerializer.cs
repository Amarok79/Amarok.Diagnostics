// Copyright (c) 2024, Olaf Kober <olaf.kober@outlook.com>

using System.Diagnostics;
using Amarok.Diagnostics.Persistence.Tracing.Protos;


namespace Amarok.Diagnostics.Persistence.Tracing.Writer.Internal;


internal sealed class ParentSpanIdSerializer : SerializerBase<ActivitySpanId>
{
    public ParentSpanIdSerializer(
        Int32 maxNumberOfItems,
        ObjectsPool objectsPool
    )
        : base(maxNumberOfItems, objectsPool)
    {
    }


    protected override void AppendDefineRecord(
        ActivitySpanId value,
        Int32 id,
        TraceRecord record
    )
    {
        var define = ObjectsPool.GetDefineParentSpanId();

        define.Id = id;
        define.SpanId = value.ToHexString();

        record.DefineParentSpanId = define;
    }

    protected override void AppendResetRecord(
        TraceRecord record
    )
    {
        record.ResetParentSpanIds = new TraceResetParentSpanIds();
    }
}
