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


        private static void LoadArtifactsFromNode(JsonNode objects, Visual visual)
        {
            SemanticModel semanticModel = visual.GetReportPage().GetPowerBIReport().GetSemanticModel();

            var artifacts = new HashSet<DatabaseArtifact>();
            artifacts.UnionWith(ExtractArtifactsFromNode(objects, "Column", visual, semanticModel));
            artifacts.UnionWith(ExtractArtifactsFromNode(objects, "Measure", visual, semanticModel));
            artifacts.UnionWith(ExtractArtifactsFromNode(objects, "Hierarchy", visual, semanticModel));
                        
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
                    var nodeName = artifactType == "Hierarchy" ? "Hierarchy" : "Property";
                    string artifactName = artifactNode[nodeName].ToString();
                    if (artifactNode["Expression"].AsObject().ContainsKey("SourceRef") && artifactNode["Expression"]["SourceRef"]["Entity"] != null)
                    {
                        // There are 2 ways an artifact table can be represented: [ArtifactType].SourceRef.Entity and [ArtifactType].SourceRef.Source.
                        // the latter needs extra data from a "From" node to correctly interprete it. However, only [ArtifactType].SourceRef.Entity is needed
                        // as the [ArtifactType].SourceRef.Source is already present in the prototypeQuery.
                        string tableName = artifactNode["Expression"]["SourceRef"]["Entity"].ToString();
                        if (!semanticModel.TryFindArtifactByName(artifactName, tableName, out DatabaseArtifact artifact))
                            AnalysisWarningPublisher.GetInstance().PublishWarning(new MissingArtifactWarning(source, artifactType, artifactName, tableName));
                        else
                            artifacts.Add(artifact);
                    }
                }
            }
            return artifacts;
        }
    }
}
