using FindMyMeasure.Database;
using FindMyMeasure.PowerBI;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace FindMyMeasure.Loaders.PBIX.PBIR
{
    public class PowerBIReportPbirLoader
    {
        /// <summary>
        /// Loads a PowerBI report from a .pbix file and parses its layout to extract pages, visuals, and filters.
        /// </summary>
        /// <param name="pbixFile">The .pbix Zip archive that is going to be analysed.</param>
        /// <param name="powerBIReport">The report object that needs to be completed with all the pages.</param>
        /// <param name="semanticModelBackend">The semantic model to use for resolving measure and column references.</param>
        /// <param name="analyseHiddenPages">Whether to include hidden report pages in the analysis.</param>
        /// <param name="analyseHiddenVisuals">Whether to include hidden visuals in the analysis.</param>
        /// <returns>A new PowerBIReport instance with all pages, visuals, and filters loaded.</returns>
        /// <exception cref="Exception">Thrown if the .pbix file structure is invalid or the layout cannot be parsed.</exception>
        public static PowerBIReport LoadFromPbix(ZipArchive pbixFile, PowerBIReport powerBIReport, SemanticModel semanticModelBackend, bool analyseHiddenPages, bool analyseHiddenVisuals)
        {
            // Extract the report.json file from the .pbix zip archive
            string reportContent = null;

            ZipArchiveEntry reportEntry = pbixFile.GetEntry("Report/definition/report.json") ?? throw new Exception("report.json of pbix file not found");
            StreamReader streamReader = new StreamReader(reportEntry.Open(), Encoding.UTF8);
            reportContent = streamReader.ReadToEnd();

            if (reportContent is null) throw new Exception("report.json of pbix file is empty");


            // Parse the layout JSON to extract report structure
            JsonNode reportJsonNode = JsonNode.Parse(reportContent) ?? throw new Exception("Unable to parse report.json of pbix file into json format");

            var pageEntries = pbixFile.Entries.Where((x) =>
            {
                return x.FullName.StartsWith("Report/definition/pages") && x.FullName.EndsWith("page.json");
            });
            // Load all report pages
            foreach (var pageEntry in pageEntries)
            {
                
                streamReader = new StreamReader(pageEntry.Open(), Encoding.UTF8);
                string pageContent = streamReader.ReadToEnd();

                if (pageContent is null) throw new Exception($"Couldn't read {pageEntry.FullName}");
                JsonNode page = JsonNode.Parse(pageContent) ?? throw new Exception($"Unable to parse {pageEntry.FullName} of pbix file into json format");

                if (page != null && page.GetValueKind() == JsonValueKind.Object)
                {
                    ReportPage reportPage = ReportPagePbirLoader.LoadReportPageFromJson(pbixFile, page.AsObject(), powerBIReport, analyseHiddenPages, analyseHiddenVisuals);
                    if (reportPage != null)
                    {
                        powerBIReport.AddReportPage(reportPage);
                    }
                }
            }

            JsonNode filtersNode = reportJsonNode["filterConfig"];

            // Load report-level filters
            var filters = FilterPbirLoader.LoadMultipleFiltersFromJson(filtersNode, powerBIReport); //Filter.LoadMultipleFiltersFromJson(filtersNode, powerBIReport);
            foreach (var filter in filters)
                powerBIReport.AddFilter(filter);
            return powerBIReport;
            
        }
    }
}
