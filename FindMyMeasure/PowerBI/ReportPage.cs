using System.Collections.Generic;

namespace FindMyMeasure.PowerBI
{
    public class ReportPage : PowerBINode
    {
        private string _name;
        private string _displayName;
        private PowerBIReport _parentReport;

        private HashSet<Filter> _filters = new HashSet<Filter>();
        private HashSet<Visual> _visuals = new HashSet<Visual>();

        public override string Name { get => this._displayName; }
        internal ReportPage(string name, string displayName, PowerBIReport powerBIReport)
        {
            this._name = name;
            this._displayName = displayName;
            this._parentReport = powerBIReport;
        }

        public bool AddFilter(Filter filter)
        {
            return this._filters.Add(filter);
        }

        public bool AddVisual(Visual visual)
        {
            return this._visuals.Add(visual);
        }

        public HashSet<Visual> GetVisuals() => this._visuals;

        public HashSet<Filter> GetFilters() => this._filters;

        public PowerBIReport GetPowerBIReport() => this._parentReport;

        public override bool Equals(object obj)
        {
            if(obj is ReportPage reportPage)
                return reportPage._name == this._name && reportPage._displayName == this._displayName && reportPage._parentReport == this._parentReport;
            return false;
        }

        public override int GetHashCode()
        {
            return new { this._name, this._displayName, this._parentReport.Path }.GetHashCode();
        }
    }
}
