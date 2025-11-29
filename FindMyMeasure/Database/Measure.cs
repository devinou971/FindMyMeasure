using FindMyMeasure.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FindMyMeasure.Database
{
    /// <summary>
    /// Represents the usage state of a measure or column in a semantic model and reports.
    /// </summary>
    public enum UsageState
    {
        /// <summary>The measure/column is not used anywhere.</summary>
        Unused,
        /// <summary>The measure/column is used directly or indirectly in a report or relationship.</summary>
        Used,
        /// <summary>The measure/column is used by other measures/columns, but all of those are Unused.</summary>
        UsedByUnused
    }

    /// <summary>
    /// Represents a measure in a semantic model with dependency tracking.
    /// Measures can be used in visuals, filters, relationships, and other measures.
    /// </summary>
    public class Measure : IDataInput, IModelReferenceTarget, IEquatable<Measure>
    {
        private string _name;
        private string _expression;
        private Table _table;
        private ulong _measureId;

        /// <summary>
        /// Stores all the objects (visuals, filters, measures, columns, relationships) that depend on this measure.
        /// </summary>
        private HashSet<IModelReferenceTarget> _measureUsages = new HashSet<IModelReferenceTarget>();

        /// <summary>
        /// Gets the name of this measure.
        /// </summary>
        public string Name => _name;

        /// <summary>
        /// Gets the DAX expression that defines this measure's calculation.
        /// </summary>
        public string Expression => _expression;

        /// <summary>
        /// Gets the unique identifier for this measure (0 if in disconnected mode).
        /// </summary>
        public ulong MeasureId => _measureId;
        public string Type => "Measure";
        public Table ParentTable => this._table;

        /// <summary>
        /// Initializes a new instance of the Measure class.
        /// </summary>
        /// <param name="measureId">The unique identifier for this measure.</param>
        /// <param name="measureName">The name of the measure.</param>
        /// <param name="expression">The DAX expression defining the measure.</param>
        /// <param name="table">The table that contains this measure.</param>
        public Measure(ulong measureId, string measureName, string expression, Table table)
        {
            this._measureId = measureId;
            this._expression = expression;
            this._name = measureName;
            this._table = table;
        }

        /// <summary>
        /// Gets all objects that depend on this measure.
        /// </summary>
        /// <returns>A HashSet of IModelReferenceTarget objects.</returns>
        public HashSet<IModelReferenceTarget> GetDependents()
        {
            return this._measureUsages;
        }

        /// <summary>
        /// Calculates the usage state of this measure based on its dependents.
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
            
            foreach (var usage in this._measureUsages)
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

        /// <summary>
        /// Gets the hash code for this measure based on its name.
        /// Note: Uses name instead of ID because measure IDs can be 0 in disconnected mode.
        /// </summary>
        public override int GetHashCode()
        {
            return this.Name.GetHashCode();
        }

        /// <summary>
        /// Returns a string representation of this measure in the format "TableName.MeasureName".
        /// </summary>
        public override string ToString()
        {
            return this._table.ToString() + "." + this.Name;
        }

        /// <summary>
        /// Adds an object that depends on this measure.
        /// </summary>
        /// <param name="target">The object that uses this measure.</param>
        /// <returns>True if the dependent was added; false if it already existed.</returns>
        public bool AddDependent(IModelReferenceTarget target)
        {
            return this._measureUsages.Add(target);
        }

        /// <summary>
        /// Determines whether this measure is equal to another measure.
        /// Two measures are equal if they have the same name (semantic models don't allow duplicate measure names).
        /// </summary>
        /// <param name="other">The measure to compare with.</param>
        /// <returns>True if the measures have the same name; otherwise, false.</returns>
        public bool Equals(Measure other)
        {
            // Measure names are unique within a semantic model, so we use name for equality instead of ID
            return other.Name == this.Name && other.ParentTable == this.ParentTable;
        }

        /// <summary>
        /// Gets the target type identifier for this object.
        /// </summary>
        public string GetTargetType()
        {
            return "Measure";
        }
    }
}
