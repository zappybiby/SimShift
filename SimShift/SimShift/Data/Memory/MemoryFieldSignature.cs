using System;
using System.Collections.Generic;
using System.Linq;

namespace SimTelemetry.Domain.Memory
{
    public class MemoryFieldSignature<T> : MemoryField<T>
    {
        public MemoryFieldSignature(string name, MemoryAddress type, string signature, IEnumerable<MemoryFieldSignaturePointer> pointers, int size)
            : base(name, type, 0, size)
        {
            this.Signature = signature;
            this.Pointers = pointers;
            this.Initialized = false;
        }

        public MemoryFieldSignature(string name, MemoryAddress type, string signature, IEnumerable<int> pointers, int size)
            : base(name, type, 0, size)
        {
            this.Signature = signature;
            this.Pointers = pointers.Select(pointer => new MemoryFieldSignaturePointer(pointer, false)).ToList();
            this.Initialized = false;
        }

        public MemoryFieldSignature(string name, MemoryAddress type, string signature, IEnumerable<MemoryFieldSignaturePointer> pointers, int size, Func<T, T> convert)
            : base(name, type, 0, size)
        {
            this.Signature = signature;
            this.Pointers = pointers;
            this.Initialized = false;
            this.Conversion = convert;
        }

        public MemoryFieldSignature(string name, MemoryAddress type, string signature, IEnumerable<int> pointers, int size, Func<T, T> convert)
            : base(name, type, 0, size)
        {
            this.Signature = signature;
            this.Pointers = pointers.Select(pointer => new MemoryFieldSignaturePointer(pointer, false)).ToList();
            this.Initialized = false;
            this.Conversion = convert;
        }

        public int[] AddressTree { get; protected set; }

        public bool Initialized { get; protected set; }

        public IEnumerable<MemoryFieldSignaturePointer> Pointers { get; protected set; }

        public string Signature { get; protected set; }

        public override void Refresh()
        {
            if (!this.Initialized)
            {
                this.Scan();
            }

            if (!this.Initialized)
            {
                return;
            }

            base.Refresh();
        }

        public virtual void Scan()
        {
            if (this.Memory.Scanner.Enabled == false)
            {
                throw new Exception("Please enable SignatureScanner first");
            }

            var result = this.Memory.Scanner.Scan<uint>(MemoryRegionType.EXECUTE, this.Signature);

            foreach (var ptr in this.Pointers)
            {
                ptr.Refresh(this.Memory);
            }

            // Search the address and offset.);
            switch (this.AddressType)
            {
                case MemoryAddress.StaticAbsolute:
                case MemoryAddress.Static:
                    if (result == 0)
                    {
                        return;
                    }

                    this.AddressTree = new int[1 + this.Pointers.Count()];

                    if (this.Pointers.Count() == 0)
                    {
                        // The result is directly our address
                        this.Address = (int) result;
                        this.AddressTree[0] = (int) result;
                    }
                    else
                    {
                        // We must follow one pointer.
                        var computedAddress = 0;
                        if (this.AddressType == MemoryAddress.Static)
                        {
                            computedAddress = this.Memory.BaseAddress + (int) result;
                        }
                        else
                        {
                            computedAddress = (int) result;
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

                        this.Address = computedAddress;
                    }

                    break;
                case MemoryAddress.Dynamic:
                    this.Offset = (int) result;

                    foreach (var ptr in this.Pointers)
                    {
                        if (ptr.Additive)
                        {
                            this.Offset += ptr.Offset;
                        }
                    }

                    break;
                default:
                    throw new Exception("AddressType for '" + this.Name + "' is not valid");
                    break;
            }

            this.Initialized = true;
        }
    }
}