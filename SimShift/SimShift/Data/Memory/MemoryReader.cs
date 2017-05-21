using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Timers;

using SimShift.Data.Memory;

namespace SimTelemetry.Domain.Memory
{
    public class MemoryReader
    {
        protected Process _Process;

        protected List<MemoryRegion> _regions = new List<MemoryRegion>();

        protected IntPtr m_hProcess = IntPtr.Zero;

        private bool _diagnostic = false;

        private Timer _diagnosticTimer = null;

        private int _readCalls;

        public bool Diagnostic
        {
            get => this._diagnostic;

            set
            {
                this._diagnostic = value;
                if (this._diagnostic)
                {
                    this._diagnosticTimer = new Timer(1000);
                    this._diagnosticTimer.Elapsed += (a, b) =>
                        {
                            this.ReadCalls = this._readCalls;
                            this._readCalls = 0;
                        };
                    this._diagnosticTimer.AutoReset = true;
                    this._diagnosticTimer.Start();
                }
                else
                {
                    if (this._diagnosticTimer != null)
                    {
                        this._diagnosticTimer.Stop();
                        this._diagnosticTimer = null;
                    }
                }
            }
        }

        public Process Process => this._Process;

        public int ReadCalls { get; private set; }

        public IList<MemoryRegion> Regions => this._regions;

        public virtual bool Close()
        {
            if (this.m_hProcess == null || this.m_hProcess == IntPtr.Zero)
            {
                return false;
            }

            var iRetValue = MemoryReaderApi.CloseHandle(this.m_hProcess);
            return iRetValue != 0;
        }

        public virtual bool Open(Process p)
        {
            this.m_hProcess = MemoryReaderApi.OpenProcess((uint) MemoryReaderApi.AccessType.PROCESS_VM_READ, 0, (uint) p.Id);

            var result = (this.m_hProcess == IntPtr.Zero) ? false : true;
            if (result)
            {
                this._Process = p;
            }

            if (result)
            {
                this.ScanRegions();
            }

            return result;
        }

        public virtual bool Open(Process p, bool scanRegions)
        {
            this.m_hProcess = MemoryReaderApi.OpenProcess((uint) MemoryReaderApi.AccessType.PROCESS_VM_READ, 0, (uint) p.Id);

            var result = (this.m_hProcess == IntPtr.Zero) ? false : true;
            if (result)
            {
                this._Process = p;
            }

            if (result && scanRegions)
            {
                this.ScanRegions();
            }

            return result;
        }

        public virtual byte[] Read(IntPtr memoryAddress, uint bytesToRead)
        {
            if (this.Diagnostic)
            {
                this._readCalls++;
            }

            IntPtr ptrBytesReaded;
            var buffer = new byte[bytesToRead];
            MemoryReaderApi.ReadProcessMemory(this.m_hProcess, memoryAddress, buffer, bytesToRead, out ptrBytesReaded);
            return buffer;
        }

        public virtual bool Read(IntPtr memoryAddress, byte[] buffer)
        {
            if (this.Diagnostic)
            {
                this._readCalls++;
            }

            IntPtr ptrBytesReaded;

            MemoryReaderApi.ReadProcessMemory(this.m_hProcess, memoryAddress, buffer, (uint) buffer.Length, out ptrBytesReaded);
            return (int) ptrBytesReaded == buffer.Length;
        }

        public virtual bool Read(int memoryAddress, byte[] buffer)
        {
            if (this.Diagnostic)
            {
                this._readCalls++;
            }

            IntPtr ptrBytesReaded;

            MemoryReaderApi.ReadProcessMemory(this.m_hProcess, (IntPtr) memoryAddress, buffer, (uint) buffer.Length, out ptrBytesReaded);
            return (int) ptrBytesReaded == buffer.Length;
        }

        public T Read<T>(IntPtr address, uint size, Func<byte[], int, T> converter)
        {
            return converter(this.Read(address, size), 0);
        }

        public T Read<T>(int address, uint size, Func<byte[], int, T> converter)
        {
            return converter(this.Read((IntPtr) address, size), 0);
        }

        public byte ReadByte(IntPtr address)
        {
            return this.Read(address, 1)[0];
        }

        public byte ReadByte(int address)
        {
            return this.Read((IntPtr) address, 1)[0];
        }

        public byte[] ReadBytes(IntPtr address, uint size)
        {
            return this.Read(address, size);
        }

        public byte[] ReadBytes(int address, uint size)
        {
            return this.Read((IntPtr) address, size);
        }

        public double ReadDouble(IntPtr address)
        {
            return BitConverter.ToDouble(this.Read(address, 8), 0);
        }

        public double ReadDouble(int address)
        {
            return BitConverter.ToDouble(this.Read((IntPtr) address, 8), 0);
        }

        public float ReadFloat(IntPtr address)
        {
            return BitConverter.ToSingle(this.Read(address, 4), 0);
        }

        public float ReadFloat(int address)
        {
            return BitConverter.ToSingle(this.Read((IntPtr) address, 4), 0);
        }

        public short ReadInt16(IntPtr address)
        {
            return BitConverter.ToInt16(this.Read(address, 2), 0);
        }

        public short ReadInt16(int address)
        {
            return BitConverter.ToInt16(this.Read((IntPtr) address, 2), 0);
        }

        public int ReadInt32(IntPtr address)
        {
            return BitConverter.ToInt32(this.Read(address, 4), 0);
        }

