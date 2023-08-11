// Copyright (c) 2023, Olaf Kober <olaf.kober@outlook.com>

namespace Amarok.Diagnostics.Persistence.Tracing.Reader.Internal;


internal sealed class OperationNameMap : InterningMapBase<String>
{
    public OperationNameMap(
        Int32 capacity = 4096
    )
        : base(capacity)
    {
    }
}
