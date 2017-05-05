using System.Collections.Generic;
using System.Xml;

namespace SimTelemetry.Domain.Memory
{
    public interface IDataNode
    {
        Dictionary<string, IDataField> Fields { get; }

        string Name { get; }

        IDataNode Clone(string newName, int newAddress);

        IEnumerable<IDataField> GetDataFields();

        void GetDebugInfo(XmlWriter file);

        T ReadAs<T>(string field);

        byte[] ReadBytes(string field);
    }
}