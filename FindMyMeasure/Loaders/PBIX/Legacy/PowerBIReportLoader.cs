using FindMyMeasure.Database;
using FindMyMeasure.PowerBI;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace FindMyMeasure.Loaders.PBIX.Legacy
{
    public class PowerBIReportLoader
    {

        /// <summary>
        /// Loads a PowerBI report from a .pbix file and parses its layout to extract pages, visuals, and filters.
        /// </summary>
        /// <param name="pbixPath">The full path to the .pbix file.</param>
        /// <param name="semanticModelBackend">The semantic model to use for resolving measure and column references.</param>
        /// <param name="analyseHiddenPages">Whether to include hidden report pages in the analysis.</param>
        /// <param name="analyseHiddenVisuals">Whether to include hidden visuals in the analysis.</param>
        /// <returns>A new PowerBIReport instance with all pages, visuals, and filters loaded.</returns>
        /// <exception cref="Exception">Thrown if the .pbix file structure is invalid or the layout cannot be parsed.</exception>
        public static PowerBIReport LoadFromPbix(ZipArchive pbixFile, PowerBIReport powerBIReport, SemanticModel semanticModelBackend, bool analyseHiddenPages, bool analyseHiddenVisuals)
        {
            // Extract the Layout file from the .pbix zip archive
            string layoutContent = null;
            ZipArchiveEntry layoutEntry = pbixFile.GetEntry("Report/Layout") ?? throw new Exception("Layout of pbix file not found");
            StreamReader streamReader = new StreamReader(layoutEntry.Open(), Encoding.Unicode);
            layoutContent = streamReader.ReadToEnd();
            streamReader.Close();

            if (layoutContent is null) throw new Exception("Layout of pbix file is empty");

            // Parse the layout JSON to extract report structure
            JsonNode layoutJsonNode = JsonNode.Parse(layoutContent) ?? throw new Exception("Unable to parse Layout of pbix file into json format");
            JsonNode pagesNodes = layoutJsonNode["sections"] ?? throw new Exception("Layout of PBIX has no section objects");
            JsonNode filtersNode = layoutJsonNode["filters"];

            // Load all report pages
            foreach (JsonNode page in pagesNodes.AsArray())
            {
                if (page != null && page.GetValueKind() == JsonValueKind.Object)
                {
                    ReportPage reportPage = ReportPageLoader.LoadReportPageFromJson(page.AsObject(), powerBIReport, analyseHiddenPages, analyseHiddenVisuals); // ReportPage.LoadFromJson(page.AsObject(), powerBIReport, analyseHiddenPages, analyseHiddenVisuals);
                    if (reportPage != null)
                    {
                        powerBIReport.AddReportPage(reportPage);
                    }
                }
            }

            // Load report-level filters
            var filters = FilterLoader.LoadMultipleFiltersFromJson(filtersNode, powerBIReport); //Filter.LoadMultipleFiltersFromJson(filtersNode, powerBIReport);
            foreach (var filter in filters)
                powerBIReport.AddFilter(filter);
            return powerBIReport;
        }
    }
}
