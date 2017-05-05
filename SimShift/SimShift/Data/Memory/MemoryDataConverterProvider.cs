using System;

namespace SimTelemetry.Domain.Memory
{
    public class MemoryDataConverterProvider<T>
    {
        public MemoryDataConverterProvider(Func<byte[], int, T> byte2obj, Func<object, T> obj2obj)
        {
            Byte2Obj = byte2obj;
            Obj2Obj = obj2obj;
        }

        public Func<byte[], int, T> Byte2Obj { get; private set; }

        public Type DataType
        {
            get
            {
                return typeof(T);
            }
        }

        public Func<object, T> Obj2Obj { get; private set; }
    }
}