using System;

using SimShift.Data.Memory;

namespace SimTelemetry.Domain.Memory
{
    public class MemoryField<T> : IMemoryObject
    {
        protected T _OldValue;

        protected T _Value;

        protected int readCounter = 0;

        public MemoryField(string name, MemoryAddress type, int address, int size)
        {
            this.Name = name;
            this.ValueType = typeof(T);
            this.Address = address;
            this.Size = size;
            this.Offset = 0;
            this.AddressType = type;
        }

        public MemoryField(string name, MemoryAddress type, int address, int offset, int size)
        {
            this.Name = name;
            this.ValueType = typeof(T);
            this.Address = address;
            this.Size = size;
            this.Offset = offset;
            this.AddressType = type;
        }

        public MemoryField(string name, MemoryAddress type, MemoryPool pool, int offset, int size)
        {
            this.Name = name;
            this.ValueType = typeof(T);
            this.Size = size;
            this.Offset = offset;
            this.Pool = pool;
            this.AddressType = type;
        }

        public MemoryField(string name, MemoryAddress type, int address, int size, Func<T, T> conversion)
        {
            this.Name = name;
            this.ValueType = typeof(T);
            this.Address = address;
            this.Size = size;
            this.Offset = 0;
            this.Conversion = conversion;
            this.AddressType = type;
        }

        public MemoryField(string name, MemoryAddress type, int address, int offset, int size, Func<T, T> conversion)
        {
            this.Name = name;
            this.ValueType = typeof(T);
            this.Address = address;
            this.Size = size;
            this.Offset = offset;
            this.Conversion = conversion;
            this.AddressType = type;
        }

        public MemoryField(string name, MemoryAddress type, MemoryPool pool, int offset, int size, Func<T, T> conversion)
        {
            this.Name = name;
            this.ValueType = typeof(T);
            this.Size = size;
            this.Offset = offset;
            this.Pool = pool;
            this.Conversion = conversion;
            this.AddressType = type;
        }

        public int Address { get; protected set; }

        public MemoryAddress AddressType { get; protected set; }

        public Func<T, T> Conversion { get; protected set; }

        public bool IsConstant => false;

        public bool IsDynamic => this.AddressType == MemoryAddress.Dynamic;

        public bool IsStatic => this.AddressType == MemoryAddress.Static || this.AddressType == MemoryAddress.StaticAbsolute;

        public MemoryProvider Memory { get; protected set; }

        public string Name { get; protected set; }

        public int Offset { get; protected set; }

        public MemoryPool Pool { get; protected set; }

        public int Size { get; protected set; }

        public virtual T Value => this._Value;

        public Type ValueType { get; protected set; }

        public object Clone()
        {
            var newObj = new MemoryField<T>(this.Name, this.AddressType, this.Address, this.Offset, this.Size, this.Conversion);
            return newObj;
        }

        public virtual bool HasChanged()
        {
            if (this.readCounter < 2)
            {
                return true;
            }

            if (this._OldValue == null)
            {
                return true;
            }

            if (this._Value == null)
            {
                return true;
            }

            return !this._Value.Equals(this._OldValue);
        }

        public void MarkDirty()
        {
            this.readCounter = 0;
        }

        public virtual object Read()
        {
            return this.Value;
        }

        public virtual TOut ReadAs<TOut>()
        {
            return MemoryDataConverter.Cast<T, TOut>(this.Value);
        }

        public virtual void Refresh()
        {
            this.readCounter++;
            this._OldValue = this._Value;

            if (this.IsStatic)
            {
                this.RefreshStatic();
            }
            else
            {
                this.RefreshDynamic();
            }

            if (this.Value != null && this.Conversion != null)
            {
                this._Value = this.Conversion(this._Value);
            }
        }

        public void SetPool(MemoryPool pool)
        {
            this.Pool = pool;
        }

        public void SetProvider(MemoryProvider provider)
        {
            this.Memory = provider;
        }

        protected virtual void RefreshDynamic()
        {
            if (this.Pool == null || this.Pool.Value == null)
            {
                return;
            }

            this._Value = MemoryDataConverter.Read<T>(this.Pool.Value, this.Offset);
        }

        protected virtual void RefreshStatic()
        {
            if (this.Memory == null)
            {
                return;
            }

            var computedAddress = 0;
            if (this.Address != 0 && this.Offset != 0)
            {
                computedAddress = this.Memory.Reader.ReadInt32(this.Memory.BaseAddress + this.Address) + this.Offset;
            }
            else
            {
                computedAddress = this.AddressType == MemoryAddress.Static ? this.Memory.BaseAddress + this.Address : this.Address;
            }

            var data = this.Memory.Reader.ReadBytes(computedAddress, (uint) this.Size);
            this._Value = MemoryDataConverter.Read<T>(data, 0);
        }
    }
}