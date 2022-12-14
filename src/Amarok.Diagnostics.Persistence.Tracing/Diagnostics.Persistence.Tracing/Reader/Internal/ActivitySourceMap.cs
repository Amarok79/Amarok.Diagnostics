// Copyright (c) 2023, Olaf Kober <olaf.kober@outlook.com>

namespace Amarok.Diagnostics.Persistence.Tracing.Reader.Internal;


internal sealed class ActivitySourceMap : InterningMapBase<ActivitySourceInfo>
{
    public ActivitySourceMap(Int32 capacity = 4096)
        : base(capacity)
    {
    }
}
