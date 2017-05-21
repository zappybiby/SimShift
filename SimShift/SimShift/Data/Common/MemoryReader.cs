using System;
using System.Diagnostics;
using System.Text;

namespace SimShift.Data.Common
{
    public class MemoryReader
    {
        protected const uint PROCESS_QUERY_INFORMATION = 0x0400;

        protected const uint PROCESS_QUERY_LIMITED_INFORMATION = 0x0100;

        protected const uint PROCESS_VM_READ = 0x0010;

        protected const uint PROCESS_VM_WRITE = 0x0028;

        protected IntPtr m_hProcess = IntPtr.Zero;

        protected Process m_ReadProcess;

        public Process ReadProcess
        {
            get => this.m_ReadProcess;

            set => this.m_ReadProcess = value;
        }

        public virtual bool Close()
        {
            if (this.m_hProcess == null || this.m_hProcess == IntPtr.Zero)
            {
                return false;
            }

            var iRetValue = ProcessMemoryReaderApi.CloseHandle(this.m_hProcess);
            return iRetValue != 0;
        }

        public virtual bool Open()
        {
            this.m_hProcess = ProcessMemoryReaderApi.OpenProcess(PROCESS_VM_READ, 0, (uint) this.m_ReadProcess.Id);
            return (this.m_hProcess == IntPtr.Zero) ? false : true;
        }

        public virtual byte[] Read(IntPtr memoryAddress, uint bytesToRead)
        {
            IntPtr ptrBytesReaded;
            var buffer = new byte[bytesToRead];
            ProcessMemoryReaderApi.ReadProcessMemory(this.m_hProcess, memoryAddress, buffer, bytesToRead, out ptrBytesReaded);
            return buffer;
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
    }
}