        public int ReadInt32(int address)
        {
            return BitConverter.ToInt32(this.Read((IntPtr) address, 4), 0);
        }

        public long ReadInt64(IntPtr address)
        {
            return BitConverter.ToInt64(this.Read(address, 8), 0);
        }

        public long ReadInt64(int address)
        {
            return BitConverter.ToInt64(this.Read((IntPtr) address, 8), 0);
        }

        public string ReadString(IntPtr address, uint size)
        {
            int i = 0;
            byte[] bt = this.ReadBytes(address, size);
            for (i = 0; i < bt.Length; i++)
            {
                if (bt[i] == 0)
                {
                    break;
                }
            }

            return Encoding.ASCII.GetString(bt, 0, i);
        }

        public string ReadString(int address, uint size)
        {
            int i = 0;
            byte[] bt = this.ReadBytes(address, size);
            for (i = 0; i < bt.Length; i++)
            {
                if (bt[i] == 0)
                {
                    break;
                }
            }

            return Encoding.ASCII.GetString(bt, 0, i);
        }

        public ushort ReadUInt16(IntPtr address)
        {
            return BitConverter.ToUInt16(this.Read(address, 2), 0);
        }

        public ushort ReadUInt16(int address)
        {
            return BitConverter.ToUInt16(this.Read((IntPtr) address, 2), 0);
        }

        public uint ReadUInt32(IntPtr address)
        {
            return BitConverter.ToUInt32(this.Read(address, 4), 0);
        }

        public uint ReadUInt32(int address)
        {
            return BitConverter.ToUInt32(this.Read((IntPtr) address, 4), 0);
        }

        public ulong ReadUInt64(IntPtr address)
        {
            return BitConverter.ToUInt64(this.Read(address, 8), 0);
        }

        public ulong ReadUInt64(int address)
        {
            return BitConverter.ToUInt64(this.Read((IntPtr) address, 8), 0);
        }

        protected void ScanRegions()
        {
            this.ScanRegions(true);
        }

        protected void ScanRegions(bool onlyMe)
        {
            var memRegionAddr = new IntPtr();
            string targetExeName = Path.GetFileName(this._Process.MainModule.FileName);
            while (true)
            {
                var regionInfo = new MemoryReaderApi.MEMORY_BASIC_INFORMATION();
                if (MemoryReaderApi.VirtualQueryEx(this._Process.Handle, memRegionAddr, out regionInfo, (uint) Marshal.SizeOf(regionInfo)) != 0)
                {
                    if (regionInfo.BaseAddress.ToInt64() + regionInfo.RegionSize >= 0x80000000)
                    {
                        break;
                    }

                    memRegionAddr = new IntPtr(regionInfo.BaseAddress.ToInt32() + regionInfo.RegionSize);
                    if ((regionInfo.State & 0x10000) != 0)
                    {
                        // MemoryReaderApi.PageFlags.Free)
                        continue;
                    }

                    if (onlyMe)
                    {
                        StringBuilder processName = new StringBuilder(255);
                        MemoryReaderApi.GetMappedFileName(this._Process.Handle, memRegionAddr, processName, processName.Capacity);

                        if (!processName.ToString().Contains(targetExeName))
                        {
                            continue;
                        }
                    }

                    if (true || (regionInfo.State & (uint) MemoryReaderApi.PageFlags.MEM_COMMIT) != 0 && (regionInfo.Protect & (uint) MemoryReaderApi.PageFlags.WRITABLE) != 0 && (regionInfo.Protect & (uint) MemoryReaderApi.PageFlags.PAGE_GUARD) == 0)
                    {
                        // TODO: Parse commit, writability & guard.
                        bool execute = ((regionInfo.Protect & (uint) MemoryReaderApi.PageFlags.PAGE_EXECUTE) != 0) || ((regionInfo.Protect & (uint) MemoryReaderApi.PageFlags.PAGE_EXECUTE_READ) != 0) || ((regionInfo.Protect & (uint) MemoryReaderApi.PageFlags.PAGE_EXECUTE_READWRITE) != 0) || ((regionInfo.Protect & (uint) MemoryReaderApi.PageFlags.PAGE_EXECUTE_WRITECOPY) != 0);
                        var region = new MemoryRegion(regionInfo.BaseAddress.ToInt32(), (int) regionInfo.RegionSize, execute);
                        this._regions.Add(region);
                    }
                }
                else
                {
                    // int err = MemoryReaderApi.GetLastError();
                    // if (err != 0)
                    // throw new Exception("Failed to scan memory regions.");
                    break; // last block, done!
                }
            }
        }
    }

    public class MemoryRegion
    {
        public int BaseAddress;

        public byte[] Data;

        public bool Execute;

        public int Size;

        public MemoryRegion(int baseAddress, int size, bool execute)
        {
            this.BaseAddress = baseAddress;
            this.Size = size;
            this.Execute = execute;
            this.Data = new byte[0];
        }

        public bool MatchesType(MemoryRegionType type)
        {
            if (type == MemoryRegionType.EXECUTE && this.Execute == false)
            {
                return false;
            }

            return true;
        }

        internal void DestroySigScan()
        {
            this.Data = new byte[0];
        }

        internal void PrepareSigScan(MemoryReader reader)
        {
            if (this.Size > 0x300000)
            {
                return;
            }

            this.Data = new byte[this.Size];
            reader.Read(this.BaseAddress, this.Data);
        }
    }
}