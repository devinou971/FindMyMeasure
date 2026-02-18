using FindMyMeasure.Interfaces;
using System.Collections.Generic;

namespace FindMyMeasure.PowerBI
{
    public class Visual : PowerBINode, IPowerBILeafNode, IModelReferenceTarget
    {

        private string _name;
        private string _visualTitle;
        private string _visualType;
        private ReportPage _parentPage;
        private HashSet<IDataInput> _dataInputs = new HashSet<IDataInput>();
        private HashSet<Filter> _filters = new HashSet<Filter>();

        public override string Name { get => this._name; } 
        public string Title { get => this._visualTitle; } 
        public string VisualType { get => this._visualType; } 

        public string Type { get { return this._visualType; } }

        internal Visual(string visualName, string visualTitle, string visualType, ReportPage parentPage)
        {
            this._name = visualName;
            this._visualType = visualType;
            this._parentPage = parentPage;
            this._visualTitle = visualTitle;
        }

        public bool AddDataInput(IDataInput input)
        {
            return _dataInputs.Add(input);
        }

        public bool AddFilter(Filter filter)
        {
            return this._filters.Add(filter);
        }

        public HashSet<Filter> GetFilters() => this._filters;

        public HashSet<IDataInput> GetDataInputs()
        {
            return _dataInputs;
        }

        public override string ToString()
        {
            return $"{_visualType} : '{_name}' from page '{this._parentPage.Name}' in report '{this._parentPage.GetPowerBIReport().Name}'";
        }

        public ReportPage GetReportPage()
        {
            return this._parentPage;
        }

        public override bool Equals(object obj)
        {
            if(obj is Visual v)
                return v._name == this._name && v._visualType == this._visualType && v._parentPage == this._parentPage;
            return false;
        }

        public override int GetHashCode()
        {
            return new { this.Name, this.VisualType, this._parentPage }.GetHashCode();
        }
    }
}
