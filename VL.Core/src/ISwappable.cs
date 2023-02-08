using System;

namespace VL.Core
{
    public interface ISwappableGenericType
    {
        object Swap(Type newType, Swapper swapObject);
    }

    public delegate object Swapper(object value, Type compiletimeType);
}
