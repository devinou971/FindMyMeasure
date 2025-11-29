namespace FindMyMeasure.PowerBI
{
    /// <summary>
    /// Abstract base class representing a node in the PowerBI object hierarchy.
    /// This class forms the tree structure of PowerBI objects from reports down to columns and measures.
    /// </summary>
    public abstract class PowerBINode
    {
        /// <summary>
        /// Gets the display name of this PowerBI node.
        /// </summary>
        public abstract string Name { get; }

        /// <summary>
        /// Gets the hash code for this node, used for equality comparisons and collection lookups.
        /// </summary>
        public abstract override int GetHashCode();

        /// <summary>
        /// Determines whether the specified object is equal to the current node.
        /// Two PowerBINode objects are considered equal if they have the same hash code.
        /// </summary>
        /// <param name="obj">The object to compare with the current node.</param>
        /// <returns>True if the objects are equal; otherwise, false.</returns>
        public override bool Equals(object obj)
        {
            if (obj is PowerBINode)
            {
                return this.GetHashCode() == ((PowerBINode)obj).GetHashCode();
            }
            return false;
        }

        /// <summary>
        /// Returns a string representation of this node, using its Name property.
        /// </summary>
        public override string ToString()
        {
            return this.Name;
        }
    }
}
