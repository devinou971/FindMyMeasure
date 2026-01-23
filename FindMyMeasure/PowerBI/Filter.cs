using FindMyMeasure.Database;
using FindMyMeasure.Interfaces;
using FindMyMeasure.WarningClasses;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace FindMyMeasure.PowerBI
{
    /// <summary>
    /// Represents a filter in a PowerBI report, page, or visual.
    /// Filters can reference columns and measures from the semantic model and track their dependencies.
    /// </summary>
    public class Filter : PowerBINode, IPowerBILeafNode, IModelReferenceTarget
    {
        private PowerBINode _parent;
        private string _conditions = "";
        private static int NextId = 0;
        private int _id;

        public string Type { get {
                switch (this._parent)
                {
                    case PowerBIReport _:
                        return "PowerBI Report Filter";
                    case ReportPage _:
                        return "PowerBI Report Page Filter";
                    case Visual _:
                        return "PowerBI Visual Filter";
                    default:
                        return "PowerBI Filter";
                };
            
            } }

        private HashSet<IDataInput> dataInputs = new HashSet<IDataInput>();

        public override string Name { get {
                switch (this._parent)
                {
                    case PowerBIReport _:
                        return $"Report Filter '{_id}'";
                    case ReportPage _:
                        return $"Page Filter '{_id}'";
                    case Visual _:
                        return $"Visual Filter '{_id}'";
                    default:
                        return $"Filter '{_id}'";
                }
            } }

        /// <summary>
        /// Initializes a new instance of the Filter class.
        /// </summary>
        /// <param name="parent">The parent node (PowerBIReport, ReportPage, or Visual).</param>
        /// <param name="conditions">The filter conditions as a JSON string.</param>
        public Filter(PowerBINode parent, string conditions)
        {
            this._parent = parent;
            this._conditions = conditions;
            this._id = NextId++;
        }

        /// <summary>
        /// Gets the parent node of this filter.
        /// </summary>
        /// <returns>The parent PowerBINode.</returns>
        public PowerBINode GetParent()
        {
            return this._parent;
        }

        private static HashSet<DatabaseArtifact> ExtractArtifactsFromExpressionNode(JsonNode expressionNode, string artifactType, IPowerBILeafNode source, SemanticModel semanticModel)
        {
            HashSet <DatabaseArtifact> artifacts = new HashSet <DatabaseArtifact>();

            if (expressionNode.TryFindNodesByPropertyName(artifactType, out HashSet<JsonNode> artifactNodes))
            {
                foreach (JsonNode artifactNode in artifactNodes)
                {
                    string nodeName = artifactType == "Hierarchy" ? "Hierarchy" : "Property";
                    string artifactName = artifactNode[nodeName].ToString();
                    string tableName = artifactNode["Expression"]["SourceRef"]["Entity"].ToString();
                    if (!semanticModel.TryFindArtifactByName(artifactType, artifactName, tableName, out DatabaseArtifact artifact))
                        AnalysisWarningPublisher.GetInstance().PublishWarning(new MissingArtifactWarning(source, artifactType, artifactName, tableName));
                    else
                        artifacts.Add(artifact);
                }
            }
            return artifacts;
        }

        /// <summary>
        /// Loads a filter from a JSON object and extracts column and measure references.
        /// </summary>
        /// <param name="filterObject">The JSON filter object to parse.</param>
        /// <param name="parent">The parent node containing this filter.</param>
        /// <param name="semanticModel">The semantic model to resolve column and measure references.</param>
        /// <returns>A new Filter instance with all dependencies resolved.</returns>
        /// <exception cref="ArgumentNullException">Thrown when filterObject is null.</exception>
        private static Filter LoadFromJson(JsonObject filterObject, PowerBINode parent, SemanticModel semanticModel)
        {
            if (filterObject is null)
                throw new ArgumentNullException(nameof(filterObject), "the filter node is null.");

            // Extract filter expressions - they can be in filterExpressionMetadata or directly in expression
            IEnumerable<JsonNode> expressionNodes;
            if (filterObject.ContainsKey("filterExpressionMetadata"))
                expressionNodes = filterObject["filterExpressionMetadata"]["expressions"].AsArray();
            else
                expressionNodes = new List<JsonNode>() { filterObject["expression"] };

            Filter filter = new Filter(parent, filterObject.ToString());

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
        public static Filter LoadFromJson(JsonObject filterNode, PowerBIReport scopePowerBIReport)
        {
            return Filter.LoadFromJson(filterNode, scopePowerBIReport, scopePowerBIReport.GetSemanticModel());
        }

        /// <summary>
        /// Loads a filter from a JSON object at the report page scope.
        /// </summary>
        /// <param name="filterNode">The JSON filter object.</param>
        /// <param name="scopeReportPage">The report page scope for resolving references.</param>
        /// <returns>A new Filter instance.</returns>
        public static Filter LoadFromJson(JsonObject filterNode, ReportPage scopeReportPage)
        {
            return Filter.LoadFromJson(filterNode, scopeReportPage, scopeReportPage.GetPowerBIReport().GetSemanticModel());
        }

        /// <summary>
        /// Loads a filter from a JSON object at the visual scope.
        /// </summary>
        /// <param name="filterNode">The JSON filter object.</param>
        /// <param name="scopeVisual">The visual scope for resolving references.</param>
        /// <returns>A new Filter instance.</returns>
        public static Filter LoadFromJson(JsonObject filterNode, Visual scopeVisual)
        {
            return Filter.LoadFromJson(filterNode, scopeVisual, scopeVisual.GetReportPage().GetPowerBIReport().GetSemanticModel());
        }

        /// <summary>
        /// Loads multiple filters from a JSON node collection.
        /// </summary>
        /// <param name="filtersNode">The JSON node containing filter data (serialized as a string).</param>
        /// <param name="scope">The scope node (PowerBIReport, ReportPage, or Visual) for resolving references.</param>
        /// <returns>A HashSet of loaded Filter instances.</returns>
        public static HashSet<Filter> LoadMultipleFiltersFromJson(JsonNode filtersNode, PowerBINode scope)
        {
            HashSet <Filter> filters = new HashSet <Filter>();
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
                            filters.Add(Filter.LoadFromJson(filterNode.AsObject(), scope, report.GetSemanticModel()));
                        else if (scope is ReportPage reportPage)
                            filters.Add(Filter.LoadFromJson(filterNode.AsObject(), scope, reportPage.GetPowerBIReport().GetSemanticModel()));
                        else if (scope is Visual visual)
                            filters.Add(Filter.LoadFromJson(filterNode.AsObject(), scope, visual.GetReportPage().GetPowerBIReport().GetSemanticModel()));
                    }
                }
            }
            return filters;
        }

        /// <summary>
        /// Adds a data input (column or measure) that this filter depends on.
        /// </summary>
        /// <param name="dataInput">The column or measure this filter references.</param>
        /// <returns>True if the input was added, false if it already existed in the set.</returns>
        public bool AddDataInput(IDataInput dataInput)
        {
            return this.dataInputs.Add(dataInput);
        }

        /// <summary>
        /// Gets all data inputs (columns and measures) that this filter depends on.
        /// </summary>
        public HashSet<IDataInput> GetDataInputs()
        {
            return this.dataInputs;
        }

        /// <summary>
        /// Returns a human-readable description of the filter and its parent context.
        /// </summary>
        public override string ToString()
        {
            switch (this._parent)
            {
                case PowerBIReport _:
                    return $"Report Filter '{_id}' from report '{this._parent.Name}'";
                case ReportPage _:
                    return $"Page Filter '{_id}' from page '{this._parent.Name}'";
                case Visual _:
                    return $"Visual Filter '{_id}' from visual '{this._parent.Name}' in page {((Visual)this._parent).GetReportPage().Name}";
                default:
                    return $"Filter '{_id}' from '{this._parent.Name}'";
            }
        }

        public override bool Equals(object obj)
        {
            if (obj is Filter f)
                return f._id == this._id && f._parent == this._parent;
            return false;
        }

        public override int GetHashCode()
        {
            return new { this._id, this._parent }.GetHashCode();
        }
    }
}
