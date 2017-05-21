using System;

using SimShift.Data.Memory;

namespace SimTelemetry.Domain.Memory
{
    public class MemoryFieldFunc<T> : IMemoryObject
    {
        protected bool IsChanging = false;

        public MemoryFieldFunc(string name, Func<MemoryPool, T> validationFunc)
        {
            this.Name = name;
            this.ValidationFunc = validationFunc;
            this.IsChanging = true;
        }

        public MemoryFieldFunc(string name, Func<MemoryPool, T> validationFunc, bool isChanging)
        {
            this.Name = name;
            this.ValidationFunc = validationFunc;
            this.IsChanging = isChanging;
        }

        public int Address => 0;

        public MemoryAddress AddressType => MemoryAddress.Constant;

        public bool IsConstant => false;

        public bool IsDynamic => false;

        public bool IsStatic => false;

        public MemoryProvider Memory { get; protected set; }

        public string Name { get; protected set; }

        public int Offset => 0;

        public MemoryPool Pool { get; protected set; }

        public int Size => 0;

        public Func<MemoryPool, T> ValidationFunc { get; protected set; }

        public Type ValueType => typeof(T);

        public object Clone()
        {
            var newObj = new MemoryFieldFunc<T>(this.Name, this.ValidationFunc);
            return newObj;
        }

        public virtual bool HasChanged()
        {
            return this.IsChanging;
        }

        public void MarkDirty()
        {
            this.IsChanging = true;
        }

        public object Read()
        {
            return this.ValidationFunc(this.Pool);
        }

        public TOut ReadAs<TOut>()
        {
            return MemoryDataConverter.Cast<T, TOut>(this.ValidationFunc(this.Pool));
        }

        public void Refresh()
        {
            // Done!
        }

        public void SetPool(MemoryPool pool)
        {
            this.Pool = pool;
        }

        public void SetProvider(MemoryProvider provider)
        {
            this.Memory = provider;
        }
    }
}