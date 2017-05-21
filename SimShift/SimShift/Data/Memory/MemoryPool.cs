using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;

using SimShift.Data.Memory;

namespace SimTelemetry.Domain.Memory
{
    public class MemoryPool : IMemoryObject, IDataNode
    {
        private readonly Dictionary<string, IMemoryObject> _fields = new Dictionary<string, IMemoryObject>();

        private readonly Dictionary<string, MemoryPool> _pools = new Dictionary<string, MemoryPool>();

        public MemoryPool(string name, MemoryAddress type, string signature, int size)
        {
            this.Name = name;
            this.Address = 0;
            this.Offset = 0;
            this.Size = size;
            this.AddressType = type;
            this.Signature = signature;
            this.Pointers = new List<MemoryFieldSignaturePointer>();

            this.Value = new byte[size];
        }

        public MemoryPool(string name, MemoryAddress type, string signature, IEnumerable<int> pointers, int size)
        {
            this.Name = name;
            this.Address = 0;
            this.Offset = 0;
            this.Size = size;
            this.AddressType = type;
            this.Signature = signature;
            this.Pointers = pointers.Select(pointer => new MemoryFieldSignaturePointer(pointer, false)).ToList();

            this.Value = new byte[size];
        }

        public MemoryPool(string name, MemoryAddress type, string signature, IEnumerable<MemoryFieldSignaturePointer> pointers, int size)
        {
            this.Name = name;
            this.Address = 0;
            this.Offset = 0;
            this.Size = size;
            this.AddressType = type;
            this.Signature = signature;
            this.Pointers = pointers;

            this.Value = new byte[size];
        }

        public MemoryPool(string name, MemoryAddress type, int address, IEnumerable<int> pointers, int size)
        {
            this.Name = name;
            this.Address = address;
            this.Offset = 0;
            this.Size = size;
            this.AddressType = type;
            this.Signature = string.Empty;
            this.Pointers = pointers.Select(pointer => new MemoryFieldSignaturePointer(pointer, false)).ToList();

            this.Value = new byte[this.Size];
        }

        public MemoryPool(string name, MemoryAddress type, int address, IEnumerable<MemoryFieldSignaturePointer> pointers, int size)
        {
            this.Name = name;
            this.Address = address;
            this.Offset = 0;
            this.Size = size;
            this.AddressType = type;
            this.Signature = string.Empty;
            this.Pointers = pointers;

            this.Value = new byte[this.Size];
        }

        public MemoryPool(string name, MemoryAddress type, int address, int size)
        {
            this.Name = name;
            this.Address = address;
            this.Offset = 0;
            this.Size = size;
            this.AddressType = type;
            this.Signature = string.Empty;
            this.Pointers = new List<MemoryFieldSignaturePointer>();

            this.Value = new byte[this.Size];
        }

        public MemoryPool(string name, MemoryAddress type, int address, int offset, int size)
        {
            this.Name = name;
            this.Address = address;
            this.Offset = offset;
            this.Size = size;
            this.AddressType = type;
            this.Signature = string.Empty;
            this.Pointers = new List<MemoryFieldSignaturePointer>();

            this.Value = new byte[this.Size];
        }

        public MemoryPool(string name, MemoryAddress type, MemoryPool pool, int offset, int size)
        {
            this.Name = name;
            this.Pool = pool;
            this.Offset = offset;
            this.Size = size;
            this.AddressType = type;
            this.Signature = string.Empty;
            this.Pointers = new List<MemoryFieldSignaturePointer>();

            this.Value = new byte[this.Size];
        }

        public int Address { get; protected set; }

        public int[] AddressTree { get; protected set; }

        public MemoryAddress AddressType { get; protected set; }

        public Dictionary<string, IMemoryObject> Fields => this._fields;

        public bool IsConstant => false;

        public bool IsDynamic => this.AddressType == MemoryAddress.Dynamic;

        public bool IsSignature => this.Signature != string.Empty;

        public bool IsStatic => this.AddressType == MemoryAddress.Static || this.AddressType == MemoryAddress.StaticAbsolute;

        public bool IsTemplate { get; protected set; }

        public MemoryProvider Memory { get; set; }

        public string Name { get; protected set; }

