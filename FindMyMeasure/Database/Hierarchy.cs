using FindMyMeasure.Enums;
using FindMyMeasure.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FindMyMeasure.Database
{
    public class Hierarchy : IDataInput, IModelReferenceTarget, IEquatable<Hierarchy>
    {
        private string _name;
        private Table _table;
        private ulong _hierarchyId;
        private HashSet<IModelReferenceTarget> _hierarchyUsages = new HashSet<IModelReferenceTarget>();

        public string Name => this._name;

        public ulong HierarchyId => this._hierarchyId;

        public string Type => "Hierarchy";

        public string Expression => null;

        public Table ParentTable => this._table;

        public Hierarchy(ulong hierarchyId, string name, Table table)
        {
            this._hierarchyId = hierarchyId;
            this._name = name;
            this._table = table;
        }

        public bool AddDependent(IModelReferenceTarget target)
        {
            return this._hierarchyUsages.Add(target);
        }

        public bool Equals(Hierarchy other)
        {
            return other.Name == this.Name && other.ParentTable == this.ParentTable;
        }

        public HashSet<IModelReferenceTarget> GetDependents()
        {
            return this._hierarchyUsages;
        }

        public string GetTargetType()
        {
            return "Hierarchy";
        }

        public UsageState GetUsageState()
        {
            // Default to Unused if no dependents
            List<UsageState> usageStates = new List<UsageState>() { UsageState.Unused };

            foreach (var usage in this._hierarchyUsages)
            {
                // Direct usage in PowerBI reports/pages/visuals, relationships, or calculated tables
                if (usage is IPowerBILeafNode || usage is Relationship || usage is Table)
                {
                    return UsageState.Used;
                }
                // Usage in other measures or columns - check their usage state recursively
                else if (usage is IDataInput)
                {
                    var dataInput = (IDataInput)usage;
                    var dependentState = dataInput.GetUsageState();

                    // If the dependent is unused or only used by unused items, propagate that state
                    if (dependentState == UsageState.Unused || dependentState == UsageState.UsedByUnused)
                    {
                        usageStates.Add(UsageState.UsedByUnused);
                    }
                    // If the dependent is actually used, this measure is also used
                    else
                    {
                        usageStates.Add(UsageState.Used);
                    }
                }
            }
            // Return the maximum state (Used > UsedByUnused > Unused)
            return usageStates.Max();
        }
    }
}
