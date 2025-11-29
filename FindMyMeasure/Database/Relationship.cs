using FindMyMeasure.Interfaces;
using System;

namespace FindMyMeasure.Database
{
    /// <summary>
    /// Represents a relationship between two columns in a semantic model.
    /// </summary>
    public class Relationship : IModelReferenceTarget, IEquatable<Relationship>
    {
        private Column _fromColumn;
        private Column _toColumn;
        private string _name;
        private bool _isActive;

        /// <summary>
        /// Gets the name of this relationship.
        /// </summary>
        public string Name => _name; 

        /// <summary>
        /// Gets the column that this relationship originates from (the "many" side in a many-to-one relationship).
        /// </summary>
        public Column FromColumn => _fromColumn;

        /// <summary>
        /// Gets the column that this relationship points to (the "one" side in a many-to-one relationship).
        /// </summary>
        public Column ToColumn  => _toColumn;

        /// <summary>
        /// Gets a value indicating whether this relationship is active.
        /// </summary>
        public bool IsActive => _isActive;

        /// <summary>
        /// Initializes a new instance of the Relationship class.
        /// </summary>
        /// <param name="relationshipName">The name of the relationship.</param>
        /// <param name="fromColumn">The column where the relationship originates.</param>
        /// <param name="toColumn">The column where the relationship points to.</param>
        /// <param name="isActive">Whether this relationship is active.</param>
        public Relationship(string relationshipName, Column fromColumn, Column toColumn, bool isActive)
        {
            this._fromColumn = fromColumn;
            this._toColumn = toColumn;
            this._name = relationshipName;
            this._isActive = isActive;
        }

        /// <summary>
        /// Initializes a new instance of the Relationship class with an active relationship (default).
        /// </summary>
        /// <param name="relationshipName">The name of the relationship.</param>
        /// <param name="fromColumn">The column where the relationship originates.</param>
        /// <param name="toColumn">The column where the relationship points to.</param>
        public Relationship(string relationshipName, Column fromColumn, Column toColumn) : this(relationshipName, fromColumn, toColumn, true) { }

        /// <summary>
        /// Determines whether this relationship is equal to another relationship.
        /// Two relationships are equal if they connect the same columns.
        /// </summary>
        /// <param name="other">The relationship to compare with.</param>
        /// <returns>True if the relationships connect the same columns; otherwise, false.</returns>
        public bool Equals(Relationship other)
        {
            return other._fromColumn == this._fromColumn && other._toColumn == this._toColumn;
        }

        /// <summary>
        /// Gets the hash code for this relationship based on its name.
        /// </summary>
        public override int GetHashCode()
        {
            return this._name.GetHashCode();
        }

        /// <summary>
        /// Gets the target type identifier for this object.
        /// </summary>
        public string GetTargetType()
        {
            return "Relationship";
        }
    }
}
