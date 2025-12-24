using FindMyMeasure.Enums;
using FindMyMeasure.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FindMyMeasure.Database
{
    /// <summary>
    /// Represents a column in a semantic model table, including calculated columns.
    /// </summary>
    public class Column : DatabaseArtifact
    {
        /// <summary>
        /// Initializes a new instance of the Column class.
        /// </summary>
        /// <param name="columnId">The unique identifier for this column.</param>
        /// <param name="columnName">The name of the column.</param>
        /// <param name="expression">The DAX expression for calculated columns, or null for regular columns.</param>
        /// <param name="table">The table that contains this column.</param>
        public Column(ulong columnId, string columnName, string expression, Table table) 
            : base(columnId, columnName, string.IsNullOrEmpty(expression) ? "Column" : "CalculatedColumn", table)
        {
        }

        /// <summary>
        /// Initializes a new instance of the Column class for a regular column.
        /// </summary>
        /// <param name="columnId">The unique identifier for this column.</param>
        /// <param name="columnName">The name of the column.</param>
        /// <param name="table">The table that contains this column.</param>
        public Column(ulong columnId, string columnName, Table table) 
            : this(columnId, columnName, null, table) 
        { }


        /// <summary>
        /// Gets the hash code for this column based on its table and name.
        /// </summary>
        public override int GetHashCode()
        {
            return (this._table.ToString() + "." + this._name).GetHashCode();
        }

        /// <summary>
        /// Gets the target type identifier for this column ("Column" or "CalculatedColumn").
        /// </summary>
        public override string GetTargetType()
        {
            return this._type.ToString();
        }
    }
}
