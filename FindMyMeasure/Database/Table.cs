using FindMyMeasure.Interfaces;
using System;
using System.Collections.Generic;

namespace FindMyMeasure.Database
{
    /// <summary>
    /// Represents a table in a semantic model, containing columns and measures.
    /// </summary>
    public class Table : IEquatable<Table>, IModelReferenceTarget
    {
        private string _name;
        private ulong _tableId;

        private HashSet<Measure> _measures = new HashSet<Measure>();
        private HashSet<Column> _columns = new HashSet<Column>();

        public string Name { get => _name; }
        public ulong TableId { get => _tableId; }

        /// <summary>
        /// Initializes a new instance of the Table class.
        /// </summary>
        /// <param name="name">The name of the table.</param>
        /// <param name="tableId">The unique identifier for the table.</param>
        public Table(string name, ulong tableId)
        {
            this._name = name;
            this._tableId = tableId;
        }

        /// <summary>
        /// Initializes a new instance of the Table class with a default ID (0).
        /// </summary>
        /// <param name="name">The name of the table.</param>
        public Table(string name) : this(name, 0) { }

        /// <summary>
        /// Adds a measure to this table.
        /// </summary>
        /// <param name="measure">The measure to add.</param>
        /// <returns>True if the measure was added; false if it already existed.</returns>
        public bool AddMeasure(Measure measure)
        {
            return this._measures.Add(measure);
        }

        /// <summary>
        /// Adds a column to this table.
        /// </summary>
        /// <param name="column">The column to add.</param>
        /// <returns>True if the column was added; false if it already existed.</returns>
        public bool AddColumn(Column column)
        {
            return this._columns.Add(column);
        }

        /// <summary>
        /// Gets the hash code for this table based on its name.
        /// </summary>
        public override int GetHashCode()
        {
            return ("Table:" + this.Name).GetHashCode();
        }

        /// <summary>
        /// Determines whether this table is equal to another table.
        /// Two tables are equal if they have the same name and table ID.
        /// </summary>
        /// <param name="other">The table to compare with.</param>
        /// <returns>True if the tables are equal; otherwise, false.</returns>
        public bool Equals(Table other)
        {
            return other.Name == this.Name && other.TableId == this.TableId;
        }

        /// <summary>
        /// Gets the target type identifier for this object ("Table").
        /// </summary>
        public string GetTargetType()
        {
            return "Table";
        }
    }
}
