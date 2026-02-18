using FindMyMeasure.Database;
using FindMyMeasure.Interfaces;
using FindMyMeasure.PowerBI;
using FindMyMeasure.WarningClasses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace FindMyMeasure.Loaders
{
    internal class VisualLoader
    {
        internal static Visual LoadVisualFromJson(JsonNode visualNode, ReportPage parentPage, bool analyseHiddenVisuals) // TODO Refactoring : Use JsonObject instead of JsonNode when loading visuals
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
            if (!analyseHiddenVisuals && displayNode != null && displayNode["mode"] != null && displayNode["mode"].GetValue<String>() == "hidden")
                return null;

            var visualTitle = singleVisual["vcObjects"]?["title"]?[0]?["properties"]?["text"]?["expr"]?["Literal"]?["Value"]?.GetValue<string>();

            Visual visual = new Visual(visualName, visualTitle, visualTypeStr, parentPage);

            if (visualNode["filters"] != null)
            {
                var filters = FilterLoader.LoadMultipleFiltersFromJson(visualNode["filters"], visual);// Filter.LoadMultipleFiltersFromJson(visualNode["filters"], visual);
                foreach(var filter in filters) 
                    visual.AddFilter(filter);
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
                    if (!semanticModel.TryFindArtifactByName(artifactName, tableName, out DatabaseArtifact artifact))
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
    }
}
