using System;

using SimTelemetry.Domain.Memory;

namespace SimShift.Data.Memory
{
    public interface IMemoryObject : IDataField, ICloneable
    {
        int Address { get; }

        MemoryAddress AddressType { get; }

        bool IsConstant { get; }

        bool IsDynamic { get; }

        bool IsStatic { get; }

        MemoryProvider Memory { get; }

        string Name { get; }

        int Offset { get; }

        MemoryPool Pool { get; }

        int Size { get; }

        Type ValueType { get; }

        bool HasChanged();

        void MarkDirty();

        object Read();

        T ReadAs<T>();

        void Refresh();

        void SetPool(MemoryPool pool);

        void SetProvider(MemoryProvider provider);
    }
}