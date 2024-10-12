// Copyright (c) 2024, Olaf Kober <olaf.kober@outlook.com>

// ReSharper disable InconsistentNaming

using Amarok.Diagnostics.Persistence.Protos;
using Amarok.Diagnostics.Persistence.Tracing.Protos;
using Microsoft.Extensions.ObjectPool;


namespace Amarok.Diagnostics.Persistence.Tracing.Writer.Internal;


internal abstract class ObjectsPool
{
    private static readonly ObjectsPool sNonPoolingInstance = new NonPoolingObjectsPool();


    public static ObjectsPool Create(Boolean usePooling)
    {
        return usePooling ? new PoolingObjectsPool() : sNonPoolingInstance;
    }


    public abstract TraceRecords GetRecords();

    public abstract TraceRecord GetRecord();

    public abstract TraceDefineSource GetDefineSource();

    public abstract TraceDefineOperation GetDefineOperation();

    public abstract TraceDefineTraceId GetDefineTraceId();

    public abstract TraceDefineParentSpanId GetDefineParentSpanId();

    public abstract TraceActivityTag GetActivityTag();

    public abstract TraceDefineTag GetDefineTag();

    public abstract TraceActivity GetActivity();

    public abstract void ReturnRecords(TraceRecords records);


    private sealed class NonPoolingObjectsPool : ObjectsPool
    {
        public override TraceRecords GetRecords()
        {
            return new TraceRecords();
        }

        public override TraceRecord GetRecord()
        {
            return new TraceRecord();
        }

        public override TraceDefineSource GetDefineSource()
        {
            return new TraceDefineSource();
        }

        public override TraceDefineOperation GetDefineOperation()
        {
            return new TraceDefineOperation();
        }

        public override TraceDefineTraceId GetDefineTraceId()
        {
            return new TraceDefineTraceId();
        }

        public override TraceDefineParentSpanId GetDefineParentSpanId()
        {
            return new TraceDefineParentSpanId();
        }

        public override TraceActivityTag GetActivityTag()
        {
            return new TraceActivityTag {
                Value = new AnyValue(),
            };
        }

        public override TraceDefineTag GetDefineTag()
        {
            return new TraceDefineTag();
        }

        public override TraceActivity GetActivity()
        {
            return new TraceActivity();
        }

        public override void ReturnRecords(TraceRecords records)
        {
        }
    }

    private sealed class PoolingObjectsPool : ObjectsPool
    {
        private readonly ObjectPool<TraceRecords> mRecordsPool;
        private readonly ObjectPool<TraceRecord> mRecordPool;
        private readonly ObjectPool<TraceDefineSource> mDefineSourcePool;
        private readonly ObjectPool<TraceDefineOperation> mDefineOperationPool;
        private readonly ObjectPool<TraceDefineTraceId> mDefineTraceIdPool;
        private readonly ObjectPool<TraceDefineParentSpanId> mDefineParentSpanIdPool;
        private readonly ObjectPool<TraceActivityTag> mActivityTagPool;
        private readonly ObjectPool<TraceDefineTag> mDefineTagPool;
        private readonly ObjectPool<TraceActivity> mActivityPool;


        public PoolingObjectsPool()
        {
            mRecordsPool            = ObjectPool.Create(new RecordsPooledObjectPolicy());
            mRecordPool             = ObjectPool.Create(new RecordPooledObjectPolicy());
            mDefineSourcePool       = ObjectPool.Create(new DefineSourcePooledObjectPolicy());
            mDefineOperationPool    = ObjectPool.Create(new DefineOperationPooledObjectPolicy());
            mDefineTraceIdPool      = ObjectPool.Create(new DefineTraceIdPooledObjectPolicy());
            mDefineParentSpanIdPool = ObjectPool.Create(new DefineParentSpanIdPooledObjectPolicy());
            mActivityTagPool        = ObjectPool.Create(new ActivityTagPooledObjectPolicy());
            mDefineTagPool          = ObjectPool.Create(new DefineTagPooledObjectPolicy());
            mActivityPool           = ObjectPool.Create(new ActivityPooledObjectPolicy());
        }


        public override TraceRecords GetRecords()
        {
            return mRecordsPool.Get();
        }

        public override TraceRecord GetRecord()
        {
            return mRecordPool.Get();
        }

        public override TraceDefineSource GetDefineSource()
        {
            return mDefineSourcePool.Get();
        }

        public override TraceDefineOperation GetDefineOperation()
        {
            return mDefineOperationPool.Get();
        }

