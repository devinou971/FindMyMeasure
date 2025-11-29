using FindMyMeasure.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FindMyMeasure.Database
{
    /// <summary>
    /// Represents a column in a semantic model table, including calculated columns.
    /// </summary>
    public class Column : IDataInput, IModelReferenceTarget, IEquatable<Column>
    {
        public enum ColumnTypeEnum
        {
            Column,
            CalculatedColumn
        };

        private string _name;
        private Table _table;
        private ColumnTypeEnum _columnType;
        private ulong _columnId;
        private string _expression;

        private HashSet<IModelReferenceTarget> _columnUsages = new HashSet<IModelReferenceTarget>();

        public string Name => _name;

        /// <summary>
        /// Gets the DAX expression for calculated columns, or null for regular columns.
        /// </summary>
        public string Expression => _expression;

        /// <summary>
        /// Gets the type of this column (regular or calculated).
        /// </summary>
        public ColumnTypeEnum ColumnType => _columnType;

        /// <summary>
        /// Gets the unique identifier for this column.
        /// </summary>
        public ulong ColumnId => _columnId;
        public string Type => this._columnType.ToString();
        public Table ParentTable => this._table;

        /// <summary>
        /// Initializes a new instance of the Column class.
        /// </summary>
        /// <param name="columnId">The unique identifier for this column.</param>
        /// <param name="columnName">The name of the column.</param>
        /// <param name="expression">The DAX expression for calculated columns, or null for regular columns.</param>
        /// <param name="table">The table that contains this column.</param>
        public Column(ulong columnId, string columnName, string expression, Table table)
        {
            this._name = columnName;
            this._table = table;
            this._expression = expression;
            this._columnType = string.IsNullOrEmpty(expression) ? ColumnTypeEnum.Column : ColumnTypeEnum.CalculatedColumn;
            this._columnId = columnId;
        }

        /// <summary>
        /// Initializes a new instance of the Column class for a regular column.
        /// </summary>
        /// <param name="columnId">The unique identifier for this column.</param>
        /// <param name="columnName">The name of the column.</param>
        /// <param name="table">The table that contains this column.</param>
        public Column(ulong columnId, string columnName, Table table) : this(columnId, columnName, null, table) { }

        /// <summary>
        /// Gets all objects that depend on this column.
        /// </summary>
        /// <returns>A HashSet of IModelReferenceTarget objects.</returns>
        public HashSet<IModelReferenceTarget> GetDependents()
        {
            return this._columnUsages;
        }

        /// <summary>
        /// Gets the hash code for this column based on its table and name.
        /// </summary>
        public override int GetHashCode()
        {
            return (this._table.ToString() + "." + this._name).GetHashCode();
        }

        /// <summary>
        /// Returns a string representation of this column in the format "TableName.ColumnName".
        /// </summary>
        public override string ToString()
        {
            return this._table.ToString() + "." + this._name;
        }

        /// <summary>
        /// Adds an object that depends on this column.
        /// </summary>
        /// <param name="target">The object that uses this column.</param>
        /// <returns>True if the dependent was added; false if it already existed.</returns>
        public bool AddDependent(IModelReferenceTarget target)
        {
            return this._columnUsages.Add(target);
        }

        /// <summary>
        /// Determines whether this column is equal to another column.
        /// Two columns are equal if they have the same table, column ID, and name.
        /// </summary>
        /// <param name="other">The column to compare with.</param>
        /// <returns>True if the columns are equal; otherwise, false.</returns>
        public bool Equals(Column other)
        {
            // checking columnName is necessary in disconnected mode where columnId is always 0
            return other._table == this._table && other.ColumnId == this.ColumnId && this._name == other.Name; 
        }

        /// <summary>
        /// Calculates the usage state of this column based on its dependents.
        /// - Used if referenced by a PowerBI leaf node, relationship, or table.
        /// - UsedByUnused if only referenced by unused measures/columns.
        /// - Unused if not referenced.
        /// </summary>
        /// <returns>The UsageState of this column.</returns>
        public UsageState GetUsageState()
        {
            List<UsageState> usageStates = new List<UsageState>() { UsageState.Unused };
            foreach (var usage in this._columnUsages)
            {
                if (usage is IPowerBILeafNode || usage is Relationship || usage is Table)
                {
                    return UsageState.Used;
                }
                else if (usage is IDataInput)
                {
                    var dataInput = (IDataInput)usage;
                    if (dataInput.GetUsageState() == UsageState.Unused || dataInput.GetUsageState() == UsageState.UsedByUnused)
                    {
                        usageStates.Add(UsageState.UsedByUnused);
                    }
                    else
                    {
                        usageStates.Add(UsageState.Used);
                    }
                }
            }
            return usageStates.Max();
        }

        /// <summary>
        /// Gets the target type identifier for this column ("Column" or "CalculatedColumn").
        /// </summary>
        public string GetTargetType()
        {
            return this._columnType.ToString();
        }
    }
}
