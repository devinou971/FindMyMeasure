using FindMyMeasure.Database;
using System.Collections.Generic;

namespace FindMyMeasure.Interfaces
{
    public interface IDataInput
    {
        string Name { get; }
        string Type { get; }
        string Expression { get; }
        Table ParentTable { get; }

        HashSet<IModelReferenceTarget> GetDependents();
        bool AddDependent(IModelReferenceTarget target);
        UsageState GetUsageState();

    }
}