        public override TraceDefineTraceId GetDefineTraceId()
        {
            return mDefineTraceIdPool.Get();
        }

        public override TraceDefineParentSpanId GetDefineParentSpanId()
        {
            return mDefineParentSpanIdPool.Get();
        }

        public override TraceActivityTag GetActivityTag()
        {
            return mActivityTagPool.Get();
        }

        public override TraceDefineTag GetDefineTag()
        {
            return mDefineTagPool.Get();
        }

        public override TraceActivity GetActivity()
        {
            return mActivityPool.Get();
        }

        public override void ReturnRecords(TraceRecords records)
        {
            // ReSharper disable once ForCanBeConvertedToForeach
            for (var r = 0; r < records.Items.Count; r++)
            {
                var record = records.Items[r];

                if (record.Activity != null)
                {
                    // ReSharper disable once ForCanBeConvertedToForeach
                    for (var t = 0; t < record.Activity.Tags.Count; t++)
                    {
                        mActivityTagPool.Return(record.Activity.Tags[t]);
                    }

                    mActivityPool.Return(record.Activity);
                }
                else if (record.DefineSource != null)
                    mDefineSourcePool.Return(record.DefineSource);
                else if (record.DefineOperation != null)
                    mDefineOperationPool.Return(record.DefineOperation);
                else if (record.DefineTraceId != null)
                    mDefineTraceIdPool.Return(record.DefineTraceId);
                else if (record.DefineParentSpanId != null)
                    mDefineParentSpanIdPool.Return(record.DefineParentSpanId);
                else if (record.DefineTag != null)
                    mDefineTagPool.Return(record.DefineTag);

                mRecordPool.Return(record);
            }

            mRecordsPool.Return(records);
        }


        private sealed class RecordsPooledObjectPolicy : PooledObjectPolicy<TraceRecords>
        {
            public override TraceRecords Create()
            {
                return new TraceRecords();
            }

            public override Boolean Return(TraceRecords obj)
            {
                obj.Items.Clear();

                return true;
            }
        }

        private sealed class RecordPooledObjectPolicy : PooledObjectPolicy<TraceRecord>
        {
            public override TraceRecord Create()
            {
                return new TraceRecord();
            }

            public override Boolean Return(TraceRecord obj)
            {
                obj.ClearData();

                return true;
            }
        }

        private sealed class DefineSourcePooledObjectPolicy : PooledObjectPolicy<TraceDefineSource>
        {
            public override TraceDefineSource Create()
            {
                return new TraceDefineSource();
            }

            public override Boolean Return(TraceDefineSource obj)
            {
                return true;
            }
        }

        private sealed class DefineOperationPooledObjectPolicy : PooledObjectPolicy<TraceDefineOperation>
        {
            public override TraceDefineOperation Create()
            {
                return new TraceDefineOperation();
            }

            public override Boolean Return(TraceDefineOperation obj)
            {
                return true;
            }
        }

        private sealed class DefineTraceIdPooledObjectPolicy : PooledObjectPolicy<TraceDefineTraceId>
        {
            public override TraceDefineTraceId Create()
            {
                return new TraceDefineTraceId();
            }

            public override Boolean Return(TraceDefineTraceId obj)
            {
                return true;
            }
        }

        private sealed class DefineParentSpanIdPooledObjectPolicy : PooledObjectPolicy<TraceDefineParentSpanId>
        {
            public override TraceDefineParentSpanId Create()
            {
                return new TraceDefineParentSpanId();
            }

            public override Boolean Return(TraceDefineParentSpanId obj)
            {
                return true;
            }
        }

        private sealed class ActivityTagPooledObjectPolicy : PooledObjectPolicy<TraceActivityTag>
        {
            public override TraceActivityTag Create()
            {
                return new TraceActivityTag {
                    Value = new AnyValue(),
                };
            }

            public override Boolean Return(TraceActivityTag obj)
            {
                obj.Value.ClearValues();

                return true;
            }
        }

        private sealed class DefineTagPooledObjectPolicy : PooledObjectPolicy<TraceDefineTag>
        {
            public override TraceDefineTag Create()
            {
                return new TraceDefineTag();
            }

            public override Boolean Return(TraceDefineTag obj)
            {
                return true;
            }
        }

        private sealed class ActivityPooledObjectPolicy : PooledObjectPolicy<TraceActivity>
        {
            public override TraceActivity Create()
            {
                return new TraceActivity();
            }

            public override Boolean Return(TraceActivity obj)
            {
                obj.Tags.Clear();

                return true;
            }
        }
    }
}
