using FindMyMeasure.Database;
using FindMyMeasure.Interfaces;
using FindMyMeasure.PowerBI;
using FindMyMeasure.WarningClasses;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace FindMyMeasure.Loaders.PBIX.Legacy
{
    internal class FilterLoader
    {
        /// <summary>
        /// Loads a filter from a JSON object and extracts column and measure references.
        /// </summary>
        /// <param name="filterObject">The JSON filter object to parse.</param>
        /// <param name="parent">The parent node containing this filter.</param>
        /// <param name="semanticModel">The semantic model to resolve column and measure references.</param>
        /// <returns>A new Filter instance with all dependencies resolved.</returns>
        /// <exception cref="ArgumentNullException">Thrown when filterObject is null.</exception>
        internal static Filter LoadFilterFromJson(JsonObject filterObject, PowerBINode parent, SemanticModel semanticModel)
        {
            if (filterObject is null)
                throw new ArgumentNullException(nameof(filterObject), "the filter node is null.");

            // Extract filter expressions - they can be in filterExpressionMetadata or directly in expression
            IEnumerable<JsonNode> expressionNodes;
            if (filterObject.ContainsKey("filterExpressionMetadata"))
                expressionNodes = filterObject["filterExpressionMetadata"]["expressions"].AsArray();
            else
                expressionNodes = new List<JsonNode>() { filterObject["expression"] };

            string filterName = null;
            if (filterObject["name"] != null)
                filterName = filterObject["name"].GetValue<string>();
            else
                filterName = parent.Name; // filters may not have names if they are visual filters.

            Filter filter = new Filter(filterName, parent, filterObject.ToString());

            // Process each expression in the filter
            foreach (var expressionNode in expressionNodes)
            {
                var artifacts = new HashSet<DatabaseArtifact>();
                artifacts.UnionWith(ExtractArtifactsFromExpressionNode(expressionNode, "Column", filter, semanticModel));
                artifacts.UnionWith(ExtractArtifactsFromExpressionNode(expressionNode, "Measure", filter, semanticModel));
                artifacts.UnionWith(ExtractArtifactsFromExpressionNode(expressionNode, "Hierarchy", filter, semanticModel));

                foreach (var artifact in artifacts)
                {
                    filter.AddDataInput(artifact);
                    artifact.AddDependent(filter);
                }
            }
            return filter;
        }

        /// <summary>
        /// Loads a filter from a JSON object at the report scope.
        /// </summary>
        /// <param name="filterNode">The JSON filter object.</param>
        /// <param name="scopePowerBIReport">The report scope for resolving references.</param>
        /// <returns>A new Filter instance.</returns>
        internal static Filter LoadFilterFromJson(JsonObject filterNode, PowerBIReport scopePowerBIReport)
        {
            return LoadFilterFromJson(filterNode, scopePowerBIReport, scopePowerBIReport.GetSemanticModel());
        }

        /// <summary>
        /// Loads a filter from a JSON object at the report page scope.
        /// </summary>
        /// <param name="filterNode">The JSON filter object.</param>
        /// <param name="scopeReportPage">The report page scope for resolving references.</param>
        /// <returns>A new Filter instance.</returns>
        internal static Filter LoadFilterFromJson(JsonObject filterNode, ReportPage scopeReportPage)
        {
            return LoadFilterFromJson(filterNode, scopeReportPage, scopeReportPage.GetPowerBIReport().GetSemanticModel());
        }

        /// <summary>
        /// Loads a filter from a JSON object at the visual scope.
        /// </summary>
        /// <param name="filterNode">The JSON filter object.</param>
        /// <param name="scopeVisual">The visual scope for resolving references.</param>
        /// <returns>A new Filter instance.</returns>
        internal static Filter LoadFilterFromJson(JsonObject filterNode, Visual scopeVisual)
        {
            return LoadFilterFromJson(filterNode, scopeVisual, scopeVisual.GetReportPage().GetPowerBIReport().GetSemanticModel());
        }

        /// <summary>
        /// Loads multiple filters from a JSON node collection.
        /// </summary>
        /// <param name="filtersNode">The JSON node containing filter data (serialized as a string).</param>
        /// <param name="scope">The scope node (PowerBIReport, ReportPage, or Visual) for resolving references.</param>
        /// <returns>A HashSet of loaded Filter instances.</returns>
        internal static HashSet<Filter> LoadMultipleFiltersFromJson(JsonNode filtersNode, PowerBINode scope)
        {
            HashSet<Filter> filters = new HashSet<Filter>();
            if (filtersNode != null)
            {
                // Deserialize the filters JSON string into an array
                JsonNode filtersArray = JsonNode.Parse(filtersNode.GetValue<string>());
                foreach (var filterNode in filtersArray.AsArray())
                {
                    if (filterNode.GetValueKind() == JsonValueKind.Object)
                    {
                        // Load each filter, passing the appropriate scope to resolve references
                        if (scope is PowerBIReport report)
                            filters.Add(LoadFilterFromJson(filterNode.AsObject(), scope, report.GetSemanticModel()));
                        else if (scope is ReportPage reportPage)
                            filters.Add(LoadFilterFromJson(filterNode.AsObject(), scope, reportPage.GetPowerBIReport().GetSemanticModel()));
                        else if (scope is Visual visual)
                            filters.Add(LoadFilterFromJson(filterNode.AsObject(), scope, visual.GetReportPage().GetPowerBIReport().GetSemanticModel()));
                    }
                }
            }
            return filters;
        }

        internal static HashSet<DatabaseArtifact> ExtractArtifactsFromExpressionNode(JsonNode expressionNode, string artifactType, IPowerBILeafNode source, SemanticModel semanticModel)
        {
            HashSet<DatabaseArtifact> artifacts = new HashSet<DatabaseArtifact>();

            if (expressionNode.TryFindNodesByPropertyName(artifactType, out HashSet<JsonNode> artifactNodes))
            {
                foreach (JsonNode artifactNode in artifactNodes)
                {
                    string nodeName = artifactType == "Hierarchy" ? "Hierarchy" : "Property";
                    string artifactName = artifactNode[nodeName].ToString();
                    string tableName = artifactNode["Expression"]["SourceRef"]["Entity"].ToString();
                    if (!semanticModel.TryFindArtifactByName(artifactName, tableName, out DatabaseArtifact artifact))
                        AnalysisWarningPublisher.GetInstance().PublishWarning(new MissingArtifactWarning(source, artifactType, artifactName, tableName));
                    else
                        artifacts.Add(artifact);
                }
            }
            return artifacts;
        }
    }
}
