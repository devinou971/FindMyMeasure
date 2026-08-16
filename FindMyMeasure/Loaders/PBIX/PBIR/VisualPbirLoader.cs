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

namespace FindMyMeasure.Loaders.PBIX.PBIR
{
    internal class VisualPbirLoader
    {
        internal static Visual LoadVisualFromJson(JsonNode visualNode, ReportPage parentPage, bool analyseHiddenVisuals)
        {
            JsonNode visualNameNode = visualNode["name"] ?? throw new ArgumentException("Visual node has no name");

            string visualName = visualNameNode.ToString();
            JsonNode singleVisual = visualNode["visual"];

            if (singleVisual is null)
            {
                // This can happend for visual groups, which are not analysed for now
                return null;
            }

            string visualTypeStr = singleVisual["visualType"].ToString();
            JsonNode displayNode = singleVisual["isHidden"];
            if (!analyseHiddenVisuals && displayNode != null && displayNode.GetValue<bool>())
                return null;

            var visualTitle = singleVisual["visualContainerObjects"]?["title"]?[0]?["properties"]?["text"]?["expr"]?["Literal"]?["Value"]?.GetValue<string>();

            Visual visual = new Visual(visualName, visualTitle, visualTypeStr, parentPage);

            if (visualNode["filterConfig"] != null)
            {
                var filters = FilterPbirLoader.LoadMultipleFiltersFromJson(visualNode["filterConfig"], visual);
                foreach (var filter in filters)
                    visual.AddFilter(filter);
            }

            if (singleVisual != null)
            {
                JsonNode queryNode = singleVisual["query"];
                JsonNode objectsNode = singleVisual["objects"];
                if (queryNode != null && queryNode["queryState"] != null)
                    LoadArtifactsFromNode(queryNode["queryState"], visual);
                if (objectsNode != null)
                    LoadArtifactsFromNode(objectsNode, visual);
            }
            return visual;
        }


        private static void LoadArtifactsFromNode(JsonNode node, Visual visual)
        {
            SemanticModel semanticModel = visual.GetReportPage().GetPowerBIReport().GetSemanticModel();

            var artifacts = new HashSet<DatabaseArtifact>();
            artifacts.UnionWith(ExtractArtifactsFromNode(node, "Column", visual, semanticModel));
            artifacts.UnionWith(ExtractArtifactsFromNode(node, "Measure", visual, semanticModel));
            artifacts.UnionWith(ExtractArtifactsFromNode(node, "Hierarchy", visual, semanticModel));
                        
            foreach (var artifact in artifacts)
            {
                visual.AddDataInput(artifact);
                artifact.AddDependent(visual);
            }

        }

        private static HashSet<DatabaseArtifact> ExtractArtifactsFromNode(JsonNode node, string artifactType, IPowerBILeafNode source, SemanticModel semanticModel)
        {
            HashSet<DatabaseArtifact> artifacts = new HashSet<DatabaseArtifact>();
            if (node.TryFindNodesByPropertyName(artifactType, out HashSet<JsonNode> artifactNodes))
            {
                foreach (JsonNode artifactNode in artifactNodes)
                {
                    var nameOfTheNodeContainingTheArtifactName = artifactType == "Hierarchy" ? "Hierarchy" : "Property";
                    if (artifactNode["Expression"].AsObject().ContainsKey("SourceRef"))
                    {
                        string artifactName = artifactNode[nameOfTheNodeContainingTheArtifactName].ToString();
                        string tableName = null;
                        // There are 2 ways an artifact table can be represented: [ArtifactType].SourceRef.Entity and [ArtifactType].SourceRef.Source.
                        if (artifactNode["Expression"]["SourceRef"]["Entity"] != null)
                        {
                            // This is the way to interprete [ArtifactType].SourceRef.Entity
                            tableName = artifactNode["Expression"]["SourceRef"]["Entity"].ToString();
                            
                        }
                        else if(artifactNode["Expression"]["SourceRef"]["Source"] != null)
                        {
                            // This is the way to interprete [ArtifactType].SourceRef.Source
                            if (artifactNode.Parent != null && artifactNode.Parent.Parent != null && artifactNode.Parent.Parent.Parent != null && artifactNode.Parent.Parent.Parent["From"]!= null)
                            {
                                var fromNode = artifactNode.Parent.Parent.Parent["From"];
                                string tableId = artifactNode["Expression"]["SourceRef"]["Source"].GetValue<string>();
                                var table = fromNode.AsArray().FirstOrDefault((x) =>
                                {
                                    return x["Name"].GetValue<string>() == tableId;
                                });
                                tableName = table["Entity"].GetValue<string>();

                                if (!semanticModel.TryFindArtifactByName(artifactName, tableName, out DatabaseArtifact artifact))
                                    AnalysisWarningPublisher.GetInstance().PublishWarning(new MissingArtifactWarning(source, artifactType, artifactName, tableName));
                                else
                                    artifacts.Add(artifact);
                            }
                        }
                        if (tableName != null)
                        {
                            if (!semanticModel.TryFindArtifactByName(artifactName, tableName, out DatabaseArtifact artifact))
                                AnalysisWarningPublisher.GetInstance().PublishWarning(new MissingArtifactWarning(source, artifactType, artifactName, tableName));
                            else
                                artifacts.Add(artifact);
                        }
                    }
                }
            }
            return artifacts;
        }
    }
}
