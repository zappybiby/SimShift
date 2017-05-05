using System;
using System.Collections.Generic;
using System.Linq;

namespace SimTelemetry.Domain.Memory
{
    public class MemoryProvider : IDataProvider
    {
        private readonly IList<MemoryPool> _pools = new List<MemoryPool>();

        public MemoryProvider(MemoryReader reader)
        {
            if (reader != null)
            {
                BaseAddress = reader.Process.MainModule.BaseAddress.ToInt32();
                Reader = reader;

                Scanner = new MemorySignatureScanner(this);
            }
        }

        public int BaseAddress { get; protected set; }

        public IList<MemoryPool> Pools
        {
            get
            {
                return _pools;
            }
        }

        public MemoryReader Reader { get; protected set; }

        public MemorySignatureScanner Scanner { get; protected set; }

        public void Add(IDataNode pool)
        {
            _pools.Add((MemoryPool) pool);
            ((MemoryPool) pool).SetProvider(this);
        }

        public bool Contains(string name)
        {
            return _pools.Any(x => x.Name == name);
        }

        public IDataNode Get(string name)
        {
            return _pools.Where(x => x.Name == name).Cast<IDataNode>().FirstOrDefault();
        }

        public IEnumerable<IDataNode> GetAll()
        {
            return _pools;
        }

        public void MarkDirty()
        {
            _pools.SelectMany(x => x.Fields.Values).ToList().ForEach(x => x.MarkDirty());
        }

        public void Refresh()
        {
            foreach (var pool in _pools)
            {
                pool.Refresh();
            }
        }

        public void Remove(IDataNode pool)
        {
            _pools.Remove((MemoryPool) pool);
        }
    }
}