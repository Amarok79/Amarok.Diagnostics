// Copyright (c) 2023, Olaf Kober <olaf.kober@outlook.com>

using Amarok.Diagnostics.Persistence.Tracing.Protos;


namespace Amarok.Diagnostics.Persistence.Tracing.Writer.Internal;


internal sealed class OperationSerializer : SerializerBase<String>
{
    public OperationSerializer(Int32 maxNumberOfItems, ObjectsPool objectsPool)
        : base(maxNumberOfItems, objectsPool)
    {
    }


    protected override void AppendDefineRecord(String value, Int32 id, TraceRecord record)
    {
        var define = ObjectsPool.GetDefineOperation();

        define.Id = id;
        define.Name = value;

        record.DefineOperation = define;
    }

    protected override void AppendResetRecord(TraceRecord record)
    {
        record.ResetOperations = new TraceResetOperations();
    }
}
