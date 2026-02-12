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
        private string _visualTitle;
        private string _visualType;
        private ReportPage _parentPage;
        private HashSet<IDataInput> _dataInputs = new HashSet<IDataInput>();
        private HashSet<Filter> _filters = new HashSet<Filter>();

        public override string Name { get => this._name; } 
        public string Title { get => this._visualTitle; } 
        public string VisualType { get => this._visualType; } 

        public string Type { get { return this._visualType; } }

        private Visual(string visualName, string visualTitle, string visualType, ReportPage parentPage)
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

            var visualTitle = singleVisual["vcObjects"]?["title"]?[0]?["properties"]?["text"]?["expr"]?["Literal"]?["Value"]?.GetValue<string>();
            
            Visual visual = new Visual(visualName, visualTitle, visualTypeStr, parentPage);
            
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
                    LoadArtifactsFromJson(prototypeQueryNode, visual);
            }
            return visual;
        }

        private static HashSet<DatabaseArtifact> ExtractArtifactsFromSelectNode(JsonNode expressionNode, string artifactType, Dictionary<string, string> tableNameCorrespondance, IPowerBILeafNode source, SemanticModel semanticModel)
        {
            HashSet<DatabaseArtifact> artifacts = new HashSet<DatabaseArtifact>();
            if (expressionNode.TryFindNodesByPropertyName(artifactType, out HashSet<JsonNode> artifactNodes))
            {
                foreach (JsonNode artifactNode in artifactNodes)
                {
                    string nodeName = artifactType == "Hierarchy" ? "Hierarchy" : "Property";
                    string artifactName = artifactNode[nodeName].ToString();
                    string tableName = tableNameCorrespondance[artifactNode["Expression"]["SourceRef"]["Source"].ToString()];
                    if (!semanticModel.TryFindArtifactByName(artifactType, artifactName, tableName, out DatabaseArtifact artifact))
                        AnalysisWarningPublisher.GetInstance().PublishWarning(new MissingArtifactWarning(source, artifactType, artifactName, tableName));
                    else
                        artifacts.Add(artifact);
                }
            }
            return artifacts;
        }

        private static void LoadArtifactsFromJson(JsonNode prototypeQueryNode, Visual visual)
        {
            JsonNode selectNodes = prototypeQueryNode["Select"] ?? throw new ArgumentException("Visual node has no config.singleVisual.prototypeQuery.Select subnode");
            JsonNode fromNodes = prototypeQueryNode["From"] ?? throw new ArgumentException("Visual node has no config.singleVisual.prototypeQuery.From subnode");
            SemanticModel semanticModel = visual.GetReportPage().GetPowerBIReport().GetSemanticModel();

            Dictionary<string, string> tableNameCorrespondance = new Dictionary<string, string>();
            foreach (var fromNode in fromNodes.AsArray())
                tableNameCorrespondance.Add(fromNode["Name"].ToString(), fromNode["Entity"].ToString());

            foreach (var node in selectNodes.AsArray())
            {
                var artifacts = new HashSet<DatabaseArtifact>();
                artifacts.UnionWith(ExtractArtifactsFromSelectNode(node, "Column", tableNameCorrespondance, visual, semanticModel));
                artifacts.UnionWith(ExtractArtifactsFromSelectNode(node, "Measure", tableNameCorrespondance, visual, semanticModel));
                artifacts.UnionWith(ExtractArtifactsFromSelectNode(node, "Hierarchy", tableNameCorrespondance, visual, semanticModel));

                foreach (var artifact in artifacts)
                {
                    visual.AddDataInput(artifact);
                    artifact.AddDependent(visual);
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
