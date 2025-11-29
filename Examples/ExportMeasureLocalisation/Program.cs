using FindMyMeasure.Database;
using FindMyMeasure.Interfaces;
using FindMyMeasure.PowerBI;
using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Collections.Specialized;
using FindMyMeasure;

namespace ExportMeasureLocalisation
{
    internal class Program
    {

        static void Main(string[] args)
        {
            Console.WriteLine("FindMyMeasure - Export Measure Localisation");
            Console.WriteLine("This small program reads information from the reports listed in the reporList.txt file, and exports the measure locations in " + Properties.Settings.Default.ExportPath);
            Console.WriteLine("Extra note : the reports have to be opened in PowerBI");
            Console.WriteLine("=================================================\n");
            Console.WriteLine("Loading report list from reportList.txt ...");

            // Read report paths from a simple text file named reportList.txt (one path per line)
            List<string> reportPaths = new List<string>();
            using (StreamReader sr = new StreamReader("reportList.txt"))
            {
                string content = sr.ReadToEnd();
                string[] lines = content.Split(new string[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
                foreach (string line in lines)
                {
                    if (!string.IsNullOrWhiteSpace(line) && File.Exists(line.Trim()))
                    {
                        reportPaths.Add(line.Trim());
                    }
                }
            }

            // We'll cache loaded semantic models so we don't reload the same model multiple times
            List<SemanticModel> semanticModels = new List<SemanticModel>();

            // Process each report path from the list
            foreach (string reportPath in reportPaths)
            {
                // Try to get an external connection string from the .pbix file. If it exists, it indicates a live connection to PowerBI/SSAS model.
                var connectionString = Utils.ExtractConnectionStringFromReport(reportPath);
                string modelName = null;
                SemanticModel semanticModel = null;
                try
                {
                    if (!String.IsNullOrEmpty(connectionString))
                    {
                        modelName = connectionString.Split(new string[] { "Initial Catalog=" }, StringSplitOptions.None).Last().Split(';').First();

                        // Reuse an already-loaded semantic model that matches the connection string
                        semanticModel = semanticModels.FirstOrDefault(x => x.ConnectionString == connectionString);
                        if (semanticModel == null)
                        {
                            // If not cached, create a new semantic model and load it fully
                            semanticModel = new SemanticModel(modelName, connectionString);
                            Console.Write("Loading Semantic model at :" + connectionString + " ... ");
                            semanticModel.LoadFullModel();
                            semanticModels.Add(semanticModel);
                        }
                    }
                    else
                    {
                        // If the report does not contain a connection string, attempt to find a local live semantic model
                        // This requires the Power BI Desktop report to be opened locally, and a corresponding local model to exist
                        semanticModel = Utils.ListAllLocalSemanticModels().FirstOrDefault(x => x.Name == reportPath.Split(System.IO.Path.DirectorySeparatorChar).Last().Replace(".pbix", ""));
                        if (semanticModel == null)
                        {
                            // Inform the user and skip this report if we couldn't find a matching local model
                            Console.WriteLine("Couldn't find the semantic model for report " + reportPath + "\nAre you sure the report is opened in PowerBI ?");
                            continue;
                        }
                        else
                        {
                            Console.Write("Loading local Semantic model :" + semanticModel.Name + " ... ");
                            semanticModel.LoadFullModel();
                            semanticModels.Add(semanticModel);
                        }
                    }
                } catch (Exception e)
                {
                    // Catch-all for any error during semantic model loading - print details and continue with next report
                    Console.WriteLine("Couldn't load the semantic model for report " + reportPath + "\n" + e.ToString());
                    continue;
                }


                // Load the Power BI report and analyze pages/visuals/filters
                Console.WriteLine("\nLoadind PowerBI Report: " + reportPath + " ...");
                PowerBIReport powerBIReport = PowerBIReport.LoadFromPbix(reportPath, semanticModel, Properties.Settings.Default.AnalyseHiddenPages, Properties.Settings.Default.AnalyseHiddenVisuals);
                Console.WriteLine("Nb report pages: " + powerBIReport.GetReportPages().Count);
                Console.WriteLine("Nb visuals: " + powerBIReport.GetVisuals().Count);
                Console.WriteLine("Nb filters: " + powerBIReport.GetFilters().Count + "\n\n");
            }


            // Prepare CSV output: header row followed by one line per measure occurrence
            string csvDel = Properties.Settings.Default.CsvDelimiter;
            HashSet<string> csvLines = new HashSet<string>() { $"Model{csvDel}Measure{csvDel}Report{csvDel}Page" };

            // Iterate through each cached semantic model and enumerate measures and their visual dependents
            foreach (var semanticModel in semanticModels)
            {
                foreach (var measure in semanticModel.GetMeasures())
                {
                    // Get dependents of the measure and select those that are visuals
                    var visualsList = measure.GetDependents().Where(d => d is Visual).Cast<Visual>().ToList();
                    foreach (var visual in visualsList)
                    {
                        // For each visual dependent, capture the page and report names for the CSV
                        var reportPageName = visual.GetReportPage().Name;
                        var reportName = visual.GetReportPage().GetPowerBIReport().Name;
                        csvLines.Add($"{semanticModel.Name}{csvDel}{measure.Name}{csvDel}{reportName}{csvDel}{reportPageName}");
                    }
                }
            }

            // Write results to the configured export path. Use Windows-1252 encoding to match historical behavior.
            Console.WriteLine("Writing results to file " + Properties.Settings.Default.ExportPath + "...");

            try
            {
                File.WriteAllText(Properties.Settings.Default.ExportPath, string.Join("\n", csvLines), Encoding.GetEncoding(1252));

            }
            catch (IOException e)
            {
                // Common error: the output file is locked by another program. Provide a clear message and rethrow.
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Could not write the output file.\nMost of the time, it is because the file is opened in another program.\nPlease close it and try again.");
                Console.WriteLine(e.ToString());
                Console.ResetColor();
                Console.WriteLine("End of program, hit any key to close.");
                Console.ReadKey();
                throw;
            }
            Console.WriteLine("Done");

            // Pause so the console window stays open when running interactively
            Console.WriteLine("End of program, hit any key to close.");
            Console.ReadKey();

        }
    }
}
