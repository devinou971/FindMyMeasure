using System.Collections.Generic;
using System.Text.Json.Nodes;

namespace FindMyMeasure
{
    public class JsonNodeComparer : IEqualityComparer<JsonNode>
    {
        public bool Equals(JsonNode x, JsonNode y)
        {
            return JsonNode.DeepEquals(x, y);
        }

        public int GetHashCode(JsonNode obj)
        {
            return obj.ToJsonString().GetHashCode();
        }
    }
}