        public int Offset { get; protected set; }

        public IEnumerable<MemoryFieldSignaturePointer> Pointers { get; protected set; }

        public MemoryPool Pool { get; protected set; }

        public Dictionary<string, MemoryPool> Pools => this._pools;

        public string Signature { get; protected set; }

        public int Size { get; protected set; }

        public byte[] Value { get; protected set; }

        public Type ValueType => typeof(MemoryPool);

        Dictionary<string, IDataField> IDataNode.Fields
        {
            get
            {
                return this._fields.Values.Cast<IDataField>().ToDictionary(x => x.Name, x => x);
            }
        }

        public void Add<T>(T obj)
            where T : IMemoryObject
        {
            if (typeof(T).Name.Contains("MemoryPool"))
            {
                throw new Exception();
            }

            if (!this._fields.ContainsKey(obj.Name))
            {
                this._fields.Add(obj.Name, obj);

                obj.SetPool(this);
                if (this.Memory != null)
                {
                    obj.SetProvider(this.Memory);
                }
            }
        }

        public void Add(MemoryPool obj)
        {
            if (!this._pools.ContainsKey(obj.Name))
            {
                this._pools.Add(obj.Name, obj);

                obj.SetPool(this);
                if (this.Memory != null)
                {
                    obj.SetProvider(this.Memory);
                }
            }
        }

        public void ClearPools()
        {
            this._pools.Clear();
        }

        public object Clone()
        {
            // cannot clone without arguments.
            return null;
        }

        public MemoryPool Clone(string newName, MemoryPool newPool, int offset, int size)
        {
            var target = new MemoryPool(newName, this.AddressType, newPool, offset, size);
            this.CloneContents(target);
            return target;
        }

        public IDataNode Clone(string newName, int address)
        {
            var target = new MemoryPool(newName, this.AddressType, address, this.Size);
            this.CloneContents(target);
            return (IDataNode) target;
        }

        public IEnumerable<IDataField> GetDataFields()
        {
            return this.Fields.Select(x => (IDataField) x.Value);
        }

        public void GetDebugInfo(XmlWriter file)
        {
            file.WriteStartElement("debug");
            file.WriteAttributeString("name", this.Name);
            file.WriteAttributeString("fields", this.Fields.Count().ToString());

            file.WriteAttributeString("template", this.IsTemplate.ToString());
            if (this.AddressTree == null)
            {
                file.WriteAttributeString("address", this.Address.ToString("X"));
            }
            else
            {
                file.WriteAttributeString("address", string.Concat(this.AddressTree.Select(x => x.ToString("X") + ", ")));
            }

            file.WriteAttributeString("size", this.Size.ToString("X"));
            file.WriteAttributeString("offset", this.Offset.ToString("X"));

            foreach (var field in this.Fields)
            {
                file.WriteStartElement("debug-field");
                file.WriteAttributeString("name", field.Value.Name);
                file.WriteAttributeString("address", field.Value.Address.ToString());
                file.WriteAttributeString("size", field.Value.Size.ToString());
                file.WriteAttributeString("offset", field.Value.Offset.ToString());
                file.WriteAttributeString("type", field.Value.ValueType.ToString());
                file.WriteEndElement();
            }

            foreach (var pool in this.Pools)
            {
                pool.Value.GetDebugInfo(file);
            }

            file.WriteEndElement();

            // return string.Format("Type:{0}, Fields: {1}, IsTemplate: {2}, Address: 0x{3:X}, Offset: 0x{4:X}, Size: 0x{5:X}", AddressType, Fields.Count(), IsTemplate, Address, Offset, Size);
        }

        public bool HasChanged()
        {
            return false;
        }

        public void MarkDirty()
        { }

        public object Read()
        {
            return new byte[0];
        }

        public TOut ReadAs<TOut>()
        {
            return MemoryDataConverter.Read<TOut>(new byte[32], 0);
        }

        public TOut ReadAs<TOut>(int offset)
        {
            return MemoryDataConverter.Read<TOut>(this.Value, offset);
        }

        public TOut ReadAs<TSource, TOut>(int offset)
        {
            return MemoryDataConverter.Read<TSource, TOut>(this.Value, offset);
        }

