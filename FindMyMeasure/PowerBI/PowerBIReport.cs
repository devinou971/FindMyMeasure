using FindMyMeasure.Database;
using System.Collections.Generic;

namespace FindMyMeasure.PowerBI
{
    /// <summary>
    /// Represents a PowerBI report file (.pbix) with its pages, visuals, and filters.
    /// Also tracks the semantic model it connects to.
    /// </summary>
    public class PowerBIReport : PowerBINode
    {
        private string _name;
        private string _path;
        private HashSet<ReportPage> _pages = new HashSet<ReportPage>();
        private HashSet<Filter> _filters = new HashSet<Filter>();
        private SemanticModel _semanticModel;

        /// <summary>
        /// Gets the name of this report (derived from the .pbix filename).
        /// </summary>
        public override string Name { get => this._name; }

        /// <summary>
        /// Gets the file path to this report's .pbix file.
        /// </summary>
        public string Path { get => this._path; }

        /// <summary>
        /// Gets the semantic model that this report connects to.
        /// </summary>
        public SemanticModel SemanticModel { get => this._semanticModel; }

        /// <summary>
        /// Initializes a new instance of the PowerBIReport class.
        /// </summary>
        /// <param name="name">The report name.</param>
        /// <param name="path">The file path to the .pbix file.</param>
        /// <param name="semanticModel">The semantic model this report uses.</param>
        internal PowerBIReport(string name, string path, SemanticModel semanticModel)
        {
            this._name = name;
            this._path = path;
            this._semanticModel = semanticModel;
        }

        internal bool AddFilter(Filter filter)
        {
            return this._filters.Add(filter);
        }

        internal bool AddReportPage(ReportPage reportPage)
        {
            return this._pages.Add(reportPage);
        }
        

        /// <summary>
        /// Gets the semantic model associated with this report.
        /// </summary>
        public SemanticModel GetSemanticModel() => _semanticModel;
        
        /// <summary>
        /// Gets all report pages in this report.
        /// </summary>
        public HashSet<ReportPage> GetReportPages() => this._pages;

        /// <summary>
        /// Gets all visuals across all pages in this report.
        /// </summary>
        /// <returns>A HashSet of all Visual objects in the report.</returns>
        public HashSet<Visual> GetVisuals()
        {
            HashSet<Visual> visuals = new HashSet<Visual>();
            foreach(var page in this._pages)
                visuals.UnionWith(page.GetVisuals());
            return visuals;
        }

        /// <summary>
        /// Gets all filters across all pages and visuals in this report.
        /// </summary>
        /// <returns>A HashSet of all Filter objects in the report.</returns>
        public HashSet<Filter> GetFilters()
        {
            HashSet<Filter> filters = new HashSet<Filter>() ;
            // Report-level filters
            filters.UnionWith(this._filters);
            // Page-level and visual-level filters
            foreach(var page in this._pages)
            {
                filters.UnionWith(page.GetFilters());
                foreach(var visual in page.GetVisuals())
                    filters.UnionWith(visual.GetFilters());
            }
            return filters;
        }

        public override bool Equals(object obj)
        {
            if(obj is PowerBIReport report)
                return report._path == this._path;
            return false;
        }

        public override int GetHashCode()
        {
            return _path.GetHashCode();
        }
    }
}
