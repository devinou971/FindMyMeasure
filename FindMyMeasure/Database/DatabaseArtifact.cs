using FindMyMeasure.Enums;
using FindMyMeasure.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FindMyMeasure.Database
{
    public class DatabaseArtifact : IDataInput, IModelReferenceTarget, IEquatable<DatabaseArtifact>
    {
        // That would be for Measures, Columns and Hierarchies

        protected ulong _id;
        protected string _name;
        protected string _type;
        protected string _expression;
        protected Table _table;
        protected HashSet<IModelReferenceTarget> _usages = new HashSet<IModelReferenceTarget>();

        //public enum ArtifactType
        //{
        //    Measure, 
        //    Column, 
        //    CalculatedColumn,
        //    Hierarchy
        //}

        internal DatabaseArtifact(ulong id, string name, string type, string expression, Table table)
        {
            this._id = id;
            this._name = name;
            this._type = type;
            this._expression = expression;
            this._table = table;
        }

        internal DatabaseArtifact(ulong id, string name, string type, Table table)
        {
            this._id = id;
            this._name = name;
            this._type = type;
            this._table = table;
        }

        /// <summary>
        /// Gets the unique identifier for this artifact (0 if in disconnected mode).
        /// </summary>
        public ulong Id => _id;

        /// <summary>
        /// Gets the name of this artifact.
        /// </summary>
        public string Name => _name;

        public string Type => _type;

        /// <summary>
        /// Gets the DAX expression that defines this artifact. That is usefull for measures and calculated columns
        /// </summary>
        public string Expression => _expression;

        public Table ParentTable => _table;


        /// <summary>
        /// Adds an object that depends on this artifact. 
        /// For example, a measure can be dependent of another measure. Or a PowerBI Visual can be dependent of a column. 
        /// </summary>
        /// <param name="target">The object that uses this measure.</param>
        /// <returns>True if the dependent was added; false if it already existed.</returns>
        public bool AddDependent(IModelReferenceTarget target)
        {
            return this._usages.Add(target);
        }

        public bool Equals(DatabaseArtifact other)
        {
            if (other == null)
                return false;
            var artifact = other as DatabaseArtifact;
            return artifact.Id == this.Id && artifact.Type == this.Type && artifact.Name == this.Name && artifact.ParentTable == this.ParentTable;
        }

        /// <summary>
        /// Gets all objects that depend on this artifact.
        /// </summary>
        /// <returns>A HashSet of IModelReferenceTarget objects.</returns>
        public HashSet<IModelReferenceTarget> GetDependents() => this._usages;


        public override string ToString()
        {
            return this._table.ToString() + "." + this.Name;
        }


        /// <summary>
        /// Calculates the usage state of this artifact based on its dependents.
        /// 
        /// Logic:
        /// - If used by any PowerBI leaf node, relationship, or table: Used
        /// - If used by measures/columns, check their usage state recursively
        /// - If no dependents: Unused
        /// </summary>
        /// <returns>The UsageState of this measure.</returns>
        public UsageState GetUsageState()
        {
            // Default to Unused if no dependents
            List<UsageState> usageStates = new List<UsageState>() { UsageState.Unused };

            foreach (var usage in this._usages)
            {
                // Usage in other measures or columns - check their usage state recursively
                if (usage is IDataInput)
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
                else // Direct usage in PowerBI reports/pages/visuals, relationships, or calculated tables
                {
                    return UsageState.Used;
                }
            }
            // Return the maximum state (Used > UsedByUnused > Unused)
            return usageStates.Max();
        }
    }
}
