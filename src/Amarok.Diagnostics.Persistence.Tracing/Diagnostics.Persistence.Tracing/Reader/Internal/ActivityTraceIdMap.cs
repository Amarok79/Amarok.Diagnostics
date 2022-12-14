﻿// Copyright (c) 2023, Olaf Kober <olaf.kober@outlook.com>

namespace Amarok.Diagnostics.Persistence.Tracing.Reader.Internal;


internal sealed class ActivityTraceIdMap : InterningMapBase<String>
{
    public ActivityTraceIdMap(Int32 capacity = 4096)
        : base(capacity)
    {
    }
}
