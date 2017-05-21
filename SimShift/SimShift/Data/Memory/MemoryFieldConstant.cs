using System;

using SimShift.Data.Memory;

namespace SimTelemetry.Domain.Memory
{
    public class MemoryFieldConstant<T> : IMemoryObject
    {
        private bool firstCall = true;

        public MemoryFieldConstant(string name, T staticValue)
        {
            this.Name = name;
            this.StaticValue = staticValue;
        }

        public int Address => 0;

        public MemoryAddress AddressType => MemoryAddress.Constant;

        public bool IsConstant => true;

        public bool IsDynamic => false;

        public bool IsStatic => false;

        public MemoryProvider Memory { get; protected set; }

        public string Name { get; protected set; }

        public int Offset => 0;

        public MemoryPool Pool { get; protected set; }

        public int Size => 0;

        public T StaticValue { get; protected set; }

        public Type ValueType => typeof(T);

        public object Clone()
        {
            var newObj = new MemoryFieldConstant<T>(this.Name, this.StaticValue);
            return newObj;
        }

        public bool HasChanged()
        {
            if (this.firstCall)
            {
                this.firstCall = false;
                return true;
            }

            return false;
        }

        public void MarkDirty()
        {
            this.firstCall = true;
        }

        public virtual object Read()
        {
            return this.StaticValue;
        }

        public TOut ReadAs<TOut>()
        {
            return MemoryDataConverter.Cast<T, TOut>(this.StaticValue);
        }

        public void Refresh()
        {
            // Done!
        }

        public void SetPool(MemoryPool pool)
        {
            this.Pool = pool; // don't care
        }

        public void SetProvider(MemoryProvider provider)
        {
            this.Memory = provider; // don't care
        }
    }
}