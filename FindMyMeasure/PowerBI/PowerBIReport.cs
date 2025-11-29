using FindMyMeasure.Database;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace FindMyMeasure.PowerBI
{
    /// <summary>
    /// Represents a PowerBI report file (.pbix) with its pages, visuals, and filters.
    /// Also tracks the semantic model it connects to.
    /// </summary>
    public class PowerBIReport : PowerBINode
    {
        private string _name;
        private string _path;
        private HashSet<ReportPage> _pages = new HashSet<ReportPage>();
        private HashSet<Filter> _filters = new HashSet<Filter>();
        private SemanticModel _semanticModel;

        /// <summary>
        /// Gets the name of this report (derived from the .pbix filename).
        /// </summary>
        public override string Name { get => this._name; }

        /// <summary>
        /// Gets the file path to this report's .pbix file.
        /// </summary>
        public string Path { get => this._path; }

        /// <summary>
        /// Gets the semantic model that this report connects to.
        /// </summary>
        public SemanticModel SemanticModel { get => this._semanticModel; }

        /// <summary>
        /// Initializes a new instance of the PowerBIReport class.
        /// </summary>
        /// <param name="name">The report name.</param>
        /// <param name="path">The file path to the .pbix file.</param>
        /// <param name="semanticModel">The semantic model this report uses.</param>
        private PowerBIReport(string name, string path, SemanticModel semanticModel)
        {
            this._name = name;
            this._path = path;
            this._semanticModel = semanticModel;
        }

        /// <summary>
        /// Loads a PowerBI report from a .pbix file and parses its layout to extract pages, visuals, and filters.
        /// </summary>
        /// <param name="pbixPath">The full path to the .pbix file.</param>
        /// <param name="semanticModelBackend">The semantic model to use for resolving measure and column references.</param>
        /// <param name="analyseHiddenPages">Whether to include hidden report pages in the analysis.</param>
        /// <param name="analyseHiddenVisuals">Whether to include hidden visuals in the analysis.</param>
        /// <returns>A new PowerBIReport instance with all pages, visuals, and filters loaded.</returns>
        /// <exception cref="Exception">Thrown if the .pbix file structure is invalid or the layout cannot be parsed.</exception>
        public static PowerBIReport LoadFromPbix(string pbixPath, SemanticModel semanticModelBackend, bool analyseHiddenPages, bool analyseHiddenVisuals)
        {
            // Extract the Layout file from the .pbix zip archive
            string layoutContent = null;
            using (ZipArchive pbixFile = ZipFile.OpenRead(pbixPath))
            {
                ZipArchiveEntry layoutEntry = pbixFile.GetEntry("Report/Layout") ?? throw new Exception("Layout of pbix file not found");
                StreamReader streamReader = new StreamReader(layoutEntry.Open(), Encoding.Unicode);
                layoutContent = streamReader.ReadToEnd();
            }
            if (layoutContent is null) throw new Exception("Layout of pbix file is empty");

            string pbiReportName = pbixPath.Split(System.IO.Path.DirectorySeparatorChar).Last().Replace(".pbix", "");

            PowerBIReport powerBIReport = new PowerBIReport(pbiReportName, pbixPath, semanticModelBackend);
            
            // Parse the layout JSON to extract report structure
            JsonNode layoutJsonNode = JsonNode.Parse(layoutContent) ?? throw new Exception("Unable to parse Layout of pbix file into json format");
            JsonNode pagesNodes = layoutJsonNode["sections"] ?? throw new Exception("Layout of PBIX has no section objects");
            JsonNode filtersNode = layoutJsonNode["filters"];

            // Load all report pages
            foreach (JsonNode page in pagesNodes.AsArray())
            {
                if (page != null && page.GetValueKind() == JsonValueKind.Object)
                {
                    ReportPage reportPage = ReportPage.LoadFromJson(page.AsObject(), powerBIReport, analyseHiddenPages, analyseHiddenVisuals);
                    if(reportPage!=null)
                        powerBIReport._pages.Add(reportPage);
                }
            }
            
            // Load report-level filters
            var filters = Filter.LoadMultipleFiltersFromJson(filtersNode, powerBIReport);
            powerBIReport._filters.UnionWith(filters);

            return powerBIReport;
        }

        /// <summary>
        /// Loads a PowerBI report with default settings (includes hidden pages and visuals).
        /// </summary>
        /// <param name="pbixPath">The full path to the .pbix file.</param>
        /// <param name="semanticModelBackend">The semantic model to use for resolving references.</param>
        /// <returns>A new PowerBIReport instance.</returns>
        public static PowerBIReport LoadFromPbix(string pbixPath, SemanticModel semanticModelBackend)
        {
            return LoadFromPbix(pbixPath, semanticModelBackend, analyseHiddenPages: true, analyseHiddenVisuals: true);
        }

        /// <summary>
        /// Gets the semantic model associated with this report.
        /// </summary>
        public SemanticModel GetSemanticModel() => _semanticModel;
        
        /// <summary>
        /// Gets all report pages in this report.
        /// </summary>
        public HashSet<ReportPage> GetReportPages() => this._pages;

        /// <summary>
        /// Gets all visuals across all pages in this report.
        /// </summary>
        /// <returns>A HashSet of all Visual objects in the report.</returns>
        public HashSet<Visual> GetVisuals()
        {
            HashSet<Visual> visuals = new HashSet<Visual>();
            foreach(var page in this._pages)
                visuals.UnionWith(page.GetVisuals());
            return visuals;
        }

        /// <summary>
        /// Gets all filters across all pages and visuals in this report.
        /// </summary>
        /// <returns>A HashSet of all Filter objects in the report.</returns>
        public HashSet<Filter> GetFilters()
        {
            HashSet<Filter> filters = new HashSet<Filter>() ;
            // Report-level filters
            filters.UnionWith(this._filters);
            // Page-level and visual-level filters
            foreach(var page in this._pages)
            {
                filters.UnionWith(page.GetFilters());
                foreach(var visual in page.GetVisuals())
                    filters.UnionWith(visual.GetFilters());
            }
            return filters;
        }

        /// <summary>
        /// Gets the hash code for this report based on its file path.
        /// </summary>
        public override int GetHashCode()
        {
            return ("PowerBIReport:" + this._path).GetHashCode();
        }
    }
}
