using System;

namespace SimShift.Data.Common
{
    public class MemoryWriter : MemoryReader
    {
        public override bool Open()
        {
            this.m_hProcess = ProcessMemoryReaderApi.OpenProcess(PROCESS_VM_WRITE | PROCESS_VM_READ, 0, (uint) this.m_ReadProcess.Id);
            return this.m_hProcess != IntPtr.Zero;
        }

        public void WriteByte(IntPtr address, byte value)
        {
            this.Write(address, new byte[1] { value });
        }

        public void WriteByte(int address, byte value)
        {
            this.Write((IntPtr) address, new byte[1] { value });
        }

        public void WriteBytes(IntPtr address, byte[] value)
        {
            this.Write(address, value);
        }

        public void WriteBytes(int address, byte[] value)
        {
            this.Write((IntPtr) address, value);
        }

        public void WriteDouble(IntPtr address, double value)
        {
            this.Write(address, BitConverter.GetBytes(value));
        }

        public void WriteDouble(int address, double value)
        {
            this.Write((IntPtr) address, BitConverter.GetBytes(value));
        }

        public void WriteFloat(IntPtr address, float value)
        {
            this.Write(address, BitConverter.GetBytes(value));
        }

        public void WriteFloat(int address, float value)
        {
            this.Write((IntPtr) address, BitConverter.GetBytes(value));
        }

        public void WriteInt16(IntPtr address, short value)
        {
            this.Write(address, BitConverter.GetBytes(value));
        }

        public void WriteInt16(int address, short value)
        {
            this.Write((IntPtr) address, BitConverter.GetBytes(value));
        }

        public void WriteInt32(IntPtr address, int value)
        {
            this.Write(address, BitConverter.GetBytes(value));
        }

        public void WriteInt32(int address, int value)
        {
            this.Write((IntPtr) address, BitConverter.GetBytes(value));
        }

        public void WriteInt64(IntPtr address, long value)
        {
            this.Write(address, BitConverter.GetBytes(value));
        }

        public void WriteInt64(int address, long value)
        {
            this.Write((IntPtr) address, BitConverter.GetBytes(value));
        }

        public void WriteUInt16(IntPtr address, ushort value)
        {
            this.Write(address, BitConverter.GetBytes(value));
        }

        public void WriteUInt16(int address, ushort value)
        {
            this.Write((IntPtr) address, BitConverter.GetBytes(value));
        }

        public void WriteUInt32(IntPtr address, uint value)
        {
            this.Write(address, BitConverter.GetBytes(value));
        }

        public void WriteUInt32(int address, uint value)
        {
            this.Write((IntPtr) address, BitConverter.GetBytes(value));
        }

        public void WriteUInt64(IntPtr address, ulong value)
        {
            this.Write(address, BitConverter.GetBytes(value));
        }

        public void WriteUInt64(int address, ulong value)
        {
            this.Write((IntPtr) address, BitConverter.GetBytes(value));
        }

        protected void Write(IntPtr address, byte[] data)
        {
            int bytesWritten;
            ProcessMemoryReaderApi.WriteProcessMemory(this.m_hProcess, address, data, (UIntPtr) data.Length, out bytesWritten);
        }
    }
}