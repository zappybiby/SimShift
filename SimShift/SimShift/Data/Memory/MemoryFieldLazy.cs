using System;

namespace SimTelemetry.Domain.Memory
{
    public class MemoryFieldLazy<T> : MemoryField<T>
    {
        protected Lazy<T> _LazyValue;

        protected bool Refreshed = false;

        public MemoryFieldLazy(string name, MemoryAddress type, int address, int size)
            : base(name, type, address, size)
        { }

        public MemoryFieldLazy(string name, MemoryAddress type, int address, int offset, int size)
            : base(name, type, address, offset, size)
        { }

        public MemoryFieldLazy(string name, MemoryAddress type, MemoryPool pool, int offset, int size)
            : base(name, type, pool, offset, size)
        { }

        public MemoryFieldLazy(string name, MemoryAddress type, int address, int size, Func<T, T> conversion)
            : base(name, type, address, size, conversion)
        { }

        public MemoryFieldLazy(string name, MemoryAddress type, int address, int offset, int size, Func<T, T> conversion)
            : base(name, type, address, offset, size, conversion)
        { }

        public MemoryFieldLazy(string name, MemoryAddress type, MemoryPool pool, int offset, int size, Func<T, T> conversion)
            : base(name, type, pool, offset, size, conversion)
        { }

        public override T Value
        {
            get
            {
                if (this._LazyValue == null)
                {
                    this.Refresh();
                }

                return this._LazyValue.Value;
            }
        }

        public override bool HasChanged()
        {
            if (!this.Refreshed)
            {
                return false;
            }

            if (this.readCounter < 2)
            {
                return true;
            }

            if (this._OldValue == null)
            {
                return true;
            }

            bool what = this._OldValue.Equals(this._Value);
            return !what;
        }

        public override void Refresh()
        {
            this.Refreshed = false;
            if (this._LazyValue == null || this._LazyValue.IsValueCreated)
            {
                this._LazyValue = new Lazy<T>(
                    () =>
                        {
                            this.readCounter++;
                            this.Refreshed = true;
                            this._OldValue = this._Value;
                            if (this.IsStatic)
                            {
                                this.RefreshStatic();
                            }
                            else
                            {
                                this.RefreshDynamic();
                            }

                            if (this._Value != null && this.Conversion != null)
                            {
                                this._Value = this.Conversion(this._Value);
                            }

                            return this._Value;
                        });
            }
        }
    }
}