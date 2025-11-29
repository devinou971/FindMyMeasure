using System.Collections.Generic;

namespace FindMyMeasure.Interfaces
{
    public interface IPowerBILeafNode
    {
        string Name { get; }
        bool AddDataInput(IDataInput dataInput);
        HashSet<IDataInput> GetDataInputs();
    }
}
