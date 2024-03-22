// Copyright (c) 2024, Olaf Kober <olaf.kober@outlook.com>

namespace Amarok.Diagnostics.Persistence.Tracing.Reader.Internal;


internal abstract class InterningMapBase<T>
    where T : class
{
    private T?[] mArray;


    protected InterningMapBase(
        Int32 capacity
    )
    {
        mArray = new T[capacity];
    }


    public void Reset()
    {
        Array.Clear(mArray, 0, mArray.Length);
    }


    public void Define(
        Int32 id,
        T item
    )
    {
        if (id >= mArray.Length)
        {
            _ResizeTo(id);
        }

        if (mArray[id] == null)
        {
            mArray[id] = item;
        }
        else
        {
            _ThrowAlreadyDefinedException(id);
        }
    }

    private void _ResizeTo(
        Int32 id
    )
    {
        var tmp = new T[id + 4096];
        Array.Copy(mArray, tmp, mArray.Length);
        mArray = tmp;
    }

    private static void _ThrowAlreadyDefinedException(
        Int32 id
    )
    {
        throw new FormatException($"{typeof(T).Name} with id '{id}' is already defined.");
    }


    public T Lookup(
        Int32 id
    )
    {
        if (id < mArray.Length)
        {
            var item = mArray[id];

            if (item != null)
            {
                return item;
            }
        }

        throw _MakeNotDefinedException(id);
    }

    private static Exception _MakeNotDefinedException(
        Int32 id
    )
    {
        return new FormatException($"{typeof(T).Name} with id '{id}' is not defined.");
    }
}
