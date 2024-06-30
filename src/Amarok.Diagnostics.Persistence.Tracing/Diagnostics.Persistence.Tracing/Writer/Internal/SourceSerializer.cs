// Copyright (c) 2024, Olaf Kober <olaf.kober@outlook.com>

using System.Diagnostics;
using Amarok.Diagnostics.Persistence.Tracing.Protos;


namespace Amarok.Diagnostics.Persistence.Tracing.Writer.Internal;


internal sealed class SourceSerializer : SerializerBase<ActivitySource>
{
    public SourceSerializer(Int32 maxNumberOfItems, ObjectsPool objectsPool)
        : base(maxNumberOfItems, objectsPool)
    {
    }


    protected override void AppendDefineRecord(ActivitySource value, Int32 id, TraceRecord record)
    {
        var define = ObjectsPool.GetDefineSource();

        define.Id      = id;
        define.Name    = value.Name;
        define.Version = value.Version ?? String.Empty;

        record.DefineSource = define;
    }

    protected override void AppendResetRecord(TraceRecord record)
    {
        record.ResetSources = new TraceResetSources();
    }
}
