using FindMyMeasure.Database;
using FindMyMeasure.Interfaces;
using FindMyMeasure.WarningClasses;
using System;
using System.Collections.Generic;
using System.Text.Json.Nodes;

namespace FindMyMeasure.PowerBI
{
    public class Visual : PowerBINode, IPowerBILeafNode, IModelReferenceTarget
    {

        private string _name;
        private string _visualType;
        private ReportPage _parentPage;
        private HashSet<IDataInput> _dataInputs = new HashSet<IDataInput>();
        private HashSet<Filter> _filters = new HashSet<Filter>();

        public override string Name => this._name;
        public string VisualType => this._visualType;

        private Visual(string name, String visualType, ReportPage parentPage)
        {
            this._name = name;
            this._visualType = visualType;
            this._parentPage = parentPage;
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

        public override int GetHashCode()
        {
            return (_parentPage.Name + this._visualType + this._name).GetHashCode();
        }

        public static Visual LoadFromJson(JsonNode visualNode, ReportPage parentPage, bool analyseHiddenVisuals) // TODO Refactoring : Use JsonObject instead of JsonNode when loading visuals
        {
            if (visualNode["config"] == null)
                return null;

            string configString = visualNode["config"].GetValue<string>();
            JsonNode configNode = JsonNode.Parse(configString);

            JsonNode visualNameNode = configNode["name"] ?? throw new ArgumentException("Visual node has no name");
            
            string visualName = visualNameNode.ToString();
            JsonNode singleVisual = configNode["singleVisual"];

            if (singleVisual is null)
            {
                // This can happend for visual groups, which are not analysed for now
                return null; 
            }
            string visualTypeStr = singleVisual["visualType"].ToString();
            JsonNode displayNode = singleVisual["display"];
            if(!analyseHiddenVisuals && displayNode != null && displayNode["mode"] != null && displayNode["mode"].GetValue<String>() == "hidden")
                return null;

            Visual visual = new Visual(visualName, visualTypeStr, parentPage);
            
            if (visualNode["filters"] != null)
            {
                var filters = Filter.LoadMultipleFiltersFromJson(visualNode["filters"], visual);
                visual._filters.UnionWith(filters);
            }

            JsonNode singleVisualNode = configNode["singleVisual"];
            if (singleVisualNode != null)
            {
                JsonNode prototypeQueryNode = singleVisualNode["prototypeQuery"];
                if (prototypeQueryNode != null)
                    LoadMeasuresNColumnsFromJson(prototypeQueryNode, visual);
            }
            return visual;
        }

        private static void LoadMeasuresNColumnsFromJson(JsonNode prototypeQueryNode, Visual visual)
        {
            JsonNode selectNodes = prototypeQueryNode["Select"] ?? throw new ArgumentException("Visual node has no config.singleVisual.prototypeQuery.Select subnode");
            JsonNode fromNodes = prototypeQueryNode["From"] ?? throw new ArgumentException("Visual node has no config.singleVisual.prototypeQuery.From subnode");
            SemanticModel semanticModel = visual.GetReportPage().GetPowerBIReport().GetSemanticModel();

            Dictionary<string, string> tableNameCorrespondance = new Dictionary<string, string>();
            foreach (var fromNode in fromNodes.AsArray())
                tableNameCorrespondance.Add(fromNode["Name"].ToString(), fromNode["Entity"].ToString());

            foreach (var node in selectNodes.AsArray())
            {
                HashSet<JsonNode> columnNodes;
                HashSet<JsonNode> measureNodes;

                if (node.TryFindNodesByPropertyName("Measure", out measureNodes))
                {
                    foreach (JsonNode measureNode in measureNodes)
                    {
                        string measureName = measureNode["Property"].ToString();
                        string tableName = tableNameCorrespondance[measureNode["Expression"]["SourceRef"]["Source"].ToString()];

                        if (! semanticModel.TryFindMeasureByName(measureName, out Measure measure))
                        {
                            AnalysisWarningPublisher.GetInstance().PublishWarning(new MissingMeasureWarning(visual, measureName, tableName));
                        }
                        else
                        {
                            visual.AddDataInput(measure);
                            measure.AddDependent(visual);
                        }
                    }
                }
                else if (node.TryFindNodesByPropertyName("Column", out columnNodes))
                {
                    foreach (JsonNode columnNode in columnNodes)
                    {
                        string columnName = columnNode["Property"].ToString();
                        string tableName = tableNameCorrespondance[columnNode["Expression"]["SourceRef"]["Source"].ToString()];
                        if (!semanticModel.TryFindColumnByName(columnName, tableName, out Column column))
                        {
                            AnalysisWarningPublisher.GetInstance().PublishWarning(new MissingColumnWarning(visual, columnName, tableName));
                        }
                        else
                        {
                            visual.AddDataInput(column);
                            column.AddDependent(visual);
                        }
                    }
                }
            }
        }

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

        public string GetTargetType()
        {
            return this.VisualType;
        }
    }
}
