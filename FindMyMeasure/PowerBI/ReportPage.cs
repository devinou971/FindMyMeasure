using System;
using System.Collections.Generic;
using System.Text.Json.Nodes;

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

        private ReportPage(string name, string displayName, PowerBIReport powerBIReport)
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

        public override int GetHashCode()
        {
            return ("ReportPage:" + this._parentReport.Name + this._name).GetHashCode();
        }

        public HashSet<Visual> GetVisuals() => this._visuals;
        public HashSet<Filter> GetFilters() => this._filters;

        public static ReportPage LoadFromJson(JsonObject pageNode, PowerBIReport powerBIReport, bool analyseHiddenPages, bool analyseHiddenVisuals)
        {
            if (!pageNode.ContainsKey("config"))
                throw new ArgumentException("Passed page node doesn't contain any \"config\" subnode");
            if (!pageNode.ContainsKey("visualContainers"))
                throw new ArgumentException("Passed page node doesn't contain any \"visualContainers\" subnode");

            JsonNode configNode = JsonNode.Parse(pageNode["config"].GetValue<string>());
            if (!analyseHiddenPages)
            {
                if (!(configNode["visibility"] is null) && configNode["visibility"].ToString() == "1")
                    return null;
            }

            JsonNode visualsNode = pageNode["visualContainers"];
            ReportPage reportPage = new ReportPage(pageNode["name"].ToString(), pageNode["displayName"].ToString(), powerBIReport);

            foreach (JsonNode visualNode in visualsNode.AsArray())
            {
                JsonNode visualConfigNode = JsonNode.Parse(visualNode["config"].GetValue<string>());

                if (visualNode != null && visualConfigNode != null /*&& visualConfigNode["singleVisual"] != null*/)
                {
                    Visual visual = Visual.LoadFromJson(visualNode, reportPage, analyseHiddenVisuals);
                    if(visual != null )
                        reportPage.AddVisual(visual);
                }
            }

            var filters = Filter.LoadMultipleFiltersFromJson(pageNode["filters"], reportPage);
            reportPage._filters.UnionWith(filters);

            return reportPage;
        }


        public PowerBIReport GetPowerBIReport() => this._parentReport;
    }
}
