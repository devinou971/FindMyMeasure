using FindMyMeasure.PowerBI;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace FindMyMeasure.Loaders.PBIX.PBIR
{
    internal class ReportPagePbirLoader
    {
        internal static ReportPage LoadReportPageFromJson(ZipArchive pbixFile, JsonObject pageNode, PowerBIReport powerBIReport, bool analyseHiddenPages, bool analyseHiddenVisuals)
        {
            if (!analyseHiddenPages)
            {
                if (!(pageNode["visibility"] is null) && (pageNode["visibility"].ToString() == "1" || pageNode["visibility"].ToString() == "HiddenInViewMode"))
                    return null;
            }

            ReportPage reportPage = new ReportPage(pageNode["name"].ToString(), pageNode["displayName"].ToString(), powerBIReport);

            var visualsEntries = pbixFile.Entries.Where<ZipArchiveEntry>((x) =>
            {
                return x.FullName.StartsWith($"Report/definition/pages/{pageNode["name"].ToString()}/visuals/") && x.FullName.EndsWith($"/visual.json");
            });
            foreach (var visualEntry in visualsEntries)
            {
                var streamReader = new StreamReader(visualEntry.Open(), Encoding.UTF8);
                string visualContent = streamReader.ReadToEnd() ?? throw new Exception($"{visualEntry.FullName} of pbix file is empty");
                JsonNode visualNode = JsonNode.Parse(visualContent) ?? throw new Exception($"Unable to parse {visualEntry.FullName} of pbix file into json format");

                if (visualNode != null)
                {
                    Visual visual = VisualPbirLoader.LoadVisualFromJson(visualNode, reportPage, analyseHiddenVisuals);
                    if (visual != null)
                        reportPage.AddVisual(visual);
                }

            }

            var filters = FilterPbirLoader.LoadMultipleFiltersFromJson(pageNode["filterConfig"], reportPage);
            foreach (var filter in filters)
                reportPage.AddFilter(filter);

            return reportPage;
        }

    }
}
