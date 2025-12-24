using FindMyMeasure.Enums;
using FindMyMeasure.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FindMyMeasure.Database
{
    /// <summary>
    /// Represents a measure in a semantic model with dependency tracking.
    /// Measures can be used in visuals, filters, relationships, and other measures.
    /// </summary>
    public class Measure : DatabaseArtifact
    {
        /// <summary>
        /// Initializes a new instance of the Measure class.
        /// </summary>
        /// <param name="measureId">The unique identifier for this measure.</param>
        /// <param name="measureName">The name of the measure.</param>
        /// <param name="expression">The DAX expression defining the measure.</param>
        /// <param name="table">The table that contains this measure.</param>
        public Measure(ulong measureId, string measureName, string expression, Table table) 
            : base(measureId, measureName, "Measure", expression, table)
        {
            
        }

        /// <summary>
        /// Gets the hash code for this measure based on its name.
        /// Note: Uses name instead of ID because measure IDs can be 0 in disconnected mode.
        /// </summary>
        public override int GetHashCode()
        {
            return this.Name.GetHashCode();
        }
    }
}
