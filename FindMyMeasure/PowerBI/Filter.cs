using FindMyMeasure.Interfaces;
using System.Collections.Generic;

namespace FindMyMeasure.PowerBI
{
    /// <summary>
    /// Represents a filter in a PowerBI report, page, or visual.
    /// Filters can reference columns and measures from the semantic model and track their dependencies.
    /// </summary>
    public class Filter : PowerBINode, IPowerBILeafNode, IModelReferenceTarget
    {
        private PowerBINode _parent;
        private string _conditions = "";
        private static int NextId = 0;
        private int _id;

        public string Type { get {
                switch (this._parent)
                {
                    case PowerBIReport _:
                        return "PowerBI Report Filter";
                    case ReportPage _:
                        return "PowerBI Report Page Filter";
                    case Visual _:
                        return "PowerBI Visual Filter";
                    default:
                        return "PowerBI Filter";
                };
            
            } }

        private HashSet<IDataInput> dataInputs = new HashSet<IDataInput>();

        public override string Name { get {
                switch (this._parent)
                {
                    case PowerBIReport _:
                        return $"Report Filter '{_id}'";
                    case ReportPage _:
                        return $"Page Filter '{_id}'";
                    case Visual _:
                        return $"Visual Filter '{_id}'";
                    default:
                        return $"Filter '{_id}'";
                }
            } }

        /// <summary>
        /// Initializes a new instance of the Filter class.
        /// </summary>
        /// <param name="parent">The parent node (PowerBIReport, ReportPage, or Visual).</param>
        /// <param name="conditions">The filter conditions as a JSON string.</param>
        internal Filter(PowerBINode parent, string conditions)
        {
            this._parent = parent;
            this._conditions = conditions;
            this._id = NextId++;
        }

        /// <summary>
        /// Gets the parent node of this filter.
        /// </summary>
        /// <returns>The parent PowerBINode.</returns>
        public PowerBINode GetParent()
        {
            return this._parent;
        }
        
        /// <summary>
        /// Adds a data input (column or measure) that this filter depends on.
        /// </summary>
        /// <param name="dataInput">The column or measure this filter references.</param>
        /// <returns>True if the input was added, false if it already existed in the set.</returns>
        public bool AddDataInput(IDataInput dataInput)
        {
            return this.dataInputs.Add(dataInput);
        }

        /// <summary>
        /// Gets all data inputs (columns and measures) that this filter depends on.
        /// </summary>
        public HashSet<IDataInput> GetDataInputs()
        {
            return this.dataInputs;
        }

        /// <summary>
        /// Returns a human-readable description of the filter and its parent context.
        /// </summary>
        public override string ToString()
        {
            switch (this._parent)
            {
                case PowerBIReport _:
                    return $"Report Filter '{_id}' from report '{this._parent.Name}'";
                case ReportPage _:
                    return $"Page Filter '{_id}' from page '{this._parent.Name}'";
                case Visual _:
                    return $"Visual Filter '{_id}' from visual '{this._parent.Name}' in page {((Visual)this._parent).GetReportPage().Name}";
                default:
                    return $"Filter '{_id}' from '{this._parent.Name}'";
            }
        }

        public override bool Equals(object obj)
        {
            if (obj is Filter f)
                return f._id == this._id && f._parent == this._parent;
            return false;
        }

        public override int GetHashCode()
        {
            return new { this._id, this._parent }.GetHashCode();
        }
    }
}
