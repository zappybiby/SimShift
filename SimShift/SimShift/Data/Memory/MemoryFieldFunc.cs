using System;

using SimShift.Data.Memory;

namespace SimTelemetry.Domain.Memory
{
    public class MemoryFieldFunc<T> : IMemoryObject
    {
        protected bool IsChanging = false;

        public MemoryFieldFunc(string name, Func<MemoryPool, T> validationFunc)
        {
            Name = name;
            ValidationFunc = validationFunc;
            IsChanging = true;
        }

        public MemoryFieldFunc(string name, Func<MemoryPool, T> validationFunc, bool isChanging)
        {
            Name = name;
            ValidationFunc = validationFunc;
            IsChanging = isChanging;
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
                return false;
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

        public Func<MemoryPool, T> ValidationFunc { get; protected set; }

        public Type ValueType
        {
            get
            {
                return typeof(T);
            }
        }

        public object Clone()
        {
            var newObj = new MemoryFieldFunc<T>(Name, ValidationFunc);
            return newObj;
        }

        public virtual bool HasChanged()
        {
            return IsChanging;
        }

        public void MarkDirty()
        {
            IsChanging = true;
        }

        public object Read()
        {
            return ValidationFunc(Pool);
        }

        public TOut ReadAs<TOut>()
        {
            return MemoryDataConverter.Cast<T, TOut>(ValidationFunc(Pool));
        }

        public void Refresh()
        {
            // Done!
        }

        public void SetPool(MemoryPool pool)
        {
            Pool = pool;
        }

        public void SetProvider(MemoryProvider provider)
        {
            Memory = provider;
        }
    }
}