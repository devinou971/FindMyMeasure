using FindMyMeasure.PowerBI;
using System;
using System.Text.Json.Nodes;

namespace FindMyMeasure.Loaders.PBIX.Legacy
{
    internal class ReportPageLoader
    {
        internal static ReportPage LoadReportPageFromJson(JsonObject pageNode, PowerBIReport powerBIReport, bool analyseHiddenPages, bool analyseHiddenVisuals)
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
                    Visual visual = VisualLoader.LoadVisualFromJson(visualNode, reportPage, analyseHiddenVisuals);
                    if (visual != null)
                        reportPage.AddVisual(visual);
                }
            }

            var filters = FilterLoader.LoadMultipleFiltersFromJson(pageNode["filters"], reportPage);
            foreach (var filter in filters)
                reportPage.AddFilter(filter);

            return reportPage;
        }

    }
}
