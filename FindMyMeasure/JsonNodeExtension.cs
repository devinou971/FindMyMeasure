using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace FindMyMeasure
{

    public static class JsonNodeExtension
    {
        /// <summary>
        /// Attempts to locate all <see cref="JsonNode"/> within the current node or its descendants that has the specified
        /// property name.
        /// </summary>
        /// <remarks>This method performs a recursive search through the current node and its descendants.  If the
        /// node is an object, it searches its properties. If the node is an array, it searches  its elements. The search
        /// stops as soon as a matching property name is found.</remarks>
        /// <param name="node">The root <see cref="JsonNode"/> to search from.</param>
        /// <param name="name">The property name to search for.</param>
        /// <param name="result">When this method returns, contains the <see cref="JsonNode"/> with the specified property name, if found; otherwise, <c>null</c>.</param>
        /// <returns><c>true</c> if a node with the specified property name is found; otherwise, <c>false</c>.</returns>
        public static bool TryFindNodesByPropertyName(this JsonNode node, string name, out HashSet<JsonNode> results)
        {
            if (node.GetValueKind() == JsonValueKind.Object && node.Parent is JsonObject)
            {
                if (node.GetPropertyName() == name)
                {
                    results = new HashSet<JsonNode>(new JsonNodeComparer()) { node };
                    return true;
                }

            }
            results = new HashSet<JsonNode>(new JsonNodeComparer());

            if (node.GetValueKind() == JsonValueKind.Object)
            {
                foreach (KeyValuePair<string, JsonNode> pair in node.AsObject())
                {
                    if (pair.Value.TryFindNodesByPropertyName(name, out HashSet<JsonNode> nodes))
                    {
                        results.UnionWith(nodes);
                    }
                }
            }
            else if (node.GetValueKind() == JsonValueKind.Array)
            {
                foreach (JsonNode n in node.AsArray())
                {
                    if (n.TryFindNodesByPropertyName(name, out HashSet<JsonNode> nodes))
                    {
                        results.UnionWith(nodes);
                    }
                }
            }
            return results.Count > 0;
        }
    }
}
