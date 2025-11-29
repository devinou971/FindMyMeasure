using ExportUsages;
using FindMyMeasure;
using FindMyMeasure.Database;
using FindMyMeasure.Interfaces;
using FindMyMeasure.PowerBI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace ExportUsages
{
    internal class Program
    {
        /// <summary>
        /// Exports usage information for all measures and columns in PowerBI reports to a CSV file.
        /// 
        /// Workflow:
        /// 1. Read report paths from reportList.txt
        /// 2. Load semantic models (Analysis Services, local PowerBI Desktop, etc.)
        /// 3. Parse each PowerBI report to extract measure/column usage
        /// 4. Generate CSV output with usage statistics
        /// </summary>
        static void Main(string[] args)
        {
            Console.WriteLine("ListMyMeasure - Export Usages Tool");
            Console.WriteLine("This tool exports the usages of all measures and columns used in PowerBI reports listed in reportList.txt" +
                "The result is outputed in the " + Properties.Settings.Default.ExportPath);
            Console.WriteLine("=================================================\n");

            // ===== STEP 1: Load report paths from file =====
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


            // ===== STEP 2: Load semantic models and parse reports =====
            List<SemanticModel> semanticModels = new List<SemanticModel>();
            foreach (string reportPath in reportPaths)
            {
                // Extract connection string from the report (Analysis Services or local model)
                var connectionString = Utils.ExtractConnectionStringFromReport(reportPath);
                string modelName = null;
                SemanticModel semanticModel = null;
                try
                {
                    if (!String.IsNullOrEmpty(connectionString))
                    {
                        // Connected to Analysis Services or similar backend
                        modelName = connectionString.Split(new string[] { "Initial Catalog=" }, StringSplitOptions.None).Last().Split(';').First();

                        // Check if we already loaded this semantic model (multiple reports may share the same model)
                        semanticModel = semanticModels.FirstOrDefault(x => x.ConnectionString == connectionString);
                        if (semanticModel == null)
                        {
                            semanticModel = new SemanticModel(modelName, connectionString);
                            Console.Write("Loading Semantic model at :" + connectionString + " ... ");
                            semanticModel.LoadFullModel();
                            semanticModels.Add(semanticModel);
                        }
                    }
                    else
                    {
                        // No connection string - try to find a local PowerBI Desktop model running on this machine
                        semanticModel = Utils.ListAllLocalSemanticModels().FirstOrDefault(x => x.Name == reportPath.Split(System.IO.Path.DirectorySeparatorChar).Last().Replace(".pbix", ""));
                        if (semanticModel == null)
                        {
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
                }
                catch (Exception e)
                {
                    Console.WriteLine("Couldn't load the semantic model for report " + reportPath + "\n" + e.ToString());
                    continue;
                }


                // Load the PowerBI report file and extract pages, visuals, and filters
                Console.WriteLine("\nLoadind PowerBI Report: " + reportPath + " ...");
                PowerBIReport powerBIReport = PowerBIReport.LoadFromPbix(reportPath, semanticModel, Properties.Settings.Default.AnalyseHiddenPages, Properties.Settings.Default.AnalyseHiddenVisuals);
                Console.WriteLine("Nb report pages: " + powerBIReport.GetReportPages().Count);
                Console.WriteLine("Nb visuals: " + powerBIReport.GetVisuals().Count);
                Console.WriteLine("Nb filters: " + powerBIReport.GetFilters().Count + "\n\n");
            }



            // ===== STEP 3: Export usages to CSV =====
            string csvDel = Properties.Settings.Default.CsvDelimiter;
            HashSet<string> csvLines = new HashSet<string>() { $"Model{csvDel}Type{csvDel}Name{csvDel}Table{csvDel}Status{csvDel}NumberOfUses{csvDel}UsedInType{csvDel}UsedInName{csvDel}UsedInTable{csvDel}UsedInReport{csvDel}UsedInReportPage" };

            foreach (var semanticModel in semanticModels)
            {
                var modelName = semanticModel.Name;
                var measures = semanticModel.GetMeasures();
                var columns = semanticModel.GetColumns();
                var dataInputs = measures.Cast<IDataInput>().Union(columns.Cast<IDataInput>()).ToList();

                // Process each measure and column to extract usage information
                foreach (var dataInput in dataInputs)
                {
                    var name = dataInput.Name;
                    var table = dataInput.ParentTable.Name;
                    var status = dataInput.GetUsageState().ToString();
                    var numberOfUses = dataInput.GetDependents().Count;
                    
                    if (numberOfUses == 0)
                    {
                        // Item is not used anywhere
                        csvLines.Add($"{modelName}{csvDel}{dataInput.Type}{csvDel}{name}{csvDel}{table}{csvDel}{status}{csvDel}{numberOfUses}{csvDel}N/A{csvDel}N/A{csvDel}N/A{csvDel}N/A{csvDel}N/A");
                    } 
                    else
                    {
                        // Item is used - generate a CSV row for each usage
                        foreach (var dependent in dataInput.GetDependents())
                        {
                            var usedInType = dependent.GetTargetType();
                            var usedInName = dependent.Name;
                            var usedInTable = (dependent is IDataInput) ? ((IDataInput)dependent).ParentTable.Name : "";
                            var usedInReport = "";
                            var usedInReportPage = "";
                            
                            // Extract report and page context if this is a PowerBI object
                            if (dependent is IPowerBILeafNode)
                            {
                                switch (dependent)
                                {
                                    case PowerBIReport pbiReport:
                                        {
                                            usedInReport = pbiReport.Name;
                                            break;
                                        }
                                    case ReportPage reportPage:
                                        {
                                            usedInReport = reportPage.GetPowerBIReport().Name;
                                            usedInReportPage = reportPage.Name;
                                            break;
                                        }
                                    case Visual visual:
                                        {
                                            usedInReport = visual.GetReportPage().GetPowerBIReport().Name;
                                            usedInReportPage = visual.GetReportPage().Name;
                                            break;
                                        }
                                    default:
                                        break;
                                }
                            }
                            csvLines.Add($"{modelName}{csvDel}{dataInput.Type}{csvDel}{name}{csvDel}{table}{csvDel}{status}{csvDel}{numberOfUses}{csvDel}{usedInType}{csvDel}{usedInName}{csvDel}{usedInTable}{csvDel}{usedInReport}{csvDel}{usedInReportPage}");
                        }
                    }
                }
            }


            // ===== STEP 4: Write CSV to file =====
            Console.WriteLine("Writing results to file " + Properties.Settings.Default.ExportPath + "...");

            try
            {
                File.WriteAllText(Properties.Settings.Default.ExportPath, string.Join("\n", csvLines), Encoding.GetEncoding(1252));
            }
            catch (IOException e)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Could not write the output file.\nMost of the time, it is because the file is opened in another program.\nPlease close it and try again.");
                Console.WriteLine(e.ToString());
                Console.ResetColor();
                Console.WriteLine("End of program, hit any key to close.");
                Console.ReadKey();
                throw;
            }
            Console.WriteLine("Done" +
                "The results are in " + Properties.Settings.Default.ExportPath);

            Console.WriteLine("End of program, hit any key to close.");
            Console.ReadKey();
        }
    }
}
