namespace SimTelemetry.Domain.Memory
{
    public class MemoryFieldSignaturePointer
    {
        public MemoryFieldSignaturePointer(string signature, bool additive)
        {
            this.Signature = signature;
            this.Additive = additive;
            this.MarkDirty();
        }

        public MemoryFieldSignaturePointer(int offset, bool additive)
        {
            this.Offset = offset;
            this.Additive = additive;
            this.IsDirty = false;
        }

        public bool Additive { get; private set; }

        public bool IsDirty { get; private set; }

        public int Offset { get; private set; }

        public string Signature { get; private set; }

        public void MarkDirty()
        {
            this.IsDirty = true;
        }

        public void Refresh(MemoryProvider master)
        {
            if (this.IsDirty && master.Scanner.Enabled && this.Signature != string.Empty)
            {
                this.Offset = master.Scanner.Scan<int>(MemoryRegionType.EXECUTE, this.Signature);
                this.IsDirty = false;
            }
        }
    }
}