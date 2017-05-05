using System;

using SimShift.Data.Memory;

namespace SimTelemetry.Domain.Memory
{
    public class MemoryFieldConstant<T> : IMemoryObject
    {
        private bool firstCall = true;

        public MemoryFieldConstant(string name, T staticValue)
        {
            Name = name;
            StaticValue = staticValue;
        }

        public int Address
        {
            get
            {
                return 0;
            }
        }

        public MemoryAddress AddressType
        {
            get
            {
                return MemoryAddress.Constant;
            }
        }

        public bool IsConstant
        {
            get
            {
                return true;
            }
        }

        public bool IsDynamic
        {
            get
            {
                return false;
            }
        }

        public bool IsStatic
        {
            get
            {
                return false;
            }
        }

        public MemoryProvider Memory { get; protected set; }

        public string Name { get; protected set; }

        public int Offset
        {
            get
            {
                return 0;
            }
        }

        public MemoryPool Pool { get; protected set; }

        public int Size
        {
            get
            {
                return 0;
            }
        }

        public T StaticValue { get; protected set; }

        public Type ValueType
        {
            get
            {
                return typeof(T);
            }
        }

        public object Clone()
        {
            var newObj = new MemoryFieldConstant<T>(Name, StaticValue);
            return newObj;
        }

        public bool HasChanged()
        {
            if (firstCall)
            {
                firstCall = false;
                return true;
            }
            return false;
        }

        public void MarkDirty()
        {
            firstCall = true;
        }

        public virtual object Read()
        {
            return StaticValue;
        }

        public TOut ReadAs<TOut>()
        {
            return MemoryDataConverter.Cast<T, TOut>(StaticValue);
        }

        public void Refresh()
        {
            // Done!
        }

        public void SetPool(MemoryPool pool)
        {
            Pool = pool; // don't care
        }

        public void SetProvider(MemoryProvider provider)
        {
            Memory = provider; // don't care
        }
    }
}