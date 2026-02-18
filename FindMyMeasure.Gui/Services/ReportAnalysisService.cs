using FindMyMeasure.Database;
using FindMyMeasure.Gui.Exceptions;
using FindMyMeasure.Gui.Models;
using FindMyMeasure.Interfaces;
using FindMyMeasure.Loaders;
using FindMyMeasure.PowerBI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static FindMyMeasure.Gui.Models.ReportAnalysisConfiguration;

namespace FindMyMeasure.Gui.Services
{
    internal class ReportAnalysisService
    {
        private IEnumerable<ReportAnalysisConfiguration> reportConfigs;
        public ReportAnalysisService(IEnumerable<ReportAnalysisConfiguration> reportConfigs) {
            this.reportConfigs = reportConfigs;
        }
        public async Task<ReportAnalysisResult> RunAsync(IProgress<double> progressValue, IProgress<string> progressMessage)
        {
            progressMessage.Report("Starting the loading of semantic models and reports ...");
            HashSet<SemanticModel> semanticModels = LoadSemanticModels(this.reportConfigs, progressMessage, progressValue);
            progressValue.Report(45);
            HashSet<PowerBIReport> powerBIReports = LoadReports(this.reportConfigs, semanticModels, progressMessage, progressValue);
            progressValue.Report(90);
            HashSet<DataGridUsageRecord> usageRecords = ProcessUsageRecords(semanticModels);
            progressValue.Report(100);
            return new ReportAnalysisResult(semanticModels, reportConfigs, usageRecords);
        }

        private HashSet<SemanticModel> LoadSemanticModels(IEnumerable<ReportAnalysisConfiguration> dataGridReports, IProgress<string> progressMessage, IProgress<double> progressValue)
        {
            HashSet<SemanticModel> semanticModels = new HashSet<SemanticModel>();

            progressMessage.Report("Retrieving all connection strings ...");

            // First resolve local semantic models
            if (dataGridReports.Where(x => x.ModelType == ModelConnectionType.Local).Count() > 0)
            {
                List<SemanticModel> localSemanticModels = Utils.ListAllLocalSemanticModels(); // TODO : Make this async ?
                foreach (var report in dataGridReports.Where(x => x.ModelType == ModelConnectionType.Local))
                {
                    SemanticModel correspondingSemanticModel = localSemanticModels.FirstOrDefault<SemanticModel>(x => x.Name.Equals(report.ReportName.Remove(report.ReportName.Length - 5)));
                    if (correspondingSemanticModel == null)
                        throw new SemanticModelNotFoundException($"The report \"{report.ReportName}\" seems to use a local model but no local semantic model matching its name could be found. Did you open the report in PowerBI Desktop ?");
                    semanticModels.Add(correspondingSemanticModel);
                }
            }
            progressValue.Report(5);

            // Then resolve remote semantic models
            foreach (var report in dataGridReports.Where(x => x.ModelType == ModelConnectionType.Remote))
            {
                SemanticModel semanticModel = new SemanticModel(report.ModelName, report.ModelConnectionString);
                semanticModels.Add(semanticModel);
            }
            progressValue.Report(10);

            progressMessage.Report("Loading all semantic models ...");
            // Load all semantic models
            foreach (var obj in semanticModels.Select((semanticModel, index) => new { semanticModel, index }))
            {
                try
                {

                    progressMessage.Report($"Loading semantic model : {obj.semanticModel.Name} ...");
                    obj.semanticModel.LoadFullModel();
                    double progressPercent = 10 + (obj.index + 1.0) / semanticModels.Count * 35;
                    progressValue.Report(progressPercent);
                }
                catch (Exception ex)
                {
                    throw new Exception($"An error occured while loading the semantic model \"{obj.semanticModel.Name}\". Error message: {ex.Message}");
                }
            }

            return semanticModels;
        }

        private HashSet<PowerBIReport> LoadReports(IEnumerable<ReportAnalysisConfiguration> dataGridReports, HashSet<SemanticModel> semanticModels, IProgress<string> progressMessage, IProgress<double> progressValue)
        {
            HashSet<PowerBIReport> powerBIReports = new HashSet<PowerBIReport>();

            // Finally load all PowerBI reports
            progressMessage.Report("Loading all PowerBI reports ...");
            foreach (var obj in dataGridReports.Select((report, index) => new { report, index }))
            {
                try
                {
                    SemanticModel semanticModel = null;
                    if (obj.report.ModelType == ModelConnectionType.Local)
                        semanticModel = semanticModels.FirstOrDefault<SemanticModel>(x => x.Name.Equals(obj.report.ReportName.Remove(obj.report.ReportName.Length - 5)));
                    else
                        semanticModel = semanticModels.FirstOrDefault<SemanticModel>(x => x.ConnectionString.Equals(obj.report.ModelConnectionString));
                    progressMessage.Report($"Loading PowerBI report : {obj.report.ReportName} ...");
                    PowerBIReport powerBIReport = PowerBIReportLoader.LoadFromPbix(obj.report.ReportPath, semanticModel, obj.report.AnalyseHiddenPages, obj.report.AnalyseHiddenVisuals); // TODO : Make this async ?
                    powerBIReports.Add(powerBIReport);
                    double progressPercent = 45 + (obj.index + 1.0) / dataGridReports.Count() * 55;
                    progressValue.Report(progressPercent);
                }
                catch (Exception ex)
                {
                    throw new Exception($"An error occured while loading the PowerBI report \"{obj.report.ReportName}\". Error message: {ex.Message}");
                }
            }

            return powerBIReports;
        }

        private HashSet<DataGridUsageRecord> ProcessUsageRecords(HashSet<SemanticModel> semanticModels)
        {
            HashSet<DataGridUsageRecord> usageRecords = new HashSet<DataGridUsageRecord>();
            foreach (var semanticModel in semanticModels)
            {
                var dataInputs = semanticModel.GetMeasures().Cast<IDataInput>();
                dataInputs = dataInputs.Union(semanticModel.GetColumns());
                dataInputs = dataInputs.Union(semanticModel.GetHierarchies());
                foreach (var dataInput in dataInputs)
                    usageRecords.Add(new DataGridUsageRecord(dataInput, semanticModel.Name));
            }
            return usageRecords;
        }
    }
}
