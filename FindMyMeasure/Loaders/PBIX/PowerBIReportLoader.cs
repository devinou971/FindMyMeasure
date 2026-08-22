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

namespace FindMyMeasure.Loaders
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
        public static PowerBIReport LoadFromPbix(string pbixPath, SemanticModel semanticModelBackend, bool analyseHiddenPages, bool analyseHiddenVisuals)
        {
            // Extract the Layout file from the .pbix zip archive
            using (ZipArchive pbixFile = ZipFile.OpenRead(pbixPath))
            {
                string pbiReportName = pbixPath.Split(System.IO.Path.DirectorySeparatorChar).Last().Replace(".pbix", "");
                PowerBIReport powerBIReport = new PowerBIReport(pbiReportName, pbixPath, semanticModelBackend);

                // Checking wheter to use the PBIR or Legacy loaders
                ZipArchiveEntry layoutEntry = pbixFile.GetEntry("Report/Layout");

                if (layoutEntry == null)
                {
                    return FindMyMeasure.Loaders.PBIX.PBIR.PowerBIReportPbirLoader.LoadFromPbix(pbixFile, powerBIReport, semanticModelBackend, analyseHiddenPages, analyseHiddenVisuals);
                }
                else 
                {
                    return FindMyMeasure.Loaders.PBIX.Legacy.PowerBIReportLoader.LoadFromPbix(pbixFile, powerBIReport, semanticModelBackend, analyseHiddenPages, analyseHiddenVisuals);
                }

            }
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

    }
}
