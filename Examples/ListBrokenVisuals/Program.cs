using FindMyMeasure;
using FindMyMeasure.Database;
using FindMyMeasure.Interfaces;
using FindMyMeasure.PowerBI;
using FindMyMeasure.WarningClasses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ListBrokenVisuals
{
    internal class Program
    {

        // Local implementation of warning subscribers to print missing column/measure warnings to console
        // Implements both IMissingColumnWarningSubscriber and IMissingMeasureWarningSubscriber so it can be
        // registered with the AnalysisWarningPublisher to receive and display warnings detected during analysis.
        class WarningSubscriber : IMissingColumnWarningSubscriber, IMissingMeasureWarningSubscriber
        {
            // Called when a MissingColumnWarning is published. Prints a human-readable message to the console.
            public void OnWarningReceived(MissingColumnWarning warning)
            {
                Console.WriteLine($"The column {warning.ColumnName} from table {warning.TableName} used in {warning.Sender} doesn't exist");
            }

            // Called when a MissingMeasureWarning is published. Prints a human-readable message to the console.
            public void OnWarningReceived(MissingMeasureWarning warning)
            {
                Console.WriteLine($"The measure {warning.MeasureName} from table {warning.TableName} used in {warning.Sender} doesn't exist");
            }
        }

        // Main entry point for the console tool.
        // This program looks for a specific open Power BI Desktop semantic model and analyzes a sample report
        // to list visuals that reference missing measures or columns.
        static void Main(string[] args)
        {
            // Print header and brief instructions
            Console.WriteLine("ListMyMeasure - List Broken Visuals Tool");
            Console.WriteLine("This tool lists all the visuals where a column or measure used is missing from the semantic model.");
            Console.WriteLine("Make sure to open the reports in PowerBI Desktop before running this tool.");
            Console.WriteLine("=================================================\n");

            // Attempt to locate the local semantic model by name. The named model must be opened in Power BI Desktop.
            SemanticModel semanticModel = Utils.ListAllLocalSemanticModels().FirstOrDefault(x=> x.Name == "Store Sales - With broken visuals");
            if(semanticModel == null)
            {
                // If we cannot find the model, inform the user and exit early
                Console.WriteLine("The semantic model couldn't be found. Are you sure the report is opened in PowerBI ?");
                return;
            }

            // Load the full semantic model metadata (tables, columns, measures, etc.) before analysis
            semanticModel.LoadFullModel();

            // Create and register a subscriber to display missing column/measure warnings
            var warningSubscriber = new WarningSubscriber();
            AnalysisWarningPublisher.GetInstance().SubscribeToMissingColumnWarning(warningSubscriber);
            AnalysisWarningPublisher.GetInstance().SubscribeToMissingMeasureWarning(warningSubscriber);

            // Load the example Power BI report and run the analysis. The LoadFromPbix call will trigger
            // analysis that may publish missing column/measure warnings which our subscriber will receive.
            PowerBIReport report = PowerBIReport.LoadFromPbix(@"reportExamples\Store Sales - With broken visuals.pbix", semanticModel, true, true);

            // Note: The program intentionally ends here after loading the report because the warnings are
            // printed via the registered subscriber. If further processing or summary output is desired,
            // it can be added after the report is loaded.
        }
    }
}