        public TOut ReadAs<TOut>(string field)
        {
            if (this.Fields.ContainsKey(field))
            {
                return this.Fields[field].ReadAs<TOut>();
            }
            else
            {
                return MemoryDataConverter.Read<TOut>(new byte[32], 0);
            }
        }

        public byte[] ReadBytes(string field)
        {
            var oField = this.Fields[field];
            if (oField.HasChanged())
            {
                return MemoryDataConverter.Rawify(oField.Read);
            }
            else
            {
                return new byte[0];
            }
        }

        public void Refresh()
        {
            if (this.IsTemplate)
            {
                return;
            }

            var computedAddress = 0;

            if (this.IsSignature && this.Offset == 0 && this.Address == 0 && this.Memory.Scanner.Enabled)
            {
                var result = this.Memory.Scanner.Scan<uint>(MemoryRegionType.EXECUTE, this.Signature);

                // Search the address and offset.
                switch (this.AddressType)
                {
                    case MemoryAddress.StaticAbsolute:
                    case MemoryAddress.Static:
                        if (result == 0)
                        {
                            return;
                        }

                        if (this.Pointers.Count() == 0)
                        {
                            // The result is directly our address
                            this.Address = (int) result;
                        }
                        else
                        {
                            // We must follow one pointer.
                            if (this.AddressType == MemoryAddress.Static)
                            {
                                computedAddress = this.Memory.BaseAddress + (int) result;
                            }
                            else
                            {
                                computedAddress = (int) result;
                            }

                            this.Address = computedAddress;
                        }

                        break;
                    case MemoryAddress.Dynamic:
                        this.Offset = (int) result;
                        break;
                    default:
                        throw new Exception("AddressType for '" + this.Name + "' is not valid");
                        break;
                }
            }

            // Refresh pointers too
            foreach (var ptr in this.Pointers)
            {
                ptr.Refresh(this.Memory);
            }

            // Refresh this memory block.
            if (this.Size > 0)
            {
                this.AddressTree = new int[1 + this.Pointers.Count()];
                if (this.IsStatic)
                {
                    if (this.Address != 0 && this.Offset != 0)
                    {
                        computedAddress = this.Memory.Reader.ReadInt32(this.Memory.BaseAddress + this.Address) + this.Offset;
                    }
                    else
                    {
                        computedAddress = this.AddressType == MemoryAddress.Static ? this.Memory.BaseAddress + this.Address : this.Address;
                    }
                }
                else
                {
                    computedAddress = this.Pool == null ? 0 : MemoryDataConverter.Read<int>(this.Pool.Value, this.Offset);
                }

                int treeInd = 0;
                foreach (var ptr in this.Pointers)
                {
                    this.AddressTree[treeInd++] = computedAddress;
                    if (ptr.Additive)
                    {
                        computedAddress += ptr.Offset;
                    }
                    else
                    {
                        computedAddress = this.Memory.Reader.ReadInt32(computedAddress + ptr.Offset);
                    }
                }

                this.AddressTree[treeInd] = computedAddress;

                // Read into this buffer.
                this.Memory.Reader.Read(computedAddress, this.Value);
            }

            // Refresh underlying fields.
            foreach (var field in this.Fields)
            {
                field.Value.Refresh();
            }

            foreach (var pool in this.Pools.Values)
            {
                pool.Refresh();
            }
        }

        public void SetPool(MemoryPool pool)
        {
            if (this.Pool == pool)
            {
                return;
            }

            this.Pool = pool;
        }

        public void SetProvider(MemoryProvider provider)
        {
            this.Memory = provider;
            foreach (var field in this._fields)
            {
                field.Value.SetProvider(provider);
            }

            foreach (var pool in this._pools)
            {
                pool.Value.SetProvider(provider);
            }
        }

        public void SetTemplate(bool yes)
        {
            this.IsTemplate = yes;
        }

        protected void CloneContents(MemoryPool target)
        {
            foreach (var pool in this.Pools)
            {
                target.Add(pool.Value.Clone(pool.Key, target, pool.Value.Offset, pool.Value.Size));
            }

            foreach (var field in this.Fields)
            {
                target.Add((IMemoryObject) field.Value.Clone());
            }
        }
    }
}