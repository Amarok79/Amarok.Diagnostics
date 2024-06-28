// Copyright (c) 2024, Olaf Kober <olaf.kober@outlook.com>

namespace Amarok.Diagnostics.Persistence.Tracing.Reader.Internal;


internal sealed class ActivityParentSpanIdMap : InterningMapBase<String>
{
    public ActivityParentSpanIdMap(Int32 capacity = 4096)
        : base(capacity)
    {
    }
}
