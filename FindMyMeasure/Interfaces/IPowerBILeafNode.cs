using System.Collections.Generic;

namespace FindMyMeasure.Interfaces
{
    public interface IPowerBILeafNode
    {
        string Name { get; }
        string Type { get; }
        bool AddDataInput(IDataInput dataInput);
    }
}
